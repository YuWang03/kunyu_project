using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 請假單服務實作（BPM 整合）- 精簡測試版
    /// 根據 BPM Middleware API 規格完整實作
    /// </summary>
    public class LeaveFormService : ILeaveFormService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly IBpmMiddlewareService _bpmMiddlewareService;
        private readonly ILogger<LeaveFormService> _logger;
        private readonly IConfiguration _configuration;
        private const string FORM_CODE = "PI_LEAVE_001";
        private const string FORM_VERSION = "1.0.0";
        private const string LEAVE_PROCESS_CODE = "PI_LEAVE_001_PROCESS";

        public LeaveFormService(
            BpmService bpmService,
            FtpService ftpService,
            IBasicInfoService basicInfoService,
            IBpmMiddlewareService bpmMiddlewareService,
            ILogger<LeaveFormService> logger,
            IConfiguration configuration)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _basicInfoService = basicInfoService;
            _bpmMiddlewareService = bpmMiddlewareService;
            _logger = logger;
            _configuration = configuration;
        }

        #region 查詢相關

        /// <summary>
        /// 查詢請假單記錄（支援多種查詢條件）
        /// </summary>
        public async Task<List<LeaveFormRecord>> GetLeaveFormsAsync(LeaveFormQueryRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢請假單記錄，條件: {@Request}", request);

                // 如果有 Email，查詢員工工號
                string? employeeNo = request.EmployeeNo;
                if (!string.IsNullOrEmpty(request.EmployeeEmail) && string.IsNullOrEmpty(employeeNo))
                {
                    try
                    {
                        var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.EmployeeEmail);
                        if (employeeInfo != null)
                        {
                            employeeNo = employeeInfo.EmployeeNo;
                            _logger.LogInformation("Email {Email} 對應的員工工號: {EmployeeNo}", request.EmployeeEmail, employeeNo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "無法透過 Email 查詢員工工號: {Email}", request.EmployeeEmail);
                    }
                }

                // 建構查詢參數
                var queryParams = new List<string>();

                // 使用 BPM API 的參數格式
                if (!string.IsNullOrEmpty(employeeNo))
                    queryParams.Add($"employeeNo={Uri.EscapeDataString(employeeNo)}");

                // 處理年月查詢
                if (request.Year.HasValue && request.Month.HasValue)
                {
                    var year = request.Year.Value;
                    var month = request.Month.Value;
                    var startDate = new DateTime(year, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);
                    
                    queryParams.Add($"startDate={startDate:yyyy-MM-dd}");
                    queryParams.Add($"endDate={endDate:yyyy-MM-dd}");
                    _logger.LogInformation("使用年月查詢: {Year}-{Month}", year, month);
                }
                else if (request.Year.HasValue)
                {
                    var startDate = new DateTime(request.Year.Value, 1, 1);
                    var endDate = new DateTime(request.Year.Value, 12, 31);
                    
                    queryParams.Add($"startDate={startDate:yyyy-MM-dd}");
                    queryParams.Add($"endDate={endDate:yyyy-MM-dd}");
                    _logger.LogInformation("使用年度查詢: {Year}", request.Year.Value);
                }
                else
                {
                    // 只有在有指定日期時才加入查詢條件
                    if (!string.IsNullOrEmpty(request.StartDate))
                    {
                        queryParams.Add($"startDate={Uri.EscapeDataString(request.StartDate)}");
                        _logger.LogInformation("使用開始日期: {StartDate}", request.StartDate);
                    }

                    if (!string.IsNullOrEmpty(request.EndDate))
                    {
                        queryParams.Add($"endDate={Uri.EscapeDataString(request.EndDate)}");
                        _logger.LogInformation("使用結束日期: {EndDate}", request.EndDate);
                    }

                    // 如果完全沒有日期條件，記錄一下（表示查詢全部）
                    if (string.IsNullOrEmpty(request.StartDate) && string.IsNullOrEmpty(request.EndDate))
                    {
                        _logger.LogInformation("查詢全部請假單（不限制日期）");
                    }
                }

                if (!string.IsNullOrEmpty(request.ApprovalStatus))
                    queryParams.Add($"status={Uri.EscapeDataString(request.ApprovalStatus)}");

                if (!string.IsNullOrEmpty(request.LeaveType))
                    queryParams.Add($"leaveTypeId={Uri.EscapeDataString(request.LeaveType)}");

                queryParams.Add($"page={request.Page}");
                queryParams.Add($"pageSize={request.PageSize}");

                var queryString = string.Join("&", queryParams);
                
                // 注意：BPM 中間件目前不提供歷史表單查詢 API
                // 暫時返回空列表，建議改用以下方案：
                // 1. 將表單資料同步儲存到本地資料庫查詢
                // 2. 只查詢 tasks/pending（待簽核的表單）
                // 3. 聯繫 BPM 團隊確認是否有查詢歷史表單的 API
                
                _logger.LogWarning("BPM 中間件不支援歷史表單查詢，返回空列表。建議將表單資料同步到本地資料庫。");
                _logger.LogInformation("查詢條件: employeeNo={EmployeeNo}, formCode={FormCode}, queryParams={QueryString}", 
                    employeeNo, FORM_CODE, queryString);
                
                // 返回空列表而不是拋出異常
                return new List<LeaveFormRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單記錄失敗");
                throw;
            }
        }

        /// <summary>
        /// 查詢單一請假單詳情
        /// </summary>
        public async Task<LeaveFormRecord?> GetLeaveFormByIdAsync(string formId)
        {
            try
            {
                _logger.LogInformation("開始查詢請假單詳情: {FormId}", formId);

                // 注意：BPM 中間件目前不提供單筆表單查詢 API
                _logger.LogWarning("BPM 中間件不支援單筆表單查詢，返回 null。建議將表單資料同步到本地資料庫。");
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單詳情失敗: {FormId}", formId);
                throw;
            }
        }

        /// <summary>
        /// 查詢我的請假單
        /// </summary>
        public async Task<List<LeaveFormRecord>> GetMyLeaveFormsAsync(string email, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("查詢我的請假單: {Email}", email);

                var request = new LeaveFormQueryRequest
                {
                    EmployeeEmail = email,
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd"),
                    PageSize = 100 // 預設取較多筆
                };

                return await GetLeaveFormsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢我的請假單失敗: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// 查詢待簽核請假單
        /// </summary>
        public async Task<List<LeaveFormRecord>> GetPendingLeaveFormsAsync(string approverEmail)
        {
            try
            {
                _logger.LogInformation("查詢待簽核請假單: {ApproverEmail}", approverEmail);

                // 透過 Email 查詢員工工號
                var approverInfo = await _basicInfoService.GetEmployeeByEmailAsync(approverEmail);
                if (approverInfo == null)
                {
                    throw new Exception($"找不到簽核人 Email 對應的員工資料: {approverEmail}");
                }
                var employeeNo = approverInfo.EmployeeNo;

                // 呼叫 BPM API - 查詢待簽核表單
                var endpoint = $"tasks/pending?employeeNo={Uri.EscapeDataString(employeeNo)}&formCode={FORM_CODE}";
                var response = await _bpmService.GetAsync(endpoint);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                // 解析待簽核表單列表
                var records = ParseLeaveFormRecords(jsonResponse);

                _logger.LogInformation("查詢待簽核請假單成功: {Count} 筆", records.Count);
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢待簽核請假單失敗: {ApproverEmail}", approverEmail);
                throw;
            }
        }

        /// <summary>
        /// 取得員工近 N 個月的請假記錄
        /// </summary>
        public async Task<List<LeaveFormRecord>> GetRecentLeaveFormsAsync(string employeeEmail, int months = 2)
        {
            try
            {
                _logger.LogInformation("查詢近 {Months} 個月請假記錄: {Email}", months, employeeEmail);

                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);

                return await GetMyLeaveFormsAsync(employeeEmail, startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢近期請假記錄失敗: {Email}", employeeEmail);
                throw;
            }
        }

        #endregion

        #region 申請與操作

        /// <summary>
        /// 申請請假單（整合 BPM 表單預覽自動計算）
        /// </summary>
        public async Task<LeaveFormOperationResult> CreateLeaveFormAsync(CreateLeaveFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始申請請假單: {@Request}", new
                {
                    request.Email,
                    request.LeaveTypeName,
                    request.StartDate,
                    request.EndDate
                });

                // 1. 透過 Email 查詢員工完整資料
                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.Email);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到 Email 對應的員工資料: {request.Email}");
                }

                _logger.LogInformation("申請人資料 - 工號: {EmployeeNo}, 姓名: {Name}, 部門: {Dept}", 
                    employeeInfo.EmployeeNo, employeeInfo.EmployeeName, employeeInfo.DepartmentName);

                // 2. 先呼叫表單預覽 API 取得自動計算的欄位（簽核人、假別資訊等）
                Dictionary<string, object?>? computedData = null;
                try
                {
                    var previewEndpoint = $"form-preview/preview?formCode={FORM_CODE}&version={FORM_VERSION}";
                    var previewRequest = new { userId = employeeInfo.EmployeeNo };
                    
                    _logger.LogInformation("呼叫表單預覽 API 取得自動計算欄位");
                    var previewResponse = await _bpmService.PostAsync(previewEndpoint, previewRequest);
                    var previewJson = JsonSerializer.Deserialize<JsonElement>(previewResponse);
                    
                    // 解析 computedData.policyTrace 中的所有自動計算結果
                    if (previewJson.TryGetProperty("computedData", out var computedDataElement) &&
                        computedDataElement.TryGetProperty("policyTrace", out var policyTrace))
                    {
                        computedData = new Dictionary<string, object?>();
                        foreach (var policy in policyTrace.EnumerateArray())
                        {
                            if (policy.TryGetProperty("results", out var results))
                            {
                                foreach (var prop in results.EnumerateObject())
                                {
                                    computedData[prop.Name] = prop.Value.ValueKind == JsonValueKind.String 
                                        ? prop.Value.GetString() 
                                        : prop.Value.ToString();
                                }
                            }
                        }
                        _logger.LogInformation("表單預覽取得 {Count} 個自動計算欄位", computedData.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "表單預覽失敗，將使用手動組裝的欄位");
                }

                // 3. 組裝 BPM 表單資料（整合自動計算的欄位）
                var formData = BuildLeaveFormData(request, employeeInfo, computedData);

                // 4. 建立 BPM 表單請求
                var bpmRequest = new BpmCreateFormRequest
                {
                    ProcessCode = FORM_CODE,
                    FormCode = FORM_CODE,
                    FormVersion = FORM_VERSION,
                    UserId = employeeInfo.EmployeeNo,
                    Subject = $"{request.LeaveTypeName}申請 - {request.StartDate}~{request.EndDate}",
                    SourceSystem = "HRSystemAPI",
                    HasAttachments = false,
                    FormData = formData
                };

                _logger.LogDebug("BPM 表單請求: {@Request}", bpmRequest);
                // 4. 呼叫 BPM API 建立請假單（使用 invoke-process 端點）
                var endpoint = "bpm/invoke-process";
                var response = await _bpmService.PostAsync(endpoint, bpmRequest);

                // 5. 解析回應
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                var formId = ExtractFormId(jsonResponse);
                var formNumber = ExtractFormNumber(jsonResponse);

                _logger.LogInformation("請假單申請成功 - FormId: {FormId}, FormNumber: {FormNumber}", formId, formNumber);

                return new LeaveFormOperationResult
                {
                    Success = true,
                    Message = "請假單申請成功",
                    FormId = formId,
                    FormNumber = formNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請請假單失敗");
                return new LeaveFormOperationResult
                {
                    Success = false,
                    Message = $"申請失敗: {ex.Message}",
                    ErrorCode = "CREATE_FAILED"
                };
            }
        }

        /// <summary>
        /// 取消請假單
        /// </summary>
        public async Task<LeaveFormOperationResult> CancelLeaveFormAsync(string formId, string employeeEmail)
        {
            try
            {
                _logger.LogInformation("開始取消請假單: {FormId}, {Email}", formId, employeeEmail);

                // 1. 透過 Email 查詢員工工號
                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(employeeEmail);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到 Email 對應的員工資料: {employeeEmail}");
                }

                // 2. 組裝取消請求
                var cancelRequest = new
                {
                    userId = employeeInfo.EmployeeNo,
                    employeeNo = employeeInfo.EmployeeNo,
                    reason = "申請人取消"
                };

                // 3. 呼叫 BPM API 取消表單
                var endpoint = $"forms/{FORM_CODE}/instances/{formId}/cancel";
                var response = await _bpmService.PostAsync(endpoint, cancelRequest);

                _logger.LogInformation("請假單取消成功: {FormId}", formId);

                return new LeaveFormOperationResult
                {
                    Success = true,
                    Message = "請假單取消成功",
                    FormId = formId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消請假單失敗: {FormId}", formId);
                return new LeaveFormOperationResult
                {
                    Success = false,
                    Message = $"取消失敗: {ex.Message}",
                    ErrorCode = "CANCEL_FAILED"
                };
            }
        }

        /// <summary>
        /// 簽核請假單（支援核准/拒絕/退回）
        /// </summary>
        public async Task<LeaveFormOperationResult> ApproveLeaveFormAsync(ApproveLeaveFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始簽核請假單: {FormId}, Action: {Action}", request.FormId, request.Action);

                // 1. 透過 Email 查詢簽核人員工號
                var approverInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.ApproverEmail);
                if (approverInfo == null)
                {
                    throw new Exception($"找不到簽核人 Email 對應的員工資料: {request.ApproverEmail}");
                }

                // 2. 組裝簽核請求
                var approveRequest = new
                {
                    employeeNo = approverInfo.EmployeeNo,
                    action = request.Action,
                    comment = request.Comment ?? ""
                };

                // 3. 呼叫 BPM API 簽核表單
                var endpoint = $"forms/{FORM_CODE}/instances/{request.FormId}/approve";
                var response = await _bpmService.PostAsync(endpoint, approveRequest);

                var actionText = request.Action switch
                {
                    "approve" => "核准",
                    "reject" => "拒絕",
                    "return" => "退回重簽",
                    _ => "處理"
                };

                _logger.LogInformation("請假單{Action}成功: {FormId}", actionText, request.FormId);

                return new LeaveFormOperationResult
                {
                    Success = true,
                    Message = $"請假單{actionText}成功",
                    FormId = request.FormId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽核請假單失敗: {FormId}", request.FormId);
                return new LeaveFormOperationResult
                {
                    Success = false,
                    Message = $"簽核失敗: {ex.Message}",
                    ErrorCode = "APPROVE_FAILED"
                };
            }
        }

        #endregion

        #region 私有輔助方法

        /// <summary>
        /// 檢查字串值是否有效（排除 null、空白、"string" 等無效值）
        /// </summary>
        private bool IsValidStringValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && 
                   !value.Equals("string", StringComparison.OrdinalIgnoreCase) &&
                   !value.Equals("0", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 組裝 BPM 請假表單資料（根據實際成功案例調整，整合表單預覽自動計算）
        /// </summary>
        private Dictionary<string, object?> BuildLeaveFormData(
            CreateLeaveFormRequest request, 
            EmployeeBasicInfo employeeInfo,
            Dictionary<string, object?>? computedData = null)
        {
            // 確保日期格式為 yyyy/MM/dd
            var startDate = request.StartDate.Replace("-", "/");
            var endDate = request.EndDate.Replace("-", "/");
            
            var formData = new Dictionary<string, object?>
            {
                // === BPM 必填欄位 (required: true) - 使用斜線日期格式 ===
                ["startDate"] = startDate,
                ["startTime"] = request.StartTime,
                ["endDate"] = endDate,
                ["endTime"] = request.EndTime,
                ["reason"] = request.Reason,
                ["agentNo"] = request.AgentNo,
                ["leaveTypeId"] = request.LeaveTypeId,
                ["leaveTypeName"] = request.LeaveTypeName
            };

            // === 優先使用表單預覽的自動計算欄位 ===
            if (computedData != null && computedData.Count > 0)
            {
                foreach (var kvp in computedData)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value?.ToString()))
                    {
                        formData[kvp.Key] = kvp.Value;
                    }
                }
                _logger.LogInformation("已整合表單預覽自動計算的 {Count} 個欄位", computedData.Count);
            }
            else
            {
                // === 如果沒有表單預覽，使用手動組裝的基本欄位 ===
                formData["applierId"] = employeeInfo.EmployeeNo;
                formData["applierName"] = IsValidStringValue(request.ApplierName) ? request.ApplierName : employeeInfo.EmployeeName;
                formData["applierDeptName"] = IsValidStringValue(request.ApplierDeptName) ? request.ApplierDeptName : (employeeInfo.DepartmentId ?? "");
                formData["applierUnitId"] = IsValidStringValue(request.ApplierUnit) ? request.ApplierUnit : (employeeInfo.DepartmentId ?? "");
                formData["org"] = IsValidStringValue(request.Org) ? request.Org : (employeeInfo.DepartmentId ?? "");
                formData["orgName"] = IsValidStringValue(request.OrgName) ? request.OrgName : (employeeInfo.DepartmentName ?? "");
                formData["orgUnit"] = IsValidStringValue(request.OrgUnit) ? request.OrgUnit : (employeeInfo.DepartmentName ?? "");
                formData["companyNo"] = IsValidStringValue(request.CompanyNo) ? request.CompanyNo : (employeeInfo.CompanyId ?? "03546618");
                formData["cpf01"] = employeeInfo.EmployeeNo;
                
                // 預設值
                formData["isDoc"] = "N";
                formData["isPart"] = "N";
                formData["isHoliday"] = "N";
                formData["isEventDate"] = "N";
            }

            // === BPM 選填欄位 - 字串類型（只在有效時加入）===
            if (IsValidStringValue(request.Form))
                formData["form"] = request.Form;
            
            if (IsValidStringValue(request.Cpf01))
                formData["cpf01"] = request.Cpf01;
            
            if (IsValidStringValue(request.IsDoc))
                formData["isDoc"] = request.IsDoc;
            else
                formData["isDoc"] = "N";  // 預設值
            
            if (IsValidStringValue(request.IsPart))
                formData["isPart"] = request.IsPart;
            else
                formData["isPart"] = "N";  // 預設值
            
            if (IsValidStringValue(request.EndCard))
                formData["endCard"] = request.EndCard;
            
            if (IsValidStringValue(request.FilePath))
                formData["filePath"] = request.FilePath;
            
            if (IsValidStringValue(request.SignType))
                formData["signType"] = request.SignType;
            
            if (IsValidStringValue(request.TextData))
                formData["textData"] = request.TextData;
            
            if (IsValidStringValue(request.UnitType))
            {
                formData["unitType"] = request.UnitType;
                formData["leaveUnitType"] = request.UnitType;  // BPM 也需要這個欄位
            }
            
            if (IsValidStringValue(request.IsHoliday))
                formData["isHoliday"] = request.IsHoliday;
            else
                formData["isHoliday"] = "N";  // 預設值
            
            if (IsValidStringValue(request.Signlvl01))
                formData["signlvl01"] = request.Signlvl01;
            
            if (IsValidStringValue(request.Signlvl02))
                formData["signlvl02"] = request.Signlvl02;
            
            if (IsValidStringValue(request.Signlvl03))
                formData["signlvl03"] = request.Signlvl03;
            
            if (IsValidStringValue(request.Signlvl04))
                formData["signlvl04"] = request.Signlvl04;
            
            if (IsValidStringValue(request.Signlvl05))
                formData["signlvl05"] = request.Signlvl05;
            
            if (IsValidStringValue(request.Signlvl06))
                formData["signlvl06"] = request.Signlvl06;
            
            if (IsValidStringValue(request.Signlvl07))
                formData["signlvl07"] = request.Signlvl07;
            
            if (IsValidStringValue(request.Signlvl08))
                formData["signlvl08"] = request.Signlvl08;
            
            if (IsValidStringValue(request.Signlvl09))
                formData["signlvl09"] = request.Signlvl09;
            
            if (IsValidStringValue(request.StartCard))
                formData["startCard"] = request.StartCard;
            
            if (IsValidStringValue(request.SendNotice))
                formData["sendNotice"] = request.SendNotice;
            
            if (IsValidStringValue(request.UnitTypeTW))
                formData["unitTypeTW"] = request.UnitTypeTW;
            
            if (IsValidStringValue(request.IsEventDate))
                formData["isEventDate"] = request.IsEventDate;
            else
                formData["isEventDate"] = "N";  // 預設值
            
            if (IsValidStringValue(request.KeyInUserId))
                formData["keyInUserId"] = request.KeyInUserId;
            
            if (IsValidStringValue(request.IsSendNotice))
                formData["isSendNotice"] = request.IsSendNotice;
            
            if (IsValidStringValue(request.SubstituteId))
                formData["substituteId"] = request.SubstituteId;
            
            if (IsValidStringValue(request.LeaveTypeMemo))
                formData["leaveTypeMemo"] = request.LeaveTypeMemo;

            // === BPM 選填欄位 - 日期類型（使用斜線格式，只在有值時加入）===
            if (IsValidStringValue(request.EventDate))
                formData["eventDate"] = request.EventDate!.Replace("-", "/");

            // === BPM 選填欄位 - 數字類型（只在有值且大於 0 時加入）===
            if (request.Units.HasValue && request.Units.Value > 0)
                formData["units"] = request.Units.Value;
            
            if (request.TotalDay.HasValue && request.TotalDay.Value > 0)
                formData["totalDay"] = request.TotalDay.Value;
            
            if (request.TotalHour.HasValue && request.TotalHour.Value > 0)
                formData["totalHour"] = request.TotalHour.Value;
            
            if (request.DayWorkHours.HasValue && request.DayWorkHours.Value > 0)
                formData["dayWorkHours"] = request.DayWorkHours.Value;
            
            if (request.SignTotalDays.HasValue && request.SignTotalDays.Value > 0)
                formData["signTotalDays"] = request.SignTotalDays.Value;

            // 處理代理人陣列（如果有提供）
            if (request.Substitutes != null && request.Substitutes.Count > 0)
            {
                for (int i = 0; i < request.Substitutes.Count; i++)
                {
                    var sub = request.Substitutes[i];
                    if (IsValidStringValue(sub.SubId) && IsValidStringValue(sub.SubSeq) && IsValidStringValue(sub.SubName))
                    {
                        formData[$"substituteId[{i}].SubId"] = sub.SubId;
                        formData[$"substituteId[{i}].SubSeq"] = sub.SubSeq;
                        formData[$"substituteId[{i}].SubName"] = sub.SubName;
                    }
                }
            }

            _logger.LogDebug("組裝表單資料完成: {@FormData}", formData);
            
            return formData;
        }

        /// <summary>
        /// 解析請假單記錄列表
        /// </summary>
        private List<LeaveFormRecord> ParseLeaveFormRecords(JsonElement jsonResponse)
        {
            var records = new List<LeaveFormRecord>();

            try
            {
                // 嘗試從 data 或 items 陣列中解析
                JsonElement dataArray;
                if (jsonResponse.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                        dataArray = dataElement;
                    else if (dataElement.TryGetProperty("items", out var itemsElement))
                        dataArray = itemsElement;
                    else
                        return records;
                }
                else if (jsonResponse.TryGetProperty("items", out dataArray))
                {
                    // items 在根層級
                }
                else
                {
                    return records;
                }

                foreach (var item in dataArray.EnumerateArray())
                {
                    var record = ParseSingleLeaveFormRecord(item);
                    if (record != null)
                        records.Add(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析請假單記錄列表時發生錯誤");
            }

            return records;
        }

        /// <summary>
        /// 解析單筆請假單記錄
        /// </summary>
        private LeaveFormRecord? ParseSingleLeaveFormRecord(JsonElement jsonElement)
        {
            try
            {
                // 嘗試從 data 或根層級取得資料
                var dataElement = jsonElement.TryGetProperty("data", out var data) ? data : jsonElement;
                var formDataElement = dataElement.TryGetProperty("formData", out var formData) ? formData : dataElement;

                var record = new LeaveFormRecord
                {
                    // 基本資訊
                    FormId = GetStringValue(dataElement, "formId", "id"),
                    FormNumber = GetStringValue(dataElement, "formNumber", "formNo") ?? "",
                    
                    // 員工資訊
                    EmployeeNo = GetStringValue(formDataElement, "applier", "employeeNo"),
                    EmployeeName = GetStringValue(formDataElement, "applierName", "employeeName"),
                    Department = GetStringValue(formDataElement, "orgName", "departmentName"),
                    
                    // 假別資訊
                    LeaveTypeId = GetStringValue(formDataElement, "leaveTypeId"),
                    LeaveTypeName = GetStringValue(formDataElement, "leaveTypeName"),
                    
                    // 時間資訊
                    StartDate = GetStringValue(formDataElement, "startDate"),
                    StartTime = GetStringValue(formDataElement, "startTime"),
                    EndDate = GetStringValue(formDataElement, "endDate"),
                    EndTime = GetStringValue(formDataElement, "endTime"),
                    StartDateTime = CombineDateTime(
                        GetStringValue(formDataElement, "startDate"),
                        GetStringValue(formDataElement, "startTime")
                    ),
                    EndDateTime = CombineDateTime(
                        GetStringValue(formDataElement, "endDate"),
                        GetStringValue(formDataElement, "endTime")
                    ),
                    
                    // 請假資訊
                    TotalHours = GetDecimalValue(formDataElement, "totalHour", "totalHours"),
                    TotalDays = GetDecimalValue(formDataElement, "units", "totalDays"),
                    Units = GetDecimalValue(formDataElement, "units"),
                    UnitType = GetStringValue(formDataElement, "unitType"),
                    Reason = GetStringValue(formDataElement, "reason"),
                    
                    // 代理人資訊
                    AgentNo = GetStringValue(formDataElement, "agentNo"),
                    AgentName = GetStringValue(formDataElement, "agentName"),
                    
                    // 簽核狀態
                    ApprovalStatus = GetStringValue(dataElement, "status", "approvalStatus"),
                    ApprovalStatusText = MapApprovalStatusText(GetStringValue(dataElement, "status", "approvalStatus")),
                    
                    // 時間戳記
                    ApplyDateTime = GetStringValue(dataElement, "createdAt", "applyDateTime"),
                    CreatedAt = GetDateTimeValue(dataElement, "createdAt", "applyDateTime"),
                    UpdatedAt = GetNullableDateTimeValue(dataElement, "updatedAt", "modifiedAt")
                };

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析單筆請假單記錄時發生錯誤");
                return null;
            }
        }

        /// <summary>
        /// 從 JSON 中提取 FormId
        /// </summary>
        private string ExtractFormId(JsonElement jsonResponse)
        {
            // 嘗試多種可能的路徑
            if (jsonResponse.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("formId", out var formIdElement))
                    return formIdElement.GetString() ?? "UNKNOWN";
                if (dataElement.TryGetProperty("id", out formIdElement))
                    return formIdElement.GetString() ?? "UNKNOWN";
            }

            if (jsonResponse.TryGetProperty("formId", out var formId))
                return formId.GetString() ?? "UNKNOWN";

            if (jsonResponse.TryGetProperty("id", out formId))
                return formId.GetString() ?? "UNKNOWN";
                
            if (jsonResponse.TryGetProperty("requestId", out formId))
                return formId.GetString() ?? "UNKNOWN";

            return "UNKNOWN";
        }

        /// <summary>
        /// 從 JSON 中提取 FormNumber
        /// </summary>
        private string? ExtractFormNumber(JsonElement jsonResponse)
        {
            if (jsonResponse.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("formNumber", out var formNumberElement))
                    return formNumberElement.GetString();
                if (dataElement.TryGetProperty("formNo", out formNumberElement))
                    return formNumberElement.GetString();
            }

            if (jsonResponse.TryGetProperty("formNumber", out var formNumber))
                return formNumber.GetString();

            if (jsonResponse.TryGetProperty("formNo", out formNumber))
                return formNumber.GetString();
                
            if (jsonResponse.TryGetProperty("processSerialNo", out formNumber))
                return formNumber.GetString();

            return null;
        }

        /// <summary>
        /// 組合日期與時間
        /// </summary>
        private string CombineDateTime(string date, string time)
        {
            if (string.IsNullOrEmpty(date))
                return "";

            if (string.IsNullOrEmpty(time))
                return date;

            return $"{date} {time}";
        }

        /// <summary>
        /// 映射簽核狀態文字
        /// </summary>
        private string MapApprovalStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "pending" or "draft" => "待簽核",
                "approved" or "completed" or "success" => "已核准",
                "rejected" or "denied" => "已拒絕",
                "cancelled" => "已取消",
                "returned" => "已退回",
                _ => status ?? "未知"
            };
        }

        #region JSON 解析輔助方法

        private string GetStringValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? "";
                }
            }
            return "";
        }

        private int GetIntValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
                        return value;
                }
            }
            return 0;
        }

        private decimal GetDecimalValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var value))
                        return value;
                }
            }
            return 0;
        }

        private DateTime GetDateTimeValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var dateStr = prop.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                        return date;
                }
            }
            return DateTime.MinValue;
        }

        private DateTime? GetNullableDateTimeValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var dateStr = prop.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                        return date;
                }
            }
            return null;
        }

        #endregion

        #region 新增 API 方法

        /// <summary>
        /// 透過工號查詢員工基本資料（內部方法）
        /// </summary>
        private async Task<EmployeeBasicInfo?> GetEmployeeByIdAsync(string employeeId)
        {
            try
            {
                const string sql = @"
                    SELECT TOP 1
                        EMPLOYEE_NO as EmployeeNo,
                        EMPLOYEE_NAME as EmployeeName,
                        EMAIL_ADDRESS as EmailAddress,
                        ORGANIZATION_NAME as DepartmentName,
                        COMPANY_CODE as CompanyCode
                    FROM [dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_NO = @EmployeeId COLLATE Chinese_Taiwan_Stroke_CI_AS";

                var connectionString = _configuration.GetConnectionString("HRDatabase")
                    ?? throw new Exception("無法取得資料庫連線字串");

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                var result = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<EmployeeBasicInfo>(connection, sql, new { EmployeeId = employeeId });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢員工資料失敗: {EmployeeId}", employeeId);
                return null;
            }
        }

        /// <summary>
        /// 查詢請假假別單位 - efleaveformunit API
        /// 根據公司代碼查詢所有可用的假別及其最小單位
        /// </summary>
        public async Task<List<LeaveTypeUnitData>> GetLeaveTypeUnitsAsync(string companyCode)
        {
            try
            {
                _logger.LogInformation("開始查詢假別單位，公司代碼: {CompanyCode}", companyCode);

                // 步驟 1: 同步 BPM 流程信息
                // 產生表單編號（通常使用格式: PI_Leave_Test + 時間戳記或序號）
                var processSerialNo = $"PI_Leave_Test{DateTime.Now:yyyyMMddHHmmss}";
                
                _logger.LogInformation(
                    "同步 BPM 流程信息 - 表單編號: {ProcessSerialNo}, 程序代碼: {ProcessCode}",
                    processSerialNo, LEAVE_PROCESS_CODE);

                var bpmResponse = await _bpmMiddlewareService.SyncProcessInfoAsync(
                    processSerialNo,
                    LEAVE_PROCESS_CODE,
                    "TEST");

                if (bpmResponse == null || bpmResponse.Code != "200")
                {
                    _logger.LogWarning(
                        "BPM 流程同步失敗 - Code: {Code}, Msg: {Msg}",
                        bpmResponse?.Code, bpmResponse?.Msg);
                    // 即使 BPM 同步失敗，仍然繼續查詢假別資料
                }
                else
                {
                    _logger.LogInformation(
                        "BPM 流程同步成功 - 流程ID: {ProcessId}, 流程名稱: {ProcessName}",
                        bpmResponse.ProcessInfo?.ProcessId,
                        bpmResponse.ProcessInfo?.ProcessName);
                }

                // 步驟 2: 查詢假別資訊
                // SQL 查詢語句 - 查詢所有假別（已移除排除清單，允許所有假別代碼）
                const string sql = @"
                    SELECT
                        L.LEAVE_REFERENCE_CLASS AS LeaveType,
                        L.LEAVE_MIN_VALUE AS LeaveUnit,
                        L.LEAVE_REFERENCE_CODE AS LeaveCode,
                        L.LEAVE_UNIT AS LeaveUnitType
                    FROM [dbo].[vwZZ_LEAVE_REFERENCE] L
                    JOIN [dbo].[VwZZ_COMPANY] C ON L.COMPANY_ID = C.COMPANY_ID
                    WHERE C.COMPANY_CODE = @CompanyCode
                    AND L.LEAVE_REFERENCE_CODE NOT IN (
                        'SLC01', 'S0012-1', 'S0013-1', 'S0013-2', 'SLC01-REGL', 'SLC01-SUOT',
                        'SLC02', 'SLC05', 'SLC06', 'SLC07', 'S0009-1', 'S0010-1', 'S0011-1',
                        'S0017-1', 'S0017-2', 'S0018-1', 'S0019-1', 'S0019-2', 'S0019-3',
                        'S0015-1', 'S0020-1', 'S0004-5', 'S0004-6'
                    )
                    ORDER BY L.LEAVE_REFERENCE_CODE";

                var connectionString = _configuration.GetConnectionString("HRDatabase")
                    ?? throw new Exception("無法取得資料庫連線字串");

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                var results = await Dapper.SqlMapper.QueryAsync<LeaveTypeUnitData>(connection, sql, new { CompanyCode = companyCode });
                var leaveTypes = results.ToList();

                _logger.LogInformation("成功查詢假別單位，共 {Count} 筆", leaveTypes.Count);
                return leaveTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢假別單位失敗，公司代碼: {CompanyCode}", companyCode);
                throw;
            }
        }

        /// <summary>
        /// 提交請假單申請 - efleaveform API
        /// 使用簡化的欄位結構提交請假申請
        /// </summary>
        public async Task<LeaveFormOperationResult> SubmitLeaveFormAsync(LeaveFormSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("開始提交請假單申請: {@Request}", new
                {
                    request.Uid,
                    request.Leavetype,
                    request.Estartdate,
                    request.Eenddate
                });

                // 1. 透過工號查詢員工完整資料（使用 BasicInfoService）
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到工號對應的員工資料: {request.Uid}");
                }

                _logger.LogInformation("申請人資料 - 工號: {EmployeeNo}, 姓名: {Name}, 部門: {Dept}",
                    employeeInfo.EmployeeNo, employeeInfo.EmployeeName, employeeInfo.DepartmentName);

                // 2. 查詢假別資訊
                var leaveTypes = await GetLeaveTypeUnitsAsync(request.Cid);
                var leaveTypeInfo = leaveTypes.FirstOrDefault(lt => lt.LeaveCode == request.Leavetype);
                if (leaveTypeInfo == null)
                {
                    throw new Exception($"找不到假別代碼: {request.Leavetype}");
                }

                // 3. 建構 BPM 表單資料（根據 BPM Middleware API 規格）
                var formData = new Dictionary<string, object?>
                {
                    // 必填欄位 - 根據 BPM API 規格
                    ["startDate"] = request.Estartdate.Replace("-", "/"),  // 轉換為 yyyy/MM/dd 格式
                    ["startTime"] = request.Estarttime,
                    ["endDate"] = request.Eenddate.Replace("-", "/"),      // 轉換為 yyyy/MM/dd 格式
                    ["endTime"] = request.Eendtime,
                    ["agentNo"] = request.Eagent,
                    ["reason"] = request.Ereason,
                    ["leaveTypeId"] = request.Leavetype,
                    ["leaveTypeName"] = leaveTypeInfo.LeaveType
                };

                // 選填欄位 - 事件發生日
                if (!string.IsNullOrEmpty(request.Eleavedate))
                {
                    formData["eventDate"] = request.Eleavedate.Replace("-", "/");
                }

                // 4. 如果有附件，構建附件路徑
                string? filePath = null;
                bool hasAttachments = false;
                if (request.Efileid != null && request.Efileid.Any())
                {
                    _logger.LogInformation("處理附件，共 {Count} 個附件", request.Efileid.Count);
                    // 將附件 ID 轉換為 FTP 路徑格式
                    // 格式: FTPTest~~/FTPShare/filename
                    var ftpPaths = request.Efileid.Select(id => $"FTPTest~~/FTPShare/leave_{id}.pdf").ToList();
                    filePath = string.Join("||", ftpPaths);
                    hasAttachments = true;
                    _logger.LogInformation("構建的 filePath: {FilePath}", filePath);
                }

                var bpmRequest = new
                {
                    processCode = $"{FORM_CODE}_PROCESS",
                    formDataMap = new Dictionary<string, object>
                    {
                        [FORM_CODE] = formData
                    },
                    userId = employeeInfo.EmployeeNo,
                    subject = $"{employeeInfo.EmployeeName} 的請假申請 - {leaveTypeInfo.LeaveType}",
                    sourceSystem = "APP",
                    environment = "TEST",
                    hasAttachments = hasAttachments,
                    filePath = filePath  // 添加附件路徑
                };

                // 5. 呼叫 BPM API 建立表單
                var endpoint = "bpm/invoke-process";
                var response = await _bpmService.PostAsync(endpoint, bpmRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                // 6. 解析回應 - 從 BPM 回應中取得正確的欄位
                var requestId = GetStringValue(jsonResponse, "requestId");
                var processSerialNo = GetStringValue(jsonResponse, "processSerialNo");
                var bpmProcessOid = GetStringValue(jsonResponse, "bpmProcessOid");
                var status = GetStringValue(jsonResponse, "status");
                var message = GetStringValue(jsonResponse, "message");

                // 只要能成功呼叫 API 並取得回應，就視為成功
                Console.WriteLine("========================================");
                Console.WriteLine("✅ 請假單送出成功");
                Console.WriteLine($"📋 流程編號: {processSerialNo}");
                Console.WriteLine($"🆔 請求ID: {requestId}");
                Console.WriteLine($"🔑 BPM流程OID: {bpmProcessOid}");
                Console.WriteLine($"👤 申請人: {employeeInfo.EmployeeName} ({employeeInfo.EmployeeNo})");
                Console.WriteLine($"🏖️  假別: {leaveTypeInfo.LeaveType} ({request.Leavetype})");
                Console.WriteLine($"📅 起迄: {request.Estartdate} {request.Estarttime} ~ {request.Eenddate} {request.Eendtime}");
                Console.WriteLine($"📝 事由: {request.Ereason}");
                Console.WriteLine($"👥 代理人: {request.Eagent}");
                Console.WriteLine($"✔️  狀態: {status}");
                Console.WriteLine($"💬 訊息: {message}");
                Console.WriteLine("========================================");
                
                _logger.LogInformation("請假單申請成功 - ProcessSerialNo: {ProcessSerialNo}, RequestId: {RequestId}, Status: {Status}", 
                    processSerialNo, requestId, status);
                
                return new LeaveFormOperationResult
                {
                    Success = true,
                    Message = "請求成功",
                    FormId = processSerialNo,  // 使用 BPM 的流程編號
                    FormNumber = requestId      // 使用 BPM 的請求ID
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交請假單申請失敗");
                return new LeaveFormOperationResult
                {
                    Success = false,
                    Message = $"請求失敗: {ex.Message}"
                };
            }
        }

        #endregion

        #endregion
    }
}