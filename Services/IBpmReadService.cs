using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM Read 服務介面
    /// 處理 POST /app/bpmread 的業務邏輯
    /// </summary>
    public interface IBpmReadService
    {
        /// <summary>
        /// 處理 BPM Read 請求
        /// 從 BPM 中間件擷取表單資料並存入本地資料庫
        /// </summary>
        /// <param name="request">BPM Read 請求</param>
        /// <returns>處理結果</returns>
        Task<BpmReadResponse> ProcessBpmReadAsync(BpmReadRequest request);

        /// <summary>
        /// 驗證 BSKEY 是否有效
        /// </summary>
        /// <param name="bskey">第三方識別碼</param>
        /// <returns>是否有效</returns>
        bool ValidateBskey(string? bskey);

        /// <summary>
        /// 從 BPM 中間件取得單一表單資料
        /// </summary>
        /// <param name="processSerialNo">表單處理單號</param>
        /// <param name="formCode">表單格式代碼</param>
        /// <returns>BPM 流程資料</returns>
        Task<BpmProcessData?> FetchFormFromBpmMiddlewareAsync(string processSerialNo, string formCode);

        /// <summary>
        /// 將表單資料存入本地資料庫
        /// </summary>
        /// <param name="form">BPM 表單資料</param>
        /// <returns>儲存結果</returns>
        Task<bool> SaveFormToLocalDbAsync(BpmForm form);
    }
}
