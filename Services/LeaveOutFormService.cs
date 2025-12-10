using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 外出外訓申請單服務實作（BPM 整合）
    /// </summary>
    public class LeaveOutFormService : ILeaveOutFormService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<LeaveOutFormService> _logger;
        private const string PROCESS_CODE = "PI_Trainning_PROCESS"; // 外出外訓流程代碼
        private const string FORM_CODE = "PI_Trainning"; // 外出外訓表單代碼

        public LeaveOutFormService(
            BpmService bpmService,
            FtpService ftpService,
            IBasicInfoService basicInfoService,
            ILogger<LeaveOutFormService> logger)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        #region 申請外出外訓單

        /// <summary>
        /// 申請外出外訓單
        /// </summary>
        public async Task<LeaveOutFormOperationResult> CreateLeaveOutFormAsync(CreateLeaveOutFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理外出外訓申請，員工工號: {Uid}, 類型: {Type}", request.Uid, request.Etype);

                // 1. 驗證必填欄位
                var validationResult = ValidateCreateRequest(request);
                if (!string.IsNullOrEmpty(validationResult))
                {
                    _logger.LogWarning("外出外訓申請驗證失敗: {Message}", validationResult);
                    return new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = validationResult
                    };
                }

                // 2. 驗證日期時間格式
                if (!ValidateDateTimeFormat(request, out string dateTimeError))
                {
                    _logger.LogWarning("日期時間格式驗證失敗: {Message}", dateTimeError);
                    return new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = dateTimeError
                    };
                }

                // 3. 查詢員工基本資料
                EmployeeBasicInfo? employeeInfo = null;
                try
                {
                    employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid!);
                    if (employeeInfo == null)
                    {
                        _logger.LogWarning("找不到員工資料，工號: {Uid}", request.Uid);
                        return new LeaveOutFormOperationResult
                        {
                            Code = "203",
                            Msg = $"找不到員工資料，工號: {request.Uid}"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "查詢員工資料失敗，工號: {Uid}", request.Uid);
                    return new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = "查詢員工資料失敗"
                    };
                }

                // 4. 處理附件
                string? hdnFilePath = null;
                
                // 優先使用 efileurl（從附件上傳 API 取得的實際 URL）
                if (!string.IsNullOrEmpty(request.Efileurl))
                {
                    hdnFilePath = request.Efileurl;
                    _logger.LogInformation("使用上傳的 hdnFilePath: {FilePath}", hdnFilePath);
                }
                // 如果沒有 efileurl 但有 efileid，則根據 efileid 構建 FTP 路徑（向後相容性）
                else if (request.Efileid != null && request.Efileid.Any())
                {
                    _logger.LogInformation("處理附件，共 {Count} 個附件", request.Efileid.Count);
                    // 將附件 ID 轉換為 FTP 路徑格式
                    // 格式: FTPTest~~/FTPShare/filename
                    var ftpPaths = request.Efileid.Select(id => $"FTPTest~~/FTPShare/leaveout_{id}.pdf").ToList();
                    hdnFilePath = string.Join("||", ftpPaths);
                    _logger.LogInformation("構建的 hdnFilePath: {FilePath}", hdnFilePath);
                }

                // 5. 建構 BPM API 請求資料
                var bpmRequest = BuildBpmCreateRequest(request, employeeInfo, hdnFilePath);

                // 6. 呼叫 BPM API 送出申請
                try
                {
                    _logger.LogInformation("呼叫 BPM API，請求資料: {@BpmRequest}", bpmRequest);
                    var response = await _bpmService.PostAsync("bpm/invoke-process", bpmRequest);
                    var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                    
                    _logger.LogInformation("BPM API 回應: {@Response}", jsonResponse);
                    
                    // 檢查 status 欄位
                    if (jsonResponse.TryGetProperty("status", out var statusProp))
                    {
                        var status = statusProp.GetString();
                        if (status == "SUCCESS")
                        {
                            // 成功時取得 bpmProcessOid 和 processSerialNo
                            var formId = jsonResponse.TryGetProperty("bpmProcessOid", out var oidProp) 
                                ? oidProp.GetString() 
                                : null;
                            var serialNo = jsonResponse.TryGetProperty("processSerialNo", out var serialProp) 
                                ? serialProp.GetString() 
                                : null;
                            
                            _logger.LogInformation("外出外訓申請成功，表單ID: {FormId}, 流水號: {SerialNo}", formId, serialNo);
                            
                            return new LeaveOutFormOperationResult
                            {
                                Code = "200",
                                Msg = "請求成功",
                                FormId = formId ?? serialNo
                            };
                        }
                        else
                        {
                            // 失敗時取得錯誤訊息
                            var errorMsg = jsonResponse.TryGetProperty("message", out var msgProp) 
                                ? msgProp.GetString() 
                                : "未知錯誤";
                            
                            _logger.LogWarning("外出外訓申請失敗: {Message}", errorMsg);
                            return new LeaveOutFormOperationResult
                            {
                                Code = "203",
                                Msg = $"請求失敗，{errorMsg}"
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("BPM API 回應格式異常: {@Response}", jsonResponse);
                        return new LeaveOutFormOperationResult
                        {
                            Code = "203",
                            Msg = "BPM API 回應格式異常"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "呼叫 BPM API 失敗");
                    return new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = $"送出申請失敗: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理外出外訓申請時發生未預期的錯誤");
                return new LeaveOutFormOperationResult
                {
                    Code = "203",
                    Msg = "系統錯誤，請聯絡管理員"
                };
            }
        }

        #endregion

        #region 查詢相關

        /// <summary>
        /// 查詢外出外訓單記錄
        /// </summary>
        public async Task<List<LeaveOutFormRecord>> GetLeaveOutFormsAsync(string employeeNo, string? startDate = null, string? endDate = null)
        {
            try
            {
                _logger.LogInformation("查詢外出外訓單記錄，員工工號: {EmployeeNo}", employeeNo);

                var queryParams = new List<string>
                {
                    $"employeeNo={Uri.EscapeDataString(employeeNo)}"
                };

                if (!string.IsNullOrEmpty(startDate))
                    queryParams.Add($"startDate={Uri.EscapeDataString(startDate)}");
                
                if (!string.IsNullOrEmpty(endDate))
                    queryParams.Add($"endDate={Uri.EscapeDataString(endDate)}");

                var endpoint = $"forms/{FORM_CODE}?{string.Join("&", queryParams)}";
                var response = await _bpmService.GetAsync(endpoint);

                if (response == null)
                    return new List<LeaveOutFormRecord>();

                // 解析回應資料
                var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                var records = ParseLeaveOutFormRecords(jsonResponse);
                _logger.LogInformation("查詢到 {Count} 筆外出外訓單記錄", records.Count);

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢外出外訓單記錄失敗");
                return new List<LeaveOutFormRecord>();
            }
        }

        /// <summary>
        /// 查詢單一外出外訓單詳情
        /// </summary>
        public async Task<LeaveOutFormRecord?> GetLeaveOutFormByIdAsync(string formId)
        {
            try
            {
                _logger.LogInformation("查詢外出外訓單詳情，表單ID: {FormId}", formId);

                var endpoint = $"forms/{FORM_CODE}/{formId}";
                var response = await _bpmService.GetAsync(endpoint);

                if (response == null)
                    return null;

                // 解析單筆資料
                var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                var record = ParseLeaveOutFormRecord(jsonResponse);
                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢外出外訓單詳情失敗，表單ID: {FormId}", formId);
                return null;
            }
        }

        /// <summary>
        /// 取消外出外訓單
        /// </summary>
        public async Task<LeaveOutFormOperationResult> CancelLeaveOutFormAsync(string formId, string cancelReason)
        {
            try
            {
                _logger.LogInformation("取消外出外訓單，表單ID: {FormId}, 原因: {Reason}", formId, cancelReason);

                var cancelRequest = new Dictionary<string, object>
                {
                    { "formId", formId },
                    { "cancelReason", cancelReason },
                    { "cancelledAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                var response = await _bpmService.PostAsync($"bpm/cancel-form?formId={formId}", cancelRequest);
                var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);

                if (jsonResponse.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    return new LeaveOutFormOperationResult
                    {
                        Code = "200",
                        Msg = "取消成功"
                    };
                }
                else
                {
                    return new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = "取消失敗"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消外出外訓單失敗");
                return new LeaveOutFormOperationResult
                {
                    Code = "203",
                    Msg = $"取消失敗: {ex.Message}"
                };
            }
        }

        #endregion

        #region 私有方法 - 驗證

        /// <summary>
        /// 驗證必填欄位
        /// </summary>
        private string ValidateCreateRequest(CreateLeaveOutFormRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Tokenid))
                return "tokenid 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Cid))
                return "cid 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Uid))
                return "uid 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Etype))
                return "etype 為必填";
            
            if (request.Etype != "A" && request.Etype != "B")
                return "etype 必須為 A(外出) 或 B(外訓)";
            
            if (string.IsNullOrWhiteSpace(request.Edate))
                return "edate 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Estarttime))
                return "estarttime 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Eendtime))
                return "eendtime 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Elocation))
                return "elocation 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Ereason))
                return "ereason 為必填";
            
            if (string.IsNullOrWhiteSpace(request.Ereturncompany))
                return "ereturncompany 為必填";
            
            if (request.Ereturncompany != "T" && request.Ereturncompany != "F")
                return "ereturncompany 必須為 T(是) 或 F(否)";

            return string.Empty;
        }

        /// <summary>
        /// 驗證日期時間格式
        /// </summary>
        private bool ValidateDateTimeFormat(CreateLeaveOutFormRequest request, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 驗證日期格式 (yyyy-MM-dd)
            if (!DateTime.TryParseExact(request.Edate, "yyyy-MM-dd", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out _))
            {
                errorMessage = "edate 格式錯誤，必須為 yyyy-MM-dd";
                return false;
            }

            // 驗證起始時間格式 (HH:mm)
            if (!TimeSpan.TryParseExact(request.Estarttime, "hh\\:mm", 
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "estarttime 格式錯誤，必須為 HH:mm";
                return false;
            }

            // 驗證截止時間格式 (HH:mm)
            if (!TimeSpan.TryParseExact(request.Eendtime, "hh\\:mm", 
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "eendtime 格式錯誤，必須為 HH:mm";
                return false;
            }

            // 驗證時間邏輯（截止時間應大於起始時間）
            var startTime = TimeSpan.Parse(request.Estarttime!);
            var endTime = TimeSpan.Parse(request.Eendtime!);
            if (endTime <= startTime)
            {
                errorMessage = "截止時間必須大於起始時間";
                return false;
            }

            return true;
        }

        #endregion

        #region 私有方法 - 資料轉換

        /// <summary>
        /// 建構 BPM API 請求資料
        /// </summary>
        private Dictionary<string, object> BuildBpmCreateRequest(
            CreateLeaveOutFormRequest request, 
            EmployeeBasicInfo employeeInfo,
            string? hdnFilePath)
        {
            // 轉換日期格式從 yyyy-MM-dd 到 yyyy/MM/dd
            var formattedDate = DateTime.Parse(request.Edate!).ToString("yyyy/MM/dd");
            
            // 判斷類型：A=外出, B=外訓
            var category = request.Etype == "A" ? "外出" : "外訓";
            
            // 轉換是否返回公司：T=Y, F=N
            var isReturn = request.Ereturncompany == "T" ? "Y" : "N";
            
            // 組裝表單資料（符合 PI_Trainning 格式）
            var formData = new Dictionary<string, object>
            {
                { "category", category },
                { "applyDate", formattedDate },
                { "startTime", request.Estarttime! },
                { "endTime", request.Eendtime! },
                { "applyPlace", request.Elocation! },
                { "applyReason", request.Ereason! },
                { "isReturn", isReturn }
            };

            // 組裝 formDataMap（使用正確的表單代碼）
            var formDataMap = new Dictionary<string, object>
            {
                { FORM_CODE, formData }
            };

            // 組裝 BPM 請求
            var formTypeName = category;
            var bpmRequest = new Dictionary<string, object>
            {
                { "processCode", PROCESS_CODE },
                { "formDataMap", formDataMap },
                { "userId", request.Uid! },
                { "subject", $"{formTypeName}申請 - {request.Edate} {request.Estarttime}~{request.Eendtime}" },
                { "sourceSystem", "HRSystemAPI" },
                { "environment", "TEST" },
                { "hasAttachments", !string.IsNullOrEmpty(hdnFilePath) }
            };

            // 如果有附件路徑，加入 hdnFilePath 和 filePath
            if (!string.IsNullOrEmpty(hdnFilePath))
            {
                bpmRequest.Add("hdnFilePath", hdnFilePath);
                bpmRequest.Add("filePath", hdnFilePath);  // 同時提供 filePath
            }

            return bpmRequest;
        }

        /// <summary>
        /// 解析外出外訓單記錄清單
        /// </summary>
        private List<LeaveOutFormRecord> ParseLeaveOutFormRecords(System.Text.Json.JsonElement response)
        {
            var records = new List<LeaveOutFormRecord>();

            try
            {
                if (response.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            var record = ParseLeaveOutFormRecordFromJson(item);
                            if (record != null)
                                records.Add(record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析外出外訓單記錄清單失敗");
            }

            return records;
        }

        /// <summary>
        /// 解析單筆外出外訓單記錄
        /// </summary>
        private LeaveOutFormRecord? ParseLeaveOutFormRecord(System.Text.Json.JsonElement response)
        {
            try
            {
                if (response.TryGetProperty("data", out var dataElement))
                {
                    return ParseLeaveOutFormRecordFromJson(dataElement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析外出外訓單記錄失敗");
            }

            return null;
        }

        /// <summary>
        /// 從 JSON 元素解析外出外訓單記錄
        /// </summary>
        private LeaveOutFormRecord? ParseLeaveOutFormRecordFromJson(JsonElement jsonElement)
        {
            try
            {
                var record = new LeaveOutFormRecord
                {
                    FormId = jsonElement.GetProperty("formId").GetString() ?? "",
                    EmployeeNo = jsonElement.GetProperty("employeeNo").GetString() ?? "",
                    EmployeeName = jsonElement.GetProperty("employeeName").GetString() ?? "",
                    FormType = jsonElement.GetProperty("formType").GetString() ?? "",
                    FormTypeName = jsonElement.GetProperty("formTypeName").GetString() ?? "",
                    Date = jsonElement.GetProperty("leaveOutDate").GetString() ?? "",
                    StartTime = jsonElement.GetProperty("startTime").GetString() ?? "",
                    EndTime = jsonElement.GetProperty("endTime").GetString() ?? "",
                    Location = jsonElement.GetProperty("location").GetString() ?? "",
                    Reason = jsonElement.GetProperty("reason").GetString() ?? "",
                    ReturnToCompany = jsonElement.GetProperty("returnToCompany").GetBoolean(),
                    ApprovalStatus = jsonElement.GetProperty("status").GetString(),
                };

                // 解析附件檔案ID
                if (jsonElement.TryGetProperty("fileIds", out var fileIdsElement) && 
                    fileIdsElement.ValueKind == JsonValueKind.Array)
                {
                    record.FileIds = new List<string>();
                    foreach (var fileId in fileIdsElement.EnumerateArray())
                    {
                        var id = fileId.GetString();
                        if (!string.IsNullOrEmpty(id))
                            record.FileIds.Add(id);
                    }
                }

                // 解析時間戳記
                if (jsonElement.TryGetProperty("createdAt", out var createdAtElement))
                {
                    if (DateTime.TryParse(createdAtElement.GetString(), out var createdAt))
                        record.CreatedAt = createdAt;
                }

                if (jsonElement.TryGetProperty("updatedAt", out var updatedAtElement))
                {
                    if (DateTime.TryParse(updatedAtElement.GetString(), out var updatedAt))
                        record.UpdatedAt = updatedAt;
                }

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析外出外訓單記錄 JSON 失敗");
                return null;
            }
        }

        #endregion
    }
}
