using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 中間件服務介面
    /// 用於與 BPM Middleware API 進行整合
    /// </summary>
    public interface IBpmMiddlewareService
    {
        /// <summary>
        /// 同步流程信息 - syncProcessInfo API
        /// 根據表單編號和流程代碼查詢 BPM 中的流程資訊
        /// </summary>
        /// <param name="processSerialNo">表單編號</param>
        /// <param name="processCode">處理程序代碼</param>
        /// <param name="environment">環境代碼（TEST/PROD）</param>
        /// <returns>BPM 流程同步結果</returns>
        Task<BpmSyncProcessInfoResponse> SyncProcessInfoAsync(string processSerialNo, string processCode, string environment = "TEST");

        /// <summary>
        /// 查詢請假單程序代碼
        /// </summary>
        /// <returns>請假單在 BPM 系統中的程序代碼</returns>
        Task<string> GetLeaveProcessCodeAsync();

        /// <summary>
        /// 查詢業務出差單程序代碼
        /// </summary>
        /// <returns>業務出差單在 BPM 系統中的程序代碼</returns>
        Task<string> GetBusinessTripProcessCodeAsync();
    }
}
