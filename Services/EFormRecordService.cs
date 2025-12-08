using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 簽核記錄詳細資料服務實作
    /// </summary>
    public class EFormRecordService : IEFormRecordService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<EFormRecordService> _logger;

        public EFormRecordService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            ILogger<EFormRecordService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        /// <summary>
        /// 取得簽核記錄詳細資料
        /// </summary>
        public async Task<EFormRecordResponse> GetFormRecordDetailAsync(EFormRecordRequest request)
        {
            try
            {
                _logger.LogInformation("開始取得簽核記錄詳細資料，UID: {Uid}, FormId: {FormId}", 
                    request.Uid, request.FormId);

                // 1. 解析表單類型以取得 processCode 和 formCode
                var formType = ParseFormType(request.FormId);
                var (processCode, formCode, formVersion) = GetProcessCodeFromFormType(formType);

                // 2. 從 BPM 取得表單詳細資料（使用 sync-process-info）
                var endpoint = $"bpm/sync-process-info?processSerialNo={request.FormId}&processCode={processCode}&environment=TEST&formCode={formCode}&formVersion={formVersion}";
                _logger.LogInformation("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogDebug("BPM API 回應: {Response}", responseJson);
                
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
                    return new EFormRecordResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                // 取得表單資料和流程資料
                JsonElement formInfo = default;
                JsonElement processInfo = default;
                
                if (syncResponse.TryGetProperty("formInfo", out var formInfoElement))
                {
                    // formInfo 下面會有 formCode 作為 key (例如 "PI_LEAVE_001")
                    formInfo = formInfoElement;
                }
                
                if (syncResponse.TryGetProperty("processInfo", out var processInfoElement))
                {
                    processInfo = processInfoElement;
                }

                // 3. 取得申請人資訊
                var applicantId = request.Uid;
                if (processInfo.ValueKind != JsonValueKind.Undefined && 
                    processInfo.TryGetProperty("requesterIdEmployeeId", out var requesterIdElement))
                {
                    applicantId = requesterIdElement.GetString() ?? request.Uid;
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
                
                _logger.LogDebug("申請人 ID: {ApplicantId}", applicantId);
                
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(applicantId);
                var userName = employeeInfo?.EmployeeName ?? TryGetStringValue(formData, "applierName", "requesterName") ?? "未知";
                var userDepartment = employeeInfo?.DepartmentName ?? TryGetStringValue(formData, "applierDeptName", "departmentName", "orgName") ?? "未知";

                _logger.LogDebug("表單類型: {FormType}", formType);

                // 4. 建立回應資料
                var recordData = new EFormRecordData
                {
                    Uid = applicantId,
                    UName = userName,
                    UDepartment = userDepartment,
                    FormId = request.FormId,
                    EFormType = formType,
                    EFormName = GetFormTypeName(formType)
                };

                // 5. 設定時間標題
                if (formType == "R") // 出勤確認單
                {
                    recordData.EStartTitle = "上班時間";
                    recordData.EEndTitle = "";
                }
                else
                {
                    recordData.EStartTitle = "起始時間";
                    recordData.EEndTitle = "結束時間";
                }

                // 6. 取得表單欄位資料
                // 起始日期和時間
                recordData.EStartDate = FormatDate(TryGetStringValue(formData, 
                    "startDate", "applyDate", "leaveStartDate", "overtimeDate", 
                    "exceptionDate", "tripStartDate", "businessTripStartDate") ?? "");
                
                recordData.EStartTime = TryGetStringValue(formData, 
                    "startTime", "leaveStartTime", "overtimeStartTime", 
                    "exceptionTime", "tripStartTime") ?? "";

                // 結束日期和時間（出勤確認單不需要）
                if (formType != "R")
                {
                    recordData.EEndDate = FormatDate(TryGetStringValue(formData, 
                        "endDate", "leaveEndDate", "overtimeEndDate", 
                        "exceptionEndTime", "tripEndDate", "businessTripEndDate") ?? "");
                    
                    recordData.EEndTime = TryGetStringValue(formData, 
                        "endTime", "leaveEndTime", "overtimeEndTime", "tripEndTime") ?? "";
                }

                // 事由
                recordData.EReason = TryGetStringValue(formData, 
                    "reason", "leaveReason", "overtimeReason", "exceptionDescription", 
                    "tripPurpose", "businessTripPurpose", "description") ?? "";

                // 代理人
                recordData.EAgent = TryGetStringValue(formData, 
                    "agentId", "agent", "delegateId", "delegate") ?? "";

                // 7. 處理附件
                var attachments = new List<EFormRecordAttachment>();
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

                        attachments.Add(new EFormRecordAttachment
                        {
                            EFileId = index.ToString(),
                            EFileName = fileName,
                            ESFileName = originalFileName,
                            EFileUrl = fileUrl
                        });
                        index++;
                    }
                }
                // 也檢查 filePath 欄位（可能是用 || 分隔的多個檔案路徑）
                else if (formData.TryGetProperty("filePath", out var filePathElement))
                {
                    var filePath = filePathElement.GetString();
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        var files = filePath.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < files.Length; i++)
                        {
                            var file = files[i];
                            var fileName = System.IO.Path.GetFileName(file);
                            attachments.Add(new EFormRecordAttachment
                            {
                                EFileId = (i + 1).ToString(),
                                EFileName = fileName,
                                ESFileName = fileName,
                                EFileUrl = file
                            });
                        }
                    }
                }

                recordData.Attachments = attachments;
                recordData.EFileType = attachments.Any() ? "C" : "";

                // 8. 處理表單流程（從 processInfo.actInstanceInfos 取得）
                var formFlows = new List<EFormRecordFlow>();
                
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

                        formFlows.Add(new EFormRecordFlow
                        {
                            WorkItem = workItem,
                            WorkStatus = workStatus
                        });
                    }
                }
                
                // 如果沒有流程資料，至少加入表單已送出的記錄
                if (!formFlows.Any())
                {
                    formFlows.Add(new EFormRecordFlow
                    {
                        WorkItem = "表單已送出",
                        WorkStatus = "已完成"
                    });
                }

                recordData.EFormFlow = formFlows;

                _logger.LogInformation("成功取得簽核記錄詳細資料，FormId: {FormId}, 附件數: {AttachmentCount}, 流程數: {FlowCount}",
                    request.FormId, attachments.Count, formFlows.Count);

                return new EFormRecordResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = recordData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得簽核記錄詳細資料時發生錯誤，FormId: {FormId}", request.FormId);
                return new EFormRecordResponse
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
            var upperFormId = formId.ToUpperInvariant();
            
            if (upperFormId.Contains("LEAVE") && !upperFormId.Contains("CANCEL"))
                return "L"; // 請假單
            else if (upperFormId.Contains("CANCEL"))
                return "D"; // 銷假單
            else if (upperFormId.Contains("OUT"))
                return "O"; // 外出外訓單
            else if (upperFormId.Contains("OVERTIME"))
                return "A"; // 加班單
            else if (upperFormId.Contains("ATTENDANCE") || upperFormId.Contains("EXCEPTION"))
                return "R"; // 出勤確認單
            else if (upperFormId.Contains("TRIP") || upperFormId.Contains("BUSINESS"))
                return "T"; // 出差單

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
        /// 嘗試從 JsonElement 中取得字串值（支援多個可能的屬性名稱）
        /// </summary>
        private string? TryGetStringValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (element.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                    else if (prop.ValueKind != JsonValueKind.Null)
                    {
                        return prop.ToString();
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

        /// <summary>
        /// 根據表單類型取得對應的 ProcessCode 和 FormCode
        /// </summary>
        private (string processCode, string formCode, string formVersion) GetProcessCodeFromFormType(string formType)
        {
            return formType switch
            {
                "L" => ("PI_LEAVE_001_PROCESS", "Leave_Application_001", "1.0.0"), // 請假單
                "A" => ("PI_OVERTIME_001_PROCESS", "Overtime_Application_001", "1.0.0"), // 加班單
                "R" => ("PI_ATTENDANCE_001_PROCESS", "Attendance_Exception_001", "1.0.0"), // 出勤確認單
                "T" => ("PI_TRIP_001_PROCESS", "Business_Trip_001", "1.0.0"), // 出差單
                "O" => ("PI_OUT_001_PROCESS", "Leave_Out_001", "1.0.0"), // 外出外訓單
                "D" => ("PI_CANCEL_001_PROCESS", "Cancel_Leave_001", "1.0.0"), // 銷假單
                _ => ("PI_LEAVE_001_PROCESS", "Leave_Application_001", "1.0.0") // 預設為請假單
            };
        }

        #endregion
    }
}