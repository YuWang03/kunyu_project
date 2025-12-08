using System.Text.Json;
using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using Dapper;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 銷假單服務實作（BPM 整合）
    /// 根據 BPM Middleware API 規格實作銷假單功能
    /// </summary>
    public class CancelLeaveService : ICancelLeaveService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<CancelLeaveService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _hrDatabaseConnectionString;
        private readonly string _hrDatabase104ConnectionString;
        
        // BPM 表單相關常數
        private const string FORM_CODE = "PI_CANCEL_LEAVE_001";
        private const string FORM_VERSION = "1.0.0";
        private const string LEAVE_FORM_CODE = "PI_LEAVE_001"; // 原請假單代碼

        public CancelLeaveService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            ILogger<CancelLeaveService> logger,
            IConfiguration configuration)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _logger = logger;
            _configuration = configuration;
            _hrDatabaseConnectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration));
            _hrDatabase104ConnectionString = configuration.GetConnectionString("HRDatabase104")
                ?? _hrDatabaseConnectionString; // 如果未配置，使用 HR Database 作為備用
        }

        #region 查詢可銷假的請假單列表

        /// <summary>
        /// 查詢可銷假的請假單列表
        /// 返回起始日未到的個人請假表單，並驗證 104 DB 的簽核狀態
        /// </summary>
        public async Task<CancelLeaveListResponse> GetCancelLeaveListAsync(CancelLeaveListRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢可銷假的請假單列表，使用者工號: {Uid}", request.Uid);

                // 1. 查詢員工基本資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    _logger.LogWarning("找不到員工資訊: {Uid}", request.Uid);
                    return new CancelLeaveListResponse
                    {
                        Code = "203",
                        Msg = "找不到員工資訊"
                    };
                }

                // 2. 查詢該員工的請假單記錄（透過 BPM API）
                var today = DateTime.Now.Date;

                var queryEndpoint = $"bpm/process-instances?processCode={LEAVE_FORM_CODE}_PROCESS&userId={request.Uid}&status=ACTIVE";

                string responseBody;
                try
                {
                    responseBody = await _bpmService.GetAsync(queryEndpoint);
                    Console.WriteLine($"====== BPM 查詢請假單回應 ======");
                    Console.WriteLine($"查詢端點: {queryEndpoint}");
                    Console.WriteLine($"回應內容: {responseBody}");
                    Console.WriteLine($"================================");
                    _logger.LogDebug("BPM 查詢回應: {Response}", responseBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 查詢 BPM 失敗: {ex.Message}");
                    _logger.LogError(ex, "查詢 BPM 請假單記錄失敗");
                    
                    return new CancelLeaveListResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new CancelLeaveListData
                        {
                            Efleveldata = new List<CancelLeaveItem>()
                        }
                    };
                }

                // 3. 解析 BPM 回應
                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                Console.WriteLine($"JSON 解析成功，根屬性: {string.Join(", ", bpmResponse.EnumerateObject().Select(p => p.Name))}");
                
                if (!bpmResponse.TryGetProperty("processInstances", out var processInstancesElement) &&
                    !bpmResponse.TryGetProperty("data", out processInstancesElement))
                {
                    Console.WriteLine($"⚠️ BPM 回應中未找到 processInstances 或 data 屬性");
                    _logger.LogWarning("BPM API 回應中沒有找到表單列表");
                    
                    return new CancelLeaveListResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        Data = new CancelLeaveListData
                        {
                            Efleveldata = new List<CancelLeaveItem>()
                        }
                    };
                }
                
                Console.WriteLine($"✅ 找到資料陣列，項目數: {processInstancesElement.GetArrayLength()}");

                // 4. 轉換為 APP 格式，並驗證 104 DB 簽核狀態
                var cancelLeaveItems = new List<CancelLeaveItem>();

                // 遍歷流程實例
                foreach (var processInstance in processInstancesElement.EnumerateArray())
                {
                    try
                    {
                        // 取得流程序號
                        var processSerialNo = GetStringValue(processInstance, "processSerialNo", "serialNumber", "formId");
                        
                        // 如果指定了 Formid，則只處理該表單
                        if (!string.IsNullOrEmpty(request.Formid) && processSerialNo != request.Formid)
                        {
                            continue;
                        }

                        // 取得表單資料
                        if (processInstance.TryGetProperty("formData", out var formDataProp) &&
                            formDataProp.TryGetProperty(LEAVE_FORM_CODE, out var leaveFormData))
                        {
                            var startDate = GetStringValue(leaveFormData, "startDate");
                            var startTime = GetStringValue(leaveFormData, "startTime");
                            var endDate = GetStringValue(leaveFormData, "endDate");
                            var endTime = GetStringValue(leaveFormData, "endTime");
                            var leaveTypeName = GetStringValue(leaveFormData, "leaveTypeName");
                            var leaveTypeCode = GetStringValue(leaveFormData, "leaveTypeId", "leaveType");
                            var reason = GetStringValue(leaveFormData, "reason");
                            var agentNo = GetStringValue(leaveFormData, "agentNo", "agentId");

                            // 解析附件
                            var attachmentsList = new List<CancelLeaveAttachment>();
                            if (leaveFormData.TryGetProperty("attachments", out var attachmentsElement) &&
                                attachmentsElement.ValueKind == JsonValueKind.Array)
                            {
                                int fileId = 1;
                                foreach (var attachment in attachmentsElement.EnumerateArray())
                                {
                                    var cancelAttachment = new CancelLeaveAttachment
                                    {
                                        Efileid = fileId.ToString()
                                    };

                                    if (attachment.TryGetProperty("fileName", out var fileNameElement))
                                        cancelAttachment.Efilename = fileNameElement.GetString() ?? "";

                                    if (attachment.TryGetProperty("originalFileName", out var originalFileNameElement))
                                        cancelAttachment.Esfilename = originalFileNameElement.GetString() ?? "";

                                    if (attachment.TryGetProperty("fileUrl", out var fileUrlElement))
                                        cancelAttachment.Efileurl = fileUrlElement.GetString() ?? "";

                                    attachmentsList.Add(cancelAttachment);
                                    fileId++;
                                }
                            }
                            
                            // 檢查起始日期是否未到（>= 今天）
                            if (DateTime.TryParse(startDate.Replace("/", "-"), out var startDateTime) && 
                                startDateTime.Date >= today)
                            {
                                // ✅ 檢查 104 DB 的簽核狀態
                                bool isApproved = await CheckLeaveApprovalStatusInDb(
                                    request.Uid, 
                                    leaveTypeCode, 
                                    startDateTime.Date,
                                    DateTime.TryParse(endDate.Replace("/", "-"), out var endDateTime) ? endDateTime.Date : startDateTime.Date
                                );

                                // 只有已簽核完畢 (InsertFlag = 1) 的才加入列表
                                if (isApproved)
                                {
                                    var item = new CancelLeaveItem
                                    {
                                        Uid = request.Uid,
                                        Uname = employeeInfo.EmployeeName ?? "",
                                        Udepartment = employeeInfo.DepartmentName ?? "",
                                        Formid = processSerialNo,
                                        Leavetype = leaveTypeName,
                                        Estartdate = startDate.Replace("/", "-"),
                                        Estarttime = startTime,
                                        Eenddate = endDate.Replace("/", "-"),
                                        Eendtime = endTime,
                                        Ereason = reason,
                                        Eagent = agentNo,
                                        Efiletype = "C",
                                        Attachments = attachmentsList
                                    };
                                    
                                    cancelLeaveItems.Add(item);
                                    _logger.LogInformation("找到可銷假的請假單: {FormId}, {LeaveType}, {StartDate}，簽核狀態已確認", 
                                        processSerialNo, leaveTypeName, startDate);
                                }
                                else
                                {
                                    _logger.LogInformation("請假單 {FormId} 尚未簽核完成，略過", processSerialNo);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "轉換請假單資料失敗，跳過此筆記錄");
                    }
                }

                _logger.LogInformation("查詢到 {Count} 筆可銷假的請假單", cancelLeaveItems.Count);

                return new CancelLeaveListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new CancelLeaveListData
                    {
                        Efleveldata = cancelLeaveItems
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢可銷假的請假單列表時發生錯誤");
                return new CancelLeaveListResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 檢查 104 DB 中的請假申請簽核狀態
        /// 返回 true 表示已簽核完畢 (InsertFlag = 1)
        /// </summary>
        private async Task<bool> CheckLeaveApprovalStatusInDb(string employeeNo, string leaveType, DateTime startDate, DateTime endDate)
        {
            try
            {
                // 如果未配置 104 DB 連接，預設返回 true（允許銷假）
                if (string.IsNullOrEmpty(_hrDatabase104ConnectionString) || 
                    _hrDatabase104ConnectionString == _hrDatabaseConnectionString)
                {
                    _logger.LogWarning("104 DB 未配置，使用預設簽核狀態（允許）");
                    return true;
                }

                using (var connection = new SqlConnection(_hrDatabase104ConnectionString))
                {
                    await connection.OpenAsync();

                    // 查詢 vwZZ_ASK_LEAVE_STATUS 視圖
                    var query = @"
                        SELECT TOP 1 InsertFlag 
                        FROM vwZZ_ASK_LEAVE_STATUS
                        WHERE EmployeeNo = @EmployeeNo 
                          AND LeaveType = @LeaveType
                          AND StartDate = @StartDate
                          AND EndDate = @EndDate
                        ORDER BY CreateDate DESC";

                    var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new
                    {
                        EmployeeNo = employeeNo,
                        LeaveType = leaveType,
                        StartDate = startDate,
                        EndDate = endDate
                    });

                    bool isApproved = result == 1; // InsertFlag = 1 表示已簽核完畢

                    _logger.LogInformation("104 DB 簽核狀態檢查 - 員工: {EmployeeNo}, 假別: {LeaveType}, 日期: {StartDate}, 簽核完畢: {IsApproved}",
                        employeeNo, leaveType, startDate.ToString("yyyy-MM-dd"), isApproved);

                    return isApproved;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢 104 DB 簽核狀態失敗，使用預設狀態（允許）");
                // 如果查詢失敗，預設允許銷假
                return true;
            }
        }

        #endregion

        #region 查詢請假單詳細資料

        /// <summary>
        /// 查詢單一請假單詳細資料
        /// </summary>
        public async Task<CancelLeaveDetailResponse> GetCancelLeaveDetailAsync(CancelLeaveDetailRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢請假單詳細資料，表單編號: {FormId}", request.Formid);

                // 1. 查詢員工基本資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    _logger.LogWarning("找不到員工資訊: {Uid}", request.Uid);
                    return new CancelLeaveDetailResponse
                    {
                        Code = "203",
                        Msg = "找不到員工資訊"
                    };
                }

                // 2. 查詢請假單詳細資料（透過 BPM API）
                var queryEndpoint = $"form/detail?formCode={LEAVE_FORM_CODE}&version={FORM_VERSION}&formId={request.Formid}";

                string responseBody;
                try
                {
                    responseBody = await _bpmService.GetAsync(queryEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "查詢 BPM 請假單詳細資料失敗");
                    return new CancelLeaveDetailResponse
                    {
                        Code = "203",
                        Msg = "查詢請假單詳細資料失敗"
                    };
                }

                // 3. 解析 BPM 回應
                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                if (!bpmResponse.TryGetProperty("success", out var successElement) || 
                    !successElement.GetBoolean())
                {
                    _logger.LogWarning("BPM API 回應失敗: {Response}", responseBody);
                    return new CancelLeaveDetailResponse
                    {
                        Code = "203",
                        Msg = "查詢請假單詳細資料失敗"
                    };
                }

                // 4. 轉換為 APP 格式（包含附件）
                var detailItem = new CancelLeaveDetailItem();

                if (bpmResponse.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("formData", out var formDataElement))
                {
                    try
                    {
                        detailItem = ConvertBpmFormToDetailItem(formDataElement, employeeInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "轉換請假單詳細資料失敗");
                        return new CancelLeaveDetailResponse
                        {
                            Code = "203",
                            Msg = "轉換請假單詳細資料失敗"
                        };
                    }
                }

                _logger.LogInformation("查詢到請假單詳細資料，表單編號: {FormId}", request.Formid);

                return new CancelLeaveDetailResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new CancelLeaveDetailData
                    {
                        Efleveldata = new List<CancelLeaveDetailItem> { detailItem }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單詳細資料時發生錯誤");
                return new CancelLeaveDetailResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        #endregion

        #region 查詢請假單預覽

        /// <summary>
        /// 查詢請假單預覽（用於銷假申請）
        /// </summary>
        public async Task<EFleavePreviewResponse> GetLeavePreviewAsync(EFleavePreviewRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢請假單預覽，表單編號: {FormId}", request.Formid);

                // 1. 查詢員工基本資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    _logger.LogWarning("找不到員工資訊: {Uid}", request.Uid);
                    return new EFleavePreviewResponse
                    {
                        Code = "203",
                        Msg = "找不到員工資訊"
                    };
                }

                // 2. 查詢請假單詳細資料（透過 BPM API）
                var queryEndpoint = $"bpm/process-instances/{request.Formid}";
                string responseBody;
                try
                {
                    responseBody = await _bpmService.GetAsync(queryEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "查詢 BPM 請假單詳細資料失敗");
                    return new EFleavePreviewResponse
                    {
                        Code = "203",
                        Msg = "查詢請假單詳細資料失敗"
                    };
                }

                // 3. 解析 BPM 回應
                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                if (!bpmResponse.TryGetProperty("success", out var successElement) || 
                    !successElement.GetBoolean())
                {
                    _logger.LogWarning("BPM API 回應失敗: {Response}", responseBody);
                    return new EFleavePreviewResponse
                    {
                        Code = "203",
                        Msg = "查詢請假單詳細資料失敗"
                    };
                }

                // 4. 轉換為預覽格式（包含附件）
                var previewData = new EFleavePreviewData();

                if (bpmResponse.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("formData", out var formDataElement))
                {
                    try
                    {
                        // 表單編號
                        previewData.Formid = request.Formid;

                        // 從表單資料中提取字段
                        if (formDataElement.TryGetProperty(LEAVE_FORM_CODE, out var leaveFormData))
                        {
                            // 提取日期時間資訊
                            previewData.Estartdate = GetStringValue(leaveFormData, "startDate");
                            previewData.Estarttime = GetStringValue(leaveFormData, "startTime");
                            previewData.Eenddate = GetStringValue(leaveFormData, "endDate");
                            previewData.Eendtime = GetStringValue(leaveFormData, "endTime");
                            
                            // 提取其他資訊
                            previewData.Ereason = GetStringValue(leaveFormData, "reason");
                            previewData.Eprocess = GetStringValue(leaveFormData, "processType", "leaveProcess");
                            previewData.Efiletype = "D"; // 銷假附件檔

                            // 提取附件列表
                            previewData.Attachments = new List<EFleaveAttachment>();

                            if (leaveFormData.TryGetProperty("attachments", out var attachmentsElement) &&
                                attachmentsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                int fileId = 1;
                                foreach (var attachment in attachmentsElement.EnumerateArray())
                                {
                                    var eattachment = new EFleaveAttachment
                                    {
                                        Efileid = fileId.ToString()
                                    };

                                    if (attachment.TryGetProperty("fileName", out var fileNameElement))
                                        eattachment.Efilename = fileNameElement.GetString() ?? "";

                                    if (attachment.TryGetProperty("originalFileName", out var originalFileNameElement))
                                        eattachment.Esfilename = originalFileNameElement.GetString() ?? "";

                                    if (attachment.TryGetProperty("fileUrl", out var fileUrlElement))
                                        eattachment.Efileurl = fileUrlElement.GetString() ?? "";

                                    if (!string.IsNullOrEmpty(eattachment.Esfilename))
                                    {
                                        previewData.Attachments.Add(eattachment);
                                        fileId++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 嘗試直接從 formData 中提取
                            previewData.Estartdate = GetStringValue(formDataElement, "startDate");
                            previewData.Estarttime = GetStringValue(formDataElement, "startTime");
                            previewData.Eenddate = GetStringValue(formDataElement, "endDate");
                            previewData.Eendtime = GetStringValue(formDataElement, "endTime");
                            previewData.Ereason = GetStringValue(formDataElement, "reason");
                            previewData.Eprocess = GetStringValue(formDataElement, "processType", "leaveProcess");
                            previewData.Efiletype = "D";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "轉換請假單預覽資料失敗");
                        return new EFleavePreviewResponse
                        {
                            Code = "203",
                            Msg = "轉換請假單預覽資料失敗"
                        };
                    }
                }

                _logger.LogInformation("查詢到請假單預覽，表單編號: {FormId}", request.Formid);

                return new EFleavePreviewResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = previewData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單預覽時發生錯誤");
                return new EFleavePreviewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        #endregion

        #region 提交銷假申請

        /// <summary>
        /// 提交銷假申請
        /// </summary>
        public async Task<CancelLeaveSubmitResponse> SubmitCancelLeaveAsync(CancelLeaveSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("開始提交銷假申請，表單編號: {FormId}，原因: {Reasons}", 
                    request.Formid, request.Reasons);

                // 1. 查詢員工基本資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到工號對應的員工資料: {request.Uid}");
                }

                _logger.LogInformation("申請人資料 - 工號: {EmployeeNo}, 姓名: {Name}, 部門: {Dept}",
                    employeeInfo.EmployeeNo, employeeInfo.EmployeeName, employeeInfo.DepartmentName);

                // 2. 查詢原請假單資料（使用正確的 BPM 端點）
                var leaveFormEndpoint = $"bpm/process-instances/{request.Formid}";
                string leaveFormResponse;
                try
                {
                    leaveFormResponse = await _bpmService.GetAsync(leaveFormEndpoint);
                    _logger.LogDebug("原請假單資料: {Response}", leaveFormResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "查詢原請假單失敗，嘗試使用備用方案");
                    
                    // 如果查詢失敗，使用最小必要欄位（讓 BPM 自己處理）
                    var minimalFormData = new Dictionary<string, object?>
                    {
                        ["originalFormId"] = request.Formid,
                        ["cancelReason"] = request.Reasons
                    };

                    var minimalBpmRequest = new
                    {
                        processCode = $"{FORM_CODE}_PROCESS",
                        formDataMap = new Dictionary<string, object>
                        {
                            [FORM_CODE] = minimalFormData
                        },
                        userId = employeeInfo.EmployeeNo,
                        subject = $"{employeeInfo.EmployeeName} 的銷假申請 - {request.Formid}",
                        sourceSystem = "APP",
                        environment = "TEST",
                        hasAttachments = false
                    };

                    try
                    {
                        var minimalEndpoint = "bpm/invoke-process";
                        var minimalResponse = await _bpmService.PostAsync(minimalEndpoint, minimalBpmRequest);
                        var minimalJsonResponse = JsonSerializer.Deserialize<JsonElement>(minimalResponse);

                        var minReqId = GetStringValue(minimalJsonResponse, "requestId");
                        var minProcSerialNo = GetStringValue(minimalJsonResponse, "processSerialNo");
                        var minStatus = GetStringValue(minimalJsonResponse, "status");
                        var minMsg = GetStringValue(minimalJsonResponse, "message");

                        Console.WriteLine("========================================");
                        Console.WriteLine("✅ 銷假單送出成功（使用簡化模式）");
                        Console.WriteLine($"📋 流程編號: {minProcSerialNo}");
                        Console.WriteLine($"🆔 請求ID: {minReqId}");
                        Console.WriteLine($"👤 申請人: {employeeInfo.EmployeeName} ({employeeInfo.EmployeeNo})");
                        Console.WriteLine($"📄 原請假單: {request.Formid}");
                        Console.WriteLine($"📝 銷假原因: {request.Reasons}");
                        Console.WriteLine($"✔️  狀態: {minStatus}");
                        Console.WriteLine($"💬 訊息: {minMsg}");
                        Console.WriteLine("========================================");

                        return new CancelLeaveSubmitResponse
                        {
                            Code = "200",
                            Msg = "請求成功"
                        };
                    }
                    catch (Exception submitEx)
                    {
                        _logger.LogError(submitEx, "簡化模式提交也失敗");
                        return new CancelLeaveSubmitResponse
                        {
                            Code = "203",
                            Msg = $"提交銷假單失敗: {submitEx.Message}"
                        };
                    }
                }

                // 3. 解析原請假單資料
                var leaveFormJson = JsonSerializer.Deserialize<JsonElement>(leaveFormResponse);
                
                // 從 formData 中取得原請假單的詳細資訊
                JsonElement originalFormData;
                if (leaveFormJson.TryGetProperty("formData", out var formDataProp) &&
                    formDataProp.TryGetProperty(LEAVE_FORM_CODE, out originalFormData))
                {
                    // 成功取得原請假單資料
                }
                else
                {
                    _logger.LogError("無法從 BPM 回應中解析原請假單資料");
                    return new CancelLeaveSubmitResponse
                    {
                        Code = "203",
                        Msg = "無法解析原請假單資料"
                    };
                }

                // 4. 建構銷假單資料（包含原請假單的所有必要欄位）
                var formData = new Dictionary<string, object?>
                {
                    // 銷假特有欄位
                    ["originalFormId"] = request.Formid,
                    ["cancelReason"] = request.Reasons,
                    
                    // 從原請假單複製必要欄位
                    ["startDate"] = GetStringValue(originalFormData, "startDate"),
                    ["startTime"] = GetStringValue(originalFormData, "startTime"),
                    ["endDate"] = GetStringValue(originalFormData, "endDate"),
                    ["endTime"] = GetStringValue(originalFormData, "endTime"),
                    ["leaveTypeId"] = GetStringValue(originalFormData, "leaveTypeId"),
                    ["leaveTypeName"] = GetStringValue(originalFormData, "leaveTypeName"),
                    ["agentNo"] = GetStringValue(originalFormData, "agentNo"),
                    ["reason"] = GetStringValue(originalFormData, "reason")
                };

                var bpmRequest = new
                {
                    processCode = $"{FORM_CODE}_PROCESS",
                    formDataMap = new Dictionary<string, object>
                    {
                        [FORM_CODE] = formData
                    },
                    userId = employeeInfo.EmployeeNo,
                    subject = $"{employeeInfo.EmployeeName} 的銷假申請 - {request.Formid}",
                    sourceSystem = "APP",
                    environment = "TEST",
                    hasAttachments = false
                };

                // 3. 呼叫 BPM API 建立銷假單
                var endpoint = "bpm/invoke-process";
                var response = await _bpmService.PostAsync(endpoint, bpmRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                // 4. 解析回應
                var requestId = GetStringValue(jsonResponse, "requestId");
                var processSerialNo = GetStringValue(jsonResponse, "processSerialNo");
                var bpmProcessOid = GetStringValue(jsonResponse, "bpmProcessOid");
                var status = GetStringValue(jsonResponse, "status");
                var message = GetStringValue(jsonResponse, "message");

                // 在 Console 顯示銷假單資訊
                Console.WriteLine("========================================");
                Console.WriteLine("✅ 銷假單送出成功");
                Console.WriteLine($"📋 流程編號: {processSerialNo}");
                Console.WriteLine($"🆔 請求ID: {requestId}");
                Console.WriteLine($"🔑 BPM流程OID: {bpmProcessOid}");
                Console.WriteLine($"👤 申請人: {employeeInfo.EmployeeName} ({employeeInfo.EmployeeNo})");
                Console.WriteLine($"📄 原請假單: {request.Formid}");
                Console.WriteLine($"📝 銷假原因: {request.Reasons}");
                Console.WriteLine($"✔️  狀態: {status}");
                Console.WriteLine($"💬 訊息: {message}");
                Console.WriteLine("========================================");
                
                _logger.LogInformation("銷假申請提交成功 - ProcessSerialNo: {ProcessSerialNo}, RequestId: {RequestId}, Status: {Status}", 
                    processSerialNo, requestId, status);

                return new CancelLeaveSubmitResponse
                {
                    Code = "200",
                    Msg = "請求成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交銷假申請時發生錯誤");
                return new CancelLeaveSubmitResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        #endregion

        #region 私有輔助方法

        /// <summary>
        /// 轉換 BPM 表單資料為銷假列表項目
        /// </summary>
        private CancelLeaveItem ConvertBpmFormToCancelLeaveItem(JsonElement form, EmployeeBasicInfo employeeInfo)
        {
            var item = new CancelLeaveItem
            {
                Uid = employeeInfo.EmployeeNo ?? "",
                Uname = employeeInfo.EmployeeName ?? "",
                Udepartment = employeeInfo.DepartmentName ?? ""
            };

            // 解析表單資料
            if (form.TryGetProperty("formId", out var formIdElement))
                item.Formid = formIdElement.GetString() ?? "";

            if (form.TryGetProperty("formData", out var formDataElement))
            {
                // 請假類別
                if (formDataElement.TryGetProperty("leaveType", out var leaveTypeElement))
                    item.Leavetype = leaveTypeElement.GetString() ?? "";

                // 請假起始日期
                if (formDataElement.TryGetProperty("startDate", out var startDateElement))
                    item.Estartdate = startDateElement.GetString() ?? "";

                // 請假起始時間
                if (formDataElement.TryGetProperty("startTime", out var startTimeElement))
                    item.Estarttime = startTimeElement.GetString() ?? "";

                // 請假結束日期
                if (formDataElement.TryGetProperty("endDate", out var endDateElement))
                    item.Eenddate = endDateElement.GetString() ?? "";

                // 請假結束時間
                if (formDataElement.TryGetProperty("endTime", out var endTimeElement))
                    item.Eendtime = endTimeElement.GetString() ?? "";

                // 請假事由
                if (formDataElement.TryGetProperty("reason", out var reasonElement))
                    item.Ereason = reasonElement.GetString() ?? "";
            }

            return item;
        }

        /// <summary>
        /// 轉換 BPM 表單資料為銷假詳細項目（包含附件）
        /// </summary>
        private CancelLeaveDetailItem ConvertBpmFormToDetailItem(JsonElement formData, EmployeeBasicInfo employeeInfo)
        {
            var item = new CancelLeaveDetailItem
            {
                Uid = employeeInfo.EmployeeNo ?? "",
                Uname = employeeInfo.EmployeeName ?? "",
                Udepartment = employeeInfo.DepartmentName ?? ""
            };

            // 解析表單資料
            if (formData.TryGetProperty("formId", out var formIdElement))
                item.Formid = formIdElement.GetString() ?? "";

            // 請假類別
            if (formData.TryGetProperty("leaveType", out var leaveTypeElement))
                item.Leavetype = leaveTypeElement.GetString() ?? "";

            // 請假起始日期
            if (formData.TryGetProperty("startDate", out var startDateElement))
                item.Estartdate = startDateElement.GetString() ?? "";

            // 請假起始時間
            if (formData.TryGetProperty("startTime", out var startTimeElement))
                item.Estarttime = startTimeElement.GetString() ?? "";

            // 請假結束日期
            if (formData.TryGetProperty("endDate", out var endDateElement))
                item.Eenddate = endDateElement.GetString() ?? "";

            // 請假結束時間
            if (formData.TryGetProperty("endTime", out var endTimeElement))
                item.Eendtime = endTimeElement.GetString() ?? "";

            // 請假事由
            if (formData.TryGetProperty("reason", out var reasonElement))
                item.Ereason = reasonElement.GetString() ?? "";

            // 代理人工號
            if (formData.TryGetProperty("agentId", out var agentIdElement))
                item.Eagent = agentIdElement.GetString() ?? "";

            // 附件處理
            item.Efiletype = "C"; // 請假附件檔
            item.Attachments = new List<CancelLeaveAttachment>();

            if (formData.TryGetProperty("attachments", out var attachmentsElement) &&
                attachmentsElement.ValueKind == JsonValueKind.Array)
            {
                int fileId = 1;
                foreach (var attachment in attachmentsElement.EnumerateArray())
                {
                    var cancelAttachment = new CancelLeaveAttachment
                    {
                        Efileid = fileId.ToString()
                    };

                    if (attachment.TryGetProperty("fileName", out var fileNameElement))
                        cancelAttachment.Efilename = fileNameElement.GetString() ?? "";

                    if (attachment.TryGetProperty("originalFileName", out var originalFileNameElement))
                        cancelAttachment.Esfilename = originalFileNameElement.GetString() ?? "";

                    if (attachment.TryGetProperty("fileUrl", out var fileUrlElement))
                        cancelAttachment.Efileurl = fileUrlElement.GetString() ?? "";

                    item.Attachments.Add(cancelAttachment);
                    fileId++;
                }
            }

            return item;
        }

        /// <summary>
        /// 從 JSON 元素中取得字串值（支援多個可能的 key）
        /// </summary>
        private string GetStringValue(JsonElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (element.TryGetProperty(key, out var value))
                {
                    return value.GetString() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        #endregion
    }
}
