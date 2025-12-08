using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 出勤確認單服務介面 - 處理未刷卡補登
    /// </summary>
    public interface IAttendancePatchFormService
    {
        /// <summary>
        /// 建立出勤確認單（補登未刷卡時間）
        /// </summary>
        /// <param name="request">申請資料</param>
        /// <returns>處理結果</returns>
        Task<AttendancePatchOperationResult> CreateAttendancePatchFormAsync(CreateAttendancePatchFormRequest request);
    }
}
