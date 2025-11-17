using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假剩餘天數服務介面
    /// </summary>
    public interface ILeaveRemainService
    {
        /// <summary>
        /// 查詢個人請假剩餘天數
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="year">查詢年度 (選填，預設當年)</param>
        /// <returns>請假剩餘天數資訊</returns>
        Task<LeaveRemainResponse?> GetLeaveRemainAsync(string employeeNo, int? year = null);

        /// <summary>
        /// 計算周年制的起訖日期
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="year">查詢年度</param>
        /// <returns>(起始日, 結束日)</returns>
        Task<(DateTime startDate, DateTime endDate)?> GetAnniversaryPeriodAsync(string employeeNo, int year);
    }
}