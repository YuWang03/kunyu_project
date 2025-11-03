using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 簡化版考勤查詢服務 - 只查詢確定存在的欄位
    /// 這個版本應該可以正常工作
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(IConfiguration configuration, ILogger<AttendanceService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 查詢個人出勤記錄
        /// </summary>
        public async Task<AttendanceRecord?> GetAttendanceRecordAsync(string employeeNo, string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out DateTime queryDate))
                {
                    _logger.LogWarning("日期格式不正確: {Date}", date);
                    return null;
                }

                var connectionString = _configuration.GetConnectionString("HRDatabase");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("資料庫連接字串未設定");
                    return null;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 查詢打卡資料，包含實際刷卡時間
                // 使用 EMPLOYEE_ID 進行查詢（employeeNo 參數實際對應 EMPLOYEE_ID）
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
                    WHERE EMPLOYEE_ID = @EmployeeId
                      AND CAST(WORK_DATE AS DATE) = CAST(@Date AS DATE)
                    ORDER BY WORK_CARD_TYPE";

                using var command = new SqlCommand(sql, connection);

                // 使用明確的參數類型
                command.Parameters.Add("@EmployeeId", SqlDbType.NVarChar, 50).Value = employeeNo;
                command.Parameters.Add("@Date", SqlDbType.Date).Value = queryDate.Date;

                var cardDataList = new List<SimplifiedCardData>();

                _logger.LogInformation("執行查詢: 員工ID={EmployeeId}, 日期={Date}, SQL日期參數={SqlDate}", 
                    employeeNo, queryDate.Date, queryDate.Date.ToString("yyyy-MM-dd"));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var cardData = new SimplifiedCardData
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
                        };
                        
                        _logger.LogInformation("讀取記錄: 員工ID={EmployeeId}, 員工編號={EmployeeNo}, 打卡類型={CardType}, 打卡時間={CardTime}, 異常代碼={Code}", 
                            cardData.EMPLOYEE_ID, cardData.EMPLOYEE_NO, cardData.WORK_CARD_TYPE, cardData.CARD_DATA_DATE, cardData.CARD_DATA_CODE);
                        
                        cardDataList.Add(cardData);
                    }
                }

                _logger.LogInformation("查詢到 {Count} 筆記錄", cardDataList.Count);

                if (cardDataList.Count == 0)
                {
                    _logger.LogInformation("查無出勤記錄: 員工編號={EmployeeNo}, 日期={Date}", employeeNo, date);
                    return null;
                }

                return BuildSimplifiedAttendanceRecord(cardDataList, queryDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤記錄時發生錯誤: 員工編號={EmployeeNo}, 日期={Date}", employeeNo, date);
                throw;
            }
        }

        /// <summary>
        /// 查詢所有員工的出勤記錄
        /// </summary>
        public async Task<List<AttendanceRecord>> GetAllAttendanceRecordsAsync(string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out DateTime queryDate))
                {
                    _logger.LogWarning("日期格式不正確: {Date}", date);
                    return new List<AttendanceRecord>();
                }

                var connectionString = _configuration.GetConnectionString("HRDatabase");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("資料庫連接字串未設定");
                    return new List<AttendanceRecord>();
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

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
                    WHERE CAST(WORK_DATE AS DATE) = CAST(@Date AS DATE)
                    ORDER BY EMPLOYEE_NO, WORK_CARD_TYPE";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@Date", SqlDbType.Date).Value = queryDate.Date;

                var cardDataList = new List<SimplifiedCardData>();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        cardDataList.Add(new SimplifiedCardData
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

                var groupedData = cardDataList
                    .GroupBy(c => c.EMPLOYEE_ID)
                    .Select(g => BuildSimplifiedAttendanceRecord(g.ToList(), queryDate))
                    .Where(r => r != null)
                    .Cast<AttendanceRecord>()
                    .ToList();

                return groupedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢所有員工出勤記錄時發生錯誤: 日期={Date}", date);
                throw;
            }
        }

        /// <summary>
        /// 建立簡化的出勤記錄
        /// </summary>
        private AttendanceRecord? BuildSimplifiedAttendanceRecord(List<SimplifiedCardData> cardDataList, DateTime queryDate)
        {
            if (cardDataList == null || cardDataList.Count == 0)
            {
                return null;
            }

            var record = new AttendanceRecord
            {
                Date = queryDate.ToString("yyyy/MM/dd"),
                ClockInTime = "應刷未刷",
                ClockInStatus = "應刷未刷",
                ClockOutTime = "應刷未刷",
                ClockOutStatus = "應刷未刷"
            };

            // 處理上班打卡 (WORK_CARD_TYPE = 0)
            var clockInData = cardDataList.FirstOrDefault(c => c.WORK_CARD_TYPE == 0);
            if (clockInData != null)
            {
                ProcessSimplifiedClockData(clockInData, record, isClockIn: true);
            }

            // 處理下班打卡 (WORK_CARD_TYPE = 1)
            var clockOutData = cardDataList.FirstOrDefault(c => c.WORK_CARD_TYPE == 1);
            if (clockOutData != null)
            {
                ProcessSimplifiedClockData(clockOutData, record, isClockIn: false);
            }

            return record;
        }

        /// <summary>
        /// 處理簡化的打卡資料
        /// 規則：
        /// 1. 若狀態為「正常」(空白)或「超時出勤」(3)顯示實際打卡時間和「正常」
        /// 2. 若為「應刷未刷」(0)或「曠職」(4)顯示「應刷未刷」
        /// 3. 遲到(1)和早退(2)顯示實際打卡時間和對應狀態
        /// 4. CARD_DATA_DATE 為 1900-01-01 表示未打卡
        /// </summary>
        private void ProcessSimplifiedClockData(SimplifiedCardData card, AttendanceRecord record, bool isClockIn)
        {
            var code = card.CARD_DATA_CODE ?? string.Empty;

            string displayTime;
            string displayStatus;

            // 檢查是否有有效的打卡時間（不是 1900-01-01）
            bool hasValidCardTime = card.CARD_DATA_DATE.HasValue 
                && card.CARD_DATA_DATE.Value.Year > 1900;

            // 判斷是否為「應刷未刷」或「曠職」
            if (code == AttendanceStatusCode.NotClocked || code == AttendanceStatusCode.Absent)
            {
                // 應刷未刷 (0) 或 曠職 (4)
                displayTime = "應刷未刷";
                displayStatus = "應刷未刷";
            }
            else if (string.IsNullOrEmpty(code) || code == AttendanceStatusCode.Overtime)
            {
                // 正常打卡（空白）或 超時出勤 (3) → 顯示「正常」
                if (hasValidCardTime)
                {
                    displayTime = card.CARD_DATA_DATE!.Value.ToString("yyyy/MM/dd HH:mm:ss");
                    displayStatus = "正常";
                }
                else
                {
                    // 理論上不應該發生，但為了安全起見
                    displayTime = "應刷未刷";
                    displayStatus = "應刷未刷";
                }
            }
            else
            {
                // 其他異常代碼（遲到1、早退2等）
                // 顯示實際打卡時間和狀態描述
                if (hasValidCardTime)
                {
                    displayTime = card.CARD_DATA_DATE!.Value.ToString("yyyy/MM/dd HH:mm:ss");
                    displayStatus = AttendanceStatusCode.GetStatusDescription(code);
                }
                else
                {
                    displayTime = "應刷未刷";
                    displayStatus = AttendanceStatusCode.GetStatusDescription(code);
                }
            }

            // 設定記錄
            if (isClockIn)
            {
                record.ClockInTime = displayTime;
                record.ClockInStatus = displayStatus;
                record.ClockInCode = code;
            }
            else
            {
                record.ClockOutTime = displayTime;
                record.ClockOutStatus = displayStatus;
                record.ClockOutCode = code;
            }
        }
    }

    /// <summary>
    /// 簡化的打卡資料模型
    /// </summary>
    internal class SimplifiedCardData
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