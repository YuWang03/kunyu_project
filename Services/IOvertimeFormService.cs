using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 加班表單服務介面（BPM 整合 - PI_OVERTIME_001）
    /// </summary>
    public interface IOvertimeFormService
    {
        /// <summary>
        /// 申請加班表單（透過 BPM API + FTP 上傳附件）- 使用 PI_OVERTIME_001
        /// </summary>
        Task<OvertimeFormOperationResult> CreateOvertimeFormAsync(CreateOvertimeFormRequest request);
    }
}
