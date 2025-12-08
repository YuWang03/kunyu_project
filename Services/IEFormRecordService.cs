using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 簽核記錄詳細資料服務介面
    /// </summary>
    public interface IEFormRecordService
    {
        /// <summary>
        /// 取得簽核記錄詳細資料
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>簽核記錄詳細資料回應</returns>
        Task<EFormRecordResponse> GetFormRecordDetailAsync(EFormRecordRequest request);
    }
}
