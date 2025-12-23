using HRSystemAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using HRSystemAPI.Services;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 表單倉儲服務介面
    /// </summary>
    public interface IBpmFormRepository
    {
        // 表單主表操作
        Task<BpmForm?> GetFormByIdAsync(string formId);
        Task<BpmForm?> GetFormWithDetailsAsync(string formId);
        Task<BpmFormQueryResult> QueryFormsAsync(BpmFormQueryRequest request);
        Task<List<BpmForm>> GetFormsByApplicantAsync(string applicantId, string? formType = null);
        Task<bool> FormExistsAsync(string formId);
        
        // 建立/更新表單
        Task<BpmForm> CreateFormAsync(BpmForm form);
        Task<BpmForm> UpdateFormAsync(BpmForm form);
        Task<BpmForm> CreateOrUpdateFormAsync(BpmForm form);
        
        // 取消表單
        Task<FormCancelResult> CancelFormAsync(FormCancelRequest request);
        
        // 同步日誌
        Task LogSyncAsync(BpmFormSyncLog log);
        Task<List<BpmFormSyncLog>> GetSyncLogsAsync(string formId, int limit = 10);
        
        // 簽核歷程
        Task AddApprovalHistoryAsync(BpmFormApprovalHistory history);
        Task<List<BpmFormApprovalHistory>> GetApprovalHistoryAsync(string formId);
        
        // 請假單特定操作
        Task<BpmLeaveForm?> GetLeaveFormDetailAsync(string formId);
        Task<List<BpmForm>> GetCancellableLeaveForms(string applicantId);
        
        // 加班單特定操作
        Task<BpmOvertimeForm?> GetOvertimeFormDetailAsync(string formId);
        
        // 出差單特定操作
        Task<BpmBusinessTripForm?> GetBusinessTripFormDetailAsync(string formId);
        
        // 銷假單特定操作
        Task<BpmCancelLeaveForm?> GetCancelLeaveFormDetailAsync(string formId);
    }

    /// <summary>
    /// BPM 表單倉儲服務實作
    /// 支援本地和遠端雙資料庫同步
    /// </summary>
    public class BpmFormRepository : IBpmFormRepository
    {
        private readonly BpmFormDbContext? _context;  // 本地資料庫
        private readonly RemoteBpmFormDbContext? _remoteContext;  // 遠端資料庫
        private readonly ILogger<BpmFormRepository> _logger;

        public BpmFormRepository(
            ILogger<BpmFormRepository> logger,
            BpmFormDbContext? context = null, 
            RemoteBpmFormDbContext? remoteContext = null)
        {
            _context = context;
            _remoteContext = remoteContext;
            _logger = logger;
        }

        #region 表單主表操作

        /// <summary>
        /// 根據表單編號取得表單
        /// </summary>
        public async Task<BpmForm?> GetFormByIdAsync(string formId)
        {
            try
            {
                if (_context == null)
                {
                    _logger.LogWarning("資料庫未連接，無法取得表單: {FormId}", formId);
                    return null;
                }
                
                return await _context.BpmForms
                    .FirstOrDefaultAsync(f => f.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得表單失敗: {FormId}", formId);
                throw;
            }
        }

        /// <summary>
        /// 根據表單編號取得表單及其詳細資料
        /// </summary>
        public async Task<BpmForm?> GetFormWithDetailsAsync(string formId)
        {
            try
            {
                if (_context == null)
                {
                    _logger.LogWarning("資料庫未連接，無法取得表單詳細資料: {FormId}", formId);
                    return null;
                }
                
                var form = await _context.BpmForms
                    .Include(f => f.LeaveForm)
                    .Include(f => f.OvertimeForm)
                    .Include(f => f.BusinessTripForm)
                    .Include(f => f.CancelLeaveForm)
                    .Include(f => f.ApprovalHistory)
                    .FirstOrDefaultAsync(f => f.FormId == formId);

                return form;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得表單詳細資料失敗: {FormId}", formId);
                throw;
            }
        }

        /// <summary>
        /// 查詢表單列表
        /// </summary>
        public async Task<BpmFormQueryResult> QueryFormsAsync(BpmFormQueryRequest request)
        {
            try
            {
                var query = _context.BpmForms.AsQueryable();

                // 套用篩選條件
                if (!string.IsNullOrEmpty(request.FormId))
                    query = query.Where(f => f.FormId == request.FormId);

                if (!string.IsNullOrEmpty(request.FormType))
                    query = query.Where(f => f.FormType == request.FormType);

                if (!string.IsNullOrEmpty(request.ApplicantId))
                    query = query.Where(f => f.ApplicantId == request.ApplicantId);

                if (!string.IsNullOrEmpty(request.CompanyId))
                    query = query.Where(f => f.CompanyId == request.CompanyId);

                if (!string.IsNullOrEmpty(request.Status))
                    query = query.Where(f => f.Status == request.Status);

                if (request.IsCancelled.HasValue)
                    query = query.Where(f => f.IsCancelled == request.IsCancelled.Value);

                if (request.ApplyDateFrom.HasValue)
                    query = query.Where(f => f.ApplyDate >= request.ApplyDateFrom.Value);

                if (request.ApplyDateTo.HasValue)
                    query = query.Where(f => f.ApplyDate <= request.ApplyDateTo.Value);

                // 計算總筆數
                var totalCount = await query.CountAsync();

                // 排序
                query = query.OrderByDescending(f => f.ApplyDate).ThenByDescending(f => f.CreatedAt);

                // 分頁
                var page = Math.Max(1, request.Page);
                var pageSize = Math.Max(1, Math.Min(100, request.PageSize));
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var forms = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new BpmFormQueryResult
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Page = page,
                    PageSize = pageSize,
                    Forms = forms
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢表單失敗");
                throw;
            }
        }

        /// <summary>
        /// 根據申請人取得表單列表
        /// </summary>
        public async Task<List<BpmForm>> GetFormsByApplicantAsync(string applicantId, string? formType = null)
        {
            try
            {
                var query = _context.BpmForms
                    .Where(f => f.ApplicantId == applicantId);

                if (!string.IsNullOrEmpty(formType))
                    query = query.Where(f => f.FormType == formType);

                return await query
                    .OrderByDescending(f => f.ApplyDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得申請人表單失敗: {ApplicantId}", applicantId);
                throw;
            }
        }

        /// <summary>
        /// 檢查表單是否存在
        /// </summary>
        public async Task<bool> FormExistsAsync(string formId)
        {
            try
            {
                return await _context.BpmForms.AnyAsync(f => f.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查表單是否存在失敗: {FormId}", formId);
                throw;
            }
        }

        #endregion

        #region 建立/更新表單

        /// <summary>
        /// 建立表單 (同時寫入本地和遠端資料庫)
        /// </summary>
        public async Task<BpmForm> CreateFormAsync(BpmForm form)
        {
            try
            {
                form.CreatedAt = DateTime.Now;
                form.UpdatedAt = DateTime.Now;

                // 1. 寫入本地資料庫
                _context.BpmForms.Add(form);
                await _context.SaveChangesAsync();
                _logger.LogInformation("建立表單成功 (本地): {FormId}, 類型: {FormType}", form.FormId, form.FormType);

                // 2. 同步寫入遠端資料庫
                try
                {
                    // 創建新的實例以避免追蹤衝突
                    var remoteForm = new BpmForm
                    {
                        FormId = form.FormId,
                        FormCode = form.FormCode,
                        FormType = form.FormType,
                        FormVersion = form.FormVersion,
                        ApplicantId = form.ApplicantId,
                        ApplicantName = form.ApplicantName,
                        ApplicantDepartment = form.ApplicantDepartment,
                        CompanyId = form.CompanyId,
                        Status = form.Status,
                        BpmStatus = form.BpmStatus,
                        FormData = form.FormData,
                        ApplyDate = form.ApplyDate,
                        SubmitTime = form.SubmitTime,
                        CurrentApproverId = form.CurrentApproverId,
                        CurrentApproverName = form.CurrentApproverName,
                        ApprovalComment = form.ApprovalComment,
                        IsCancelled = form.IsCancelled,
                        CancelReason = form.CancelReason,
                        CancelTime = form.CancelTime,
                        CancelledBy = form.CancelledBy,
                        IsSyncedToBpm = form.IsSyncedToBpm,
                        LastSyncTime = form.LastSyncTime,
                        SyncErrorMessage = form.SyncErrorMessage,
                        CreatedAt = form.CreatedAt,
                        UpdatedAt = form.UpdatedAt
                    };

                    _remoteContext.BpmForms.Add(remoteForm);
                    await _remoteContext.SaveChangesAsync();
                    _logger.LogInformation("✅ 成功同步表單到遠端資料庫 (54.46.24.34): {FormId}", form.FormId);
                }
                catch (Exception remoteEx)
                {
                    _logger.LogWarning(remoteEx, "⚠️  同步表單到遠端資料庫失敗，但本地已保存: {FormId}", form.FormId);
                    // 不拋出異常，確保本地保存成功即可
                }

                return form;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立表單失敗: {FormId}", form.FormId);
                throw;
            }
        }

        /// <summary>
        /// 更新表單 (同時更新本地和遠端資料庫)
        /// </summary>
        public async Task<BpmForm> UpdateFormAsync(BpmForm form)
        {
            try
            {
                form.UpdatedAt = DateTime.Now;

                // 1. 更新本地資料庫
                _context.BpmForms.Update(form);
                await _context.SaveChangesAsync();
                _logger.LogInformation("更新表單成功 (本地): {FormId}", form.FormId);

                // 2. 同步更新遠端資料庫
                try
                {
                    var remoteForm = await _remoteContext.BpmForms
                        .FirstOrDefaultAsync(f => f.FormId == form.FormId);

                    if (remoteForm != null)
                    {
                        // 更新遠端表單
                        remoteForm.Status = form.Status;
                        remoteForm.BpmStatus = form.BpmStatus;
                        remoteForm.FormData = form.FormData;
                        remoteForm.CurrentApproverId = form.CurrentApproverId;
                        remoteForm.CurrentApproverName = form.CurrentApproverName;
                        remoteForm.ApprovalComment = form.ApprovalComment;
                        remoteForm.IsCancelled = form.IsCancelled;
                        remoteForm.CancelReason = form.CancelReason;
                        remoteForm.CancelTime = form.CancelTime;
                        remoteForm.CancelledBy = form.CancelledBy;
                        remoteForm.LastSyncTime = form.LastSyncTime;
                        remoteForm.SyncErrorMessage = form.SyncErrorMessage;
                        remoteForm.UpdatedAt = form.UpdatedAt;

                        await _remoteContext.SaveChangesAsync();
                        _logger.LogInformation("✅ 成功同步更新表單到遠端資料庫 (54.46.24.34): {FormId}", form.FormId);
                    }
                    else
                    {
                        _logger.LogWarning("遠端資料庫找不到表單，嘗試新增: {FormId}", form.FormId);
                        // 如果遠端不存在，則新增
                        var newRemoteForm = new BpmForm
                        {
                            FormId = form.FormId,
                            FormCode = form.FormCode,
                            FormType = form.FormType,
                            FormVersion = form.FormVersion,
                            ApplicantId = form.ApplicantId,
                            ApplicantName = form.ApplicantName,
                            ApplicantDepartment = form.ApplicantDepartment,
                            CompanyId = form.CompanyId,
                            Status = form.Status,
                            BpmStatus = form.BpmStatus,
                            FormData = form.FormData,
                            ApplyDate = form.ApplyDate,
                            SubmitTime = form.SubmitTime,
                            CurrentApproverId = form.CurrentApproverId,
                            CurrentApproverName = form.CurrentApproverName,
                            ApprovalComment = form.ApprovalComment,
                            IsCancelled = form.IsCancelled,
                            CancelReason = form.CancelReason,
                            CancelTime = form.CancelTime,
                            CancelledBy = form.CancelledBy,
                            IsSyncedToBpm = form.IsSyncedToBpm,
                            LastSyncTime = form.LastSyncTime,
                            SyncErrorMessage = form.SyncErrorMessage,
                            CreatedAt = form.CreatedAt,
                            UpdatedAt = form.UpdatedAt
                        };
                        _remoteContext.BpmForms.Add(newRemoteForm);
                        await _remoteContext.SaveChangesAsync();
                        _logger.LogInformation("✅ 成功新增表單到遠端資料庫 (54.46.24.34): {FormId}", form.FormId);
                    }
                }
                catch (Exception remoteEx)
                {
                    _logger.LogWarning(remoteEx, "⚠️  同步更新表單到遠端資料庫失敗，但本地已保存: {FormId}", form.FormId);
                    // 不拋出異常，確保本地保存成功即可
                }

                return form;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表單失敗: {FormId}", form.FormId);
                throw;
            }
        }

        /// <summary>
        /// 建立或更新表單
        /// </summary>
        public async Task<BpmForm> CreateOrUpdateFormAsync(BpmForm form)
        {
            try
            {
                var existingForm = await GetFormByIdAsync(form.FormId);

                if (existingForm == null)
                {
                    return await CreateFormAsync(form);
                }
                else
                {
                    // 更新現有表單
                    existingForm.Status = form.Status;
                    existingForm.BpmStatus = form.BpmStatus;
                    existingForm.FormData = form.FormData;
                    existingForm.CurrentApproverId = form.CurrentApproverId;
                    existingForm.CurrentApproverName = form.CurrentApproverName;
                    existingForm.ApprovalComment = form.ApprovalComment;
                    existingForm.LastSyncTime = DateTime.Now;
                    existingForm.UpdatedAt = DateTime.Now;

                    return await UpdateFormAsync(existingForm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立或更新表單失敗: {FormId}", form.FormId);
                throw;
            }
        }

        #endregion

        #region 取消表單

        /// <summary>
        /// 取消表單
        /// </summary>
        public async Task<FormCancelResult> CancelFormAsync(FormCancelRequest request)
        {
            try
            {
                var form = await GetFormByIdAsync(request.FormId);

                if (form == null)
                {
                    return new FormCancelResult
                    {
                        Success = false,
                        Message = "找不到表單",
                        FormId = request.FormId,
                        ErrorCode = "FORM_NOT_FOUND"
                    };
                }

                if (form.IsCancelled)
                {
                    return new FormCancelResult
                    {
                        Success = false,
                        Message = "表單已被取消",
                        FormId = request.FormId,
                        ErrorCode = "ALREADY_CANCELLED"
                    };
                }

                // 更新取消狀態
                form.IsCancelled = true;
                form.CancelReason = request.CancelReason;
                form.CancelTime = DateTime.Now;
                form.CancelledBy = request.OperatorId;
                form.Status = "CANCELLED";
                form.UpdatedAt = DateTime.Now;

                // 1. 更新本地資料庫
                await _context.SaveChangesAsync();
                _logger.LogInformation("取消表單成功 (本地): {FormId}, 操作人: {OperatorId}", request.FormId, request.OperatorId);

                // 2. 同步更新遠端資料庫
                try
                {
                    var remoteForm = await _remoteContext.BpmForms
                        .FirstOrDefaultAsync(f => f.FormId == request.FormId);

                    if (remoteForm != null)
                    {
                        remoteForm.IsCancelled = true;
                        remoteForm.CancelReason = request.CancelReason;
                        remoteForm.CancelTime = DateTime.Now;
                        remoteForm.CancelledBy = request.OperatorId;
                        remoteForm.Status = "CANCELLED";
                        remoteForm.UpdatedAt = DateTime.Now;

                        await _remoteContext.SaveChangesAsync();
                        _logger.LogInformation("✅ 成功同步取消表單到遠端資料庫 (54.46.24.34): {FormId}", request.FormId);
                    }
                }
                catch (Exception remoteEx)
                {
                    _logger.LogWarning(remoteEx, "⚠️  同步取消表單到遠端資料庫失敗，但本地已保存: {FormId}", request.FormId);
                }

                return new FormCancelResult
                {
                    Success = true,
                    Message = "表單取消成功",
                    FormId = request.FormId,
                    SyncedToBpm = false // 同步至 BPM 由上層服務處理
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消表單失敗: {FormId}", request.FormId);
                return new FormCancelResult
                {
                    Success = false,
                    Message = $"取消表單失敗: {ex.Message}",
                    FormId = request.FormId,
                    ErrorCode = "CANCEL_FAILED"
                };
            }
        }

        #endregion

        #region 同步日誌

        /// <summary>
        /// 記錄同步日誌
        /// </summary>
        public async Task LogSyncAsync(BpmFormSyncLog log)
        {
            try
            {
                log.SyncTime = DateTime.Now;
                _context.BpmFormSyncLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄同步日誌失敗: {FormId}", log.FormId);
                // 不拋出異常，避免影響主要流程
            }
        }

        /// <summary>
        /// 取得同步日誌
        /// </summary>
        public async Task<List<BpmFormSyncLog>> GetSyncLogsAsync(string formId, int limit = 10)
        {
            try
            {
                return await _context.BpmFormSyncLogs
                    .Where(l => l.FormId == formId)
                    .OrderByDescending(l => l.SyncTime)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得同步日誌失敗: {FormId}", formId);
                return new List<BpmFormSyncLog>();
            }
        }

        #endregion

        #region 簽核歷程

        /// <summary>
        /// 新增簽核歷程
        /// </summary>
        public async Task AddApprovalHistoryAsync(BpmFormApprovalHistory history)
        {
            try
            {
                history.CreatedAt = DateTime.Now;
                _context.BpmFormApprovalHistory.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增簽核歷程失敗: {FormId}", history.FormId);
                throw;
            }
        }

        /// <summary>
        /// 取得簽核歷程
        /// </summary>
        public async Task<List<BpmFormApprovalHistory>> GetApprovalHistoryAsync(string formId)
        {
            try
            {
                return await _context.BpmFormApprovalHistory
                    .Where(h => h.FormId == formId)
                    .OrderBy(h => h.SequenceNo)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得簽核歷程失敗: {FormId}", formId);
                return new List<BpmFormApprovalHistory>();
            }
        }

        #endregion

        #region 請假單特定操作

        /// <summary>
        /// 取得請假單詳細資料
        /// </summary>
        public async Task<BpmLeaveForm?> GetLeaveFormDetailAsync(string formId)
        {
            try
            {
                return await _context.BpmLeaveForms
                    .Include(l => l.BpmForm)
                    .FirstOrDefaultAsync(l => l.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得請假單詳細資料失敗: {FormId}", formId);
                throw;
            }
        }

        /// <summary>
        /// 取得可銷假的請假單列表
        /// </summary>
        public async Task<List<BpmForm>> GetCancellableLeaveForms(string applicantId)
        {
            try
            {
                var today = DateTime.Today;

                return await _context.BpmForms
                    .Include(f => f.LeaveForm)
                    .Where(f => f.ApplicantId == applicantId
                             && f.FormType == "LEAVE"
                             && f.Status == "APPROVED"
                             && !f.IsCancelled
                             && f.LeaveForm != null
                             && f.LeaveForm.StartDate >= today)
                    .OrderByDescending(f => f.ApplyDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得可銷假請假單失敗: {ApplicantId}", applicantId);
                throw;
            }
        }

        #endregion

        #region 加班單特定操作

        /// <summary>
        /// 取得加班單詳細資料
        /// </summary>
        public async Task<BpmOvertimeForm?> GetOvertimeFormDetailAsync(string formId)
        {
            try
            {
                return await _context.BpmOvertimeForms
                    .Include(o => o.BpmForm)
                    .FirstOrDefaultAsync(o => o.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得加班單詳細資料失敗: {FormId}", formId);
                throw;
            }
        }

        #endregion

        #region 出差單特定操作

        /// <summary>
        /// 取得出差單詳細資料
        /// </summary>
        public async Task<BpmBusinessTripForm?> GetBusinessTripFormDetailAsync(string formId)
        {
            try
            {
                return await _context.BpmBusinessTripForms
                    .Include(b => b.BpmForm)
                    .FirstOrDefaultAsync(b => b.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得出差單詳細資料失敗: {FormId}", formId);
                throw;
            }
        }

        #endregion

        #region 銷假單特定操作

        /// <summary>
        /// 取得銷假單詳細資料
        /// </summary>
        public async Task<BpmCancelLeaveForm?> GetCancelLeaveFormDetailAsync(string formId)
        {
            try
            {
                return await _context.BpmCancelLeaveForms
                    .Include(c => c.BpmForm)
                    .FirstOrDefaultAsync(c => c.FormId == formId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得銷假單詳細資料失敗: {FormId}", formId);
                throw;
            }
        }

        #endregion
    }
}
