using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子表單類型服務介面
    /// </summary>
    public interface IEFormTypeService
    {
        /// <summary>
        /// 取得電子表單類型列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>電子表單類型列表回應</returns>
        Task<EFormTypeResponse> GetEFormTypesAsync(EFormTypeRequest request);
    }
}
