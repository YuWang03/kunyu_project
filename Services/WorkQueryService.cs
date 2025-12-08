using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 個人考勤查詢服務
    /// </summary>
    public class WorkQueryService : IWorkQueryService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkQueryService> _logger;

        public WorkQueryService(IConfiguration configuration, ILogger<WorkQueryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 查詢個人月度考勤記錄
        /// </summary>
        public async Task<WorkQueryData?> GetMonthlyWorkRecordsAsync(string employeeNo, string yearMonth)
        {
            try
            {
                // 解析年月
                if (!DateTime.TryParseExact(yearMonth + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime startDate))
                {
                    _logger.LogWarning("年月格式不正確: {YearMonth}", yearMonth);
                    return null;
                }

                var endDate = startDate.AddMonths(1).AddDays(-1);
                var year = startDate.Year.ToString();
                var month = startDate.Month.ToString("00");

                var connectionString = _configuration.GetConnectionString("HRDatabase");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("資料庫連接字串未設定");
                    return null;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 查詢整個月的打卡資料
                var sql = @"
                    SELECT 
                        EMPLOYEE_ID,
                        EMPLOYEE_NO,
                        WORK_DATE,
                        WORK_CARD_TYPE,
                        WORK_CARD_DATE,
                        CARD_DATA_DATE,
                        CARD_DATA_CODE
                    FROM [03546618].[dbo].[vwZZ_CARD_DATA_MATCH]
                    WHERE EMPLOYEE_NO = @EmployeeNo
                      AND WORK_DATE >= @StartDate
                      AND WORK_DATE <= @EndDate
                    ORDER BY WORK_DATE, WORK_CARD_TYPE";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar, 50).Value = employeeNo;
                command.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                command.Parameters.Add("@EndDate", SqlDbType.Date).Value = endDate;

                var cardDataList = new List<WorkCardData>();

                _logger.LogInformation("查詢月度考勤: 員工編號={EmployeeNo}, 年月={YearMonth}", employeeNo, yearMonth);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cardDataList.Add(new WorkCardData
                        {
                            EMPLOYEE_ID = reader["EMPLOYEE_ID"]?.ToString(),
                            EMPLOYEE_NO = reader["EMPLOYEE_NO"]?.ToString(),
                            WORK_DATE = reader["WORK_DATE"] as DateTime?,
                            WORK_CARD_TYPE = reader["WORK_CARD_TYPE"] != DBNull.Value 
                                ? Convert.ToInt32(reader["WORK_CARD_TYPE"]) 
                                : (int?)null,
                            WORK_CARD_DATE = reader["WORK_CARD_DATE"] as DateTime?,
                            CARD_DATA_DATE = reader["CARD_DATA_DATE"] as DateTime?,
                            CARD_DATA_CODE = reader["CARD_DATA_CODE"]?.ToString()?.Trim()
                        });
                    }
                }

                _logger.LogInformation("查詢到 {Count} 筆記錄", cardDataList.Count);

                // 記錄查詢到的原始資料
                foreach (var card in cardDataList)
                {
                    _logger.LogInformation("原始資料: 員工ID={EmpId}, 員工編號={EmpNo}, 日期={Date}, 類型={Type}, 應刷={Work}, 實刷={Card}, 代碼={Code}", 
                        card.EMPLOYEE_ID, card.EMPLOYEE_NO, card.WORK_DATE, card.WORK_CARD_TYPE, 
                        card.WORK_CARD_DATE, card.CARD_DATA_DATE, card.CARD_DATA_CODE);
                }

                // 按日期分組
                var groupedByDate = cardDataList
                    .Where(c => c.WORK_DATE.HasValue)
                    .GroupBy(c => c.WORK_DATE!.Value.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                _logger.LogInformation("分組後有 {Count} 個日期", groupedByDate.Count);

                var records = new List<WorkQueryRecord>();

                foreach (var dateGroup in groupedByDate)
                {
                    var date = dateGroup.Key;
                    var clockInData = dateGroup.FirstOrDefault(c => c.WORK_CARD_TYPE == 0);
                    var clockOutData = dateGroup.FirstOrDefault(c => c.WORK_CARD_TYPE == 1);

                    var record = new WorkQueryRecord
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        OnDuty = "T" // 預設為正常
                    };

                    // 處理上班打卡
                    if (clockInData != null)
                    {
                        record.ClockIn = clockInData.WORK_CARD_DATE?.ToString("HH:mm:ss") ?? "";
                        
                        bool hasValidCardTime = clockInData.CARD_DATA_DATE.HasValue 
                            && clockInData.CARD_DATA_DATE.Value.Year > 1900;
                        
                        var code = clockInData.CARD_DATA_CODE ?? string.Empty;
                        
                        if (hasValidCardTime)
                        {
                            record.CheckIn = clockInData.CARD_DATA_DATE!.Value.ToString("HH:mm:ss");
                            // T: 正常, F: 異常
                            // 空白或超時出勤(3)視為正常，其他視為異常
                            record.StatusIn = (string.IsNullOrEmpty(code) || code == "3") ? "T" : "F";
                        }
                        else
                        {
                            record.CheckIn = "";
                            record.StatusIn = "";
                        }

                        // 如果上班有異常，設定 OnDuty 為 F
                        if (record.StatusIn == "F")
                        {
                            record.OnDuty = "F";
                        }
                    }

                    // 處理下班打卡
                    if (clockOutData != null)
                    {
                        record.ClockOut = clockOutData.WORK_CARD_DATE?.ToString("HH:mm:ss") ?? "";
                        
                        bool hasValidCardTime = clockOutData.CARD_DATA_DATE.HasValue 
                            && clockOutData.CARD_DATA_DATE.Value.Year > 1900;
                        
                        var code = clockOutData.CARD_DATA_CODE ?? string.Empty;
                        
                        if (hasValidCardTime)
                        {
                            record.CheckOut = clockOutData.CARD_DATA_DATE!.Value.ToString("HH:mm:ss");
                            // T: 正常, F: 異常
                            record.StatusOut = (string.IsNullOrEmpty(code) || code == "3") ? "T" : "F";
                        }
                        else
                        {
                            record.CheckOut = "";
                            record.StatusOut = "";
                        }

                        // 如果下班有異常，設定 OnDuty 為 F
                        if (record.StatusOut == "F")
                        {
                            record.OnDuty = "F";
                        }
                    }

                    records.Add(record);
                }

                return new WorkQueryData
                {
                    RYear = year,
                    RMonth = month,
                    Records = records
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢月度考勤記錄時發生錯誤: 員工編號={EmployeeNo}, 年月={YearMonth}", employeeNo, yearMonth);
                throw;
            }
        }
    }

    /// <summary>
    /// 打卡資料模型
    /// </summary>
    internal class WorkCardData
    {
        public string? EMPLOYEE_ID { get; set; }       // 員工ID
        public string? EMPLOYEE_NO { get; set; }       // 員工編號
        public DateTime? WORK_DATE { get; set; }       // 應出勤日期
        public int? WORK_CARD_TYPE { get; set; }       // 應刷卡別 (0:上班 1:下班)
        public DateTime? WORK_CARD_DATE { get; set; }  // 應刷卡時間
        public DateTime? CARD_DATA_DATE { get; set; }  // 實際刷卡時間
        public string? CARD_DATA_CODE { get; set; }    // 刷卡異常代碼
    }
}
