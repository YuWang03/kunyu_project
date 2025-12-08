using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 我的表單服務介面
    /// </summary>
    public interface IEFormMyService
    {
        /// <summary>
        /// 取得我的表單列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>我的表單列表回應</returns>
        Task<EFormMyResponse> GetMyFormsAsync(EFormMyRequest request);
    }
}
