using Dapper;
using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 基本資料服務實作
    /// </summary>
    public class BasicInfoService : IBasicInfoService
    {
        private readonly string _connectionString;
        private readonly ILogger<BasicInfoService> _logger;

        public BasicInfoService(IConfiguration configuration, ILogger<BasicInfoService> logger)
        {
            _connectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        /// <summary>
        /// 取得基本資料選單列表
        /// </summary>
        public async Task<MenuListResponse> GetMenuListAsync(string uid)
        {
            try
            {
                // 驗證員工是否存在
                var employeeExists = await ValidateEmployeeAsync(uid);

                if (!employeeExists)
                {
                    _logger.LogWarning($"查無使用者ID: {uid}");
                    return new MenuListResponse
                    {
                        Code = "500",
                        Msg = $"查無使用者ID: {uid}",
                        Data = null
                    };
                }

                // 返回固定的選單列表
                _logger.LogInformation($"成功取得使用者 {uid} 的選單列表");

                return new MenuListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new MenuData
                    {
                        MenuList = new List<string>
                        {
                            "考勤查詢",
                            "請假剩餘天數",
                            "個人資訊"
                        }
                    }
                };
            }
            catch (TimeoutException)
            {
                _logger.LogWarning($"取得選單列表超時: {uid}");
                return new MenuListResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得選單列表失敗: {ex.Message}");
                return new MenuListResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                };
            }
        }

        /// <summary>
        /// 驗證員工是否存在
        /// </summary>
        private async Task<bool> ValidateEmployeeAsync(string uid)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                WHERE EMPLOYEE_NO = @Uid COLLATE Chinese_Taiwan_Stroke_CI_AS";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var count = await connection.ExecuteScalarAsync<int>(sql, new { Uid = uid });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證員工失敗");
                return false;
            }
        }

        /// <summary>
        /// 取得所有員工基本資料
        /// </summary>
        public async Task<List<EmployeeBasicInfo>> GetAllBasicInfoAsync()
        {
            const string sql = @"
                SELECT 
                    EMPLOYEE_ID AS EmployeeId,
                    EMPLOYEE_CNAME AS EmployeeName,
                    EMPLOYEE_NO AS EmployeeNo,
                    COMPANY_CNAME AS CompanyName,
                    COMPANY_ID AS CompanyId,
                    DEPARTMENT_CNAME AS DepartmentName,
                    DEPARTMENT_ID AS DepartmentId,
                    JOB_CNAME AS JobTitle,
                    EMPLOYEE_ORG_START_DATE AS JoinDate,
                    EMPLOYEE_EMAIL_1 AS Email
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryAsync<EmployeeBasicInfo>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得員工基本資料失敗");
                throw;
            }
        }

        /// <summary>
        /// 透過 Email 取得員工基本資料
        /// </summary>
        public async Task<EmployeeBasicInfo?> GetEmployeeByEmailAsync(string email)
        {
            const string sql = @"
                SELECT 
                    EMPLOYEE_ID AS EmployeeId,
                    EMPLOYEE_CNAME AS EmployeeName,
                    EMPLOYEE_NO AS EmployeeNo,
                    COMPANY_CNAME AS CompanyName,
                    COMPANY_ID AS CompanyId,
                    DEPARTMENT_CNAME AS DepartmentName,
                    DEPARTMENT_ID AS DepartmentId,
                    JOB_CNAME AS JobTitle,
                    EMPLOYEE_ORG_START_DATE AS JoinDate,
                    EMPLOYEE_EMAIL_1 AS Email
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                WHERE EMPLOYEE_EMAIL_1 = @Email";

            try
            {
                _logger.LogInformation("查詢員工資料: {Email}", email);
                
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<EmployeeBasicInfo>(sql, new { Email = email });
                
                if (result != null)
                {
                    _logger.LogInformation("找到員工: {EmployeeNo} - {Name}", result.EmployeeNo, result.EmployeeName);
                }
                else
                {
                    _logger.LogWarning("找不到 Email 對應的員工: {Email}", email);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "透過 Email 查詢員工資料失敗: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// 透過員工編號取得員工基本資料
        /// </summary>
        public async Task<EmployeeBasicInfo?> GetEmployeeByIdAsync(string employeeNo)
        {
            const string sql = @"
                SELECT 
                    EMPLOYEE_ID AS EmployeeId,
                    EMPLOYEE_CNAME AS EmployeeName,
                    EMPLOYEE_NO AS EmployeeNo,
                    COMPANY_CNAME AS CompanyName,
                    COMPANY_ID AS CompanyId,
                    DEPARTMENT_CNAME AS DepartmentName,
                    DEPARTMENT_ID AS DepartmentId,
                    JOB_CNAME AS JobTitle,
                    EMPLOYEE_ORG_START_DATE AS JoinDate,
                    EMPLOYEE_EMAIL_1 AS Email
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                WHERE EMPLOYEE_NO = @EmployeeNo";

            try
            {
                _logger.LogInformation("查詢員工資料: {EmployeeNo}", employeeNo);
                
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<EmployeeBasicInfo>(sql, new { EmployeeNo = employeeNo });
                
                if (result != null)
                {
                    _logger.LogInformation("找到員工: {EmployeeNo} - {Name}", result.EmployeeNo, result.EmployeeName);
                }
                else
                {
                    _logger.LogWarning("找不到員工編號對應的員工: {EmployeeNo}", employeeNo);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "透過員工編號查詢員工資料失敗: {EmployeeNo}", employeeNo);
                throw;
            }
        }
    }
}
