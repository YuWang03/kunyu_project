using System.Text.Json;
using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using Dapper;

namespace HRSystemAPI.Services
{
    public class OvertimeFormService : IOvertimeFormService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<OvertimeFormService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private const string FORM_CODE = "PI_OVERTIME_001";
        private const string FORM_VERSION = "1.0.0";

        public OvertimeFormService(
            BpmService bpmService, 
            FtpService ftpService, 
            IBasicInfoService basicInfoService,
            ILogger<OvertimeFormService> logger,
            IConfiguration configuration)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _basicInfoService = basicInfoService;
            _logger = logger;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException("HRDatabase connection string not found");
        }

        public async Task<OvertimeFormOperationResult> CreateOvertimeFormAsync(CreateOvertimeFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始申請加班表單: {@Request}", new { request.Email, request.ApplyDate });

                // 1. 查詢員工基本資料
                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.Email);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到 Email 對應的員工資料: {request.Email}");
                }

                _logger.LogInformation("申請人資料 - 工號: {EmployeeNo}, 姓名: {Name}", employeeInfo.EmployeeNo, employeeInfo.EmployeeName);

                // 2. 呼叫表單預覽 API 取得自動填充欄位
                Dictionary<string, object?>? computedData = null;
                try
                {
                    var previewEndpoint = $"form-preview/preview?formCode={FORM_CODE}&version={FORM_VERSION}";
                    
                    // 建立完整的表單資料用於預覽
                    var previewFormData = new Dictionary<string, object?>
                    {
                        ["userId"] = employeeInfo.EmployeeNo,
                        ["applyDate"] = request.ApplyDate.Replace("-", "/"),
                        ["startTimeF"] = request.StartTimeF.Replace("-", "/"),
                        ["endTimeF"] = request.EndTimeF.Replace("-", "/"),
                        ["startTime"] = request.StartTime.Replace("-", "/"),
                        ["endTime"] = request.EndTime.Replace("-", "/"),
                        ["detail"] = request.Detail,
                        ["processType"] = request.ProcessType
                    };
                    
                    _logger.LogInformation("呼叫表單預覽 API with full form data");
                    var previewResponse = await _bpmService.PostAsync(previewEndpoint, previewFormData);
                    var previewJson = JsonSerializer.Deserialize<JsonElement>(previewResponse);
                    
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
                        _logger.LogInformation("表單預覽取得 {Count} 個欄位", computedData.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "表單預覽失敗");
                }

                // 3. 上傳附件到 FTP
                string? filePath = null;
                if (request.Attachments != null && request.Attachments.Count > 0)
                {
                    try
                    {
                        var uploadedFiles = new List<string>();
                        foreach (var file in request.Attachments)
                        {
                            var fileName = $"overtime_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid()}_{file.FileName}";
                            var remotePath = $"/uploads/overtime/{fileName}";
                            using var stream = file.OpenReadStream();
                            var success = await _ftpService.UploadFileAsync(stream, remotePath);
                            if (success) uploadedFiles.Add(remotePath);
                        }
                        if (uploadedFiles.Count > 0)
                        {
                            filePath = string.Join("||", uploadedFiles);
                            _logger.LogInformation("已上傳 {Count} 個附件", uploadedFiles.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "附件上傳失敗");
                    }
                }

                // 4. 建立表單資料
                var formData = BuildFormData(request, employeeInfo, computedData, filePath);
                
                // Log form data for debugging
                _logger.LogInformation("表單資料: {@FormData}", formData);

                // 5. 呼叫 BPM API 建立表單
                var bpmRequest = new BpmCreateFormRequest
                {
                    ProcessCode = FORM_CODE,
                    FormCode = FORM_CODE,
                    FormVersion = FORM_VERSION,
                    UserId = employeeInfo.EmployeeNo,
                    Subject = $"加班申請 - {request.ApplyDate}",
                    SourceSystem = "HRSystemAPI",
                    HasAttachments = !string.IsNullOrEmpty(filePath),
                    FormData = formData
                };

                var response = await _bpmService.PostAsync("bpm/invoke-process", bpmRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                var formId = ExtractValue(jsonResponse, "bpmProcessOid", "formId", "id");
                var formNumber = ExtractValue(jsonResponse, "processSerialNo", "formNumber", "formNo");

                _logger.LogInformation("加班表單申請成功 - FormId: {FormId}, FormNumber: {FormNumber}", formId, formNumber);

                return new OvertimeFormOperationResult
                {
                    Success = true,
                    Message = "加班表單申請成功",
                    FormId = formId,
                    FormNumber = formNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請加班表單失敗");
                return new OvertimeFormOperationResult
                {
                    Success = false,
                    Message = $"申請失敗: {ex.Message}",
                    ErrorCode = "CREATE_FAILED"
                };
            }
        }

        private Dictionary<string, object?> BuildFormData(
            CreateOvertimeFormRequest request, 
            EmployeeBasicInfo employeeInfo, 
            Dictionary<string, object?>? computedData, 
            string? filePath)
        {
            var formData = new Dictionary<string, object?>
            {
                ["applyDate"] = request.ApplyDate.Replace("-", "/"),
                ["startTimeF"] = request.StartTimeF.Replace("-", "/"),
                ["endTimeF"] = request.EndTimeF.Replace("-", "/"),
                ["startTime"] = request.StartTime.Replace("-", "/"),
                ["endTime"] = request.EndTime.Replace("-", "/"),
                ["detail"] = request.Detail,
                ["processType"] = request.ProcessType,
                ["fillFormDate"] = DateTime.Now.ToString("yyyy/MM/dd")
            };

            // 加入表單預覽取得的自動填充欄位
            if (computedData != null)
            {
                foreach (var kvp in computedData)
                {
                    // 跳過用戶輸入的關鍵欄位，避免被 computedData 覆蓋
                    if (kvp.Key == "applyDate" || kvp.Key == "startTimeF" || kvp.Key == "endTimeF" || 
                        kvp.Key == "startTime" || kvp.Key == "endTime" || kvp.Key == "detail" || 
                        kvp.Key == "processType")
                    {
                        continue;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(kvp.Value?.ToString())) 
                    {
                        formData[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                // 如果表單預覽失敗,手動填入基本欄位
                formData["fillerId"] = employeeInfo.EmployeeNo;
                formData["fillerName"] = employeeInfo.EmployeeName;
                formData["fillerUnitId"] = employeeInfo.DepartmentId ?? "";
                formData["fillerUnitName"] = employeeInfo.DepartmentName ?? "";
                formData["applier"] = employeeInfo.EmployeeNo;
                formData["applierUnit"] = "PI";
                formData["cpf01"] = employeeInfo.EmployeeNo;
                formData["companyNo"] = employeeInfo.CompanyId ?? "03546618";
                formData["overtimeCode"] = "SLC01";
            }

            // 加入附件路徑
            if (!string.IsNullOrEmpty(filePath)) 
            {
                formData["filePath"] = filePath;
            }

            return formData;
        }

        private string ExtractValue(JsonElement jsonResponse, params string[] keys)
        {
            if (jsonResponse.TryGetProperty("data", out var dataElement))
            {
                foreach (var key in keys)
                {
                    if (dataElement.TryGetProperty(key, out var prop)) return prop.GetString() ?? "";
                }
            }
            foreach (var key in keys)
            {
                if (jsonResponse.TryGetProperty(key, out var prop)) return prop.GetString() ?? "";
            }
            return "";
        }

        // ========== 新版 APP API 方法 ==========

        public async Task<EFotApplyResponse> EFotApplyAsync(EFotApplyRequest request)
        {
            try
            {
                _logger.LogInformation("加班單預申請 - uid: {Uid}, estartdate: {Estartdate}", request.Uid, request.Estartdate);

                // 驗證結束日期與起始日期同一天
                if (request.Estartdate != request.Eenddate)
                {
                    return new EFotApplyResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，加班結束日期必須與起始日期同一天"
                    };
                }

                // 查詢員工基本資料
                _logger.LogDebug("開始查詢員工資料 - Uid: {Uid}", request.Uid);
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                _logger.LogDebug("員工資料查詢完成 - 結果: {Result}", employeeInfo != null ? "成功" : "失敗");
                if (employeeInfo == null)
                {
                    return new EFotApplyResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，找不到員工資料"
                    };
                }

                // 格式化時間為 HH:mm 格式 (移除秒數)
                var startTime = request.Estarttime.Length > 5 ? request.Estarttime.Substring(0, 5) : request.Estarttime;
                var endTime = request.Eendtime.Length > 5 ? request.Eendtime.Substring(0, 5) : request.Eendtime;

                // 轉換處理方式: C->0(補休), P->1(加班費)
                var processType = request.Eprocess == "C" ? "0" : "1";

                // 處理附件檔案路徑
                string? filePath = null;
                bool hasAttachments = false;
                if (request.Efileid != null && request.Efileid.Count > 0 && request.Efiletype == "D")
                {
                    // 將檔案ID轉換為FTP路徑格式
                    var ftpPaths = request.Efileid.Select(id => $"FTPTest~~/FTPShare/overtime_{id}.pdf").ToList();
                    filePath = string.Join("||", ftpPaths);
                    hasAttachments = true;
                }

                // 建立BPM表單資料 (使用正確的格式,包含所有必要欄位)
                var formData = new Dictionary<string, object?>
                {
                    ["detail"] = request.Ereason,
                    ["applyDate"] = request.Estartdate.Replace("-", "/"),
                    ["startTime"] = startTime,
                    ["endTime"] = endTime,
                    ["startTimeF"] = startTime,
                    ["endTimeF"] = endTime,
                    ["processType"] = processType,
                    ["fillFormDate"] = DateTime.Now.ToString("yyyy/MM/dd"),
                    // 添加必要的員工資訊欄位
                    ["applierUnit"] = employeeInfo.DepartmentId ?? "",
                    ["fillerName"] = employeeInfo.EmployeeName,
                    ["overtimeCode"] = "SLC01",
                    ["applier"] = request.Uid,
                    ["companyNo"] = request.Cid,
                    ["fillerUnitId"] = employeeInfo.DepartmentId ?? "",
                    ["fillerId"] = request.Uid,
                    ["fillerUnitName"] = employeeInfo.DepartmentName ?? "",
                    ["cpf01"] = employeeInfo.EmployeeId.ToString()  // 轉換為字串
                };

                // BPM 需要 hdnFilePath 欄位 (不是 filePath)
                if (hasAttachments && !string.IsNullOrEmpty(filePath))
                {
                    formData["hdnFilePath"] = filePath;
                }

                // 使用正確的 BPM 請求格式
                var bpmRequest = new
                {
                    processCode = "PI_OVERTIME_001_PROCESS",
                    formDataMap = new Dictionary<string, object>
                    {
                        ["PI_OVERTIME_001"] = formData
                    },
                    userId = request.Uid,  // 直接使用 uid (EMPLOYEE_NO)
                    subject = $"加班預申請 - {request.Estartdate.Replace("-", "/")}",
                    sourceSystem = "APP",
                    environment = "TEST",
                    hasAttachments = hasAttachments
                };

                _logger.LogDebug("準備呼叫 BPM API - 員工工號: {EmployeeNo}, Subject: {Subject}, Request: {Request}", 
                    request.Uid, bpmRequest.subject, JsonSerializer.Serialize(bpmRequest));
                var response = await _bpmService.PostAsync("bpm/invoke-process", bpmRequest);
                
                // 解析 BPM 回應以取得表單編號和 formId
                var processSerialNo = "未知";
                var formId = "未知";
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                    if (jsonResponse.TryGetProperty("processSerialNo", out var serialNoElement))
                    {
                        processSerialNo = serialNoElement.GetString() ?? "未知";
                    }
                    // 嘗試從回應中取得 formId（可能的字段名）
                    if (jsonResponse.TryGetProperty("formId", out var formIdElement))
                    {
                        formId = formIdElement.GetString() ?? processSerialNo;
                    }
                    else if (jsonResponse.TryGetProperty("processId", out var processIdElement))
                    {
                        formId = processIdElement.GetString() ?? processSerialNo;
                    }
                    else if (jsonResponse.TryGetProperty("processOid", out var processOidElement))
                    {
                        formId = processOidElement.GetString() ?? processSerialNo;
                    }
                    else
                    {
                        // 若無 formId，使用 processSerialNo
                        formId = processSerialNo;
                    }
                }
                catch { }
                
                // 存儲申請紀錄到數據庫
                try
                {
                    await StoreOvertimeApplicationAsync(request, processSerialNo, formId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "存儲加班申請紀錄失敗，但不影響申請結果");
                }
                
                _logger.LogInformation("✅ 加班單預申請成功");
                _logger.LogInformation("📋 表單編號: {FormNumber}", processSerialNo);
                _logger.LogInformation("📝 表單ID: {FormId}", formId);
                _logger.LogInformation("👤 員工: uid: {Uid}", request.Uid);
                _logger.LogInformation("📅 加班日期: {Date}", request.Estartdate);
                _logger.LogInformation("⏰ 加班時間: {StartTime} ~ {EndTime}", request.Estarttime, request.Eendtime);
                _logger.LogInformation("📝 事由: {Reason}", request.Ereason);
                _logger.LogInformation("💾 完整 BPM 回應: {Response}", response);

                return new EFotApplyResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Formid = formId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班單預申請失敗 - Uid: {Uid}, 錯誤訊息: {Message}, StackTrace: {StackTrace}", 
                    request.Uid, ex.Message, ex.StackTrace);
                return new EFotApplyResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                };
            }
        }

        public async Task<EFotConfirmListResponse> EFotConfirmListAsync(EFotConfirmListRequest request)
        {
            try
            {
                _logger.LogInformation("取得加班確認申請列表 - uid: {Uid}, cid: {Cid}", request.Uid, request.Cid);

                // 1. 查詢員工基本資料
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    return new EFotConfirmListResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，找不到員工資料"
                    };
                }

                // 2. 呼叫 BPM API 取得員工的待辦事項清單
                var workItemsEndpoint = $"bpm/workitems/{request.Uid}";
                string workItemsResponse;
                try
                {
                    workItemsResponse = await _bpmService.GetAsync(workItemsEndpoint);
                    _logger.LogInformation("成功取得待辦事項清單 - uid: {Uid}", request.Uid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "取得待辦事項清單失敗 - uid: {Uid}", request.Uid);
                    return new EFotConfirmListResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，無法取得待辦事項清單"
                    };
                }

                var workItemsJson = JsonSerializer.Deserialize<JsonElement>(workItemsResponse);
                
                // 檢查 API 回應狀態
                if (!workItemsJson.TryGetProperty("status", out var statusElement) || 
                    statusElement.GetString() != "SUCCESS")
                {
                    _logger.LogWarning("BPM API 回應狀態異常 - uid: {Uid}", request.Uid);
                    return new EFotConfirmListResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                // 3. 取得 workItems 陣列並篩選加班表單 (PI_OVERTIME_001_PROCESS)
                var overtimeList = new List<EFotConfirmListItem>();
                
                if (workItemsJson.TryGetProperty("workItems", out var workItemsArray) && 
                    workItemsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var workItem in workItemsArray.EnumerateArray())
                    {
                        var processSerialNumber = workItem.GetProperty("processSerialNumber").GetString() ?? "";
                        
                        // 4. 透過 processSerialNumber 查詢表單資訊，判斷是否為加班表單
                        try
                        {
                            var syncProcessEndpoint = $"bpm/sync-process-info?processSerialNo={processSerialNumber}&processCode=PI_OVERTIME_001_PROCESS&environment=TEST";
                            var syncProcessResponse = await _bpmService.GetAsync(syncProcessEndpoint);
                            var syncProcessJson = JsonSerializer.Deserialize<JsonElement>(syncProcessResponse);
                            
                            // 檢查是否成功且為加班表單
                            if (syncProcessJson.TryGetProperty("status", out var syncStatus) && 
                                syncStatus.GetString() == "SUCCESS")
                            {
                                _logger.LogInformation("找到加班表單 - ProcessSerialNo: {ProcessSerialNo}", processSerialNumber);
                                
                                // 5. 從本地數據庫查詢加班申請詳情
                                var formData = await QueryOvertimeApplicationAsync(processSerialNumber);
                                
                                if (formData != null)
                                {
                                    // 取得處理方式文字
                                    var processText = formData.Eprocess == "C" ? "補休" : "加班費";
                                    
                                    overtimeList.Add(new EFotConfirmListItem
                                    {
                                        Uid = formData.Uid ?? request.Uid,
                                        Uname = employeeInfo.EmployeeName,
                                        Udepartment = employeeInfo.DepartmentName ?? "",
                                        Formid = formData.Formid ?? processSerialNumber,
                                        Estartdate = formData.Estartdate ?? "",
                                        Estarttime = formData.Estarttime ?? "",
                                        Eenddate = formData.Eenddate ?? "",
                                        Eendtime = formData.Eendtime ?? "",
                                        Ereason = formData.Ereason ?? "",
                                        Eprocess = processText
                                    });
                                    
                                    _logger.LogInformation("成功解析加班表單 - FormId: {FormId}", formData.Formid);
                                }
                                else
                                {
                                    _logger.LogWarning("在本地數據庫中找不到加班申請詳情 - ProcessSerialNo: {ProcessSerialNo}", processSerialNumber);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 如果不是加班表單或查詢失敗，跳過此 workItem
                            _logger.LogDebug(ex, "ProcessSerialNo: {ProcessSerialNo} 不是加班表單或查詢失敗，跳過", processSerialNumber);
                            continue;
                        }
                    }
                }

                _logger.LogInformation("成功取得加班確認申請列表，共 {Count} 筆 - uid: {Uid}", overtimeList.Count, request.Uid);
                
                return new EFotConfirmListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new EFotConfirmListData
                    {
                        Efotdata = overtimeList
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得加班確認申請列表失敗");
                return new EFotConfirmListResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                };
            }
        }

        public async Task<EFotPreviewResponse> EFotPreviewAsync(EFotPreviewRequest request)
        {
            try
            {
                _logger.LogInformation("取得加班單詳情 - formid: {FormId}, uid: {Uid}", request.Formid, request.Uid);

                // 1. 首先從數據庫查詢申請紀錄
                var applicationData = await QueryOvertimeApplicationAsync(request.Formid);
                
                if (applicationData != null)
                {
                    _logger.LogInformation("從數據庫找到加班申請紀錄 - FormId: {FormId}", request.Formid);
                    
                    // 解析處理方式
                    var processText = applicationData.Eprocess == "C" ? "補休" : "加班費";

                    // 解析附件
                    List<EFotAttachment>? attachments = null;
                    if (applicationData.Efileid != null && applicationData.Efileid.Count > 0)
                    {
                        attachments = new List<EFotAttachment>();
                        for (int i = 0; i < applicationData.Efileid.Count; i++)
                        {
                            var fileId = applicationData.Efileid[i];
                            attachments.Add(new EFotAttachment
                            {
                                Efileid = fileId,
                                Efilename = $"加班確認附件檔{i + 1}",
                                Esfilename = $"overtime_{fileId}.pdf",
                                Efileurl = $"https://xxxxxx.xxxx.xx/filecenter/{request.Uid}/overtime_{fileId}.pdf"
                            });
                        }
                    }

                    // 解析時間資料 (從數據庫格式 yyyy-MM-dd HH:mm 轉換)
                    var startDate = applicationData.Estartdate;  // yyyy-MM-dd 或 yyyy/MM/dd
                    var endDate = applicationData.Eenddate;
                    var startTime = applicationData.Estarttime;  // HH:mm
                    var endTime = applicationData.Eendtime;

                    return new EFotPreviewResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new EFotPreviewData
                        {
                            Formid = request.Formid,
                            Estartdate = startDate.Replace("/", "-"),
                            Estarttime = startTime,
                            Eenddate = endDate.Replace("/", "-"),
                            Eendtime = endTime,
                            Ereason = applicationData.Ereason,
                            Eprocess = processText,
                            Efiletype = attachments != null && attachments.Count > 0 ? "D" : null,
                            Attachments = attachments
                        }
                    };
                }

                // 2. 如果數據庫中找不到，則嘗試呼叫 BPM API
                _logger.LogInformation("數據庫中未找到記錄，嘗試呼叫 BPM API - FormId: {FormId}", request.Formid);
                
                var endpoint = $"bpm/form-detail?formId={request.Formid}";
                var response = await _bpmService.GetAsync(endpoint);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                // 解析表單資料
                if (jsonResponse.TryGetProperty("data", out var dataElement))
                {
                    var formData = dataElement.GetProperty("formData");
                    
                    // 解析處理方式
                    var processTypeValue = formData.GetProperty("processType").GetString();
                    var processText = processTypeValue == "0" ? "補休" : "加班費";

                    // 解析附件
                    List<EFotAttachment>? attachments = null;
                    if (formData.TryGetProperty("filePath", out var filePathProp))
                    {
                        var filePath = filePathProp.GetString();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            var files = filePath.Split("||");
                            attachments = new List<EFotAttachment>();
                            for (int i = 0; i < files.Length; i++)
                            {
                                var fileName = Path.GetFileName(files[i]);
                                attachments.Add(new EFotAttachment
                                {
                                    Efileid = (i + 1).ToString(),
                                    Efilename = $"加班確認附件檔{i + 1}",
                                    Esfilename = fileName,
                                    Efileurl = $"https://xxxxxx.xxxx.xx/filecenter/{request.Uid}/{fileName}"
                                });
                            }
                        }
                    }

                    // 解析時間資料
                    var startTimeF = formData.GetProperty("startTimeF").GetString() ?? "";
                    var endTimeF = formData.GetProperty("endTimeF").GetString() ?? "";
                    var startParts = startTimeF.Split(' ');
                    var endParts = endTimeF.Split(' ');

                    return new EFotPreviewResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new EFotPreviewData
                        {
                            Formid = request.Formid,
                            Estartdate = startParts.Length > 0 ? startParts[0] : "",
                            Estarttime = startParts.Length > 1 ? startParts[1] : "",
                            Eenddate = endParts.Length > 0 ? endParts[0] : "",
                            Eendtime = endParts.Length > 1 ? endParts[1] : "",
                            Ereason = formData.GetProperty("detail").GetString() ?? "",
                            Eprocess = processText,
                            Efiletype = attachments != null && attachments.Count > 0 ? "D" : null,
                            Attachments = attachments
                        }
                    };
                }

                return new EFotPreviewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，找不到表單"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得加班單詳情失敗 - FormId: {FormId}", request.Formid);
                return new EFotPreviewResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                };
            }
        }

        public async Task<EFotConfirmSubmitResponse> EFotConfirmSubmitAsync(EFotConfirmSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("加班單確認申請送出 - formid: {FormId}", request.Formid);

                // 組合實際加班時間
                var actualStartTime = $"{request.Astartdate} {request.Astarttime}";
                var actualEndTime = $"{request.Aenddate} {request.Aendtime}";

                // 處理附件檔案路徑
                string? filePath = null;
                if (request.Efileid != null && request.Efileid.Count > 0 && request.Efiletype == "D")
                {
                    var ftpPaths = request.Efileid.Select(id => $"FTPTest~~/FTPShare/overtime_confirm_{id}.pdf").ToList();
                    filePath = string.Join("||", ftpPaths);
                }

                // 更新表單的實際加班時間
                var updateData = new Dictionary<string, object?>
                {
                    ["startTime"] = actualStartTime.Replace("-", "/"),
                    ["endTime"] = actualEndTime.Replace("-", "/")
                };

                if (!string.IsNullOrEmpty(filePath))
                {
                    updateData["filePath"] = filePath;
                }

                // TODO: 呼叫 BPM API 更新表單
                var endpoint = $"bpm/update-form?formId={request.Formid}";
                var response = await _bpmService.PostAsync(endpoint, updateData);

                _logger.LogInformation("加班單確認申請送出成功 - formid: {FormId}", request.Formid);

                return new EFotConfirmSubmitResponse
                {
                    Code = "200",
                    Msg = "請求成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班單確認申請送出失敗");
                return new EFotConfirmSubmitResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                };
            }
        }

        /// <summary>
        /// API: 加班確認列表查詢（不含'e'前綴版本）
        /// 別名方法，直接呼叫 EFotConfirmListAsync
        /// </summary>
        public async Task<EFotConfirmListResponse> FotConfirmListAsync(EFotConfirmListRequest request)
        {
            return await EFotConfirmListAsync(request);
        }

        /// <summary>
        /// API: 加班確認提交（POST /app/fotconfirm）
        /// 提交實際發生的加班申請表單，填具實際的加班時間及所需附件後送出
        /// 1. 先同步加班資訊 (sync-process-info)
        /// 2. 取得待辦事項 (workitems)
        /// 3. 提交確認表單
        /// </summary>
        public async Task<FotConfirmSubmitResponse> FotConfirmSubmitAsync(FotConfirmSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("加班確認提交 - formid: {FormId}, uid: {Uid}", request.Formid, request.Uid);
                _logger.LogInformation("實際加班時間: {StartDate} {StartTime} ~ {EndDate} {EndTime}", 
                    request.Astartdate, request.Astarttime, request.Aenddate, request.Aendtime);

                // 1. 同步加班資訊 via BPM middleware
                try
                {
                    var syncEndpoint = $"bpm/sync-process-info?processSerialNo={request.Formid}&formCode=Attendance_Exception_001&formVersion=1.0.0";
                    _logger.LogInformation("同步加班資訊: {Endpoint}", syncEndpoint);
                    var syncResponse = await _bpmService.GetAsync(syncEndpoint);
                    _logger.LogInformation("同步加班資訊回應: {Response}", syncResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "同步加班資訊失敗，繼續處理");
                }

                // 2. 查詢待辦事項以確認有加班預申請
                string? workItemOID = null;
                try
                {
                    var workItemsEndpoint = $"bpm/workitems/{request.Uid}";
                    _logger.LogInformation("查詢待辦事項: {Endpoint}", workItemsEndpoint);
                    var workItemsResponse = await _bpmService.GetAsync(workItemsEndpoint);
                    _logger.LogInformation("待辦事項回應: {Response}", workItemsResponse);

                    // 解析待辦事項回應
                    var workItemsJson = JsonSerializer.Deserialize<JsonElement>(workItemsResponse);
                    if (workItemsJson.TryGetProperty("workItems", out var workItemsArray))
                    {
                        foreach (var workItem in workItemsArray.EnumerateArray())
                        {
                            if (workItem.TryGetProperty("processSerialNumber", out var serialNo))
                            {
                                var serialNoStr = serialNo.GetString();
                                // 檢查是否為加班相關的待辦事項
                                if (serialNoStr != null && (serialNoStr.Contains("Overtime") || serialNoStr == request.Formid))
                                {
                                    if (workItem.TryGetProperty("workItemOID", out var oid))
                                    {
                                        workItemOID = oid.GetString();
                                        _logger.LogInformation("找到加班待辦事項 - processSerialNo: {SerialNo}, workItemOID: {OID}", 
                                            serialNoStr, workItemOID);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "查詢待辦事項失敗，繼續處理");
                }

                // 3. 處理附件檔案路徑
                string? filePath = null;
                bool hasAttachments = false;
                if (request.Efileid != null && request.Efileid.Count > 0 && request.Efiletype == "D")
                {
                    var ftpPaths = request.Efileid.Select(id => $"FTPTest~~/FTPShare/overtime_confirm_{id}.pdf").ToList();
                    filePath = string.Join("||", ftpPaths);
                    hasAttachments = true;
                }

                // 4. 組合實際加班時間並提交確認
                var actualStartTime = $"{request.Astartdate.Replace("-", "/")} {request.Astarttime}";
                var actualEndTime = $"{request.Aenddate.Replace("-", "/")} {request.Aendtime}";

                // 建立更新表單資料
                var updateData = new Dictionary<string, object?>
                {
                    ["startTime"] = actualStartTime,
                    ["endTime"] = actualEndTime,
                    ["formId"] = request.Formid
                };

                if (hasAttachments && !string.IsNullOrEmpty(filePath))
                {
                    updateData["hdnFilePath"] = filePath;
                }

                // 5. 呼叫 BPM API 提交確認
                // 使用 workItemOID 進行簽核推進（如果有的話）
                if (!string.IsNullOrEmpty(workItemOID))
                {
                    var advanceRequest = new
                    {
                        workItemOID = workItemOID,
                        formDataMap = new Dictionary<string, object>
                        {
                            ["Attendance_Exception_001"] = updateData
                        },
                        userId = request.Uid,
                        action = "APPROVE",
                        comment = "加班確認提交"
                    };

                    try
                    {
                        var advanceEndpoint = "bpm/advance-workitem";
                        _logger.LogInformation("提交加班確認: {Request}", JsonSerializer.Serialize(advanceRequest));
                        var advanceResponse = await _bpmService.PostAsync(advanceEndpoint, advanceRequest);
                        _logger.LogInformation("提交加班確認回應: {Response}", advanceResponse);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "透過 workItem 提交失敗，嘗試直接更新表單");
                    }
                }

                // 6. 也嘗試直接更新表單
                try
                {
                    var updateEndpoint = $"bpm/update-form?formId={request.Formid}";
                    var updateResponse = await _bpmService.PostAsync(updateEndpoint, updateData);
                    _logger.LogInformation("更新表單回應: {Response}", updateResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "直接更新表單失敗");
                }

                // 7. 更新本地數據庫記錄
                try
                {
                    await UpdateOvertimeConfirmationAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "更新本地數據庫記錄失敗，但不影響提交結果");
                }

                _logger.LogInformation("✅ 加班確認提交成功 - formid: {FormId}", request.Formid);

                return new FotConfirmSubmitResponse
                {
                    Code = "200",
                    Msg = "請求成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班確認提交失敗 - FormId: {FormId}, 錯誤訊息: {Message}", 
                    request.Formid, ex.Message);
                return new FotConfirmSubmitResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 更新加班確認資料到數據庫
        /// </summary>
        private async Task UpdateOvertimeConfirmationAsync(FotConfirmSubmitRequest request)
        {
            const string sql = @"
                UPDATE [dbo].[OVERTIME_APPLICATIONS] 
                SET ActualStartDate = @ActualStartDate,
                    ActualEndDate = @ActualEndDate,
                    ActualStartTime = @ActualStartTime,
                    ActualEndTime = @ActualEndTime,
                    ConfirmFileType = @ConfirmFileType,
                    ConfirmFileIds = @ConfirmFileIds,
                    ConfirmDate = @ConfirmDate,
                    ConfirmBy = @ConfirmBy
                WHERE FormId = @FormId";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    FormId = request.Formid,
                    ActualStartDate = request.Astartdate,
                    ActualEndDate = request.Aenddate,
                    ActualStartTime = request.Astarttime,
                    ActualEndTime = request.Aendtime,
                    ConfirmFileType = request.Efiletype ?? string.Empty,
                    ConfirmFileIds = request.Efileid != null ? string.Join(",", request.Efileid) : string.Empty,
                    ConfirmDate = DateTime.Now,
                    ConfirmBy = request.Uid
                });
                _logger.LogInformation("成功更新加班確認紀錄 - FormId: {FormId}", request.Formid);
            }
            catch (SqlException ex) when (ex.Message.Contains("ActualStartDate") || ex.Message.Contains("無效的資料行"))
            {
                _logger.LogWarning(ex, "OVERTIME_APPLICATIONS 表缺少確認欄位，嘗試添加");
                await AddConfirmColumnsToTableAsync();
                
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    FormId = request.Formid,
                    ActualStartDate = request.Astartdate,
                    ActualEndDate = request.Aenddate,
                    ActualStartTime = request.Astarttime,
                    ActualEndTime = request.Aendtime,
                    ConfirmFileType = request.Efiletype ?? string.Empty,
                    ConfirmFileIds = request.Efileid != null ? string.Join(",", request.Efileid) : string.Empty,
                    ConfirmDate = DateTime.Now,
                    ConfirmBy = request.Uid
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新加班確認紀錄失敗 - FormId: {FormId}", request.Formid);
            }
        }

        /// <summary>
        /// 添加確認欄位到 OVERTIME_APPLICATIONS 表
        /// </summary>
        private async Task AddConfirmColumnsToTableAsync()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ActualStartDate')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ActualStartDate] [nvarchar](50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ActualEndDate')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ActualEndDate] [nvarchar](50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ActualStartTime')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ActualStartTime] [nvarchar](50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ActualEndTime')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ActualEndTime] [nvarchar](50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ConfirmFileType')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ConfirmFileType] [nvarchar](50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ConfirmFileIds')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ConfirmFileIds] [nvarchar](max) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ConfirmDate')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ConfirmDate] [datetime] NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OVERTIME_APPLICATIONS]') AND name = 'ConfirmBy')
                    ALTER TABLE [dbo].[OVERTIME_APPLICATIONS] ADD [ConfirmBy] [nvarchar](50) NULL;";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql);
                _logger.LogInformation("成功添加確認欄位到 OVERTIME_APPLICATIONS 表");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加確認欄位失敗");
                throw;
            }
        }

        // ========== API 5: 代理人資料查詢 ==========

        /// <summary>
        /// API 5: 代理人資料查詢
        /// </summary>
        public async Task<GetAgentResponse> GetAgentAsync(GetAgentRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢代理人資料 - cid: {Cid}, uid: {Uid}", request.Cid, request.Uid);

                // 查詢公司內所有員工，依部門和姓名排序
                const string sql = @"
                    SELECT 
                        DEPARTMENT_CNAME AS Agentdept,
                        EMPLOYEE_NO AS Agentid,
                        EMPLOYEE_CNAME AS Agentname
                    FROM [dbo].[vwZZ_EMPLOYEE]
                    WHERE COMPANY_CODE = @CompanyCode
                    ORDER BY DEPARTMENT_CNAME, EMPLOYEE_CNAME";

                using var connection = new SqlConnection(_connectionString);
                var agents = await connection.QueryAsync<AgentData>(sql, new { CompanyCode = request.Cid });
                var agentList = agents.ToList();

                if (agentList.Count == 0)
                {
                    _logger.LogWarning("查無代理人資料 - cid: {Cid}", request.Cid);
                    return new GetAgentResponse
                    {
                        Code = "203",
                        Msg = "查無代理人資料",
                        Data = null
                    };
                }

                _logger.LogInformation("成功查詢代理人資料，共 {Count} 筆", agentList.Count);

                return new GetAgentResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new AgentDataWrapper
                    {
                        Agentdata = agentList
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢代理人資料失敗");
                return new GetAgentResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        // ========== 輔助方法 ==========

        /// <summary>
        /// 存儲加班申請紀錄到數據庫
        /// </summary>
        private async Task StoreOvertimeApplicationAsync(EFotApplyRequest request, string processSerialNo, string formId)
        {
            const string sql = @"
                INSERT INTO [dbo].[OVERTIME_APPLICATIONS] 
                    (FormId, ProcessSerialNo, EmployeeNo, CompanyNo, StartDate, EndDate, 
                     StartTime, EndTime, Reason, ProcessType, FileType, FileIds, CreatedDate, CreatedBy)
                VALUES 
                    (@FormId, @ProcessSerialNo, @EmployeeNo, @CompanyNo, @StartDate, @EndDate, 
                     @StartTime, @EndTime, @Reason, @ProcessType, @FileType, @FileIds, @CreatedDate, @CreatedBy)";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    FormId = formId,
                    ProcessSerialNo = processSerialNo,
                    EmployeeNo = request.Uid,
                    CompanyNo = request.Cid,
                    StartDate = request.Estartdate,
                    EndDate = request.Eenddate,
                    StartTime = request.Estarttime,
                    EndTime = request.Eendtime,
                    Reason = request.Ereason,
                    ProcessType = request.Eprocess,
                    FileType = request.Efiletype ?? string.Empty,
                    FileIds = request.Efileid != null ? string.Join(",", request.Efileid) : string.Empty,
                    CreatedDate = DateTime.Now,
                    CreatedBy = request.Uid
                });
                _logger.LogInformation("成功存儲加班申請紀錄 - FormId: {FormId}", formId);
            }
            catch (SqlException ex) when (ex.Number == 208) // 表不存在
            {
                _logger.LogWarning(ex, "OVERTIME_APPLICATIONS 表不存在，創建表並重試");
                await CreateOvertimeApplicationsTableAsync();
                
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    FormId = formId,
                    ProcessSerialNo = processSerialNo,
                    EmployeeNo = request.Uid,
                    CompanyNo = request.Cid,
                    StartDate = request.Estartdate,
                    EndDate = request.Eenddate,
                    StartTime = request.Estarttime,
                    EndTime = request.Eendtime,
                    Reason = request.Ereason,
                    ProcessType = request.Eprocess,
                    FileType = request.Efiletype ?? string.Empty,
                    FileIds = request.Efileid != null ? string.Join(",", request.Efileid) : string.Empty,
                    CreatedDate = DateTime.Now,
                    CreatedBy = request.Uid
                });
            }
        }

        /// <summary>
        /// 創建 OVERTIME_APPLICATIONS 表
        /// </summary>
        private async Task CreateOvertimeApplicationsTableAsync()
        {
            const string sql = @"
                CREATE TABLE [dbo].[OVERTIME_APPLICATIONS] (
                    [Id] [int] IDENTITY(1,1) PRIMARY KEY,
                    [FormId] [nvarchar](100) NOT NULL UNIQUE,
                    [ProcessSerialNo] [nvarchar](100) NULL,
                    [EmployeeNo] [nvarchar](50) NOT NULL,
                    [CompanyNo] [nvarchar](50) NOT NULL,
                    [StartDate] [nvarchar](50) NOT NULL,
                    [EndDate] [nvarchar](50) NOT NULL,
                    [StartTime] [nvarchar](50) NOT NULL,
                    [EndTime] [nvarchar](50) NOT NULL,
                    [Reason] [nvarchar](500) NULL,
                    [ProcessType] [nvarchar](50) NULL,
                    [FileType] [nvarchar](50) NULL,
                    [FileIds] [nvarchar](max) NULL,
                    [CreatedDate] [datetime] DEFAULT GETDATE(),
                    [CreatedBy] [nvarchar](50) NULL
                )";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(sql);
                _logger.LogInformation("成功創建 OVERTIME_APPLICATIONS 表");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "創建 OVERTIME_APPLICATIONS 表失敗");
                throw;
            }
        }

        /// <summary>
        /// 從數據庫查詢加班申請詳情 (根據 FormId)
        /// </summary>
        private async Task<EFotApplyRequest?> QueryOvertimeApplicationAsync(string formId)
        {
            const string sql = @"
                SELECT 
                    FormId AS Formid,
                    EmployeeNo AS Uid,
                    CompanyNo AS Cid,
                    StartDate AS Estartdate,
                    EndDate AS Eenddate,
                    StartTime AS Estarttime,
                    EndTime AS Eendtime,
                    Reason AS Ereason,
                    ProcessType AS Eprocess,
                    FileType AS Efiletype,
                    FileIds
                FROM [dbo].[OVERTIME_APPLICATIONS]
                WHERE FormId = @FormId OR ProcessSerialNo = @FormId";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { FormId = formId });
                
                if (result != null)
                {
                    return new EFotApplyRequest
                    {
                        Formid = result.Formid,
                        Uid = result.Uid,
                        Cid = result.Cid,
                        Estartdate = result.Estartdate,
                        Eenddate = result.Eenddate,
                        Estarttime = result.Estarttime,
                        Eendtime = result.Eendtime,
                        Ereason = result.Ereason,
                        Eprocess = result.Eprocess,
                        Efiletype = result.Efiletype,
                        Efileid = !string.IsNullOrEmpty(result.FileIds) 
                            ? result.FileIds.Split(',').ToList() 
                            : null
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查詢加班申請詳情失敗 - FormId: {FormId}", formId);
                return null;
            }
        }
    }
}
