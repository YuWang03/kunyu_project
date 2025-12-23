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
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
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
            ILeaveApplicationRepository leaveApplicationRepository,
            ILogger<CancelLeaveService> logger,
            IConfiguration configuration)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _leaveApplicationRepository = leaveApplicationRepository;
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
        /// 返回使用者自己提交的請假表單（用於銷假申請）
        /// 如果提供 formid，則只查詢該單筆資料
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

                // 2. 如果提供了 formid，則直接查詢該單筆資料並返回列表
                if (!string.IsNullOrEmpty(request.Formid))
                {
                    Console.WriteLine($"====== 查詢單筆請假單（從資料庫）: {request.Formid} ======");
                    var singleItem = await _leaveApplicationRepository.GetLeaveApplicationByFormIdAsync(request.Formid, request.Uid);
                    
                    if (singleItem != null)
                    {
                        return new CancelLeaveListResponse
                        {
                            Code = "200",
                            Msg = "請求成功",
                            Data = new CancelLeaveListData
                            {
                                Efleveldata = new List<CancelLeaveItem> { singleItem }
                            }
                        };
                    }
                    else
                    {
                        return new CancelLeaveListResponse
                        {
                            Code = "203",
                            Msg = "查無資料"
                        };
                    }
                }

                // 3. 從資料庫查詢使用者的請假單列表（起始日未到）
                Console.WriteLine($"====== 開始查詢使用者 {request.Uid} 的請假單（從資料庫） ======");
                var leaveList = await _leaveApplicationRepository.GetUserLeaveApplicationsAsync(request.Uid);

                Console.WriteLine($"====== 查詢完成，共找到 {leaveList.Count} 筆可銷假的請假單 ======");
                _logger.LogInformation("查詢完成，共找到 {Count} 筆可銷假的請假單", leaveList.Count);

                return new CancelLeaveListResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new CancelLeaveListData
                    {
                        Efleveldata = leaveList
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

        #endregion

        #region 查詢單筆請假資料

        /// <summary>
        /// 查詢單筆請假資料（根據 formid）
        /// </summary>
        public async Task<CancelLeaveSingleResponse> GetCancelLeaveSingleAsync(CancelLeaveSingleRequest request)
        {
            try
            {
                _logger.LogInformation("開始查詢單筆請假資料，表單編號: {FormId}", request.Formid);

                // 1. 查詢員工基本資訊
                var employeeInfo = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employeeInfo == null)
                {
                    _logger.LogWarning("找不到員工資訊: {Uid}", request.Uid);
                    return new CancelLeaveSingleResponse
                    {
                        Code = "203",
                        Msg = "找不到員工資訊"
                    };
                }

                // 2. 透過 BPM API 查詢表單資料
                var syncProcessEndpoint = $"bpm/sync-process-info?processSerialNo={request.Formid}&processCode=PI_LEAVE_001_PROCESS&environment=TEST";
                _logger.LogInformation("查詢表單詳細資訊: {Endpoint}", syncProcessEndpoint);
                
                string syncProcessResponse;
                try
                {
                    syncProcessResponse = await _bpmService.GetAsync(syncProcessEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "查詢 BPM 表單資料失敗 - FormId: {FormId}", request.Formid);
                    return new CancelLeaveSingleResponse
                    {
                        Code = "203",
                        Msg = "查詢表單資料失敗"
                    };
                }

                var syncProcessJson = JsonSerializer.Deserialize<JsonElement>(syncProcessResponse);
                _logger.LogInformation("BPM API 回應: {Response}", syncProcessResponse);

                // 檢查 API 回應狀態
                if (!syncProcessJson.TryGetProperty("status", out var statusElement) || 
                    statusElement.GetString() != "SUCCESS")
                {
                    var actualStatus = syncProcessJson.TryGetProperty("status", out var status) ? status.GetString() : "N/A";
                    _logger.LogWarning("BPM API 回應狀態異常 - FormId: {FormId}, 狀態: {Status}", request.Formid, actualStatus);
                    return new CancelLeaveSingleResponse
                    {
                        Code = "203",
                        Msg = "查無資料"
                    };
                }

                // 3. 解析表單資料
                if (!syncProcessJson.TryGetProperty("formInfo", out var formInfo) ||
                    !formInfo.TryGetProperty("PI_LEAVE_001", out var leaveFormData))
                {
                    _logger.LogWarning("表單資料格式異常 - FormId: {FormId}", request.Formid);
                    return new CancelLeaveSingleResponse
                    {
                        Code = "203",
                        Msg = "表單資料格式異常"
                    };
                }

                // 解析必要欄位
                var leaveTypeName = leaveFormData.TryGetProperty("leaveType_name", out var ltName) ? ltName.GetString() : "";
                var startDate = leaveFormData.TryGetProperty("startDate", out var sd) ? sd.GetString() : "";
                var startTime = leaveFormData.TryGetProperty("startTime", out var st) ? st.GetString() : "";
                var endDate = leaveFormData.TryGetProperty("endDate", out var ed) ? ed.GetString() : "";
                var endTime = leaveFormData.TryGetProperty("endTime", out var et) ? et.GetString() : "";
                var reason = leaveFormData.TryGetProperty("reason", out var r) ? r.GetString() : "";
                var agentId = leaveFormData.TryGetProperty("agentId", out var ai) ? ai.GetString() : "";
                var agentNo = leaveFormData.TryGetProperty("agentNo", out var an) ? an.GetString() : "";
                var eventDate = leaveFormData.TryGetProperty("eventDate", out var ed2) ? ed2.GetString() : "";

                // 使用 agentId，如果沒有則使用 agentNo
                var agent = !string.IsNullOrEmpty(agentId) ? agentId : agentNo;

                // 解析申請人資訊
                var requesterIdEmployeeId = leaveFormData.TryGetProperty("requesterId_employeeId", out var reqEmpId) ? reqEmpId.GetString() : "";
                var requesterName = leaveFormData.TryGetProperty("requesterId_name", out var reqName) ? reqName.GetString() : "";
                var orgName = leaveFormData.TryGetProperty("requesterId_orgName", out var orgN) ? orgN.GetString() : "";

                // 如果有 applierId，優先使用
                var applierIdEmployeeId = leaveFormData.TryGetProperty("applierId_employeeId", out var applierIdEl)
                    ? applierIdEl.GetString() 
                    : requesterIdEmployeeId;
                var applierName = leaveFormData.TryGetProperty("applierId_name", out var applierNameEl)
                    ? applierNameEl.GetString()
                    : requesterName;

                // 格式化日期
                var formattedStartDate = startDate?.Replace("/", "-") ?? "";
                var formattedEndDate = endDate?.Replace("/", "-") ?? "";
                var formattedEventDate = eventDate?.Replace("/", "-") ?? "";

                // 如果從 BPM 取不到申請人資訊，使用 request.Uid 對應的員工資訊
                if (string.IsNullOrEmpty(applierIdEmployeeId))
                {
                    applierIdEmployeeId = request.Uid ?? "";
                    _logger.LogWarning("從 BPM 無法取得申請人工號，使用 request.Uid: {Uid}", request.Uid);
                }

                if (string.IsNullOrEmpty(applierName))
                {
                    applierName = employeeInfo?.EmployeeName ?? "";
                    _logger.LogWarning("從 BPM 無法取得申請人姓名，使用員工基本資訊: {Name}", employeeInfo?.EmployeeName);
                }

                if (string.IsNullOrEmpty(orgName))
                {
                    orgName = employeeInfo?.DepartmentName ?? "";
                    _logger.LogWarning("從 BPM 無法取得申請人單位，使用員工基本資訊: {Department}", employeeInfo?.DepartmentName);
                }

                // 建立回應資料
                var item = new CancelLeaveItem
                {
                    Uid = applierIdEmployeeId ?? "",
                    Uname = applierName ?? "",
                    Udepartment = orgName ?? "",
                    Formid = request.Formid,
                    Leavetype = leaveTypeName ?? "",
                    Estartdate = formattedStartDate,
                    Estarttime = startTime ?? "",
                    Eenddate = formattedEndDate,
                    Eendtime = endTime ?? "",
                    Ereason = reason ?? "",
                    Eagent = agent ?? "",
                    Eleavedate = formattedEventDate
                };

                _logger.LogInformation("成功查詢單筆請假資料 - FormId: {FormId}, 申請人: {Name} ({Uid})", request.Formid, applierName, applierIdEmployeeId);

                return new CancelLeaveSingleResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = new CancelLeaveSingleData
                    {
                        Efleveldata = item
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢單筆請假資料時發生錯誤 - FormId: {FormId}", request.Formid);
                return new CancelLeaveSingleResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 查詢單筆請假資料的輔助方法（用於列表查詢）
        /// </summary>
        private async Task<CancelLeaveItem?> GetSingleLeaveItemAsync(string formId, string uid)
        {
            try
            {
                Console.WriteLine($"  查詢表單: {formId}");
                
                var syncProcessEndpoint = $"bpm/sync-process-info?processSerialNo={formId}&processCode=PI_LEAVE_001_PROCESS&environment=TEST";
                var syncProcessResponse = await _bpmService.GetAsync(syncProcessEndpoint);
                var syncProcessJson = JsonSerializer.Deserialize<JsonElement>(syncProcessResponse);
                
                if (!syncProcessJson.TryGetProperty("status", out var syncStatus) || 
                    syncStatus.GetString() != "SUCCESS")
                {
                    Console.WriteLine($"  ❌ 取得表單詳細資訊失敗");
                    return null;
                }

                if (!syncProcessJson.TryGetProperty("formInfo", out var formInfo) ||
                    !formInfo.TryGetProperty("PI_LEAVE_001", out var leaveForm))
                {
                    Console.WriteLine($"  ⚠️ 找不到 formInfo.PI_LEAVE_001 欄位");
                    return null;
                }

                // 解析申請人資訊
                var requesterIdEmployeeId = leaveForm.TryGetProperty("requesterId_employeeId", out var reqEmpId) ? reqEmpId.GetString() : "";
                var requesterName = leaveForm.TryGetProperty("requesterId_name", out var reqName) ? reqName.GetString() : "";
                var orgName = leaveForm.TryGetProperty("requesterId_orgName", out var orgN) ? orgN.GetString() : "";

                // 如果有 applierId，優先使用
                var applierIdEmployeeId = leaveForm.TryGetProperty("applierId_employeeId", out var applierIdEl)
                    ? applierIdEl.GetString() 
                    : requesterIdEmployeeId;
                var applierName = leaveForm.TryGetProperty("applierId_name", out var applierNameEl)
                    ? applierNameEl.GetString()
                    : requesterName;
                
                // 驗證是否為該使用者的表單
                if (applierIdEmployeeId != uid)
                {
                    Console.WriteLine($"  ⚠️ 表單不屬於使用者 {uid}，實際申請人: {applierIdEmployeeId}");
                    return null;
                }

                // 解析請假單資料
                var leaveTypeName = leaveForm.TryGetProperty("leaveType_name", out var ltName) ? ltName.GetString() : "";
                var startDate = leaveForm.TryGetProperty("startDate", out var sd) ? sd.GetString() : "";
                var startTime = leaveForm.TryGetProperty("startTime", out var st) ? st.GetString() : "";
                var endDate = leaveForm.TryGetProperty("endDate", out var ed) ? ed.GetString() : "";
                var endTime = leaveForm.TryGetProperty("endTime", out var et) ? et.GetString() : "";
                var reason = leaveForm.TryGetProperty("reason", out var r) ? r.GetString() : "";
                
                var formattedStartDate = startDate?.Replace("/", "-") ?? "";
                var formattedEndDate = endDate?.Replace("/", "-") ?? "";
                
                var item = new CancelLeaveItem
                {
                    Uid = applierIdEmployeeId ?? "",
                    Uname = applierName ?? "",
                    Udepartment = orgName ?? "",
                    Formid = formId,
                    Leavetype = leaveTypeName ?? "",
                    Estartdate = formattedStartDate,
                    Estarttime = startTime ?? "",
                    Eenddate = formattedEndDate,
                    Eendtime = endTime ?? "",
                    Ereason = reason ?? ""
                };
                
                Console.WriteLine($"  ✅ 成功取得請假單: {formId}, 申請人: {applierName} ({applierIdEmployeeId})");
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 查詢表單詳細資訊時發生錯誤: {ex.Message}");
                _logger.LogError(ex, "查詢表單詳細資訊時發生錯誤 - FormId: {FormId}", formId);
                return null;
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

                // 2. 直接返回成功（不調用 BPM 或遠端資料庫）
                Console.WriteLine("========================================");
                Console.WriteLine("✅ 銷假單送出成功");
                Console.WriteLine($"👤 申請人: {employeeInfo.EmployeeName} ({employeeInfo.EmployeeNo})");
                Console.WriteLine($"📄 原請假單: {request.Formid}");
                Console.WriteLine($"📝 銷假原因: {request.Reasons}");
                Console.WriteLine($"🕐 提交時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("========================================");

                _logger.LogInformation("銷假申請已成功提交 - 表單編號: {FormId}, 申請人: {EmployeeName}", 
                    request.Formid, employeeInfo.EmployeeName);

                return new CancelLeaveSubmitResponse
                {
                    Code = "200",
                    Msg = "請求成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "銷假申請送出 API 發生錯誤");
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
