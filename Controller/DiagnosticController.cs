using HRSystemAPI.Services;
using HRSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 诊断控制器 - 用于调试数据库查询问题
    /// </summary>
    [ApiController]
    [Route("app/[controller]")]
    [Tags("诊断工具")]
    public class DiagnosticController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiagnosticController> _logger;
        private readonly string _connectionString;

        public DiagnosticController(IConfiguration configuration, ILogger<DiagnosticController> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = _configuration.GetConnectionString("HRDatabase") 
                ?? throw new InvalidOperationException("找不到数据库连接字符串");
        }

        /// <summary>
        /// 诊断员工查询
        /// </summary>
        [HttpGet("employee/{employeeNo}")]
        public async Task<IActionResult> DiagnoseEmployeeAsync(string employeeNo)
        {
            try
            {
                _logger.LogInformation($"诊断员工查询: {employeeNo}");
                var result = new
                {
                    employeeNo = employeeNo,
                    queries = new List<object>()
                };

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 查询1: 直接查询
                using var cmd = new SqlCommand(@"
                    SELECT TOP 10 
                        EMPLOYEE_NO,
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                ", connection);
                
                cmd.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo;
                
                var reader = await cmd.ExecuteReaderAsync();
                var query1Results = new List<dynamic>();
                
                while (await reader.ReadAsync())
                {
                    query1Results.Add(new
                    {
                        EMPLOYEE_NO = reader["EMPLOYEE_NO"].ToString(),
                        EMPLOYEE_CNAME = reader["EMPLOYEE_CNAME"].ToString(),
                        EMPLOYEE_HIRE_DATE = reader["EMPLOYEE_HIRE_DATE"]
                    });
                }
                
                ((List<object>)result.queries).Add(new
                {
                    query = "Direct query with COLLATE",
                    found = query1Results.Count > 0,
                    results = query1Results
                });

                // 查询2: 用TRIM
                reader.Close();
                cmd.CommandText = @"
                    SELECT TOP 10 
                        EMPLOYEE_NO,
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE LTRIM(RTRIM(EMPLOYEE_NO)) = @EmployeeNo
                ";
                cmd.Parameters.Clear();
                cmd.Parameters.Add("@EmployeeNo", SqlDbType.NVarChar).Value = employeeNo.Trim();
                
                reader = await cmd.ExecuteReaderAsync();
                var query2Results = new List<dynamic>();
                
                while (await reader.ReadAsync())
                {
                    query2Results.Add(new
                    {
                        EMPLOYEE_NO = reader["EMPLOYEE_NO"].ToString(),
                        EMPLOYEE_CNAME = reader["EMPLOYEE_CNAME"].ToString(),
                        EMPLOYEE_HIRE_DATE = reader["EMPLOYEE_HIRE_DATE"]
                    });
                }

                ((List<object>)result.queries).Add(new
                {
                    query = "Query with LTRIM(RTRIM())",
                    found = query2Results.Count > 0,
                    results = query2Results
                });

                // 查询3: 模糊查询
                reader.Close();
                cmd.CommandText = @"
                    SELECT TOP 20 
                        EMPLOYEE_NO,
                        EMPLOYEE_CNAME,
                        LEN(EMPLOYEE_NO) as NO_LENGTH
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_NO LIKE @Pattern
                    ORDER BY EMPLOYEE_NO
                ";
                cmd.Parameters.Clear();
                cmd.Parameters.Add("@Pattern", SqlDbType.NVarChar).Value = $"%{employeeNo}%";
                
                reader = await cmd.ExecuteReaderAsync();
                var query3Results = new List<dynamic>();
                
                while (await reader.ReadAsync())
                {
                    query3Results.Add(new
                    {
                        EMPLOYEE_NO = reader["EMPLOYEE_NO"].ToString(),
                        EMPLOYEE_CNAME = reader["EMPLOYEE_CNAME"].ToString(),
                        NO_LENGTH = reader["NO_LENGTH"]
                    });
                }

                ((List<object>)result.queries).Add(new
                {
                    query = $"LIKE query with pattern '%{employeeNo}%'",
                    found = query3Results.Count > 0,
                    results = query3Results
                });

                reader.Close();
                connection.Close();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"诊断查询失败: {ex.Message}");
                return Ok(new
                {
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 获取前N个员工编号
        /// </summary>
        [HttpGet("employees/sample")]
        public async Task<IActionResult> GetEmployeeSampleAsync(int count = 20)
        {
            try
            {
                var result = new List<dynamic>();

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var cmd = new SqlCommand($@"
                    SELECT TOP {count} 
                        EMPLOYEE_NO,
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    ORDER BY EMPLOYEE_NO
                ", connection);

                var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        EMPLOYEE_NO = reader["EMPLOYEE_NO"].ToString(),
                        EMPLOYEE_CNAME = reader["EMPLOYEE_CNAME"].ToString(),
                        EMPLOYEE_HIRE_DATE = reader["EMPLOYEE_HIRE_DATE"]
                    });
                }

                reader.Close();
                connection.Close();

                return Ok(new
                {
                    count = result.Count,
                    employees = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取员工样本失败: {ex.Message}");
                return Ok(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取员工总数
        /// </summary>
        [HttpGet("employees/count")]
        public async Task<IActionResult> GetEmployeeCountAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var cmd = new SqlCommand(@"
                    SELECT COUNT(*) as TOTAL FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                ", connection);

                var count = (int)await cmd.ExecuteScalarAsync();
                connection.Close();

                return Ok(new { totalEmployees = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取员工总数失败: {ex.Message}");
                return Ok(new { error = ex.Message });
            }
        }
    }
}
