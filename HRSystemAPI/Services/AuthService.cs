using Dapper;
using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 驗證服務實作
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        /// <summary>
        /// 使用者登入（整合 Keycloak OIDC + Token ID 產生）
        /// </summary>
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                // 1. 從資料庫驗證使用者是否存在
                var userInfo = await GetUserInfoByEmailAsync(request.Email);

                if (userInfo == null)
                {
                    _logger.LogWarning("登入失敗：找不到使用者 {Email}", request.Email);
                    return null;
                }

                // 2. 檢查員工狀態（需求2：員工帳號需是「可使用的」才能正常登入）
                var employeeStatus = await CheckEmployeeStatusByEmployeeNoAsync(userInfo.EmployeeNo);
                if (!employeeStatus.IsActive)
                {
                    _logger.LogWarning("登入失敗：員工帳號不可使用 {Email}, Status: {Status}", 
                        request.Email, employeeStatus.Status);
                    return null;
                }

                // TODO: 實際串接 Keycloak OIDC
                // 這裡應該呼叫 Keycloak Token Endpoint 驗證 Email + Password
                // POST https://keycloak-server/auth/realms/{realm}/protocol/openid-connect/token

                // 3. 產生 Token ID（需求1：格式 nn + ssssss + nn）
                var tokenId = GenerateTokenId();

                // 4. 儲存 Token ID 到資料庫 tb_usermain
                await SaveTokenIdAsync(userInfo.EmployeeNo, tokenId);

                // 5. 回傳登入結果（包含 uid = 員工工號）
                var response = new LoginResponse
                {
                    TokenId = tokenId,                              // 需求1
                    AccessToken = $"mock_access_token_{Guid.NewGuid():N}", // TODO: 從 Keycloak 取得
                    RefreshToken = $"mock_refresh_token_{Guid.NewGuid():N}", // TODO: 從 Keycloak 取得
                    ExpiresIn = 3600,
                    Uid = userInfo.EmployeeNo,                      // 需求3：員工工號
                    EmployeeNo = userInfo.EmployeeNo,
                    EmployeeName = userInfo.EmployeeName,
                    Email = userInfo.Email,
                    IsActive = employeeStatus.IsActive,             // 需求2
                    Status = employeeStatus.Status
                };

                _logger.LogInformation("使用者登入成功：{Email}, TokenId: {TokenId}", request.Email, tokenId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入處理失敗：{Email}", request.Email);
                throw;
            }
        }

        /// <summary>
        /// 產生 Token ID（格式：nn + ssssss + nn = 亂數2位 + 時間序6位 + 亂數2位）
        /// </summary>
        private string GenerateTokenId()
        {
            var random = new Random();
            var prefix = random.Next(10, 99).ToString();           // 2位亂數
            var timestamp = DateTime.Now.ToString("HHmmss");       // 6位時間序
            var suffix = random.Next(10, 99).ToString();           // 2位亂數
            
            return $"{prefix}{timestamp}{suffix}";
        }

        /// <summary>
        /// 儲存 Token ID 到 tb_usermain
        /// </summary>
        private async Task SaveTokenIdAsync(string employeeNo, string tokenId)
        {
            const string sql = @"
                UPDATE tb_usermain 
                SET tokenid = @TokenId, tokentm = @TokenTime 
                WHERE unumber = @EmployeeNo";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    TokenId = tokenId,
                    TokenTime = DateTime.Now,
                    EmployeeNo = employeeNo
                });

                _logger.LogInformation("Token ID 已儲存：EmployeeNo={EmployeeNo}, TokenId={TokenId}", 
                    employeeNo, tokenId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存 Token ID 失敗：EmployeeNo={EmployeeNo}", employeeNo);
                throw;
            }
        }

        /// <summary>
        /// 驗證 Token ID 是否有效（需求1）
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string tokenId)
        {
            const string sql = @"
                SELECT COUNT(1) 
                FROM tb_usermain 
                WHERE tokenid = @TokenId";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var count = await connection.ExecuteScalarAsync<int>(sql, new { TokenId = tokenId });
                
                var isValid = count > 0;
                _logger.LogInformation("Token 驗證結果：TokenId={TokenId}, IsValid={IsValid}", tokenId, isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證 Token 失敗：TokenId={TokenId}", tokenId);
                return false;
            }
        }

        /// <summary>
        /// 檢核帳號狀態（需求4：檢核目前帳號狀態）
        /// </summary>
        public async Task<EmployeeStatusResponse> CheckEmployeeStatusAsync(string uid)
        {
            return await CheckEmployeeStatusByEmployeeNoAsync(uid);
        }

        /// <summary>
        /// 根據員工工號檢查狀態（從 tb_usermain 的 uwork 欄位）
        /// </summary>
        private async Task<EmployeeStatusResponse> CheckEmployeeStatusByEmployeeNoAsync(string employeeNo)
        {
            const string sql = @"
                SELECT uwork 
                FROM tb_usermain 
                WHERE unumber = @EmployeeNo";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var uwork = await connection.ExecuteScalarAsync<string>(sql, new { EmployeeNo = employeeNo });

                if (string.IsNullOrEmpty(uwork))
                {
                    return new EmployeeStatusResponse
                    {
                        Uid = employeeNo,
                        IsActive = false,
                        Status = "NOT_FOUND",
                        StatusName = "查無此員工"
                    };
                }

                // W: 使用, S: 停用, X: 永久停權
                var isActive = uwork == "W";
                var statusName = uwork switch
                {
                    "W" => "使用",
                    "S" => "停用",
                    "X" => "永久停權",
                    _ => "未知狀態"
                };

                return new EmployeeStatusResponse
                {
                    Uid = employeeNo,
                    IsActive = isActive,
                    Status = uwork,
                    StatusName = statusName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查員工狀態失敗：EmployeeNo={EmployeeNo}", employeeNo);
                throw;
            }
        }

        /// <summary>
        /// 刷新 Token（模擬實作）
        /// </summary>
        public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // TODO: 實際應該呼叫 Keycloak 的 Refresh Token Endpoint

                _logger.LogInformation("刷新 Token");

                // 模擬回傳新的 Token
                await Task.Delay(100); // 模擬網路延遲

                return new LoginResponse
                {
                    AccessToken = $"mock_access_token_{Guid.NewGuid():N}",
                    RefreshToken = $"mock_refresh_token_{Guid.NewGuid():N}",
                    ExpiresIn = 3600,
                    EmployeeNo = "E000001",
                    EmployeeName = "測試使用者",
                    Email = "test@company.com",
                    Status = "1"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新 Token 失敗");
                throw;
            }
        }

        /// <summary>
        /// 根據 Email 取得使用者資訊
        /// </summary>
        public async Task<UserInfo?> GetUserInfoByEmailAsync(string email)
        {
            const string sql = @"
                SELECT 
                    e.EMPLOYEE_NO AS EmployeeNo,
                    e.EMPLOYEE_NO AS Uid,
                    e.EMPLOYEE_CNAME AS EmployeeName,
                    e.EMPLOYEE_EMAIL_1 AS Email,
                    e.EMPLOYEE_WORK_STATUS AS Status,
                    c.COMPANY_CNAME AS CompanyName,
                    d.DEPARTMENT_CNAME AS DepartmentName,
                    e.JOB_CNAME AS JobTitle
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE] e
                LEFT JOIN [03546618].[dbo].[vwZZ_COMPANY] c 
                    ON e.COMPANY_ID = c.COMPANY_ID
                LEFT JOIN [03546618].[dbo].[vwZZ_DEPARTMENT] d 
                    ON e.DEPARTMENT_ID = d.DEPARTMENT_ID
                WHERE e.EMPLOYEE_EMAIL_1 = @Email";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<UserInfo>(
                    sql,
                    new { Email = email }
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者資訊失敗（Email: {Email}）", email);
                throw;
            }
        }
    }
}