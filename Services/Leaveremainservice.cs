using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假剩餘天數服務實作
    /// </summary>
    public class LeaveRemainService : ILeaveRemainService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeaveRemainService> _logger;
        private readonly string _connectionString;
        private const int HOURS_PER_DAY = 8; // 1天 = 8小時

        public LeaveRemainService(IConfiguration configuration, ILogger<LeaveRemainService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = _configuration.GetConnectionString("HRDatabase")
                ?? throw new InvalidOperationException("找不到資料庫連線字串");
        }

        /// <summary>
        /// 查詢個人請假剩餘天數
        /// </summary>
        public async Task<LeaveRemainResponse?> GetLeaveRemainAsync(string employeeNo, int? year = null)
        {
            try
            {
                int queryYear = year ?? DateTime.Now.Year;

                // 1. 取得員工基本資料和到職日
                var employeeInfo = await GetEmployeeInfoAsync(employeeNo);
                if (employeeInfo == null)
                {
                    _logger.LogWarning($"查無員工編號: {employeeNo}");
                    return null;
                }

                // 2. 計算周年制的起訖日期
                var anniversaryPeriod = await GetAnniversaryPeriodAsync(employeeNo, queryYear);
                if (anniversaryPeriod == null)
                {
                    _logger.LogWarning($"無法計算員工 {employeeNo} 的周年制區間");
                    return null;
                }

                var (startDate, endDate) = anniversaryPeriod.Value;

                // 3. 建立回應物件
                var response = new LeaveRemainResponse
                {
                    EmployeeNo = employeeNo,
                    EmployeeName = employeeInfo.Value.EmployeeName,
                    Year = queryYear.ToString(),
                    AnniversaryStart = startDate.ToString("yyyy/MM/dd"),
                    AnniversaryEnd = endDate.ToString("yyyy/MM/dd"),
                    QueryTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    LeaveTypes = new List<LeaveTypeDetail>()
                };

                // 4. 查詢各類假別剩餘天數（使用 JOIN 一次查詢）
                var leaveDetails = await GetAllLeaveRemainAsync(employeeNo, queryYear, startDate, endDate);
                response.LeaveTypes.AddRange(leaveDetails);

                // 即使沒有假別資料，也要返回基本資訊
                _logger.LogInformation($"成功查詢員工 {employeeNo} 的請假剩餘天數，共 {leaveDetails.Count} 筆假別");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢請假剩餘天數失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 計算周年制的起訖日期
        /// </summary>
        public async Task<(DateTime startDate, DateTime endDate)?> GetAnniversaryPeriodAsync(string employeeNo, int year)
        {
            try
            {
                var employeeInfo = await GetEmployeeInfoAsync(employeeNo);
                if (employeeInfo == null || !employeeInfo.Value.HireDate.HasValue)
                {
                    _logger.LogWarning($"員工 {employeeNo} 沒有到職日資料");
                    return null;
                }

                DateTime hireDate = employeeInfo.Value.HireDate.Value;

                // 計算該年度的周年起始日
                int yearsFromHire = year - hireDate.Year;
                DateTime startDate = hireDate.AddYears(yearsFromHire);
                DateTime endDate = startDate.AddYears(1).AddDays(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"計算周年制起訖日期失敗: {ex.Message}");
                return null;
            }
        }

        #region 私有方法

        /// <summary>
        /// 取得員工基本資料
        /// </summary>
        private async Task<(string EmployeeName, DateTime? HireDate)?> GetEmployeeInfoAsync(string employeeNo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT TOP 1 
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_ID = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                ";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var employeeName = reader["EMPLOYEE_CNAME"]?.ToString() ?? "";
                    var hireDate = reader["EMPLOYEE_HIRE_DATE"] as DateTime?;
                    return (employeeName, hireDate);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢員工基本資料失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 一次查詢所有假別剩餘天數
        /// </summary>
        private async Task<List<LeaveTypeDetail>> GetAllLeaveRemainAsync(
            string employeeNo,
            int year,
            DateTime startDate,
            DateTime endDate)
        {
            var leaveDetails = new List<LeaveTypeDetail>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. 查詢特休（從 vwZZ_EMPLOYEE_SPECIAL）
                var specialLeave = await GetSpecialLeaveAsync(connection, employeeNo, year);
                if (specialLeave != null)
                {
                    leaveDetails.Add(specialLeave);
                }

                // 2. 查詢其他假別（事假、病假、補休假）- 使用 JOIN
                var otherLeaves = await GetOtherLeavesAsync(connection, employeeNo, startDate, endDate);
                leaveDetails.AddRange(otherLeaves);

                return leaveDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢假別剩餘天數失敗: {ex.Message}");
                return leaveDetails;
            }
        }

        /// <summary>
        /// 查詢特休剩餘天數
        /// </summary>
        private async Task<LeaveTypeDetail?> GetSpecialLeaveAsync(
            SqlConnection connection,
            string employeeNo,
            int year)
        {
            try
            {
                var sql = @"
                    SELECT TOP 1
                        ISNULL(es.EMPLOYEE_SPECIAL_VALUE, 0) as EMPLOYEE_SPECIAL_VALUE,
                        ISNULL(es.SPECIAL_REMAIN_HOURS, 0) as SPECIAL_REMAIN_HOURS,
                        ISNULL(lr.LEAVE_MIN_VALUE, 2) as LEAVE_MIN_VALUE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE_SPECIAL] es
                    LEFT JOIN [03546618].[dbo].[vwZZ_LEAVE_REFERENCE] lr 
                        ON lr.LEAVE_REFERENCE_CLASS = N'特休'
                    WHERE es.EMPLOYEE_ID = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                      AND es.EMPLOYEE_SPECIAL_YEAR = @Year
                      AND (es.IS_CLEAR IS NULL OR es.IS_CLEAR = 0)
                ";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;
                command.Parameters.Add("@Year", SqlDbType.NVarChar).Value = year.ToString();

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // 從 decimal 讀取，避免精度問題
                    decimal totalHours = Convert.ToDecimal(reader["EMPLOYEE_SPECIAL_VALUE"]);
                    decimal remainHours = Convert.ToDecimal(reader["SPECIAL_REMAIN_HOURS"]);
                    decimal usedHours = totalHours - remainHours;
                    decimal minUnit = Convert.ToDecimal(reader["LEAVE_MIN_VALUE"]);

                    // 如果總時數為 0，表示沒有特休資料
                    if (totalHours == 0)
                    {
                        return null;
                    }

                    return CreateLeaveTypeDetail(
                        leaveTypeName: "特休",
                        leaveTypeCode: "SPECIAL",
                        minUnit: minUnit,
                        totalHours: totalHours,
                        remainHours: remainHours,
                        usedHours: usedHours
                    );
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢特休剩餘天數失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查詢其他假別（事假、病假、補休假）
        /// </summary>
        private async Task<List<LeaveTypeDetail>> GetOtherLeavesAsync(
            SqlConnection connection,
            string employeeNo,
            DateTime startDate,
            DateTime endDate)
        {
            var leaveDetails = new List<LeaveTypeDetail>();

            try
            {
                // 定義假別清單和預設值
                var leaveTypes = new Dictionary<string, (string code, decimal defaultTotal)>
                {
                    { "事假", ("PERSONAL", 56) },      // 7天 = 56小時
                    { "病假", ("SICK", 240) },         // 30天 = 240小時
                    { "補休假", ("COMPENSATORY", 0) }  // 無固定額度
                };

                foreach (var leaveType in leaveTypes)
                {
                    var sql = @"
                        SELECT 
                            lr.LEAVE_REFERENCE_CLASS,
                            ISNULL(lr.LEAVE_MIN_VALUE, 1) as LEAVE_MIN_VALUE,
                            ISNULL(SUM(al.ASK_LEAVE_HOUR - ISNULL(al.CANCEL_HOUR, 0)), 0) as UsedHours
                        FROM [03546618].[dbo].[vwZZ_LEAVE_REFERENCE] lr
                        LEFT JOIN [03546618].[dbo].[vwZZ_ASK_LEAVE] al 
                            ON al.LEAVE_REFERENCE_CLASS = lr.LEAVE_REFERENCE_CLASS
                            AND al.EMPLOYEE_ID = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                            AND al.ASK_LEAVE_START BETWEEN @StartDate AND @EndDate
                            AND al.IS_COUNT = 1
                        WHERE lr.LEAVE_REFERENCE_CLASS = @LeaveClass
                        GROUP BY lr.LEAVE_REFERENCE_CLASS, lr.LEAVE_MIN_VALUE
                    ";

                    using var command = new SqlCommand(sql, connection);
                    command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;
                    command.Parameters.Add("@LeaveClass", SqlDbType.NVarChar).Value = leaveType.Key;
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        decimal minUnit = Convert.ToDecimal(reader["LEAVE_MIN_VALUE"]);
                        decimal usedHours = Convert.ToDecimal(reader["UsedHours"]);
                        decimal totalHours = leaveType.Value.defaultTotal;
                        decimal remainHours = totalHours - usedHours;

                        // 病假特殊處理：只記錄已使用時數
                        if (leaveType.Key == "病假")
                        {
                            totalHours = 0;
                            remainHours = 0;
                        }

                        // 補休假：如果沒有使用記錄且總時數為0，則不顯示
                        if (leaveType.Key == "補休假" && usedHours == 0 && totalHours == 0)
                        {
                            continue;
                        }

                        var detail = CreateLeaveTypeDetail(
                            leaveTypeName: leaveType.Key,
                            leaveTypeCode: leaveType.Value.code,
                            minUnit: minUnit,
                            totalHours: totalHours,
                            remainHours: remainHours,
                            usedHours: usedHours
                        );

                        leaveDetails.Add(detail);
                    }
                }

                return leaveDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢其他假別剩餘天數失敗: {ex.Message}");
                return leaveDetails;
            }
        }

        /// <summary>
        /// 建立假別明細物件
        /// </summary>
        private LeaveTypeDetail CreateLeaveTypeDetail(
            string leaveTypeName,
            string leaveTypeCode,
            decimal minUnit,
            decimal totalHours,
            decimal remainHours,
            decimal usedHours)
        {
            // 病假特殊處理：顯示為無上限
            bool isSickLeave = leaveTypeName == "病假";

            // 計算天數和小時數
            var (totalDays, totalHoursRemainder) = ConvertHoursToDaysAndHours(totalHours);
            var (remainDays, remainHoursRemainder) = ConvertHoursToDaysAndHours(remainHours);
            var (usedDays, usedHoursRemainder) = ConvertHoursToDaysAndHours(usedHours);

            // 病假：不顯示剩餘天數，只顯示已使用
            if (isSickLeave)
            {
                totalDays = 0;
                totalHoursRemainder = 0;
                remainDays = 0;
                remainHoursRemainder = 0;
            }

            // 建立顯示文字
            string displayText = BuildDisplayText(leaveTypeName, remainDays, remainHoursRemainder, usedDays, usedHoursRemainder);

            return new LeaveTypeDetail
            {
                LeaveTypeName = leaveTypeName,
                LeaveTypeCode = leaveTypeCode,
                MinUnit = minUnit,

                RemainDays = remainDays,
                RemainHours = Math.Round(remainHoursRemainder, 1), // 保留1位小數
                RemainTotalHours = isSickLeave ? 0 : remainHours,

                TotalDays = totalDays,
                TotalHours = isSickLeave ? 0 : totalHours,

                UsedDays = usedDays,
                UsedHours = Math.Round(usedHoursRemainder, 1), // 保留1位小數
                UsedTotalHours = usedHours,

                DisplayText = displayText
            };
        }

        /// <summary>
        /// 轉換小時數為天數和小時數
        /// </summary>
        private (decimal days, decimal hours) ConvertHoursToDaysAndHours(decimal totalHours)
        {
            decimal days = Math.Floor(totalHours / HOURS_PER_DAY);
            decimal hours = totalHours % HOURS_PER_DAY;
            return (days, hours);
        }

        /// <summary>
        /// 建立顯示文字
        /// </summary>
        private string BuildDisplayText(string leaveTypeName, decimal remainDays, decimal remainHours, decimal usedDays, decimal usedHours)
        {
            // 病假特殊處理：顯示已使用時數
            if (leaveTypeName == "病假")
            {
                if (usedDays > 0 && usedHours > 0)
                {
                    return $"已使用 {usedDays} 天 {usedHours:0.#} 小時";
                }
                else if (usedDays > 0)
                {
                    return $"已使用 {usedDays} 天";
                }
                else if (usedHours > 0)
                {
                    return $"已使用 {usedHours:0.#} 小時";
                }
                else
                {
                    return "尚未使用";
                }
            }

            // 其他假別：顯示剩餘時數
            if (remainDays > 0 && remainHours > 0)
            {
                return $"{remainDays} 天 {remainHours:0.#} 小時";
            }
            else if (remainDays > 0)
            {
                return $"{remainDays} 天";
            }
            else if (remainHours > 0)
            {
                return $"{remainHours:0.#} 小時";
            }
            else
            {
                return "0 小時";
            }
        }

        #endregion
    }
}