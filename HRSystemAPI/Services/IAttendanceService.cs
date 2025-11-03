using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 考勤查詢服務介面
    /// </summary>
    public interface IAttendanceService
    {
        /// <summary>
        /// 查詢個人出勤記錄
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="date">查詢日期 (格式: yyyy-MM-dd)</param>
        /// <returns>出勤記錄，若無資料則返回 null</returns>
        Task<AttendanceRecord?> GetAttendanceRecordAsync(string employeeNo, string date);

        /// <summary>
        /// 查詢所有員工的出勤記錄 (依日期)
        /// </summary>
        /// <param name="date">查詢日期 (格式: yyyy-MM-dd)</param>
        /// <returns>所有員工的出勤記錄清單</returns>
        Task<List<AttendanceRecord>> GetAllAttendanceRecordsAsync(string date);
    }
}