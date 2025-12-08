using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// Token 驗證服務介面
    /// </summary>
    public interface ITokenValidationService
    {
        /// <summary>
        /// 驗證 Token 是否有效
        /// </summary>
        /// <param name="tokenId">Token標記</param>
        /// <param name="uid">員工工號</param>
        /// <param name="cid">目前所屬公司</param>
        /// <returns>驗證結果</returns>
        Task<TokenVerifyResponse> ValidateTokenAsync(string tokenId, string uid, string cid);
    }
}
