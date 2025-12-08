using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 待我審核服務實作
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            ILogger<ReviewService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
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

                return new ReviewListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new ReviewListData
                    {
                        EFormData = reviewItems
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
                
                // 2. 從 BPM 取得表單詳細資料
                var endpoint = $"bpm/forms/{request.FormId}";
                var responseJson = await _bpmService.GetAsync(endpoint);
                
                // TODO: 根據不同表單類型解析資料
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

                // TODO: 這裡應該要呼叫 BPM API 來執行真正的簽核
                // 目前先回傳模擬的成功回應用於測試
                _logger.LogInformation("簽核資料驗證成功，共 {Count} 筆待簽核項目", request.ApprovalData.Count);
                
                return new ReviewApprovalResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new ReviewApprovalResponseData
                    {
                        Status = "請求成功"
                    }
                };

                /* 
                // 未來正式版本應使用以下邏輯來實際呼叫 BPM API
                var successCount = 0;
                var failCount = 0;

                foreach (var approval in request.ApprovalData)
                {
                    try
                    {
                        _logger.LogInformation("處理簽核: FormType={FormType}, FormId={FormId}", 
                            approval.EFormType, approval.EFormId);

                        // 建立簽核請求
                        var approvalData = new
                        {
                            userId = request.Uid,
                            formId = approval.EFormId,
                            action = request.ApprovalStatus == "Y" ? "approve" : "reject",
                            comments = request.Comments,
                            flow = request.ApprovalFlow // S: 中止流程, R: 退回發起人
                        };

                        var endpoint = $"bpm/workitems/approve";
                        _logger.LogInformation("呼叫 BPM API: {Endpoint}, Data: {@ApprovalData}", endpoint, approvalData);
                        
                        var responseJson = await _bpmService.PostAsync(endpoint, approvalData);
                        
                        _logger.LogInformation("BPM API 回應: {Response}", responseJson);
                        _logger.LogInformation("簽核成功: {FormId}", approval.EFormId);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "簽核失敗: FormType={FormType}, FormId={FormId}, 錯誤訊息: {Message}", 
                            approval.EFormType, approval.EFormId, ex.Message);
                        failCount++;
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
                            Status = $"簽核成功 {successCount} 筆" + (failCount > 0 ? $"，失敗 {failCount} 筆" : "")
                        }
                    };
                }
                else
                {
                    _logger.LogError("所有簽核都失敗，總共 {TotalCount} 筆", request.ApprovalData.Count);
                    return new ReviewApprovalResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，所有簽核都失敗"
                    };
                }
                */
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
                    UName = requesterName,
                    UDepartment = department,
                    FormIdTitle = "表單編號",
                    FormId = serialNumber,
                    EFormTypeTitle = "申請類別",
                    EFormType = formType,
                    EFormName = GetFormTypeName(formType),
                    EStartTitle = formType == "R" ? "上班時間" : "起始時間",
                    EStartDate = startDate,
                    EStartTime = startTime,
                    EEndTitle = formType == "R" ? "" : "結束時間",
                    EEndDate = formType == "R" ? "" : endDate,
                    EEndTime = formType == "R" ? "" : endTime,
                    EReasonTitle = "事由",
                    EReason = reason
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得表單詳細資料失敗: {SerialNumber}, 錯誤: {Message}", serialNumber, ex.Message);
                return null;
            }
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
                    return prop.GetString();
                }
            }
            return null;
        }

        /// <summary>
        /// 解析表單詳細資料
        /// </summary>
        private async Task<ReviewDetailData?> ParseFormDetail(string formId, string formType, string responseJson)
        {
            try
            {
                var formData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // 取得申請人資訊
                var applicantId = formData.GetProperty("applicantId").GetString() ?? "";
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(applicantId);

                // 解析附件
                var attachments = new List<ReviewAttachment>();
                if (formData.TryGetProperty("attachments", out var attachmentsElement))
                {
                    var index = 1;
                    foreach (var attachment in attachmentsElement.EnumerateArray())
                    {
                        attachments.Add(new ReviewAttachment
                        {
                            EFileId = index.ToString(),
                            EFileName = attachment.GetProperty("fileName").GetString() ?? "",
                            ESFileName = attachment.GetProperty("originalFileName").GetString() ?? "",
                            EFileUrl = attachment.GetProperty("fileUrl").GetString() ?? ""
                        });
                        index++;
                    }
                }

                // 解析表單流程
                var formFlow = new List<ReviewFormFlow>();
                if (formData.TryGetProperty("approvalHistory", out var historyElement))
                {
                    foreach (var history in historyElement.EnumerateArray())
                    {
                        formFlow.Add(new ReviewFormFlow
                        {
                            WorkItem = history.GetProperty("workItem").GetString() ?? "",
                            WorkStatus = history.GetProperty("status").GetString() ?? ""
                        });
                    }
                }

                return new ReviewDetailData
                {
                    Uid = applicantId,
                    UName = employeeInfo?.EmployeeName ?? "未知",
                    UDepartment = employeeInfo?.DepartmentName ?? "未知",
                    FormId = formId,
                    EFormType = formType,
                    EFormName = GetFormTypeName(formType),
                    EStartTitle = formType == "R" ? "上班時間" : "起始時間",
                    EStartDate = formData.GetProperty("startDate").GetString() ?? "",
                    EStartTime = formData.GetProperty("startTime").GetString() ?? "",
                    EEndTitle = formType == "R" ? "" : "結束時間",
                    EEndDate = formType == "R" ? "" : (formData.GetProperty("endDate").GetString() ?? ""),
                    EEndTime = formType == "R" ? "" : (formData.GetProperty("endTime").GetString() ?? ""),
                    EReason = formData.GetProperty("reason").GetString() ?? "",
                    EAgent = formData.TryGetProperty("agentId", out var agentElement) ? agentElement.GetString() ?? "" : "",
                    EFileType = attachments.Any() ? "C" : "",
                    Attachments = attachments,
                    EFormFlow = formFlow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析表單詳細資料失敗: {FormId}", formId);
                return null;
            }
        }

        #endregion
    }
}
