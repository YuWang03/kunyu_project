using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 加班單服務介面（BPM 整合）
    /// </summary>
    public interface IOvertimeService
    {
        /// <summary>
        /// 查詢加班單記錄（透過 BPM API）
        /// </summary>
        Task<List<OvertimeRecord>> GetOvertimeRecordsAsync(OvertimeQueryRequest request);

        /// <summary>
        /// 查詢單一加班單詳情（透過 BPM API）
        /// </summary>
        Task<OvertimeRecord?> GetOvertimeByIdAsync(string formId);

        /// <summary>
        /// 申請加班單（透過 BPM API + FTP 上傳附件）
        /// </summary>
        Task<OvertimeOperationResult> CreateOvertimeAsync(CreateOvertimeRequest request);

        /// <summary>
        /// 取消加班單（透過 BPM API）
        /// </summary>
        Task<OvertimeOperationResult> CancelOvertimeAsync(string formId, string employeeNo);

        /// <summary>
        /// 更新實際加班時間（透過 BPM API）
        /// </summary>
        Task<OvertimeOperationResult> UpdateActualOvertimeAsync(string formId, UpdateActualOvertimeRequest request);

        /// <summary>
        /// 取得員工近 N 個月的加班記錄
        /// </summary>
        Task<List<OvertimeRecord>> GetRecentOvertimeRecordsAsync(string employeeNo, int months = 2);
    }
}