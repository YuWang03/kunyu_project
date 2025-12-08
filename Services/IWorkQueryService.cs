using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 個人考勤查詢服務介面
    /// </summary>
    public interface IWorkQueryService
    {
        /// <summary>
        /// 查詢個人月度考勤記錄
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="yearMonth">年月 (格式: yyyy-MM)</param>
        /// <returns>月度考勤記錄</returns>
        Task<WorkQueryData?> GetMonthlyWorkRecordsAsync(string employeeNo, string yearMonth);
    }
}
