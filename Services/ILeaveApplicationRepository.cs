using HRSystemAPI.Models;

namespace HRSystemAPI.Services;

/// <summary>
/// 請假單申請記錄資料庫介面
/// </summary>
public interface ILeaveApplicationRepository
{
    /// <summary>
    /// 儲存請假單申請記錄
    /// </summary>
    Task<bool> SaveLeaveApplicationAsync(string uid, string uname, string udepartment, 
        string formid, string leavetype, string estartdate, string estarttime, 
        string eenddate, string eendtime, string ereason);

    /// <summary>
    /// 查詢使用者的請假單列表（起始日未到）
    /// </summary>
    Task<List<CancelLeaveItem>> GetUserLeaveApplicationsAsync(string uid);

    /// <summary>
    /// 查詢單筆請假單
    /// </summary>
    Task<CancelLeaveItem?> GetLeaveApplicationByFormIdAsync(string formid, string uid);
}
