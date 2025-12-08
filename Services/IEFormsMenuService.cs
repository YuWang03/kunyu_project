using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子表單選單服務介面
    /// </summary>
    public interface IEFormsMenuService
    {
        /// <summary>
        /// 取得電子表單選單列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>電子表單選單列表回應</returns>
        Task<EFormsMenuResponse> GetEFormsMenuListAsync(EFormsMenuRequest request);
    }
}
