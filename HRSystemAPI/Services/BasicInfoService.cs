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
                    e.EMPLOYEE_ID AS EmployeeId,
                    e.EMPLOYEE_CNAME AS EmployeeName,
                    e.EMPLOYEE_NO AS EmployeeNo,
                    e.COMPANY_CNAME AS CompanyName,
                    e.DEPARTMENT_CNAME AS DepartmentName,
                    e.JOB_CNAME AS JobTitle,
                    e.EMPLOYEE_ORG_START_DATE AS JoinDate,
                    e.EMPLOYEE_EMAIL_1 AS Email
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE] e";

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
    }
}
