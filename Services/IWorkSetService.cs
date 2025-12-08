using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 考勤超時出勤設定服務介面
    /// </summary>
    public interface IWorkSetService
    {
        /// <summary>
        /// 新增或修改考勤超時出勤設定
        /// </summary>
        /// <param name="request">設定請求資料</param>
        /// <returns>操作結果</returns>
        Task<WorkSetResponse> CreateOrUpdateWorkSetAsync(WorkSetRequest request);
    }
}
