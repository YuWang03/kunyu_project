using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 签核记录仓储接口
    /// </summary>
    public interface IEFormApprovalRepository
    {
        /// <summary>
        /// 保存签核记录
        /// </summary>
        Task SaveApprovalRecordAsync(EFormApprovalRecord record);

        /// <summary>
        /// 根据用户ID获取签核记录
        /// </summary>
        Task<List<EFormApprovalRecord>> GetApprovalRecordsByUidAsync(
            string uid, 
            int page = 1, 
            int pageSize = 20, 
            string? eformType = null);

        /// <summary>
        /// 获取用户签核记录总数
        /// </summary>
        Task<int> GetApprovalRecordsCountAsync(string uid, string? eformType = null);

        /// <summary>
        /// 根据表单ID获取签核记录
        /// </summary>
        Task<List<EFormApprovalRecord>> GetApprovalRecordsByFormIdAsync(string formId);
    }
}
