using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 簽核記錄撤回服務介面
    /// </summary>
    public interface IEFormWithdrawService
    {
        /// <summary>
        /// 執行表單撤回
        /// </summary>
        Task<EFormWithdrawResponse> WithdrawFormAsync(EFormWithdrawRequest request);
    }
}
