using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 銷假單服務介面（BPM 整合）
    /// </summary>
    public interface ICancelLeaveService
    {
        /// <summary>
        /// 查詢單筆請假資料（透過 BPM API）
        /// 根據 formid 返回單一請假表單資料
        /// </summary>
        Task<CancelLeaveSingleResponse> GetCancelLeaveSingleAsync(CancelLeaveSingleRequest request);

        /// <summary>
        /// 查詢可銷假的請假單列表（透過 BPM API）
        /// 返回起始日未到的個人請假表單
        /// </summary>
        Task<CancelLeaveListResponse> GetCancelLeaveListAsync(CancelLeaveListRequest request);

        /// <summary>
        /// 查詢單一請假單詳細資料（透過 BPM API）
        /// 回傳完整詳細內容，包含附件
        /// </summary>
        Task<CancelLeaveDetailResponse> GetCancelLeaveDetailAsync(CancelLeaveDetailRequest request);

        /// <summary>
        /// 查詢請假單預覽（透過 BPM API）
        /// 用於銷假申請時顯示原請假單詳細資訊
        /// </summary>
        Task<EFleavePreviewResponse> GetLeavePreviewAsync(EFleavePreviewRequest request);

        /// <summary>
        /// 提交銷假申請（透過 BPM API）
        /// 取消已申請的請假單
        /// </summary>
        Task<CancelLeaveSubmitResponse> SubmitCancelLeaveAsync(CancelLeaveSubmitRequest request);
    }
}
