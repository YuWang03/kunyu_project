using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 外出外訓申請單服務介面（BPM 整合）
    /// </summary>
    public interface ILeaveOutFormService
    {
        /// <summary>
        /// 申請外出外訓單（透過 BPM API + FTP 上傳附件）
        /// 支援外出(A)和外訓(B)兩種類型
        /// </summary>
        /// <param name="request">外出外訓申請單請求</param>
        /// <returns>操作結果</returns>
        Task<LeaveOutFormOperationResult> CreateLeaveOutFormAsync(CreateLeaveOutFormRequest request);

        /// <summary>
        /// 查詢外出外訓單記錄（透過 BPM API）
        /// </summary>
        /// <param name="employeeNo">員工工號</param>
        /// <param name="startDate">查詢開始日期</param>
        /// <param name="endDate">查詢結束日期</param>
        /// <returns>外出外訓單記錄清單</returns>
        Task<List<LeaveOutFormRecord>> GetLeaveOutFormsAsync(string employeeNo, string? startDate = null, string? endDate = null);

        /// <summary>
        /// 查詢單一外出外訓單詳情（透過 BPM API）
        /// </summary>
        /// <param name="formId">表單ID</param>
        /// <returns>外出外訓單詳情</returns>
        Task<LeaveOutFormRecord?> GetLeaveOutFormByIdAsync(string formId);

        /// <summary>
        /// 取消外出外訓單（透過 BPM API）
        /// </summary>
        /// <param name="formId">表單ID</param>
        /// <param name="cancelReason">取消原因</param>
        /// <returns>操作結果</returns>
        Task<LeaveOutFormOperationResult> CancelLeaveOutFormAsync(string formId, string cancelReason);
    }
}
