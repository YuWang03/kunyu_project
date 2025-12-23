using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 基本資料服務介面
    /// </summary>
    public interface IBasicInfoService
    {
        /// <summary>
        /// 取得基本資料選單列表
        /// </summary>
        /// <param name="uid">使用者ID (員工編號)</param>
        /// <returns>選單列表資訊</returns>
        Task<MenuListResponse> GetMenuListAsync(string uid);

        /// <summary>
        /// 取得基本資料選單列表（支援多語言）
        /// </summary>
        /// <param name="uid">使用者ID (員工編號)</param>
        /// <param name="language">語系 (T: 繁體中文, C: 簡體中文)</param>
        /// <returns>選單列表資訊</returns>
        Task<MenuListResponse> GetMenuListAsync(string uid, string language);

        /// <summary>
        /// 取得所有員工基本資料 (舊版本，保留供內部使用)
        /// </summary>
        Task<List<EmployeeBasicInfo>> GetAllBasicInfoAsync();

        /// <summary>
        /// 透過 Email 取得員工基本資料 (舊版本，保留供內部使用)
        /// </summary>
        Task<EmployeeBasicInfo?> GetEmployeeByEmailAsync(string email);

        /// <summary>
        /// 透過員工編號取得員工基本資料
        /// </summary>
        Task<EmployeeBasicInfo?> GetEmployeeByIdAsync(string employeeNo);
    }
}
