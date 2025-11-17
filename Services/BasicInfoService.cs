using Dapper;
using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 員工基本資料服務實作
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
    }
}
