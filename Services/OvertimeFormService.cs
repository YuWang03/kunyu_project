using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public class OvertimeFormService : IOvertimeFormService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<OvertimeFormService> _logger;
        private const string FORM_CODE = "PI_OVERTIME_001";
        private const string FORM_VERSION = "1.0.0";

        public OvertimeFormService(
            BpmService bpmService, 
            FtpService ftpService, 
            IBasicInfoService basicInfoService,
            ILogger<OvertimeFormService> logger)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _basicInfoService = basicInfoService;
            _logger = logger;
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
    }
}
