using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 員工基本資料服務介面
    /// </summary>
    public interface IBasicInfoService
    {
        /// <summary>
        /// 取得所有員工基本資料
        /// </summary>
        Task<List<EmployeeBasicInfo>> GetAllBasicInfoAsync();
    }
}
