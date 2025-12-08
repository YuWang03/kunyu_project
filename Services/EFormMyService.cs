using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 我的表單服務實作
    /// </summary>
    public class EFormMyService : IEFormMyService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<EFormMyService> _logger;

        public EFormMyService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            ILogger<EFormMyService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        /// <summary>
        /// 取得我的表單列表（從代辦事項中取得）
        /// </summary>
        public async Task<EFormMyResponse> GetMyFormsAsync(EFormMyRequest request)
        {
            try
            {
                _logger.LogInformation("開始取得我的表單列表，UID: {Uid}", request.Uid);

                // 1. 從 BPM 取得待辦事項（代辦事項）
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
                    return new EFormMyResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，無法解析 BPM 回應資料"
                    };
                }

                if (bpmResponse.Status != "SUCCESS")
                {
                    _logger.LogWarning("BPM API 返回非成功狀態: Status={Status}, Message={Message}", 
                        bpmResponse.Status, bpmResponse.Message);
                    return new EFormMyResponse
                    {
                        Code = "203",
                        Msg = $"BPM API 失敗: {bpmResponse.Message}"
                    };
                }

                if (bpmResponse.WorkItems == null || !bpmResponse.WorkItems.Any())
                {
                    _logger.LogInformation("目前沒有待辦事項，WorkItems 為空或 null");
                    return new EFormMyResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new EFormMyData
                        {
                            EFormData = new List<EFormMyItem>()
                        }
                    };
                }

                _logger.LogInformation("找到 {Count} 個待辦事項", bpmResponse.WorkItems.Count);

                // 2. 取得員工基本資料
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                var userName = employeeInfo?.EmployeeName ?? "未知";
                var userDepartment = employeeInfo?.DepartmentName ?? "未知";

                // 3. 整理我的表單列表
                var myFormItems = new List<EFormMyItem>();
                
                foreach (var workItem in bpmResponse.WorkItems)
                {
                    try
                    {
                        _logger.LogInformation("處理待辦事項: {ProcessSerialNumber}", workItem.ProcessSerialNumber);
                        
                        // 解析表單編號取得表單類型
                        var formType = ParseFormType(workItem.ProcessSerialNumber);
                        _logger.LogDebug("表單類型: {FormType}", formType);
                        
                        var formDetail = await GetFormDetailBySerialNumber(
                            workItem.ProcessSerialNumber, 
                            formType, 
                            userName, 
                            userDepartment);

                        if (formDetail != null)
                        {
                            myFormItems.Add(formDetail);
                            _logger.LogInformation("成功新增我的表單: {FormId}", formDetail.FormId);
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

                _logger.LogInformation("共處理了 {Count} 個我的表單", myFormItems.Count);

                return new EFormMyResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new EFormMyData
                    {
                        EFormData = myFormItems
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得我的表單列表時發生錯誤");
                return new EFormMyResponse
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
        /// 根據表單序號取得表單詳細資料（用於列表）
        /// </summary>
        private async Task<EFormMyItem?> GetFormDetailBySerialNumber(
            string serialNumber, 
            string formType, 
            string userName, 
            string userDepartment)
        {
            try
            {
                _logger.LogInformation("取得表單詳細資料: {SerialNumber}", serialNumber);
                
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

                // 建立我的表單項目
                var item = new EFormMyItem
                {
                    UName = userName,
                    UDepartment = userDepartment,
                    FormId = serialNumber,
                    EFormType = formType,
                    EFormName = GetFormTypeName(formType)
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

                _logger.LogDebug("成功建立表單項目: {@Item}", item);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得表單詳細資料失敗: {SerialNumber}", serialNumber);
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
