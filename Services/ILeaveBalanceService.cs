using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假餘額查詢服務介面
    /// </summary>
    public interface ILeaveBalanceService
    {
        /// <summary>
        /// 查詢個人請假餘額
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="year">查詢年度</param>
        /// <returns>請假餘額資訊</returns>
        Task<LeaveBalanceResponse> GetLeaveBalanceAsync(string employeeNo, int year);

        /// <summary>
        /// 計算周年制的起訖日期
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="year">查詢年度</param>
        /// <returns>(起始日, 結束日)</returns>
        Task<(DateTime startDate, DateTime endDate)?> GetAnniversaryPeriodAsync(string employeeNo, int year);
    }
}
