using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 我的簽核列表服務實作
    /// </summary>
    public class EFormMySignService : IEFormMySignService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly IEFormApprovalRepository _approvalRepository;
        private readonly ILogger<EFormMySignService> _logger;

        public EFormMySignService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            IEFormApprovalRepository approvalRepository,
            ILogger<EFormMySignService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _approvalRepository = approvalRepository;
            _logger = logger;
        }

        /// <summary>
        /// 取得我的簽核列表（從数据库读取已签过的表单）
        /// </summary>
        public async Task<EFormMySignResponse> GetMySignFormsAsync(EFormMySignRequest request)
        {
            try
            {
                _logger.LogInformation("開始取得我的簽核列表（从数据库），UID: {Uid}", request.Uid);

                // 1. 解析分页参数
                if (!int.TryParse(request.Page, out var pageNumber) || pageNumber < 1)
                {
                    pageNumber = 1;
                }
                
                if (!int.TryParse(request.PageSize, out var pageSize) || pageSize < 1)
                {
                    pageSize = 20;
                }

                // 2. 从数据库获取签核记录
                var approvalRecords = await _approvalRepository.GetApprovalRecordsByUidAsync(
                    request.Uid, 
                    pageNumber, 
                    pageSize, 
                    request.EFormType);

                var totalCount = await _approvalRepository.GetApprovalRecordsCountAsync(
                    request.Uid, 
                    request.EFormType);

                _logger.LogInformation("从数据库查询到 {Count} 条签核记录，总数: {TotalCount}", 
                    approvalRecords.Count, totalCount);

                // 3. 转换为响应格式
                var mySignItems = new List<EFormMySignItem>();
                
                foreach (var record in approvalRecords)
                {
                    try
                    {
                        _logger.LogInformation("处理签核记录: FormId={FormId}, FormType={FormType}", 
                            record.EFormId, record.EFormType);
                        
                        var formDetail = await GetSignFormDetailFromRecord(record);

                        if (formDetail != null)
                        {
                            mySignItems.Add(formDetail);
                            _logger.LogInformation("成功添加签核记录: {FormId}", formDetail.FormId);
                        }
                        else
                        {
                            _logger.LogWarning("无法获取签核记录详细资料: FormId={FormId}", record.EFormId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理签核记录失败: FormId={FormId}", record.EFormId);
                    }
                }

                _logger.LogInformation("共处理了 {Count} 个签核记录", mySignItems.Count);

                // 4. 计算总页数
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                _logger.LogInformation("分页参数: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, TotalPages={TotalPages}", 
                    pageNumber, pageSize, totalCount, totalPages);

                return new EFormMySignResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new EFormMySignData
                    {
                        TotalCount = totalCount.ToString(),
                        Page = pageNumber.ToString(),
                        PageSize = pageSize.ToString(),
                        TotalPages = totalPages.ToString(),
                        EFormData = mySignItems
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得我的簽核列表時發生錯誤");
                return new EFormMySignResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 从数据库记录获取签核表单详细资料
        /// </summary>
        private async Task<EFormMySignItem?> GetSignFormDetailFromRecord(EFormApprovalRecord record)
        {
            try
            {
                _logger.LogInformation("从数据库记录获取表单详细资料: FormId={FormId}", record.EFormId);
                
                // 从 BPM API 获取表单详细信息
                var (processCode, formCode, formVersion) = GetProcessCodeFromFormType(record.EFormType);
                
                // 调用 BPM API 获取表单数据
                var endpoint = $"bpm/sync-process-info?processSerialNo={record.EFormId}&processCode={processCode}&environment=TEST&formCode={formCode}&formVersion={formVersion}";
                _logger.LogDebug("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogDebug("BPM 表單回應: {Response}", responseJson);

                // 解析回应
                var syncResponse = JsonSerializer.Deserialize<JsonElement>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 创建签核记录项目（使用数据库中的信息）
                var item = new EFormMySignItem
                {
                    UName = record.UName ?? "", // 签核者姓名
                    UDepartment = record.UDepartment ?? "", // 签核者部门
                    FormId = record.EFormId,
                    EFormType = record.EFormType,
                    EFormName = GetFormTypeName(record.EFormType),
                    SignAction = GetSignActionName(record.ApprovalStatus, record.ApprovalFlow), // 签核动作
                    SignDate = FormatDate(record.ApprovalDate.ToString("yyyy-MM-dd")) // 签核日期
                };

                // 根据表单类型设定标题
                if (record.EFormType == "R") // 出勤确认单
                {
                    item.EStartTitle = "上班時間";
                    item.EEndTitle = "";
                }
                else
                {
                    item.EStartTitle = "起始時間";
                    item.EEndTitle = "結束時間";
                }

                // 从 BPM API 获取表单数据
                JsonElement formData = default;
                if (syncResponse.TryGetProperty("status", out var statusElement) && 
                    statusElement.GetString() == "SUCCESS" &&
                    syncResponse.TryGetProperty("formInfo", out var formInfo))
                {
                    foreach (var prop in formInfo.EnumerateObject())
                    {
                        formData = prop.Value;
                        break;
                    }
                }

                if (formData.ValueKind != JsonValueKind.Undefined)
                {
                    // 获取申请人信息
                    var applicantUid = TryGetStringValue(formData, "applicantUid", "uid", "employeeId") ?? "";
                    var applicantInfo = await _basicInfoService.GetEmployeeByIdAsync(applicantUid);
                    item.ApplicantUName = applicantInfo?.EmployeeName ?? TryGetStringValue(formData, "applicantName", "uname", "employeeName") ?? "未知";
                    item.ApplicantDepartment = applicantInfo?.DepartmentName ?? TryGetStringValue(formData, "applicantDepartment", "department", "departmentName") ?? "未知";

                    // 尝试获取各种可能的字段名称
                    item.EStartDate = TryGetStringValue(formData, 
                        "startDate", "applyDate", "leaveStartDate", "overtimeDate", "exceptionDate", "tripStartDate") ?? "";
                    
                    item.EStartTime = TryGetStringValue(formData, 
                        "startTime", "leaveStartTime", "overtimeStartTime", "exceptionTime", "tripStartTime") ?? "";
                    
                    if (record.EFormType != "R")
                    {
                        item.EEndDate = TryGetStringValue(formData, 
                            "endDate", "leaveEndDate", "overtimeEndDate", "exceptionEndTime", "tripEndDate") ?? "";
                        
                        item.EEndTime = TryGetStringValue(formData, 
                            "endTime", "leaveEndTime", "overtimeEndTime", "tripEndTime") ?? "";
                    }

                    item.EReason = TryGetStringValue(formData, 
                        "reason", "leaveReason", "overtimeReason", "exceptionDescription", "tripPurpose", "description") ?? "";
                }

                // 格式化日期
                item.EStartDate = FormatDate(item.EStartDate);
                item.EEndDate = FormatDate(item.EEndDate);

                _logger.LogDebug("成功建立签核记录项目: {@Item}", item);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从数据库记录获取表单详细资料失败: FormId={FormId}", record.EFormId);
                
                // 即使 BPM API 失败，也返回基本信息
                return new EFormMySignItem
                {
                    UName = record.UName ?? "",
                    UDepartment = record.UDepartment ?? "",
                    FormId = record.EFormId,
                    EFormType = record.EFormType,
                    EFormName = GetFormTypeName(record.EFormType),
                    SignAction = GetSignActionName(record.ApprovalStatus, record.ApprovalFlow),
                    SignDate = FormatDate(record.ApprovalDate.ToString("yyyy-MM-dd")),
                    ApplicantUName = "未知",
                    ApplicantDepartment = "未知"
                };
            }
        }

        /// <summary>
        /// 根据签核状态和流程获取签核动作名称
        /// </summary>
        private string GetSignActionName(string approvalStatus, string approvalFlow)
        {
            if (approvalStatus == "Y")
            {
                return "核准";
            }
            else
            {
                return approvalFlow switch
                {
                    "R" => "退回",
                    "T" => "終止",
                    "J" => "駁回",
                    _ => "不同意"
                };
            }
        }

        /// <summary>
        /// 從表單編號解析表單類型
        /// </summary>
        private string ParseFormType(string formId)
        {
            // PI_MattOvertime_Process_HRM00000005 或 PI-HR-H1A-PKG-Test0000000000035 格式分析
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
        /// 根據表單序號取得簽核表單詳細資料（用於列表）
        /// </summary>
        private async Task<EFormMySignItem?> GetSignFormDetailBySerialNumber(
            string serialNumber, 
            string formType, 
            string signerName, 
            string signerDepartment,
            BpmWorkItem workItem)
        {
            try
            {
                _logger.LogInformation("取得簽核表單詳細資料: {SerialNumber}", serialNumber);
                
                // 取得 ProcessCode 和 FormCode
                var (processCode, formCode, formVersion) = GetProcessCodeFromFormType(formType);
                
                // 從 BPM API 取得表單資料（使用 sync-process-info）
                var endpoint = $"bpm/sync-process-info?processSerialNo={serialNumber}&processCode={processCode}&environment=TEST&formCode={formCode}&formVersion={formVersion}";
                _logger.LogDebug("呼叫 BPM API: {Endpoint}", endpoint);
                
                var responseJson = await _bpmService.GetAsync(endpoint);
                _logger.LogDebug("BPM 表單回應: {Response}", responseJson);

                // 解析回應
                var syncResponse = JsonSerializer.Deserialize<JsonElement>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 檢查回應狀態
                if (!syncResponse.TryGetProperty("status", out var statusElement) || 
                    statusElement.GetString() != "SUCCESS")
                {
                    _logger.LogWarning("BPM API 回應失敗: {SerialNumber}", serialNumber);
                    return null;
                }

                // 取得表單資料
                JsonElement formData = default;
                if (syncResponse.TryGetProperty("formInfo", out var formInfo))
                {
                    // formInfo 下面會有 formCode 作為 key
                    foreach (var prop in formInfo.EnumerateObject())
                    {
                        formData = prop.Value;
                        break; // 只取第一個
                    }
                }
                
                if (formData.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("無法取得表單資料: {SerialNumber}", serialNumber);
                    return null;
                }

                // 取得申請人資訊
                var applicantUid = TryGetStringValue(formData, "applicantUid", "uid", "employeeId") ?? "";
                var applicantInfo = await _basicInfoService.GetEmployeeByIdAsync(applicantUid);
                var applicantName = applicantInfo?.EmployeeName ?? TryGetStringValue(formData, "applicantName", "uname", "employeeName") ?? "未知";
                var applicantDepartment = applicantInfo?.DepartmentName ?? TryGetStringValue(formData, "applicantDepartment", "department", "departmentName") ?? "未知";

                // 取得簽核資訊（從表單資料中）
                var signAction = TryGetStringValue(formData, "approvalAction", "action", "signAction") ?? "已簽核";
                var signDate = TryGetStringValue(formData, "approvalDate", "completedDate", "signDate") ?? "";

                // 建立我的簽核記錄項目
                var item = new EFormMySignItem
                {
                    UName = signerName, // 簽核者姓名（當前用戶）
                    UDepartment = signerDepartment, // 簽核者部門（當前用戶）
                    FormId = serialNumber,
                    EFormType = formType,
                    EFormName = GetFormTypeName(formType),
                    ApplicantUName = applicantName, // 申請人姓名
                    ApplicantDepartment = applicantDepartment, // 申請人部門
                    SignAction = signAction, // 簽核動作（核准、退回、駁回等）
                    SignDate = signDate // 簽核日期
                };

                // 根據表單類型設定標題和時間欄位
                if (formType == "R") // 出勤確認單
                {
                    item.EStartTitle = "上班時間";
                    item.EEndTitle = "";
                }
                else
                {
                    item.EStartTitle = "起始時間";
                    item.EEndTitle = "結束時間";
                }

                // 嘗試取得各種可能的欄位名稱
                // 起始日期
                item.EStartDate = TryGetStringValue(formData, 
                    "startDate", "applyDate", "leaveStartDate", "overtimeDate", "exceptionDate", "tripStartDate") ?? "";
                
                // 起始時間
                item.EStartTime = TryGetStringValue(formData, 
                    "startTime", "leaveStartTime", "overtimeStartTime", "exceptionTime", "tripStartTime") ?? "";
                
                // 結束日期
                if (formType != "R")
                {
                    item.EEndDate = TryGetStringValue(formData, 
                        "endDate", "leaveEndDate", "overtimeEndDate", "exceptionEndTime", "tripEndDate") ?? "";
                    
                    // 結束時間
                    item.EEndTime = TryGetStringValue(formData, 
                        "endTime", "leaveEndTime", "overtimeEndTime", "tripEndTime") ?? "";
                }

                // 事由
                item.EReason = TryGetStringValue(formData, 
                    "reason", "leaveReason", "overtimeReason", "exceptionDescription", "tripPurpose", "description") ?? "";

                // 格式化日期（移除斜線，統一格式）
                item.EStartDate = FormatDate(item.EStartDate);
                item.EEndDate = FormatDate(item.EEndDate);
                item.SignDate = FormatDate(item.SignDate);

                _logger.LogDebug("成功建立簽核記錄項目: {@Item}", item);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得簽核表單詳細資料失敗: {SerialNumber}", serialNumber);
                return null;
            }
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
        /// 格式化日期（移除斜線或其他分隔符號）
        /// </summary>
        private string FormatDate(string? date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return "";

            // 移除所有常見的日期分隔符號
            var cleanDate = date.Replace("/", "-")
                                .Replace(".", "-")
                                .Replace(" ", "");

            // 如果日期格式是 yyyy-MM-dd，保持這個格式
            // 如果日期格式是 yyyy/MM/dd，轉換為 yyyy-MM-dd
            return cleanDate;
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
