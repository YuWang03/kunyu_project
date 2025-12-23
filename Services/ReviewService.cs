using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 待我審核服務實作
    /// 整合本地 DB 同步功能
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly IBpmFormSyncService _formSyncService;
        private readonly IBpmFormRepository _formRepository;
        private readonly IEFormApprovalRepository _approvalRepository;
        private readonly ILogger<ReviewService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewService"/> class.
        /// </summary>
        /// <param name="bpmService">The BPM service.</param>
        /// <param name="basicInfoService">The basic info service.</param>
        /// <param name="formSyncService">The BPM form sync service.</param>
        /// <param name="formRepository">The BPM form repository.</param>
        /// <param name="approvalRepository">The eForm approval repository.</param>
        /// <param name="logger">The logger instance.</param>
        public ReviewService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            IBpmFormSyncService formSyncService,
            IBpmFormRepository formRepository,
            IEFormApprovalRepository approvalRepository,
            ILogger<ReviewService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _formSyncService = formSyncService;
            _formRepository = formRepository;
            _approvalRepository = approvalRepository;
            _logger = logger;
        }

        /// <summary>
        /// 取得待我審核列表
        /// </summary>
        public async Task<ReviewListResponse> GetReviewListAsync(ReviewListRequest request)
        {
            try
            {
                _logger.LogInformation("開始取得待我審核列表，UID: {Uid}", request.Uid);

                // 1. 從 BPM 取得待辦事項
                var endpoint = $"bpm/workitems/{request.Uid}";
                _logger.LogInformation("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogInformation("BPM API 回應: {Response}", responseJson);
                
                var bpmResponse = JsonSerializer.Deserialize<BpmWorkItemsResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (bpmResponse == null)
                {
                    _logger.LogError("無法解析 BPM 回應: {Response}", responseJson);
                    return new ReviewListResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，無法解析 BPM 回應資料"
                    };
                }

                if (bpmResponse.Status != "SUCCESS")
                {
                    _logger.LogWarning("BPM API 返回非成功狀態: Status={Status}, Message={Message}", 
                        bpmResponse.Status, bpmResponse.Message);
                    return new ReviewListResponse
                    {
                        Code = "203",
                        Msg = $"BPM API 失敗: {bpmResponse.Message}"
                    };
                }

                if (bpmResponse.WorkItems == null || !bpmResponse.WorkItems.Any())
                {
                    _logger.LogInformation("目前沒有待審核項目，WorkItems 為空或 null");
                    return new ReviewListResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new ReviewListData
                        {
                            Uid = request.Uid,
                            EFormData = new List<ReviewListItem>()
                        }
                    };
                }

                _logger.LogInformation("找到 {Count} 個待辦事項", bpmResponse.WorkItems.Count);

                // 2. 取得員工基本資料
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                var userName = employeeInfo?.EmployeeName ?? "未知";
                var userDepartment = employeeInfo?.DepartmentName ?? "未知";

                // 3. 整理待審核列表
                var reviewItems = new List<ReviewListItem>();
                
                foreach (var workItem in bpmResponse.WorkItems)
                {
                    try
                    {
                        _logger.LogInformation("處理待辦事項: {ProcessSerialNumber}", workItem.ProcessSerialNumber);
                        
                        // 解析表單編號取得表單類型
                        var formType = ParseFormType(workItem.ProcessSerialNumber);
                        _logger.LogDebug("表單類型: {FormType}", formType);
                        
                        var formDetail = await GetFormDetailBySerialNumber(workItem.ProcessSerialNumber, formType);

                        if (formDetail != null)
                        {
                            reviewItems.Add(formDetail);
                            _logger.LogInformation("成功新增待辦事項: {FormId}", formDetail.FormId);
                        }
                        else
                        {
                            _logger.LogWarning("無法取得表單詳細資料: {ProcessSerialNumber}", workItem.ProcessSerialNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "處理待辦事項失敗: {ProcessSerialNumber}", workItem.ProcessSerialNumber);
                    }
                }

                _logger.LogInformation("共處理了 {Count} 個待審核項目", reviewItems.Count);

                // 4. 根據表單類型進行篩選
                if (!string.IsNullOrEmpty(request.EFormType))
                {
                    var requestedTypes = request.EFormType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToUpper())
                        .ToList();
                    
                    _logger.LogInformation("篩選表單類型: {Types}", string.Join(", ", requestedTypes));
                    
                    reviewItems = reviewItems
                        .Where(item => requestedTypes.Contains(item.EFormType.ToUpper()))
                        .ToList();
                    
                    _logger.LogInformation("篩選後剩餘 {Count} 個項目", reviewItems.Count);
                }

                // 5. 計算分頁參數
                if (!int.TryParse(request.Page, out var pageNumber) || pageNumber < 1)
                {
                    pageNumber = 1;
                }
                
                if (!int.TryParse(request.PageSize, out var pageSize) || pageSize < 1)
                {
                    pageSize = 20;
                }

                var totalCount = reviewItems.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                // 確保頁碼不超過總頁數
                if (pageNumber > totalPages && totalPages > 0)
                {
                    pageNumber = totalPages;
                }

                _logger.LogInformation("分頁參數: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, TotalPages={TotalPages}", 
                    pageNumber, pageSize, totalCount, totalPages);

                // 6. 取得當前頁的資料
                var pagedItems = reviewItems
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("返回 {Count} 個項目", pagedItems.Count);

                return new ReviewListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new ReviewListData
                    {
                        Uid = request.Uid,
                        TotalCount = totalCount.ToString(),
                        Page = pageNumber.ToString(),
                        PageSize = pageSize.ToString(),
                        TotalPages = totalPages.ToString(),
                        EFormData = pagedItems
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得待我審核列表時發生錯誤");
                return new ReviewListResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 取得待我審核詳細資料
        /// </summary>
        public async Task<ReviewDetailResponse> GetReviewDetailAsync(ReviewDetailRequest request)
        {
            try
            {
                _logger.LogInformation("開始取得待我審核詳細資料，FormId: {FormId}", request.FormId);

                // 1. 解析表單類型
                var formType = ParseFormType(request.FormId);
                var (processCode, formCode, formVersion) = GetProcessCodeFromFormType(formType);
                
                // 2. 從 BPM 取得表單詳細資料（使用 sync-process-info）
                var endpoint = $"bpm/sync-process-info?processSerialNo={request.FormId}&processCode={processCode}&environment=TEST&formCode={formCode}&formVersion={formVersion}";
                _logger.LogInformation("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogDebug("BPM API 回應: {Response}", responseJson);
                
                // 3. 解析回應資料
                var detailData = await ParseFormDetail(request.FormId, formType, responseJson);

                if (detailData == null)
                {
                    return new ReviewDetailResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                return new ReviewDetailResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = detailData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得待我審核詳細資料時發生錯誤");
                return new ReviewDetailResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 執行簽核作業
        /// 流程：
        /// 1. GET /bpm/workitems/{uid} 取得待辦事項（包含 processSerialNumber, processCode, workItemOID）
        /// 2. 根據 processCode 判斷表單類型，匹配用戶請求的表單
        /// 3. GET /bpm/sync-process-info 取得表單完整資訊（用於驗證）
        /// 4. POST /bpm/workitem-approval/complete 完成簽核
        /// </summary>
        public async Task<ReviewApprovalResponse> ApproveReviewAsync(ReviewApprovalRequest request)
        {
            try
            {
                _logger.LogInformation("開始執行簽核作業，UID: {Uid}, 簽核狀態: {Status}", 
                    request.Uid, request.ApprovalStatus);

                // 驗證必要參數
                if (string.IsNullOrEmpty(request.Uid))
                {
                    _logger.LogWarning("UID 為空");
                    return new ReviewApprovalResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                if (request.ApprovalData == null || request.ApprovalData.Count == 0)
                {
                    _logger.LogWarning("沒有簽核資料");
                    return new ReviewApprovalResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                // 驗證每筆簽核資料
                foreach (var approval in request.ApprovalData)
                {
                    if (string.IsNullOrEmpty(approval.EFormType) || string.IsNullOrEmpty(approval.EFormId))
                    {
                        _logger.LogWarning("簽核資料不完整: FormType={FormType}, FormId={FormId}", 
                            approval.EFormType, approval.EFormId);
                        return new ReviewApprovalResponse
                        {
                            Code = "203",
                            Msg = "請求失敗，主要條件不符合"
                        };
                    }
                }

                // 步驟 1: 取得使用者的待辦事項清單
                _logger.LogInformation("步驟 1: 取得待辦事項清單，UID: {Uid}", request.Uid);
                var workItemsEndpoint = $"bpm/workitems/{request.Uid}";
                var workItemsResponseJson = await _bpmService.GetAsync(workItemsEndpoint);
                _logger.LogInformation("待辦事項清單回應: {Response}", workItemsResponseJson);
                
                var workItemsResponse = JsonSerializer.Deserialize<BpmWorkItemsResponse>(workItemsResponseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (workItemsResponse == null || workItemsResponse.WorkItems == null || !workItemsResponse.WorkItems.Any())
                {
                    _logger.LogWarning("找不到待辦事項");
                    return new ReviewApprovalResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，找不到待辦事項"
                    };
                }

                _logger.LogInformation("找到 {Count} 個待辦事項", workItemsResponse.WorkItems.Count);

                // 建立表單ID到workItem的映射（使用 processSerialNumber 作為 key）
                var workItemMap = new Dictionary<string, BpmWorkItem>(StringComparer.OrdinalIgnoreCase);
                foreach (var workItem in workItemsResponse.WorkItems)
                {
                    if (!string.IsNullOrEmpty(workItem.ProcessSerialNumber))
                    {
                        workItemMap[workItem.ProcessSerialNumber] = workItem;
                        _logger.LogDebug("待辦事項映射: {SerialNo} -> ProcessCode: {ProcessCode}, WorkItemOID: {OID}", 
                            workItem.ProcessSerialNumber, workItem.ProcessCode, workItem.WorkItemOID);
                    }
                }

                // 步驟 2: 對每筆簽核資料執行簽核
                var successCount = 0;
                var failCount = 0;
                var failedFormIds = new List<string>();

                foreach (var approval in request.ApprovalData)
                {
                    try
                    {
                        _logger.LogInformation("處理簽核: FormType={FormType}, FormId={FormId}", 
                            approval.EFormType, approval.EFormId);

                        // 查找對應的 workItem（使用 eformid 匹配 processSerialNumber）
                        if (!workItemMap.TryGetValue(approval.EFormId, out var workItem))
                        {
                            _logger.LogWarning("找不到表單對應的待辦事項: {FormId}", approval.EFormId);
                            failCount++;
                            failedFormIds.Add(approval.EFormId);
                            continue;
                        }

                        // 驗證表單類型是否匹配（根據 processCode）
                        var expectedFormType = GetFormTypeByProcessCode(workItem.ProcessCode);
                        if (!string.IsNullOrEmpty(expectedFormType) && 
                            !expectedFormType.Equals(approval.EFormType, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("表單類型不匹配: 預期 {Expected}, 實際 {Actual}, FormId: {FormId}", 
                                expectedFormType, approval.EFormType, approval.EFormId);
                            // 繼續處理，不視為錯誤（可能有特殊情況）
                        }

                        // 步驟 3: 取得表單完整資訊以獲取 workItemOID（若待辦事項已有則跳過）
                        string workItemOID = workItem.WorkItemOID;
                        
                        if (string.IsNullOrEmpty(workItemOID))
                        {
                            _logger.LogInformation("待辦事項缺少 WorkItemOID，嘗試從 sync-process-info 取得");
                            
                            var processCode = workItem.ProcessCode;
                            if (string.IsNullOrEmpty(processCode))
                            {
                                // 根據表單類型推斷 processCode
                                processCode = GetProcessCodeByFormType(approval.EFormType);
                            }
                            
                            var syncEndpoint = $"bpm/sync-process-info?processSerialNo={Uri.EscapeDataString(approval.EFormId)}&processCode={Uri.EscapeDataString(processCode)}&environment=TEST";
                            _logger.LogInformation("呼叫 sync-process-info: {Endpoint}", syncEndpoint);
                            
                            try
                            {
                                var syncResponseJson = await _bpmService.GetAsync(syncEndpoint);
                                _logger.LogInformation("sync-process-info 回應: {Response}", syncResponseJson);
                                
                                var syncResponse = JsonSerializer.Deserialize<JsonElement>(syncResponseJson);
                                
                                if (syncResponse.TryGetProperty("status", out var statusProp) && 
                                    statusProp.GetString()?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    if (syncResponse.TryGetProperty("processInfo", out var processInfo) &&
                                        processInfo.TryGetProperty("workItemOID", out var oidProp))
                                    {
                                        workItemOID = oidProp.GetString() ?? "";
                                        _logger.LogInformation("從 sync-process-info 取得 WorkItemOID: {OID}", workItemOID);
                                    }
                                }
                            }
                            catch (Exception syncEx)
                            {
                                _logger.LogWarning(syncEx, "取得 sync-process-info 失敗: {FormId}", approval.EFormId);
                            }
                        }

                        if (string.IsNullOrEmpty(workItemOID))
                        {
                            _logger.LogWarning("無法取得 WorkItemOID: {FormId}", approval.EFormId);
                            failCount++;
                            failedFormIds.Add(approval.EFormId);
                            continue;
                        }

                        // 步驟 4: 呼叫 workitem-approval/complete 完成簽核
                        var completeRequest = new BpmWorkItemApprovalCompleteRequest
                        {
                            WorkItemOID = workItemOID,
                            UserId = request.Uid,
                            Comment = request.Comments ?? ""
                        };

                        // 根據簽核狀態決定要呼叫的 API
                        string approvalEndpoint;
                        object requestBody;

                        if (request.ApprovalStatus == "Y")
                        {
                            // 同意：使用 workitem-approval/complete
                            approvalEndpoint = "bpm/workitem-approval/complete";
                            requestBody = completeRequest;
                        }
                        else
                        {
                            // 不同意：根據 approvalFlow 決定
                            if (request.ApprovalFlow == "S")
                            {
                                // 中止流程
                                approvalEndpoint = "bpm/workitem-approval/terminate";
                            }
                            else if (request.ApprovalFlow == "R")
                            {
                                // 退回發起人
                                approvalEndpoint = "bpm/workitem-approval/return";
                            }
                            else
                            {
                                // 預設為拒絕
                                approvalEndpoint = "bpm/workitem-approval/reject";
                            }
                            requestBody = completeRequest;
                        }

                        _logger.LogInformation("呼叫簽核 API: {Endpoint}, WorkItemOID: {OID}, UserId: {UserId}", 
                            approvalEndpoint, workItemOID, request.Uid);
                        
                        var responseJson = await _bpmService.PostAsync(approvalEndpoint, requestBody);
                        _logger.LogInformation("簽核 API 回應: {Response}", responseJson);
                        
                        // 檢查回應
                        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
                        var isSuccess = false;
                        
                        if (response.TryGetProperty("status", out var status))
                        {
                            isSuccess = status.GetString()?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false;
                        }
                        else if (response.TryGetProperty("success", out var success))
                        {
                            isSuccess = success.GetBoolean();
                        }
                        else
                        {
                            // 如果沒有明確的狀態，假設成功（某些 API 可能不返回狀態）
                            isSuccess = true;
                        }

                        if (isSuccess)
                        {
                            _logger.LogInformation("簽核成功: {FormId}", approval.EFormId);
                            successCount++;
                            
                            // 同步簽核結果到本地 DB
                            try
                            {
                                var newStatus = request.ApprovalStatus == "Y" ? "APPROVED" : 
                                    request.ApprovalFlow == "S" ? "CANCELLED" : 
                                    request.ApprovalFlow == "R" ? "RETURNED" : "REJECTED";
                                
                                await _formSyncService.UpdateFormStatusAsync(
                                    approval.EFormId, 
                                    newStatus, 
                                    request.Comments);
                                    
                                // 記錄簽核歷程
                                var approvalHistory = new BpmFormApprovalHistory
                                {
                                    FormId = approval.EFormId,
                                    SequenceNo = 1, // 需要從現有歷程取得最大序號+1
                                    ApproverId = request.Uid,
                                    Action = request.ApprovalStatus == "Y" ? "APPROVE" : 
                                        request.ApprovalFlow == "S" ? "TERMINATE" : 
                                        request.ApprovalFlow == "R" ? "RETURN" : "REJECT",
                                    Comment = request.Comments,
                                    ActionTime = DateTime.Now
                                };
                                await _formRepository.AddApprovalHistoryAsync(approvalHistory);
                                
                                _logger.LogInformation("簽核結果已同步到本地 DB: {FormId}, 狀態: {Status}", 
                                    approval.EFormId, newStatus);
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogWarning(dbEx, "同步簽核結果到本地 DB 失敗: {FormId}", approval.EFormId);
                            }
                            
                            // ★★★ 保存签核记录到签核记录表 ★★★
                            try
                            {
                                // 获取签核者信息
                                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                                var uName = employeeInfo?.EmployeeName ?? "";
                                var uDepartment = employeeInfo?.DepartmentName ?? "";
                                
                                var approvalRecord = new EFormApprovalRecord
                                {
                                    TokenId = request.TokenId ?? "",
                                    Cid = request.Cid ?? "",
                                    Uid = request.Uid,
                                    UName = uName,
                                    UDepartment = uDepartment,
                                    EFormType = approval.EFormType,
                                    EFormId = approval.EFormId,
                                    ApprovalStatus = request.ApprovalStatus,
                                    ApprovalFlow = request.ApprovalFlow ?? "A",
                                    Comments = request.Comments,
                                    ApprovalDate = DateTime.Now
                                };
                                
                                await _approvalRepository.SaveApprovalRecordAsync(approvalRecord);
                                _logger.LogInformation("签核记录已保存到数据库: FormId={FormId}, Uid={Uid}", 
                                    approval.EFormId, request.Uid);
                            }
                            catch (Exception saveEx)
                            {
                                _logger.LogWarning(saveEx, "保存签核记录失败: {FormId}", approval.EFormId);
                                // 不影响签核流程，继续执行
                            }
                        }
                        else
                        {
                            _logger.LogWarning("簽核失敗: {FormId}, 回應: {Response}", approval.EFormId, responseJson);
                            failCount++;
                            failedFormIds.Add(approval.EFormId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "簽核失敗: FormType={FormType}, FormId={FormId}, 錯誤訊息: {Message}", 
                            approval.EFormType, approval.EFormId, ex.Message);
                        failCount++;
                        failedFormIds.Add(approval.EFormId);
                    }
                }

                _logger.LogInformation("簽核完成: 成功 {SuccessCount} 筆, 失敗 {FailCount} 筆", 
                    successCount, failCount);

                if (successCount > 0)
                {
                    return new ReviewApprovalResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new ReviewApprovalResponseData
                        {
                            Status = successCount == request.ApprovalData.Count 
                                ? "請求成功" 
                                : $"部分成功：成功 {successCount} 筆，失敗 {failCount} 筆"
                        }
                    };
                }
                else
                {
                    _logger.LogError("所有簽核都失敗，總共 {TotalCount} 筆，失敗表單: {FailedIds}", 
                        request.ApprovalData.Count, string.Join(", ", failedFormIds));
                    return new ReviewApprovalResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，所有簽核都失敗"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行簽核作業時發生錯誤");
                return new ReviewApprovalResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 根據 processCode 判斷表單類型
        /// processCode 標準內容：
        /// - 銷假單：PI_CANCEL_LEAVE_001_PROCESS
        /// - 出缺勤異常：Attendance_Exception_001_PROCESS
        /// - 請假單：PI_LEAVE_001_PROCESS
        /// - 加班單：PI_OVERTIME_001_PROCESS
        /// - 外出訓練單：PI_Trainning_PROCESS
        /// </summary>
        private string GetFormTypeByProcessCode(string processCode)
        {
            if (string.IsNullOrEmpty(processCode))
                return "";
                
            // 轉換為大寫進行比對
            var code = processCode.ToUpperInvariant();
            
            if (code.Contains("CANCEL_LEAVE") || code.Contains("CANCELLEAVE"))
                return "D"; // 銷假單
            if (code.Contains("ATTENDANCE") || code.Contains("EXCEPTION"))
                return "R"; // 出缺勤異常/出勤確認單
            if (code.Contains("LEAVE"))
                return "L"; // 請假單
            if (code.Contains("OVERTIME"))
                return "A"; // 加班單
            if (code.Contains("TRAINNING") || code.Contains("TRAINING") || code.Contains("OUTING"))
                return "O"; // 外出訓練單/外出外訓單
            if (code.Contains("TRIP") || code.Contains("BUSINESS"))
                return "T"; // 出差單
                
            return "";
        }

        #region 私有輔助方法

        /// <summary>
        /// 從表單編號解析表單類型
        /// </summary>
        private string ParseFormType(string formId)
        {
            // PI_HR_H1A_PKG_Test00000026 格式分析
            if (formId.Contains("LEAVE", StringComparison.OrdinalIgnoreCase))
                return "L"; // 請假單
            else if (formId.Contains("CANCEL", StringComparison.OrdinalIgnoreCase))
                return "D"; // 銷假單
            else if (formId.Contains("OUT", StringComparison.OrdinalIgnoreCase))
                return "O"; // 外出外訓單
            else if (formId.Contains("OVERTIME", StringComparison.OrdinalIgnoreCase))
                return "A"; // 加班單
            else if (formId.Contains("ATTENDANCE", StringComparison.OrdinalIgnoreCase))
                return "R"; // 出勤單
            else if (formId.Contains("TRIP", StringComparison.OrdinalIgnoreCase) || 
                     formId.Contains("BUSINESS", StringComparison.OrdinalIgnoreCase))
                return "T"; // 出差單

            // 預設返回請假單
            _logger.LogWarning("無法識別表單類型: {FormId}，預設為請假單", formId);
            return "L";
        }

        /// <summary>
        /// 根據表單類型取得表單名稱
        /// </summary>
        private string GetFormTypeName(string formType)
        {
            return formType switch
            {
                "L" => "請假單",
                "D" => "銷假單",
                "O" => "外出外訓單",
                "A" => "加班單",
                "R" => "出勤確認單",
                "T" => "出差單",
                _ => "未知表單"
            };
        }

        /// <summary>
        /// 根據表單序號取得表單詳細資料（用於列表）
        /// </summary>
        private async Task<ReviewListItem?> GetFormDetailBySerialNumber(string serialNumber, string formType)
        {
            try
            {
                _logger.LogInformation("取得表單詳細資料: {SerialNumber}, 表單類型: {FormType}", serialNumber, formType);
                
                // 根據表單類型取得對應的 processCode
                var processCode = GetProcessCodeByFormType(formType);
                
                // 使用 sync-process-info API 取得表單詳細資料
                var endpoint = $"bpm/sync-process-info?processSerialNo={serialNumber}&processCode={processCode}&environment=TEST";
                _logger.LogDebug("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogDebug("BPM 表單回應: {Response}", responseJson);

                var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                // 檢查狀態
                if (!response.TryGetProperty("status", out var statusProp) || statusProp.GetString() != "SUCCESS")
                {
                    _logger.LogWarning("BPM API 返回非成功狀態: {Status}", statusProp.GetString());
                    return null;
                }

                // 取得 processInfo
                if (!response.TryGetProperty("processInfo", out var processInfo))
                {
                    _logger.LogWarning("BPM 回應中沒有 processInfo");
                    return null;
                }

                // 取得申請人資訊
                var requesterName = GetJsonString(processInfo, "requesterName") ?? "未知";
                var requesterId = GetJsonString(processInfo, "requesterIdEmployeeId") ?? "";
                
                // 取得員工部門資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(requesterId);
                var department = employeeInfo?.DepartmentName ?? "未知";

                // 取得 formInfo
                string startDate = "", startTime = "", endDate = "", endTime = "", reason = "";
                
                if (response.TryGetProperty("formInfo", out var formInfo))
                {
                    // 根據不同表單類型解析對應的表單代碼資料
                    JsonElement formData = default;
                    
                    if (formType == "A") // 加班單
                    {
                        if (formInfo.TryGetProperty("PI_OVERTIME_001", out formData))
                        {
                            startDate = GetJsonString(formData, "applyDate")?.Replace("/", "-") ?? "";
                            startTime = GetJsonString(formData, "startTimeF", "startTime") ?? "";
                            endDate = GetJsonString(formData, "applyDate")?.Replace("/", "-") ?? "";
                            endTime = GetJsonString(formData, "endTimeF", "endTime") ?? "";
                            reason = GetJsonString(formData, "detail") ?? "";
                        }
                    }
                    else if (formType == "L") // 請假單
                    {
                        if (formInfo.TryGetProperty("PI_LEAVE_001", out formData))
                        {
                            startDate = GetJsonString(formData, "leaveStartDate", "startDate")?.Replace("/", "-") ?? "";
                            startTime = GetJsonString(formData, "leaveStartTime", "startTime") ?? "";
                            endDate = GetJsonString(formData, "leaveEndDate", "endDate")?.Replace("/", "-") ?? "";
                            endTime = GetJsonString(formData, "leaveEndTime", "endTime") ?? "";
                            reason = GetJsonString(formData, "leaveReason", "reason") ?? "";
                        }
                    }
                    else if (formType == "R") // 出勤確認單
                    {
                        if (formInfo.TryGetProperty("PI_ATTENDANCE_001", out formData) || 
                            formInfo.TryGetProperty("Attendance_Exception_001", out formData))
                        {
                            startDate = GetJsonString(formData, "applyDate", "attendanceDate")?.Replace("/", "-") ?? "";
                            startTime = GetJsonString(formData, "exceptionTime", "startTime") ?? "";
                            reason = GetJsonString(formData, "exceptionDescription", "detail") ?? "";
                        }
                    }
                    else if (formType == "O") // 外出外訓單
                    {
                        if (formInfo.TryGetProperty("PI_OUTING_001", out formData))
                        {
                            startDate = GetJsonString(formData, "outStartDate", "startDate")?.Replace("/", "-") ?? "";
                            startTime = GetJsonString(formData, "outStartTime", "startTime") ?? "";
                            endDate = GetJsonString(formData, "outEndDate", "endDate")?.Replace("/", "-") ?? "";
                            endTime = GetJsonString(formData, "outEndTime", "endTime") ?? "";
                            reason = GetJsonString(formData, "outReason", "reason") ?? "";
                        }
                    }
                    else if (formType == "T") // 出差單
                    {
                        if (formInfo.TryGetProperty("PI_TRIP_001", out formData) || 
                            formInfo.TryGetProperty("PI_BUSINESSTRIP_001", out formData))
                        {
                            startDate = GetJsonString(formData, "tripStartDate", "startDate")?.Replace("/", "-") ?? "";
                            startTime = GetJsonString(formData, "tripStartTime", "startTime") ?? "";
                            endDate = GetJsonString(formData, "tripEndDate", "endDate")?.Replace("/", "-") ?? "";
                            endTime = GetJsonString(formData, "tripEndTime", "endTime") ?? "";
                            reason = GetJsonString(formData, "tripReason", "destination", "reason") ?? "";
                        }
                    }
                }

                _logger.LogInformation("成功解析表單: {SerialNumber}, 申請人: {Name}, 起始日期: {StartDate}", 
                    serialNumber, requesterName, startDate);

                return new ReviewListItem
                {
                    Uid = requesterId?.Trim() ?? "",
                    UName = requesterName?.Trim() ?? "",
                    UDepartment = department?.Trim() ?? "",
                    FormIdTitle = "表單編號",
                    FormId = serialNumber?.Trim() ?? "",
                    EFormTypeTitle = "申請類別",
                    EFormType = formType?.Trim() ?? "",
                    EFormName = GetFormTypeName(formType ?? "L")?.Trim() ?? "",
                    EStartTitle = formType == "R" ? "上班時間" : "起始時間",
                    EStartDate = startDate?.Trim() ?? "",
                    EStartTime = startTime?.Trim() ?? "",
                    EEndTitle = formType == "R" ? "" : "結束時間",
                    EEndDate = formType == "R" ? "" : (endDate?.Trim() ?? ""),
                    EEndTime = formType == "R" ? "" : (endTime?.Trim() ?? ""),
                    EReasonTitle = "事由",
                    EReason = reason?.Trim() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得表單詳細資料失敗: {SerialNumber}, 錯誤: {Message}", serialNumber, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 根據表單類型取得對應的 ProcessCode, FormCode 和 FormVersion
        /// </summary>
        private (string processCode, string formCode, string formVersion) GetProcessCodeFromFormType(string formType)
        {
            return formType switch
            {
                "L" => ("PI_LEAVE_001_PROCESS", "PI_LEAVE_001", "1.0.0"), // 請假單
                "A" => ("PI_OVERTIME_001_PROCESS", "PI_OVERTIME_001", "1.0.0"), // 加班單
                "R" => ("PI_ATTENDANCE_001_PROCESS", "Attendance_Exception_001", "1.0.0"), // 出勤確認單
                "T" => ("PI_BUSINESSTRIP_001_PROCESS", "PI_BUSINESSTRIP_001", "1.0.0"), // 出差單
                "O" => ("PI_OUTING_001_PROCESS", "PI_OUTING_001", "1.0.0"), // 外出外訓單
                "D" => ("PI_CANCELLEAVE_001_PROCESS", "PI_CANCELLEAVE_001", "1.0.0"), // 銷假單
                _ => ("PI_LEAVE_001_PROCESS", "PI_LEAVE_001", "1.0.0") // 預設為請假單
            };
        }

        /// <summary>
        /// 根據表單類型取得對應的 processCode
        /// </summary>
        private string GetProcessCodeByFormType(string formType)
        {
            return formType switch
            {
                "L" => "PI_LEAVE_001_PROCESS",
                "D" => "PI_CANCELLEAVE_001_PROCESS",
                "O" => "PI_OUTING_001_PROCESS",
                "A" => "PI_OVERTIME_001_PROCESS",
                "R" => "PI_ATTENDANCE_001_PROCESS",
                "T" => "PI_BUSINESSTRIP_001_PROCESS",
                _ => "UNKNOWN_PROCESS"
            };
        }

        /// <summary>
        /// 從 JsonElement 取得字串值（嘗試多個可能的屬性名稱）
        /// </summary>
        private string? GetJsonString(JsonElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString()?.Trim();
                }
            }
            return null;
        }

        /// <summary>
        /// 解析表單詳細資料（從 BPM sync-process-info 回應）
        /// </summary>
        private async Task<ReviewDetailData?> ParseFormDetail(string formId, string formType, string responseJson)
        {
            try
            {
                var syncResponse = JsonSerializer.Deserialize<JsonElement>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 檢查 BPM 回應狀態
                if (!syncResponse.TryGetProperty("status", out var statusElement) || 
                    statusElement.GetString() != "SUCCESS")
                {
                    var errorMsg = syncResponse.TryGetProperty("message", out var msgElement) 
                        ? msgElement.GetString() 
                        : "BPM API 回應失敗";
                    _logger.LogError("BPM API 回應失敗: {Message}", errorMsg);
                    return null;
                }

                // 取得表單資料和流程資料
                JsonElement formInfo = default;
                JsonElement processInfo = default;
                
                if (syncResponse.TryGetProperty("formInfo", out var formInfoElement))
                {
                    formInfo = formInfoElement;
                }
                
                if (syncResponse.TryGetProperty("processInfo", out var processInfoElement))
                {
                    processInfo = processInfoElement;
                }

                // 取得申請人資訊
                var applicantId = "";
                if (processInfo.ValueKind != JsonValueKind.Undefined && 
                    processInfo.TryGetProperty("requesterIdEmployeeId", out var requesterIdElement))
                {
                    applicantId = requesterIdElement.GetString() ?? "";
                }

                // 取得實際的表單資料 (第一個 property 就是表單資料)
                JsonElement formData = default;
                if (formInfo.ValueKind != JsonValueKind.Undefined)
                {
                    foreach (var prop in formInfo.EnumerateObject())
                    {
                        formData = prop.Value;
                        break; // 只取第一個（應該只有一個表單資料）
                    }
                }
                
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(applicantId);
                var userName = employeeInfo?.EmployeeName ?? TryGetStringValue(formData, "applierName", "requesterName") ?? "未知";
                var userDepartment = employeeInfo?.DepartmentName ?? TryGetStringValue(formData, "applierDeptName", "departmentName", "orgName") ?? "未知";

                // 建立回應資料
                var detailData = new ReviewDetailData
                {
                    Uid = applicantId?.Trim() ?? "",
                    UName = userName?.Trim() ?? "",
                    UDepartment = userDepartment?.Trim() ?? "",
                    FormIdTitle = "表單編號",
                    FormId = formId?.Trim() ?? "",
                    EFormTypeTitle = "申請類別",
                    EFormType = formType?.Trim() ?? "",
                    EFormName = GetFormTypeName(formType ?? "L")?.Trim() ?? ""
                };

                // 設定時間標題
                if (formType == "R") // 出勤確認單
                {
                    detailData.EStartTitle = "上班時間";
                    detailData.EEndTitle = "";
                }
                else
                {
                    detailData.EStartTitle = "起始時間";
                    detailData.EEndTitle = "結束時間";
                }

                // 取得表單欄位資料
                // 起始日期和時間
                detailData.EStartDate = FormatDate(TryGetStringValue(formData, 
                    "startDate", "applyDate", "leaveStartDate", "overtimeDate", 
                    "exceptionDate", "tripStartDate", "businessTripStartDate", "eventDate") ?? "");
                
                detailData.EStartTime = TryGetStringValue(formData, 
                    "startTime", "leaveStartTime", "overtimeStartTime", 
                    "exceptionTime", "tripStartTime") ?? "";

                // 結束日期和時間（出勤確認單不需要）
                if (formType != "R")
                {
                    detailData.EEndDate = FormatDate(TryGetStringValue(formData, 
                        "endDate", "leaveEndDate", "overtimeEndDate", 
                        "exceptionEndTime", "tripEndDate", "businessTripEndDate") ?? "");
                    
                    detailData.EEndTime = TryGetStringValue(formData, 
                        "endTime", "leaveEndTime", "overtimeEndTime", "tripEndTime") ?? "";
                }

                // 事由
                detailData.EReasonTitle = "事由";
                detailData.EReason = TryGetStringValue(formData, 
                    "reason", "leaveReason", "overtimeReason", "exceptionDescription", 
                    "tripPurpose", "businessTripPurpose", "description") ?? "";

                // 代理人
                detailData.EAgentTitle = "代理人";
                detailData.EAgent = TryGetStringValue(formData, 
                    "substituteId", "agentNo", "agentId", "agent", "delegateId", "delegate") ?? "";

                // 處理附件
                var attachments = new List<ReviewAttachment>();
                if (formData.TryGetProperty("attachments", out var attachmentsElement) && 
                    attachmentsElement.ValueKind == JsonValueKind.Array)
                {
                    var index = 1;
                    foreach (var attachment in attachmentsElement.EnumerateArray())
                    {
                        var fileName = TryGetStringValue(attachment, 
                            "fileName", "name", "displayName") ?? $"附件{index}";
                        var originalFileName = TryGetStringValue(attachment, 
                            "originalFileName", "originalName", "fileName") ?? $"file{index}.pdf";
                        var fileUrl = TryGetStringValue(attachment, 
                            "fileUrl", "url", "path", "downloadUrl") ?? "";

                        attachments.Add(new ReviewAttachment
                        {
                            EFileId = index.ToString().Trim(),
                            EFileName = fileName?.Trim() ?? "",
                            ESFileName = originalFileName?.Trim() ?? "",
                            EFileUrl = fileUrl?.Trim() ?? ""
                        });
                        index++;
                    }
                }
                // 也檢查 filePath 欄位（可能是用 || 分隔的多個檔案路徑）
                else if (formData.TryGetProperty("filePath", out var filePathElement))
                {
                    var filePath = filePathElement.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        var files = filePath.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < files.Length; i++)
                        {
                            var file = files[i].Trim();
                            var fileName = System.IO.Path.GetFileName(file);
                            attachments.Add(new ReviewAttachment
                            {
                                EFileId = (i + 1).ToString(),
                                EFileName = fileName,
                                ESFileName = fileName,
                                EFileUrl = file
                            });
                        }
                    }
                }

                detailData.Attachments = attachments;
                detailData.EFileType = attachments.Any() ? "C" : "";

                // 處理表單流程（從 processInfo.actInstanceInfos 取得）
                var formFlows = new List<ReviewFormFlow>();
                
                if (processInfo.ValueKind != JsonValueKind.Undefined &&
                    processInfo.TryGetProperty("actInstanceInfos", out var actInstanceInfos) && 
                    actInstanceInfos.ValueKind == JsonValueKind.Array)
                {
                    foreach (var activity in actInstanceInfos.EnumerateArray())
                    {
                        var activityName = TryGetStringValue(activity, "activityName") ?? "";
                        var state = TryGetStringValue(activity, "state") ?? "";
                        var performerName = TryGetStringValue(activity, "performerName") ?? "";
                        var comment = TryGetStringValue(activity, "comment") ?? "";
                        
                        // 組合工作項目描述
                        var workItem = activityName;
                        if (!string.IsNullOrEmpty(performerName) && !performerName.Contains("AutoAgent"))
                        {
                            workItem = $"{activityName} - {performerName}";
                        }
                        if (!string.IsNullOrEmpty(comment))
                        {
                            workItem += $" ({comment})";
                        }
                        
                        // 轉換狀態為中文
                        var workStatus = ConvertBpmStateToChinese(state);

                        formFlows.Add(new ReviewFormFlow
                        {
                            WorkItem = workItem?.Trim() ?? "",
                            WorkStatus = workStatus?.Trim() ?? ""
                        });
                    }
                }
                
                // 如果沒有流程資料，至少加入表單已送出的記錄
                if (!formFlows.Any())
                {
                    formFlows.Add(new ReviewFormFlow
                    {
                        WorkItem = "表單已送出",
                        WorkStatus = "已完成"
                    });
                }

                detailData.EFormFlow = formFlows;

                _logger.LogInformation("成功解析表單詳細資料，FormId: {FormId}, 附件數: {AttachmentCount}, 流程數: {FlowCount}",
                    formId, attachments.Count, formFlows.Count);

                return detailData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析表單詳細資料失敗: {FormId}", formId);
                return null;
            }
        }

        /// <summary>
        /// 嘗試從 JsonElement 中取得字串值（支援多個可能的屬性名稱）
        /// </summary>
        private string? TryGetStringValue(JsonElement element, params string[] propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            foreach (var propertyName in propertyNames)
            {
                if (element.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                    else if (prop.ValueKind != JsonValueKind.Null)
                    {
                        var stringValue = prop.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(stringValue))
                        {
                            return stringValue;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 格式化日期
        /// </summary>
        private string FormatDate(string? date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return "";

            var cleanDate = date.Replace("/", "-")
                                .Replace(".", "-")
                                .Replace(" ", "");

            return cleanDate;
        }

        /// <summary>
        /// 將 BPM 狀態轉換為中文
        /// </summary>
        private string ConvertBpmStateToChinese(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return "未完成";

            // BPM 狀態格式: open.running, closed.completed, open.running.not_performed 等
            if (state.Contains("closed.completed"))
                return "已完成";
            else if (state.Contains("closed.aborted") || state.Contains("closed.terminated"))
                return "已中止";
            else if (state.Contains("open.running"))
                return "進行中";
            else if (state.Contains("not_performed"))
                return "未完成";
            else if (state.Contains("open"))
                return "未完成";
            else
                return state;
        }

        #endregion
    }
}
