using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假單服務介面（BPM 整合）
    /// </summary>
    public interface ILeaveFormService
    {
        /// <summary>
        /// 查詢請假單記錄（透過 BPM API）
        /// 支援西元年、月、日、區間查詢
        /// 預設查詢近 2 個月記錄
        /// </summary>
        Task<List<LeaveFormRecord>> GetLeaveFormsAsync(LeaveFormQueryRequest request);

        /// <summary>
        /// 查詢單一請假單詳情（透過 BPM API）
        /// 回傳完整詳細內容
        /// </summary>
        Task<LeaveFormRecord?> GetLeaveFormByIdAsync(string formId);

        /// <summary>
        /// 申請請假單（透過 BPM API + FTP 上傳附件）
        /// 支援 Word、Excel、PDF、圖片附件
        /// </summary>
        Task<LeaveFormOperationResult> CreateLeaveFormAsync(CreateLeaveFormRequest request);

        /// <summary>
        /// 取消請假單（透過 BPM API）
        /// </summary>
        Task<LeaveFormOperationResult> CancelLeaveFormAsync(string formId, string employeeEmail);

        /// <summary>
        /// 簽核請假單（透過 BPM API）- 支援核准/拒絕/退回重簽
        /// </summary>
        Task<LeaveFormOperationResult> ApproveLeaveFormAsync(ApproveLeaveFormRequest request);

        /// <summary>
        /// 查詢我的請假單（透過 BPM API）
        /// </summary>
        Task<List<LeaveFormRecord>> GetMyLeaveFormsAsync(string email, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// 查詢待簽核請假單（透過 BPM API）
        /// </summary>
        Task<List<LeaveFormRecord>> GetPendingLeaveFormsAsync(string approverEmail);

        /// <summary>
        /// 取得員工近 N 個月的請假記錄
        /// </summary>
        Task<List<LeaveFormRecord>> GetRecentLeaveFormsAsync(string employeeEmail, int months = 2);
    }
}