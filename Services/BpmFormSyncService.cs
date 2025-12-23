using HRSystemAPI.Models;
using System.Text.Json;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 表單同步服務介面
    /// 負責本地 DB 與 BPM 中間件之間的資料同步
    /// </summary>
    public interface IBpmFormSyncService
    {
        /// <summary>
        /// 從 BPM 同步表單到本地 DB
        /// 若本地不存在則建立，若存在則更新
        /// </summary>
        Task<FormSyncResult> SyncFormFromBpmAsync(string formId, string? formType = null, string? operatorId = null);

        /// <summary>
        /// 取消表單 (APP端操作)
        /// 先更新本地 DB，再同步至 BPM
        /// </summary>
        Task<FormCancelResult> CancelFormAsync(FormCancelRequest request);

        /// <summary>
        /// 批次同步多個表單
        /// </summary>
        Task<List<FormSyncResult>> BatchSyncFormsAsync(List<FormSyncRequest> requests);

        /// <summary>
        /// 確保表單存在於本地 DB
        /// 若不存在則從 BPM 拉取
        /// </summary>
        Task<BpmForm?> EnsureFormExistsAsync(string formId, string? formType = null);

        /// <summary>
        /// 取得表單（優先從本地 DB，若不存在則從 BPM 同步）
        /// </summary>
        Task<BpmForm?> GetFormWithSyncAsync(string formId, string? formType = null);

        /// <summary>
        /// 儲存新表單到本地 DB
        /// </summary>
        Task<FormSyncResult> SaveFormToLocalAsync(BpmForm form);

        /// <summary>
        /// 更新表單狀態
        /// </summary>
        Task<FormSyncResult> UpdateFormStatusAsync(string formId, string status, string? comment = null);
    }

    /// <summary>
    /// BPM 表單同步服務實作
    /// </summary>
    public class BpmFormSyncService : IBpmFormSyncService
    {
        private readonly IBpmFormRepository _repository;
        private readonly BpmService _bpmService;
        private readonly ILogger<BpmFormSyncService> _logger;
        private readonly IConfiguration _configuration;

        // 表單代碼常數
        private const string LEAVE_FORM_CODE = "PI_LEAVE_001";
        private const string OVERTIME_FORM_CODE = "PI_OVERTIME_001";
        private const string BUSINESS_TRIP_FORM_CODE = "PI_BUSINESS_TRIP_001";
        private const string CANCEL_LEAVE_FORM_CODE = "PI_CANCEL_LEAVE_001";

        public BpmFormSyncService(
            IBpmFormRepository repository,
            BpmService bpmService,
            ILogger<BpmFormSyncService> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _bpmService = bpmService;
            _logger = logger;
            _configuration = configuration;
        }

        #region 從 BPM 同步表單

        /// <summary>
        /// 從 BPM 同步表單到本地 DB
        /// </summary>
        public async Task<FormSyncResult> SyncFormFromBpmAsync(string formId, string? formType = null, string? operatorId = null)
        {
            _logger.LogInformation("開始同步表單: {FormId}, 類型: {FormType}", formId, formType);

            try
            {
                // 1. 檢查本地是否已存在
                var existingForm = await _repository.GetFormByIdAsync(formId);
                bool isNewForm = existingForm == null;

                // 2. 從 BPM 取得表單資料
                var bpmFormData = await FetchFormFromBpmAsync(formId, formType);

                if (bpmFormData == null)
                {
                    _logger.LogWarning("無法從 BPM 取得表單: {FormId}", formId);

                    // 記錄同步失敗
                    await LogSyncOperation(formId, SyncType.FETCH, SyncDirection.IN, SyncStatus.FAILED,
                        null, null, "無法從 BPM 取得表單資料", operatorId);

                    return new FormSyncResult
                    {
                        Success = false,
                        Message = "無法從 BPM 取得表單資料",
                        FormId = formId,
                        ErrorCode = "BPM_FETCH_FAILED"
                    };
                }

                // 3. 建立或更新本地表單
                BpmForm syncedForm;
                if (isNewForm)
                {
                    syncedForm = await _repository.CreateFormAsync(bpmFormData);
                    _logger.LogInformation("建立新表單: {FormId}", formId);
                }
                else
                {
                    // 更新現有表單 (保留本地取消狀態)
                    bpmFormData.Id = existingForm!.Id;
                    bpmFormData.IsCancelled = existingForm.IsCancelled;
                    bpmFormData.CancelReason = existingForm.CancelReason;
                    bpmFormData.CancelTime = existingForm.CancelTime;
                    bpmFormData.CancelledBy = existingForm.CancelledBy;
                    bpmFormData.CreatedAt = existingForm.CreatedAt;

                    syncedForm = await _repository.UpdateFormAsync(bpmFormData);
                    _logger.LogInformation("更新表單: {FormId}", formId);
                }

                // 4. 同步詳細資料
                await SyncFormDetailsAsync(syncedForm, formType);

                // 5. 記錄同步成功
                await LogSyncOperation(formId, SyncType.FETCH, SyncDirection.IN, SyncStatus.SUCCESS,
                    null, JsonSerializer.Serialize(syncedForm), null, operatorId);

                return new FormSyncResult
                {
                    Success = true,
                    Message = isNewForm ? "表單同步成功（新建）" : "表單同步成功（更新）",
                    FormId = formId,
                    Form = syncedForm,
                    IsNewForm = isNewForm,
                    IsUpdated = !isNewForm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步表單失敗: {FormId}", formId);

                // 記錄同步失敗
                await LogSyncOperation(formId, SyncType.FETCH, SyncDirection.IN, SyncStatus.FAILED,
                    null, null, ex.Message, operatorId);

                return new FormSyncResult
                {
                    Success = false,
                    Message = $"同步表單失敗: {ex.Message}",
                    FormId = formId,
                    ErrorCode = "SYNC_FAILED"
                };
            }
        }

        /// <summary>
        /// 從 BPM 取得表單資料
        /// </summary>
        private async Task<BpmForm?> FetchFormFromBpmAsync(string formId, string? formType)
        {
            try
            {
                // 嘗試透過 BPM API 取得表單資料
                var endpoint = $"bpm/forms/{formId}";
                var responseJson = await _bpmService.GetAsync(endpoint);

                _logger.LogDebug("BPM 表單查詢回應: {Response}", responseJson);

                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // 解析 BPM 回應並轉換為本地表單
                return ParseBpmFormResponse(bpmResponse, formId, formType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "從 BPM 取得表單失敗: {FormId}，嘗試其他方式", formId);

                // 嘗試透過流程實例查詢
                try
                {
                    var processCode = DetermineProcessCode(formType ?? ParseFormTypeFromId(formId));
                    var endpoint = $"bpm/process-instances?processSerialNo={formId}&processCode={processCode}";
                    var responseJson = await _bpmService.GetAsync(endpoint);

                    var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    return ParseBpmProcessInstanceResponse(bpmResponse, formId, formType);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "透過流程實例查詢表單也失敗: {FormId}", formId);
                    return null;
                }
            }
        }

        /// <summary>
        /// 解析 BPM 表單回應
        /// </summary>
        private BpmForm? ParseBpmFormResponse(JsonElement response, string formId, string? formType)
        {
            try
            {
                // 取得狀態
                var status = GetJsonStringValue(response, "status", "processStatus") ?? "PENDING";
                var bpmStatus = GetJsonStringValue(response, "bpmStatus", "originalStatus");

                // 取得申請人資訊
                var applicantId = GetJsonStringValue(response, "userId", "applicantId", "employeeNo") ?? "";
                var applicantName = GetJsonStringValue(response, "userName", "applicantName", "employeeName");
                var applicantDepartment = GetJsonStringValue(response, "departmentName", "applicantDepartment");
                var companyId = GetJsonStringValue(response, "companyId", "companyCode");

                // 取得表單代碼
                var formCode = GetJsonStringValue(response, "formCode", "processCode") ?? DetermineFormCode(formType);
                var determinedFormType = formType ?? DetermineFormType(formCode);

                // 取得表單資料
                string? formData = null;
                if (response.TryGetProperty("formData", out var formDataElement))
                {
                    formData = formDataElement.ToString();
                }
                else if (response.TryGetProperty("data", out var dataElement))
                {
                    formData = dataElement.ToString();
                }

                // 取得申請日期
                DateTime? applyDate = null;
                var applyDateStr = GetJsonStringValue(response, "applyDate", "createDate", "submitDate");
                if (!string.IsNullOrEmpty(applyDateStr) && DateTime.TryParse(applyDateStr, out var parsedDate))
                {
                    applyDate = parsedDate;
                }

                return new BpmForm
                {
                    FormId = formId,
                    FormCode = formCode,
                    FormType = determinedFormType,
                    ApplicantId = applicantId,
                    ApplicantName = applicantName,
                    ApplicantDepartment = applicantDepartment,
                    CompanyId = companyId,
                    Status = MapBpmStatusToLocal(status),
                    BpmStatus = bpmStatus ?? status,
                    FormData = formData,
                    ApplyDate = applyDate ?? DateTime.Now,
                    LastSyncTime = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 BPM 表單回應失敗");
                return null;
            }
        }

        /// <summary>
        /// 解析 BPM 流程實例回應
        /// </summary>
        private BpmForm? ParseBpmProcessInstanceResponse(JsonElement response, string formId, string? formType)
        {
            try
            {
                // 查找對應的流程實例
                JsonElement? processInstance = null;

                if (response.TryGetProperty("processInstances", out var instances) && instances.ValueKind == JsonValueKind.Array)
                {
                    foreach (var instance in instances.EnumerateArray())
                    {
                        var serialNo = GetJsonStringValue(instance, "processSerialNo", "serialNumber");
                        if (serialNo == formId)
                        {
                            processInstance = instance;
                            break;
                        }
                    }
                }
                else if (response.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var instance in data.EnumerateArray())
                    {
                        var serialNo = GetJsonStringValue(instance, "processSerialNo", "serialNumber");
                        if (serialNo == formId)
                        {
                            processInstance = instance;
                            break;
                        }
                    }
                }

                if (processInstance == null)
                {
                    return null;
                }

                return ParseBpmFormResponse(processInstance.Value, formId, formType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 BPM 流程實例回應失敗");
                return null;
            }
        }

        /// <summary>
        /// 同步表單詳細資料
        /// </summary>
        private async Task SyncFormDetailsAsync(BpmForm form, string? formType)
        {
            try
            {
                var type = formType ?? form.FormType;

                switch (type.ToUpperInvariant())
                {
                    case "LEAVE":
                        await SyncLeaveFormDetailsAsync(form);
                        break;
                    case "OVERTIME":
                        await SyncOvertimeFormDetailsAsync(form);
                        break;
                    case "BUSINESS_TRIP":
                        await SyncBusinessTripFormDetailsAsync(form);
                        break;
                    case "CANCEL_LEAVE":
                        await SyncCancelLeaveFormDetailsAsync(form);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步表單詳細資料失敗: {FormId}", form.FormId);
                // 不拋出異常，詳細資料同步失敗不影響主表單
            }
        }

        private async Task SyncLeaveFormDetailsAsync(BpmForm form)
        {
            if (string.IsNullOrEmpty(form.FormData)) return;

            try
            {
                var formData = JsonSerializer.Deserialize<JsonElement>(form.FormData);
                
                // 檢查是否已有詳細資料
                var existing = await _repository.GetLeaveFormDetailAsync(form.FormId);

                var leaveForm = existing ?? new BpmLeaveForm { FormId = form.FormId };

                // 解析請假資料
                leaveForm.LeaveTypeCode = GetJsonStringValue(formData, "leaveTypeId", "leaveType", "leaveTypeCode");
                leaveForm.LeaveTypeName = GetJsonStringValue(formData, "leaveTypeName");
                
                var startDateStr = GetJsonStringValue(formData, "startDate");
                if (DateTime.TryParse(startDateStr, out var startDate))
                    leaveForm.StartDate = startDate;

                var endDateStr = GetJsonStringValue(formData, "endDate");
                if (DateTime.TryParse(endDateStr, out var endDate))
                    leaveForm.EndDate = endDate;

                var startTimeStr = GetJsonStringValue(formData, "startTime");
                if (TimeSpan.TryParse(startTimeStr, out var startTime))
                    leaveForm.StartTime = startTime;

                var endTimeStr = GetJsonStringValue(formData, "endTime");
                if (TimeSpan.TryParse(endTimeStr, out var endTime))
                    leaveForm.EndTime = endTime;

                leaveForm.Reason = GetJsonStringValue(formData, "reason");
                leaveForm.AgentId = GetJsonStringValue(formData, "agentNo", "agentId");
                leaveForm.AgentName = GetJsonStringValue(formData, "agentName");

                leaveForm.UpdatedAt = DateTime.Now;

                // 儲存
                // Note: 這裡需要透過 DbContext 直接操作，因為 Repository 目前沒有提供更新詳細資料的方法
                _logger.LogDebug("同步請假單詳細資料: {FormId}", form.FormId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步請假單詳細資料失敗: {FormId}", form.FormId);
            }
        }

        private async Task SyncOvertimeFormDetailsAsync(BpmForm form)
        {
            // 類似 SyncLeaveFormDetailsAsync 的實作
            _logger.LogDebug("同步加班單詳細資料: {FormId}", form.FormId);
            await Task.CompletedTask;
        }

        private async Task SyncBusinessTripFormDetailsAsync(BpmForm form)
        {
            // 類似 SyncLeaveFormDetailsAsync 的實作
            _logger.LogDebug("同步出差單詳細資料: {FormId}", form.FormId);
            await Task.CompletedTask;
        }

        private async Task SyncCancelLeaveFormDetailsAsync(BpmForm form)
        {
            // 類似 SyncLeaveFormDetailsAsync 的實作
            _logger.LogDebug("同步銷假單詳細資料: {FormId}", form.FormId);
            await Task.CompletedTask;
        }

        #endregion

        #region 取消表單

        /// <summary>
        /// 取消表單
        /// </summary>
        public async Task<FormCancelResult> CancelFormAsync(FormCancelRequest request)
        {
            _logger.LogInformation("開始取消表單: {FormId}, 操作人: {OperatorId}", request.FormId, request.OperatorId);

            try
            {
                // 1. 確保表單存在於本地 DB
                var form = await EnsureFormExistsAsync(request.FormId);

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

                // 2. 更新本地 DB 的取消狀態
                var localCancelResult = await _repository.CancelFormAsync(request);

                if (!localCancelResult.Success)
                {
                    return localCancelResult;
                }

                // 3. 同步至 BPM
                bool syncedToBpm = false;
                if (request.SyncToBpm)
                {
                    try
                    {
                        syncedToBpm = await SyncCancelToBpmAsync(request);
                        
                        // 更新同步狀態
                        form.IsSyncedToBpm = syncedToBpm;
                        if (!syncedToBpm)
                        {
                            form.SyncErrorMessage = "同步至 BPM 失敗";
                        }
                        await _repository.UpdateFormAsync(form);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "同步取消至 BPM 失敗: {FormId}", request.FormId);
                        form.SyncErrorMessage = ex.Message;
                        await _repository.UpdateFormAsync(form);
                    }
                }

                // 4. 記錄同步日誌
                await LogSyncOperation(request.FormId, SyncType.CANCEL, SyncDirection.OUT,
                    syncedToBpm ? SyncStatus.SUCCESS : SyncStatus.PARTIAL,
                    JsonSerializer.Serialize(request), null,
                    syncedToBpm ? null : "BPM 同步失敗",
                    request.OperatorId);

                return new FormCancelResult
                {
                    Success = true,
                    Message = syncedToBpm ? "表單取消成功（已同步至 BPM）" : "表單取消成功（BPM 同步待處理）",
                    FormId = request.FormId,
                    SyncedToBpm = syncedToBpm
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

        /// <summary>
        /// 同步取消操作至 BPM
        /// </summary>
        private async Task<bool> SyncCancelToBpmAsync(FormCancelRequest request)
        {
            try
            {
                // 呼叫 BPM 取消 API
                var abortRequest = new
                {
                    items = new[]
                    {
                        new
                        {
                            processInstanceSerialNo = request.FormId,
                            userId = request.OperatorId,
                            abortComment = request.CancelReason,
                            environment = "TEST"
                        }
                    }
                };

                var response = await _bpmService.PostAsync("bpm/batch/abort-processes", abortRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                // 檢查回應
                if (jsonResponse.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    var firstResult = results.EnumerateArray().FirstOrDefault();
                    if (firstResult.ValueKind != JsonValueKind.Undefined)
                    {
                        if (firstResult.TryGetProperty("success", out var success))
                        {
                            return success.GetBoolean();
                        }
                    }
                }

                if (jsonResponse.TryGetProperty("status", out var status))
                {
                    return status.GetString()?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步取消至 BPM 失敗: {FormId}", request.FormId);
                return false;
            }
        }

        #endregion

        #region 批次同步

        /// <summary>
        /// 批次同步多個表單
        /// </summary>
        public async Task<List<FormSyncResult>> BatchSyncFormsAsync(List<FormSyncRequest> requests)
        {
            var results = new List<FormSyncResult>();

            foreach (var request in requests)
            {
                var result = await SyncFormFromBpmAsync(request.FormId, request.FormType, request.OperatorId);
                results.Add(result);
            }

            return results;
        }

        #endregion

        #region 確保表單存在

        /// <summary>
        /// 確保表單存在於本地 DB
        /// </summary>
        public async Task<BpmForm?> EnsureFormExistsAsync(string formId, string? formType = null)
        {
            // 先檢查本地
            var localForm = await _repository.GetFormByIdAsync(formId);

            if (localForm != null)
            {
                return localForm;
            }

            // 本地不存在，從 BPM 同步
            var syncResult = await SyncFormFromBpmAsync(formId, formType);

            return syncResult.Success ? syncResult.Form : null;
        }

        /// <summary>
        /// 取得表單（優先從本地 DB，若不存在則從 BPM 同步）
        /// </summary>
        public async Task<BpmForm?> GetFormWithSyncAsync(string formId, string? formType = null)
        {
            return await EnsureFormExistsAsync(formId, formType);
        }

        #endregion

        #region 儲存表單

        /// <summary>
        /// 儲存新表單到本地 DB
        /// </summary>
        public async Task<FormSyncResult> SaveFormToLocalAsync(BpmForm form)
        {
            try
            {
                var savedForm = await _repository.CreateOrUpdateFormAsync(form);

                await LogSyncOperation(form.FormId, SyncType.PUSH, SyncDirection.IN, SyncStatus.SUCCESS,
                    JsonSerializer.Serialize(form), null, null, form.ApplicantId);

                return new FormSyncResult
                {
                    Success = true,
                    Message = "表單儲存成功",
                    FormId = form.FormId,
                    Form = savedForm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存表單失敗: {FormId}", form.FormId);

                return new FormSyncResult
                {
                    Success = false,
                    Message = $"儲存表單失敗: {ex.Message}",
                    FormId = form.FormId,
                    ErrorCode = "SAVE_FAILED"
                };
            }
        }

        /// <summary>
        /// 更新表單狀態
        /// </summary>
        public async Task<FormSyncResult> UpdateFormStatusAsync(string formId, string status, string? comment = null)
        {
            try
            {
                var form = await _repository.GetFormByIdAsync(formId);

                if (form == null)
                {
                    return new FormSyncResult
                    {
                        Success = false,
                        Message = "找不到表單",
                        FormId = formId,
                        ErrorCode = "FORM_NOT_FOUND"
                    };
                }

                form.Status = status;
                form.ApprovalComment = comment;
                form.UpdatedAt = DateTime.Now;

                var updatedForm = await _repository.UpdateFormAsync(form);

                return new FormSyncResult
                {
                    Success = true,
                    Message = "表單狀態更新成功",
                    FormId = formId,
                    Form = updatedForm,
                    IsUpdated = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表單狀態失敗: {FormId}", formId);

                return new FormSyncResult
                {
                    Success = false,
                    Message = $"更新表單狀態失敗: {ex.Message}",
                    FormId = formId,
                    ErrorCode = "UPDATE_FAILED"
                };
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 記錄同步操作
        /// </summary>
        private async Task LogSyncOperation(string? formId, SyncType syncType, SyncDirection direction,
            SyncStatus status, string? requestData, string? responseData, string? errorMessage, string? operatorId)
        {
            var log = new BpmFormSyncLog
            {
                FormId = formId,
                SyncType = syncType.ToString(),
                SyncDirection = direction.ToString(),
                SyncStatus = status.ToString(),
                RequestData = requestData,
                ResponseData = responseData,
                ErrorMessage = errorMessage,
                OperatorId = operatorId
            };

            await _repository.LogSyncAsync(log);
        }

        /// <summary>
        /// 從 JSON 取得字串值
        /// </summary>
        private string? GetJsonStringValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }
            return null;
        }

        /// <summary>
        /// 從表單 ID 解析表單類型
        /// </summary>
        private string ParseFormTypeFromId(string formId)
        {
            if (formId.Contains("LEAVE", StringComparison.OrdinalIgnoreCase))
                return "LEAVE";
            if (formId.Contains("OVERTIME", StringComparison.OrdinalIgnoreCase))
                return "OVERTIME";
            if (formId.Contains("BUSINESS_TRIP", StringComparison.OrdinalIgnoreCase) ||
                formId.Contains("TRIP", StringComparison.OrdinalIgnoreCase))
                return "BUSINESS_TRIP";
            if (formId.Contains("CANCEL", StringComparison.OrdinalIgnoreCase))
                return "CANCEL_LEAVE";
            return "OTHER";
        }

        /// <summary>
        /// 根據表單類型取得流程代碼
        /// </summary>
        private string DetermineProcessCode(string formType)
        {
            return formType.ToUpperInvariant() switch
            {
                "LEAVE" => $"{LEAVE_FORM_CODE}_PROCESS",
                "OVERTIME" => $"{OVERTIME_FORM_CODE}_PROCESS",
                "BUSINESS_TRIP" => $"{BUSINESS_TRIP_FORM_CODE}_PROCESS",
                "CANCEL_LEAVE" => $"{CANCEL_LEAVE_FORM_CODE}_PROCESS",
                _ => $"{LEAVE_FORM_CODE}_PROCESS"
            };
        }

        /// <summary>
        /// 根據表單類型取得表單代碼
        /// </summary>
        private string DetermineFormCode(string? formType)
        {
            return formType?.ToUpperInvariant() switch
            {
                "LEAVE" => LEAVE_FORM_CODE,
                "OVERTIME" => OVERTIME_FORM_CODE,
                "BUSINESS_TRIP" => BUSINESS_TRIP_FORM_CODE,
                "CANCEL_LEAVE" => CANCEL_LEAVE_FORM_CODE,
                _ => LEAVE_FORM_CODE
            };
        }

        /// <summary>
        /// 根據表單代碼判斷表單類型
        /// </summary>
        private string DetermineFormType(string formCode)
        {
            if (formCode.Contains("LEAVE", StringComparison.OrdinalIgnoreCase))
            {
                if (formCode.Contains("CANCEL", StringComparison.OrdinalIgnoreCase))
                    return "CANCEL_LEAVE";
                return "LEAVE";
            }
            if (formCode.Contains("OVERTIME", StringComparison.OrdinalIgnoreCase))
                return "OVERTIME";
            if (formCode.Contains("BUSINESS_TRIP", StringComparison.OrdinalIgnoreCase) ||
                formCode.Contains("TRIP", StringComparison.OrdinalIgnoreCase))
                return "BUSINESS_TRIP";
            return "OTHER";
        }

        /// <summary>
        /// 將 BPM 狀態映射到本地狀態
        /// </summary>
        private string MapBpmStatusToLocal(string bpmStatus)
        {
            return bpmStatus.ToUpperInvariant() switch
            {
                "ACTIVE" => "PENDING",
                "RUNNING" => "PROCESSING",
                "COMPLETED" => "APPROVED",
                "APPROVED" => "APPROVED",
                "REJECTED" => "REJECTED",
                "TERMINATED" => "CANCELLED",
                "ABORTED" => "WITHDRAWN",
                "CANCELLED" => "CANCELLED",
                _ => "PENDING"
            };
        }

        #endregion
    }
}
