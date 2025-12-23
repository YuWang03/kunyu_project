using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 我的簽核列表服務介面
    /// </summary>
    public interface IEFormMySignService
    {
        /// <summary>
        /// 取得我的簽核列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>我的簽核列表回應</returns>
        Task<EFormMySignResponse> GetMySignFormsAsync(EFormMySignRequest request);
    }
}
