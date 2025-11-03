using System.Text;
using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public class OutingFormService
    {
        private readonly HttpClient _httpClient;
        private readonly BpmService _bpmService;
        private readonly ILogger<OutingFormService> _logger;

        public OutingFormService(
            IHttpClientFactory httpClientFactory,
            BpmService bpmService,
            ILogger<OutingFormService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BpmClient");
            _bpmService = bpmService;
            _logger = logger;
        }

        // 建立外出單
        public async Task<string> CreateOutingFormAsync(object formData)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(formData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/form/outing/create", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // 假設 BPM 回傳格式: { "formId": "xxx", "status": "success" }
                return result.GetProperty("formId").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立外出單時發生錯誤");
                throw;
            }
        }

        // 查詢待簽核外出單列表
        public async Task<PagedResponse<OutingFormListItem>> GetPendingFormsAsync(PendingOutingFormQuery query)
        {
            try
            {
                // 將 Email 轉換為 UserID
                string? approverId = null;
                if (!string.IsNullOrEmpty(query.ApproverEmail))
                {
                    approverId = await _bpmService.GetUserIdByEmailAsync(query.ApproverEmail);
                }

                // 預設查詢近 2 月的記錄（廠商需求）
                if (!query.StartDate.HasValue && !query.Year.HasValue)
                {
                    query.StartDate = DateTime.Now.AddMonths(-2);
                }
                if (!query.EndDate.HasValue && !query.Year.HasValue)
                {
                    query.EndDate = DateTime.Now;
                }

                // 組裝查詢參數
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(approverId))
                    queryParams["approverId"] = approverId;
                if (query.Year.HasValue)
                    queryParams["year"] = query.Year.Value.ToString();
                if (query.Month.HasValue)
                    queryParams["month"] = query.Month.Value.ToString();
                if (query.Day.HasValue)
                    queryParams["day"] = query.Day.Value.ToString();
                if (query.StartDate.HasValue)
                    queryParams["startDate"] = query.StartDate.Value.ToString("yyyy-MM-dd");
                if (query.EndDate.HasValue)
                    queryParams["endDate"] = query.EndDate.Value.ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(query.EmployeeName))
                    queryParams["employeeName"] = query.EmployeeName;
                queryParams["pageNumber"] = query.PageNumber.ToString();
                queryParams["pageSize"] = query.PageSize.ToString();

                // 呼叫 BPM API
                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await _httpClient.GetAsync($"/form/outing/pending?{queryString}");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // 解析 BPM 回應（根據實際 BPM API 格式調整）
                var items = new List<OutingFormListItem>();
                if (bpmResponse.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        items.Add(new OutingFormListItem
                        {
                            FormId = item.GetProperty("formId").GetString() ?? string.Empty,
                            EmployeeName = item.GetProperty("employeeName").GetString() ?? string.Empty,
                            EmployeeId = item.GetProperty("employeeId").GetString() ?? string.Empty,
                            Type = item.GetProperty("type").GetString() ?? string.Empty,
                            Date = DateTime.Parse(item.GetProperty("date").GetString() ?? DateTime.Now.ToString()),
                            StartTime = DateTime.Parse(item.GetProperty("startTime").GetString() ?? DateTime.Now.ToString()),
                            EndTime = DateTime.Parse(item.GetProperty("endTime").GetString() ?? DateTime.Now.ToString()),
                            Location = item.GetProperty("location").GetString() ?? string.Empty,
                            Status = item.GetProperty("status").GetString() ?? string.Empty,
                            CreatedAt = DateTime.Parse(item.GetProperty("createdAt").GetString() ?? DateTime.Now.ToString())
                        });
                    }
                }

                var totalCount = bpmResponse.TryGetProperty("totalCount", out var total) ? total.GetInt32() : items.Count;

                return new PagedResponse<OutingFormListItem>
                {
                    Success = true,
                    Message = "查詢成功",
                    Data = items,
                    TotalCount = totalCount,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢待簽核外出單時發生錯誤");
                return new PagedResponse<OutingFormListItem>
                {
                    Success = false,
                    Message = $"查詢失敗: {ex.Message}",
                    Data = new List<OutingFormListItem>()
                };
            }
        }

        // 取得外出單詳細內容
        public async Task<ApiResponse<OutingFormDetail>> GetFormDetailAsync(string formId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/form/outing/{formId}");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var bpmResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // 解析 BPM 回應
                var detail = new OutingFormDetail
                {
                    FormId = bpmResponse.GetProperty("formId").GetString() ?? string.Empty,
                    EmployeeName = bpmResponse.GetProperty("employeeName").GetString() ?? string.Empty,
                    EmployeeId = bpmResponse.GetProperty("employeeId").GetString() ?? string.Empty,
                    Department = bpmResponse.GetProperty("department").GetString() ?? string.Empty,
                    Type = bpmResponse.GetProperty("type").GetString() ?? string.Empty,
                    Date = DateTime.Parse(bpmResponse.GetProperty("date").GetString() ?? DateTime.Now.ToString()),
                    StartTime = DateTime.Parse(bpmResponse.GetProperty("startTime").GetString() ?? DateTime.Now.ToString()),
                    EndTime = DateTime.Parse(bpmResponse.GetProperty("endTime").GetString() ?? DateTime.Now.ToString()),
                    Location = bpmResponse.GetProperty("location").GetString() ?? string.Empty,
                    Reason = bpmResponse.GetProperty("reason").GetString() ?? string.Empty,
                    ReturnToOffice = bpmResponse.GetProperty("returnToOffice").GetBoolean(),
                    Status = bpmResponse.GetProperty("status").GetString() ?? string.Empty,
                    CreatedAt = DateTime.Parse(bpmResponse.GetProperty("createdAt").GetString() ?? DateTime.Now.ToString()),
                    Note = bpmResponse.TryGetProperty("note", out var note) ? note.GetString() : null
                };

                // 解析簽核歷程
                if (bpmResponse.TryGetProperty("approvalHistory", out var historyArray))
                {
                    foreach (var history in historyArray.EnumerateArray())
                    {
                        detail.ApprovalHistory.Add(new ApprovalHistory
                        {
                            ApproverName = history.GetProperty("approverName").GetString() ?? string.Empty,
                            Action = history.GetProperty("action").GetString() ?? string.Empty,
                            Comment = history.TryGetProperty("comment", out var comment) ? comment.GetString() : null,
                            ApprovedAt = DateTime.Parse(history.GetProperty("approvedAt").GetString() ?? DateTime.Now.ToString())
                        });
                    }
                }

                // 解析附件
                if (bpmResponse.TryGetProperty("attachments", out var attachmentArray))
                {
                    foreach (var attachment in attachmentArray.EnumerateArray())
                    {
                        detail.Attachments.Add(new AttachmentInfo
                        {
                            FileName = attachment.GetProperty("fileName").GetString() ?? string.Empty,
                            FileType = attachment.GetProperty("fileType").GetString() ?? string.Empty,
                            FilePath = attachment.GetProperty("filePath").GetString() ?? string.Empty,
                            FileSize = attachment.GetProperty("fileSize").GetInt64(),
                            UploadedAt = DateTime.Parse(attachment.GetProperty("uploadedAt").GetString() ?? DateTime.Now.ToString())
                        });
                    }
                }

                return new ApiResponse<OutingFormDetail>
                {
                    Success = true,
                    Message = "查詢成功",
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得外出單詳細內容時發生錯誤: {FormId}", formId);
                return new ApiResponse<OutingFormDetail>
                {
                    Success = false,
                    Message = $"查詢失敗: {ex.Message}"
                };
            }
        }

        // 簽核外出單
        public async Task<ApiResponse<string>> ApproveFormAsync(ApproveOutingFormRequest request)
        {
            try
            {
                // 將 Email 轉換為 UserID
                var approverId = await _bpmService.GetUserIdByEmailAsync(request.ApproverEmail);

                var approvalData = new
                {
                    formId = request.FormId,
                    approverId = approverId,
                    action = request.Action,  // approve/reject/return
                    comment = request.Comment
                };

                var jsonContent = JsonSerializer.Serialize(approvalData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/form/outing/approve", content);
                response.EnsureSuccessStatusCode();

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = request.Action == "approve" ? "簽核成功" :
                              request.Action == "reject" ? "退回成功" : "處理成功",
                    Data = request.FormId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽核外出單時發生錯誤: {FormId}", request.FormId);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"簽核失敗: {ex.Message}"
                };
            }
        }

        // 批次簽核外出單
        public async Task<ApiResponse<List<string>>> BatchApproveFormsAsync(BatchApproveOutingFormRequest request)
        {
            try
            {
                // 將 Email 轉換為 UserID
                var approverId = await _bpmService.GetUserIdByEmailAsync(request.ApproverEmail);

                var batchData = new
                {
                    formIds = request.FormIds,
                    approverId = approverId,
                    action = request.Action,  // approve/reject
                    comment = request.Comment
                };

                var jsonContent = JsonSerializer.Serialize(batchData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/form/outing/batch-approve", content);
                response.EnsureSuccessStatusCode();

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = $"批次{(request.Action == "approve" ? "簽核" : "退回")}成功，共處理 {request.FormIds.Count} 筆",
                    Data = request.FormIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次簽核外出單時發生錯誤");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = $"批次簽核失敗: {ex.Message}"
                };
            }
        }
    }
}