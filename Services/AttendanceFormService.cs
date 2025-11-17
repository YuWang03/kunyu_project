using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public class AttendanceFormService : IAttendanceFormService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<AttendanceFormService> _logger;
        private const string FORM_CODE = "Attendance_Exception_001";
        private const string FORM_VERSION = "1.0.0";

        public AttendanceFormService(
            BpmService bpmService,
            FtpService ftpService,
            IBasicInfoService basicInfoService,
            ILogger<AttendanceFormService> logger)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        public async Task<AttendanceFormOperationResult> CreateAttendanceFormAsync(CreateAttendanceFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始申請出勤異常單: {@Request}", new { request.Email, request.ApplyDate });

                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.Email);
                if (employeeInfo == null)
                {
                    throw new Exception($"找不到 Email 對應的員工資料: {request.Email}");
                }

                _logger.LogInformation("申請人資料 - 工號: {EmployeeNo}, 姓名: {Name}", employeeInfo.EmployeeNo, employeeInfo.EmployeeName);

                Dictionary<string, object?>? computedData = null;
                try
                {
                    var previewEndpoint = $"form-preview/preview?formCode={FORM_CODE}&version={FORM_VERSION}";
                    var previewRequest = new { userId = employeeInfo.EmployeeNo };
                    
                    _logger.LogInformation("呼叫表單預覽 API");
                    var previewResponse = await _bpmService.PostAsync(previewEndpoint, previewRequest);
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
                                    computedData[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
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

                string? filePath = null;
                if (request.Attachments != null && request.Attachments.Count > 0)
                {
                    try
                    {
                        var uploadedFiles = new List<string>();
                        foreach (var file in request.Attachments)
                        {
                            var fileName = $"outing_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid()}_{file.FileName}";
                            var remotePath = $"/uploads/outing/{fileName}";
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

                var formData = BuildFormData(request, employeeInfo, computedData, filePath);
                var bpmRequest = new BpmCreateFormRequest
                {
                    FormCode = FORM_CODE,
                    FormVersion = FORM_VERSION,
                    UserId = employeeInfo.EmployeeNo,
                    Subject = $"出勤異常申請 - {request.ApplyDate}",
                    SourceSystem = "HRSystemAPI",
                    HasAttachments = !string.IsNullOrEmpty(filePath),
                    FormData = formData
                };

                var response = await _bpmService.PostAsync("bpm/invoke-process", bpmRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                var formId = ExtractValue(jsonResponse, "bpmProcessOid", "formId", "id");
                var formNumber = ExtractValue(jsonResponse, "processSerialNo", "formNumber", "formNo");

                _logger.LogInformation("出勤異常單申請成功 - FormId: {FormId}, FormNumber: {FormNumber}", formId, formNumber);

                return new AttendanceFormOperationResult
                {
                    Success = true,
                    Message = "出勤異常單申請成功",
                    FormId = formId,
                    FormNumber = formNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請出勤異常單失敗");
                return new AttendanceFormOperationResult
                {
                    Success = false,
                    Message = $"申請失敗: {ex.Message}",
                    ErrorCode = "CREATE_FAILED"
                };
            }
        }

        private Dictionary<string, object?> BuildFormData(CreateAttendanceFormRequest request, EmployeeBasicInfo employeeInfo, Dictionary<string, object?>? computedData, string? filePath)
        {
            var formData = new Dictionary<string, object?>
            {
                ["applyDate"] = request.ApplyDate.Replace("-", "/"),
                ["exceptionDescription"] = request.ExceptionDescription
            };

            if (computedData != null)
            {
                foreach (var kvp in computedData)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value?.ToString())) formData[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                formData["userId"] = employeeInfo.EmployeeNo;
                formData["requesterId"] = employeeInfo.EmployeeNo;
                formData["employeeId"] = employeeInfo.EmployeeNo;
                formData["requesterName"] = employeeInfo.EmployeeName;
                formData["departmentId"] = employeeInfo.DepartmentId ?? "";
                formData["departmentName"] = employeeInfo.DepartmentName ?? "";
                formData["plant"] = "PI";
                formData["companyNo"] = employeeInfo.CompanyId ?? "03546618";
            }

            if (!string.IsNullOrEmpty(filePath)) formData["filePath"] = filePath;
            formData["formType"] = request.FormType ?? "H1A";
            formData["exceptionReason"] = request.ExceptionReason ?? "其他";
            if (!string.IsNullOrWhiteSpace(request.ExceptionTime)) formData["exceptionTime"] = request.ExceptionTime;
            if (!string.IsNullOrWhiteSpace(request.ExceptionEndTime)) formData["exceptionEndTime"] = request.ExceptionEndTime;

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

        /// <summary>
        /// 查詢出勤異常單列表
        /// </summary>
        public async Task<PagedResponse<AttendanceFormSummary>> GetFormsAsync(GetFormsQuery query)
        {
            try
            {
                _logger.LogInformation("查詢出勤異常單列表: {@Query}", query);

                // 建立查詢參數
                var queryParams = new Dictionary<string, string>
                {
                    ["pageNumber"] = query.PageNumber.ToString(),
                    ["pageSize"] = query.PageSize.ToString()
                };

                // 如果有 Email，先取得員工工號
                if (!string.IsNullOrWhiteSpace(query.Email))
                {
                    var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(query.Email);
                    if (employeeInfo != null)
                    {
                        queryParams["userId"] = employeeInfo.EmployeeNo;
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                    queryParams["status"] = query.Status;
                if (!string.IsNullOrWhiteSpace(query.StartDate))
                    queryParams["startDate"] = query.StartDate;
                if (!string.IsNullOrWhiteSpace(query.EndDate))
                    queryParams["endDate"] = query.EndDate;

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var endpoint = $"bpm/forms?formCode={FORM_CODE}&{queryString}";

                var response = await _bpmService.GetAsync(endpoint);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                var forms = new List<AttendanceFormSummary>();
                var totalCount = 0;

                if (jsonResponse.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("items", out var itemsElement))
                    {
                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            var formData = item.TryGetProperty("formData", out var fd) ? fd : item;
                            
                            forms.Add(new AttendanceFormSummary
                            {
                                Id = GetJsonString(item, "bpmProcessOid", "id", "formId"),
                                FormNumber = GetJsonString(item, "processSerialNo", "formNumber"),
                                ApplyDate = GetJsonString(formData, "applyDate"),
                                ExceptionTime = GetJsonString(formData, "exceptionTime"),
                                ExceptionEndTime = GetJsonString(formData, "exceptionEndTime"),
                                ExceptionReason = GetJsonString(formData, "exceptionReason"),
                                ExceptionDescription = GetJsonString(formData, "exceptionDescription"),
                                CreatedAt = GetJsonString(item, "createdAt", "createTime"),
                                Status = GetJsonString(item, "status", "processStatus"),
                                RequesterName = GetJsonString(formData, "requesterName"),
                                RequesterId = GetJsonString(formData, "requesterId", "userId"),
                                DepartmentName = GetJsonString(formData, "departmentName")
                            });
                        }
                    }

                    if (dataElement.TryGetProperty("totalCount", out var totalElement))
                    {
                        totalCount = totalElement.GetInt32();
                    }
                }

                return new PagedResponse<AttendanceFormSummary>
                {
                    Success = true,
                    Message = "查詢成功",
                    Data = forms,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤異常單列表失敗");
                return new PagedResponse<AttendanceFormSummary>
                {
                    Success = false,
                    Message = $"查詢失敗: {ex.Message}",
                    Data = new List<AttendanceFormSummary>(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }
        }

        /// <summary>
        /// 查詢單一出勤異常單詳細資料
        /// </summary>
        public async Task<ApiResponse<AttendanceFormDetailResponse>> GetFormByIdAsync(string formId)
        {
            try
            {
                _logger.LogInformation("查詢出勤異常單詳細資料: {FormId}", formId);

                if (string.IsNullOrWhiteSpace(formId))
                {
                    return new ApiResponse<AttendanceFormDetailResponse>
                    {
                        Success = false,
                        Message = "表單 ID 不可為空"
                    };
                }

                var endpoint = $"bpm/forms/{formId}";
                var response = await _bpmService.GetAsync(endpoint);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                AttendanceFormDetailResponse? formDetail = null;

                if (jsonResponse.TryGetProperty("data", out var dataElement))
                {
                    var formData = dataElement.TryGetProperty("formData", out var fd) ? fd : dataElement;

                    formDetail = new AttendanceFormDetailResponse
                    {
                        Id = GetJsonString(dataElement, "bpmProcessOid", "id", "formId"),
                        FormNumber = GetJsonString(dataElement, "processSerialNo", "formNumber"),
                        ApplyDate = GetJsonString(formData, "applyDate"),
                        ExceptionTime = GetJsonString(formData, "exceptionTime"),
                        ExceptionEndTime = GetJsonString(formData, "exceptionEndTime"),
                        ExceptionReason = GetJsonString(formData, "exceptionReason"),
                        ExceptionDescription = GetJsonString(formData, "exceptionDescription"),
                        CreatedAt = GetJsonString(dataElement, "createdAt", "createTime"),
                        Status = GetJsonString(dataElement, "status", "processStatus"),
                        RequesterName = GetJsonString(formData, "requesterName"),
                        RequesterId = GetJsonString(formData, "requesterId", "userId"),
                        DepartmentName = GetJsonString(formData, "departmentName"),
                        ApproverId1 = GetJsonString(formData, "approverId1"),
                        ApproverId2 = GetJsonString(formData, "approverId2"),
                        FilePath = GetJsonString(formData, "filePath"),
                        FormType = GetJsonString(formData, "formType"),
                        Plant = GetJsonString(formData, "plant"),
                        CompanyNo = GetJsonString(formData, "companyNo"),
                        FormData = formData,
                        Remarks = GetJsonString(dataElement, "remarks", "note")
                    };
                }

                return new ApiResponse<AttendanceFormDetailResponse>
                {
                    Success = formDetail != null,
                    Message = formDetail != null ? "查詢成功" : "找不到表單資料",
                    Data = formDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤異常單詳細資料失敗: {FormId}", formId);
                return new ApiResponse<AttendanceFormDetailResponse>
                {
                    Success = false,
                    Message = $"查詢失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 取消出勤異常單（呼叫 abort-process）
        /// </summary>
        public async Task<AttendanceFormOperationResult> CancelFormAsync(string formId, CancelFormRequest request)
        {
            try
            {
                _logger.LogInformation("取消出勤異常單: {FormId}, Email: {Email}", formId, request.Email);

                if (string.IsNullOrWhiteSpace(formId))
                {
                    return new AttendanceFormOperationResult
                    {
                        Success = false,
                        Message = "表單 ID 不可為空",
                        ErrorCode = "INVALID_FORM_ID"
                    };
                }

                // 取得員工工號
                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.Email);
                if (employeeInfo == null)
                {
                    return new AttendanceFormOperationResult
                    {
                        Success = false,
                        Message = $"找不到 Email 對應的員工資料: {request.Email}",
                        ErrorCode = "EMPLOYEE_NOT_FOUND"
                    };
                }

                // 呼叫 BPM abort-process API
                var abortRequest = new
                {
                    processOid = formId,
                    userId = employeeInfo.EmployeeNo,
                    reason = request.Reason
                };

                var response = await _bpmService.PostAsync("bpm/abort-process", abortRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                var success = jsonResponse.TryGetProperty("success", out var successElement) 
                    ? successElement.GetBoolean() 
                    : false;

                return new AttendanceFormOperationResult
                {
                    Success = success,
                    Message = success ? "表單取消成功" : "表單取消失敗",
                    FormId = formId,
                    ErrorCode = success ? null : "CANCEL_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消出勤異常單失敗: {FormId}", formId);
                return new AttendanceFormOperationResult
                {
                    Success = false,
                    Message = $"取消失敗: {ex.Message}",
                    ErrorCode = "CANCEL_ERROR"
                };
            }
        }

        /// <summary>
        /// 退回工作項目（呼叫 return workitem）
        /// </summary>
        public async Task<AttendanceFormOperationResult> ReturnWorkItemAsync(string workItemId, ReturnWorkItemRequest request)
        {
            try
            {
                _logger.LogInformation("退回工作項目: {WorkItemId}, Email: {Email}", workItemId, request.Email);

                if (string.IsNullOrWhiteSpace(workItemId))
                {
                    return new AttendanceFormOperationResult
                    {
                        Success = false,
                        Message = "工作項目 ID 不可為空",
                        ErrorCode = "INVALID_WORKITEM_ID"
                    };
                }

                // 取得員工工號
                var employeeInfo = await _basicInfoService.GetEmployeeByEmailAsync(request.Email);
                if (employeeInfo == null)
                {
                    return new AttendanceFormOperationResult
                    {
                        Success = false,
                        Message = $"找不到 Email 對應的員工資料: {request.Email}",
                        ErrorCode = "EMPLOYEE_NOT_FOUND"
                    };
                }

                // 呼叫 BPM return workitem API
                var returnRequest = new
                {
                    workItemId = workItemId,
                    userId = employeeInfo.EmployeeNo,
                    action = "return",
                    comment = request.Reason
                };

                var response = await _bpmService.PostAsync($"bpm/workitems/{workItemId}/return", returnRequest);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                var success = jsonResponse.TryGetProperty("success", out var successElement) 
                    ? successElement.GetBoolean() 
                    : false;

                return new AttendanceFormOperationResult
                {
                    Success = success,
                    Message = success ? "工作項目退回成功" : "工作項目退回失敗",
                    ErrorCode = success ? null : "RETURN_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退回工作項目失敗: {WorkItemId}", workItemId);
                return new AttendanceFormOperationResult
                {
                    Success = false,
                    Message = $"退回失敗: {ex.Message}",
                    ErrorCode = "RETURN_ERROR"
                };
            }
        }

        private string GetJsonString(JsonElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString() ?? "";
                }
            }
            return "";
        }
    }
}

