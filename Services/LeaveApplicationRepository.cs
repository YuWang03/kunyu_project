using Dapper;
using HRSystemAPI.Models;
using MySqlConnector;

namespace HRSystemAPI.Services;

/// <summary>
/// 請假單申請記錄資料庫實作
/// </summary>
public class LeaveApplicationRepository : ILeaveApplicationRepository
{
    private readonly ILogger<LeaveApplicationRepository> _logger;
    private readonly string _connectionString;

    public LeaveApplicationRepository(
        ILogger<LeaveApplicationRepository> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("找不到資料庫連線字串");
    }

    /// <summary>
    /// 儲存請假單申請記錄
    /// </summary>
    public async Task<bool> SaveLeaveApplicationAsync(
        string uid, string uname, string udepartment,
        string formid, string leavetype, string estartdate, string estarttime,
        string eenddate, string eendtime, string ereason)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO leave_applications 
                (uid, uname, udepartment, formid, leavetype, estartdate, estarttime, eenddate, eendtime, ereason)
                VALUES 
                (@Uid, @Uname, @Udepartment, @Formid, @Leavetype, @Estartdate, @Estarttime, @Eenddate, @Eendtime, @Ereason)
                ON DUPLICATE KEY UPDATE
                    uname = @Uname,
                    udepartment = @Udepartment,
                    leavetype = @Leavetype,
                    estartdate = @Estartdate,
                    estarttime = @Estarttime,
                    eenddate = @Eenddate,
                    eendtime = @Eendtime,
                    ereason = @Ereason,
                    updated_at = CURRENT_TIMESTAMP";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Uid = uid,
                Uname = uname,
                Udepartment = udepartment,
                Formid = formid,
                Leavetype = leavetype,
                Estartdate = estartdate,
                Estarttime = estarttime,
                Eenddate = eenddate,
                Eendtime = eendtime,
                Ereason = ereason
            });

            _logger.LogInformation("儲存請假單記錄成功: {Formid}, 受影響行數: {Rows}", formid, rowsAffected);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存請假單記錄失敗: {Formid}", formid);
            return false;
        }
    }

    /// <summary>
    /// 查詢使用者的請假單列表（起始日未到）
    /// </summary>
    public async Task<List<CancelLeaveItem>> GetUserLeaveApplicationsAsync(string uid)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    uid, uname, udepartment, formid, leavetype, 
                    DATE_FORMAT(estartdate, '%Y/%m/%d') as estartdate,
                    TIME_FORMAT(estarttime, '%H:%i') as estarttime,
                    DATE_FORMAT(eenddate, '%Y/%m/%d') as eenddate,
                    TIME_FORMAT(eendtime, '%H:%i') as eendtime,
                    ereason
                FROM leave_applications
                WHERE uid = @Uid
                ORDER BY estartdate ASC, estarttime ASC";

            var results = await connection.QueryAsync<CancelLeaveItem>(sql, new { Uid = uid });

            _logger.LogInformation("查詢使用者 {Uid} 的請假單列表，共 {Count} 筆", uid, results.Count());
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢使用者請假單列表失敗: {Uid}", uid);
            return new List<CancelLeaveItem>();
        }
    }

    /// <summary>
    /// 查詢單筆請假單
    /// </summary>
    public async Task<CancelLeaveItem?> GetLeaveApplicationByFormIdAsync(string formid, string uid)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    uid, uname, udepartment, formid, leavetype, 
                    DATE_FORMAT(estartdate, '%Y/%m/%d') as estartdate,
                    TIME_FORMAT(estarttime, '%H:%i') as estarttime,
                    DATE_FORMAT(eenddate, '%Y/%m/%d') as eenddate,
                    TIME_FORMAT(eendtime, '%H:%i') as eendtime,
                    ereason
                FROM leave_applications
                WHERE formid = @Formid AND uid = @Uid";

            var result = await connection.QueryFirstOrDefaultAsync<CancelLeaveItem>(sql, new 
            { 
                Formid = formid, 
                Uid = uid 
            });

            if (result != null)
            {
                _logger.LogInformation("查詢到請假單: {Formid}", formid);
            }
            else
            {
                _logger.LogWarning("查無請假單或不屬於該使用者: {Formid}, {Uid}", formid, uid);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查詢請假單失敗: {Formid}", formid);
            return null;
        }
    }
}
