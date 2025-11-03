using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 驗證服務介面
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 使用者登入
        /// </summary>
        Task<LoginResponse?> LoginAsync(LoginRequest request);

        /// <summary>
        /// 刷新 Token
        /// </summary>
        Task<LoginResponse?> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 根據 Email 取得使用者資訊
        /// </summary>
        Task<UserInfo?> GetUserInfoByEmailAsync(string email);

        /// <summary>
        /// 驗證 Token ID 是否有效（需求1）
        /// </summary>
        Task<bool> ValidateTokenAsync(string tokenId);

        /// <summary>
        /// 檢核帳號狀態（需求4）
        /// </summary>
        Task<EmployeeStatusResponse> CheckEmployeeStatusAsync(string uid);
    }
}