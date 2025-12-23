using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假餘額查詢服務實作
    /// </summary>
    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeaveBalanceService> _logger;
        private readonly string _connectionString;
        private const int HOURS_PER_DAY = 8; // 1天 = 8小時

        public LeaveBalanceService(IConfiguration configuration, ILogger<LeaveBalanceService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = _configuration.GetConnectionString("HRDatabase")
                ?? throw new InvalidOperationException("找不到資料庫連線字串");
        }

        /// <summary>
        /// 查詢個人請假餘額
        /// </summary>
        public async Task<LeaveBalanceResponse> GetLeaveBalanceAsync(string employeeNo, int year)
        {
            try
            {
                _logger.LogInformation($"開始查詢請假餘額 - 員工編號: {employeeNo}, 年度: {year}");

                // 1. 取得員工基本資料和到職日
                var employeeInfo = await GetEmployeeInfoAsync(employeeNo);
                if (employeeInfo == null)
                {
                    _logger.LogWarning($"查無員工編號: {employeeNo}");
                    return new LeaveBalanceResponse
                    {
                        Code = "500",
                        Msg = $"查無員工編號: {employeeNo}",
                        Data = null
                    };
                }

                _logger.LogInformation($"成功取得員工資料: {employeeInfo.Value.EmployeeName}");

                // 2. 計算周年制的起訖日期
                var anniversaryPeriod = await GetAnniversaryPeriodAsync(employeeNo, year);
                if (anniversaryPeriod == null)
                {
                    _logger.LogWarning($"無法計算員工 {employeeNo} 的周年制區間");
                    return new LeaveBalanceResponse
                    {
                        Code = "500",
                        Msg = $"無法計算員工 {employeeNo} 的周年制區間",
                        Data = null
                    };
                }

                var (startDate, endDate) = anniversaryPeriod.Value;
                _logger.LogInformation($"周年制區間: {startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");

                // 3. 查詢各類假別剩餘天數
                var leaveDetails = await GetAllLeaveBalanceAsync(employeeNo, year, startDate, endDate);

                // 4. 轉換為新的回應格式
                var leaveDataList = leaveDetails.Select(detail => new LeaveBalanceData
                {
                    LeaveType = detail.LeaveTypeName,
                    AnnualQuota = detail.TotalDays,
                    DeductedDays = detail.UsedDays,
                    RemainingDays = detail.RemainDays
                }).ToList();

                _logger.LogInformation($"成功查詢員工 {employeeNo} 的請假餘額，共 {leaveDataList.Count} 筆假別");

                return new LeaveBalanceResponse
                {
                    Code = "200",
                    Msg = "查詢成功",
                    Data = leaveDataList
                };
            }
            catch (TimeoutException)
            {
                _logger.LogWarning($"查詢請假餘額超時: {employeeNo}");
                return new LeaveBalanceResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢請假餘額失敗: {ex.Message}");
                return new LeaveBalanceResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                };
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

                _logger.LogInformation($"開始查詢員工基本資料 - 員工編號: '{employeeNo}' (長度: {employeeNo.Length})");

                var sql = @"
                    SELECT TOP 1 
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                ";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var employeeName = reader["EMPLOYEE_CNAME"]?.ToString() ?? "";
                    var hireDate = reader["EMPLOYEE_HIRE_DATE"] as DateTime?;
                    _logger.LogInformation($"找到員工資料: {employeeName}, 到職日: {hireDate}");
                    return (employeeName, hireDate);
                }

                _logger.LogWarning($"查詢員工基本資料未找到記錄 - EMPLOYEE_NO: {employeeNo}，嘗試替代查詢方式");

                // 嘗試方式1: 用 TRIM 移除前後空格
                var sqlTrim = @"
                    SELECT TOP 1 
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE LTRIM(RTRIM(EMPLOYEE_NO)) = @EmployeeNo
                ";

                command.CommandText = sqlTrim;
                command.Parameters.Clear();
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo.Trim();

                using var reader2 = await command.ExecuteReaderAsync();
                if (await reader2.ReadAsync())
                {
                    var employeeName = reader2["EMPLOYEE_CNAME"]?.ToString() ?? "";
                    var hireDate = reader2["EMPLOYEE_HIRE_DATE"] as DateTime?;
                    _logger.LogInformation($"用 TRIM 方式查詢成功 - 員工名稱: {employeeName}, 到職日期: {hireDate}");
                    return (employeeName, hireDate);
                }

                _logger.LogWarning($"所有查詢方式都未找到員工編號: {employeeNo}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢員工基本資料失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 一次查詢所有假別餘額
        /// </summary>
        private async Task<List<LeaveTypeDetail>> GetAllLeaveBalanceAsync(
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
                _logger.LogError(ex, $"查詢假別餘額失敗: {ex.Message}");
                return leaveDetails;
            }
        }

        /// <summary>
        /// 查詢特休餘額
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
                    WHERE es.EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                      AND es.EMPLOYEE_SPECIAL_YEAR = @Year
                      AND (es.IS_CLEAR IS NULL OR es.IS_CLEAR = 0)
                ";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = year;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // 特休：使用 EMPLOYEE_SPECIAL_VALUE (總共) 和 SPECIAL_REMAIN_HOURS (剩下)
                    decimal totalHours = Convert.ToDecimal(reader["EMPLOYEE_SPECIAL_VALUE"]);
                    decimal remainHours = Convert.ToDecimal(reader["SPECIAL_REMAIN_HOURS"]);
                    decimal usedHours = totalHours - remainHours; // 已請 = 總共 - 剩下
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
                _logger.LogError(ex, $"查詢特休餘額失敗: {ex.Message}");
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
                // 1. 查詢補休假（從 vwZZ_EMPLOYEE_CHANGE_HOUR）
                var compensatoryLeave = await GetCompensatoryLeaveAsync(connection, employeeNo, year: startDate.Year);
                if (compensatoryLeave != null)
                {
                    leaveDetails.Add(compensatoryLeave);
                }

                // 2. 查詢事假和病假（從 vwZZ_ASK_LEAVE 加總 ASK_LEAVE_HOUR）
                var personalAndSickLeaves = await GetPersonalAndSickLeavesAsync(connection, employeeNo, startDate, endDate);
                leaveDetails.AddRange(personalAndSickLeaves);

                return leaveDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢其他假別餘額失敗: {ex.Message}");
                return leaveDetails;
            }
        }

        /// <summary>
        /// 查詢補休假餘額（從 vwZZ_EMPLOYEE_CHANGE_HOUR）
        /// </summary>
        private async Task<LeaveTypeDetail?> GetCompensatoryLeaveAsync(
            SqlConnection connection,
            string employeeNo,
            int year)
        {
            try
            {
                var sql = @"
                    SELECT TOP 1
                        ISNULL(ch.CHANGE_HOURS, 0) as CHANGE_HOURS,
                        ISNULL(ch.CHANGE_REMAIN_HOURS, 0) as CHANGE_REMAIN_HOURS,
                        ISNULL(lr.LEAVE_MIN_VALUE, 1) as LEAVE_MIN_VALUE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE_CHANGE_HOUR] ch
                    LEFT JOIN [03546618].[dbo].[vwZZ_LEAVE_REFERENCE] lr 
                        ON lr.LEAVE_REFERENCE_CLASS = N'補休假'
                    WHERE ch.EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                      AND ch.YEAR = @Year
                ";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = year;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // 補休：使用 CHANGE_HOURS (總共) 和 CHANGE_REMAIN_HOURS (剩下)
                    decimal totalHours = Convert.ToDecimal(reader["CHANGE_HOURS"]);
                    decimal remainHours = Convert.ToDecimal(reader["CHANGE_REMAIN_HOURS"]);
                    decimal usedHours = totalHours - remainHours; // 已請 = 總共 - 剩下
                    decimal minUnit = Convert.ToDecimal(reader["LEAVE_MIN_VALUE"]);

                    // 如果總時數為 0，表示沒有補休資料
                    if (totalHours == 0)
                    {
                        return null;
                    }

                    return CreateLeaveTypeDetail(
                        leaveTypeName: "補休假",
                        leaveTypeCode: "COMPENSATORY",
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
                _logger.LogError(ex, $"查詢補休假餘額失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查詢事假和病假餘額（從 vwZZ_ASK_LEAVE 加總 ASK_LEAVE_HOUR）
        /// </summary>
        private async Task<List<LeaveTypeDetail>> GetPersonalAndSickLeavesAsync(
            SqlConnection connection,
            string employeeNo,
            DateTime startDate,
            DateTime endDate)
        {
            var leaveDetails = new List<LeaveTypeDetail>();

            try
            {
                // 定義事假和病假的全年度額度
                var leaveTypes = new Dictionary<string, (string code, decimal annualTotal)>
                {
                    { "事假", ("PERSONAL", 112) },  // 全年度14天 = 112小時
                    { "病假", ("SICK", 240) }       // 全年度30天 = 240小時
                };

                foreach (var leaveType in leaveTypes)
                {
                    var sql = @"
                        SELECT 
                            lr.LEAVE_REFERENCE_CLASS,
                            ISNULL(lr.LEAVE_MIN_VALUE, 1) as LEAVE_MIN_VALUE,
                            ISNULL(SUM(al.ASK_LEAVE_HOUR), 0) as UsedHours
                        FROM [03546618].[dbo].[vwZZ_LEAVE_REFERENCE] lr
                        LEFT JOIN [03546618].[dbo].[vwZZ_ASK_LEAVE] al 
                            ON al.LEAVE_REFERENCE_CLASS = lr.LEAVE_REFERENCE_CLASS
                            AND al.EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                            AND al.ASK_LEAVE_START BETWEEN @StartDate AND @EndDate
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
                        decimal usedHours = Convert.ToDecimal(reader["UsedHours"]); // 從 vwZZ_ASK_LEAVE 加總 ASK_LEAVE_HOUR
                        decimal totalHours = leaveType.Value.annualTotal; // 全年度額度
                        decimal remainHours = totalHours - usedHours;

                        // 如果超過全年度額度，剩餘顯示為 0
                        if (remainHours < 0)
                        {
                            remainHours = 0;
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
                _logger.LogError(ex, $"查詢事假/病假餘額失敗: {ex.Message}");
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
            // 計算天數（將小時數轉換為天數）
            var totalDays = Math.Round(totalHours / HOURS_PER_DAY, 1);
            var remainDays = Math.Round(remainHours / HOURS_PER_DAY, 1);
            var usedDays = Math.Round(usedHours / HOURS_PER_DAY, 1);

            return new LeaveTypeDetail
            {
                LeaveTypeName = leaveTypeName,
                LeaveTypeCode = leaveTypeCode,
                MinUnit = minUnit,

                RemainDays = remainDays,
                RemainHours = 0,
                RemainTotalHours = remainHours,

                TotalDays = totalDays,
                TotalHours = totalHours,

                UsedDays = usedDays,
                UsedHours = 0,
                UsedTotalHours = usedHours,

                DisplayText = $"{remainDays} 天"
            };
        }

        #endregion
    }
}
