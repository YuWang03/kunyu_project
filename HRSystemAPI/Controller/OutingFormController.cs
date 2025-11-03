using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 6. 外出單 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("6. 外出單")]
    public class OutingFormController : ControllerBase
    {
        private readonly FtpService _ftpService;
        private readonly BpmService _bpmService;
        private readonly OutingFormService _outingFormService;
        private readonly ILogger<OutingFormController> _logger;

        public OutingFormController(
            FtpService ftpService,
            BpmService bpmService,
            OutingFormService outingFormService,
            ILogger<OutingFormController> logger)
        {
            _ftpService = ftpService;
            _bpmService = bpmService;
            _outingFormService = outingFormService;
            _logger = logger;
        }

        /// <summary>
        /// 申請外出單
        /// </summary>
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyOutingForm([FromForm] OutingFormRequest request)
        {
            try
            {
                // 1. 驗證必填欄位
                if (string.IsNullOrEmpty(request.Email))
                    return BadRequest(new { success = false, message = "Email 為必填欄位" });

                if (string.IsNullOrEmpty(request.Type))
                    return BadRequest(new { success = false, message = "類別為必填欄位（外出/外訓）" });

                if (string.IsNullOrEmpty(request.Location))
                    return BadRequest(new { success = false, message = "地點為必填欄位" });

                if (string.IsNullOrEmpty(request.Reason))
                    return BadRequest(new { success = false, message = "事由為必填欄位" });

                // 驗證時間不可跨天（廠商需求：跨天需分別申請）
                if (request.StartTime.Date != request.EndTime.Date)
                    return BadRequest(new { success = false, message = "外出時間不可跨天，跨天請分別申請兩張外出單" });

                // 驗證結束時間必須大於開始時間
                if (request.EndTime <= request.StartTime)
                    return BadRequest(new { success = false, message = "截止時間必須大於起始時間" });

                // 2. 將 Email 轉換為 BPM UserID
                var userId = await _bpmService.GetUserIdByEmailAsync(request.Email);
                _logger.LogInformation("Email {Email} 對應的 UserID: {UserId}", request.Email, userId);

                // 3. 上傳附件到 FTP（逐個上傳）
                var attachmentPaths = new List<string>();
                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var file in request.Attachments)
                    {
                        var fileName = $"outing_{Guid.NewGuid()}_{file.FileName}";
                        var remotePath = $"/uploads/attachments/{fileName}";

                        using var stream = file.OpenReadStream();
                        var success = await _ftpService.UploadFileAsync(stream, remotePath);

                        if (success)
                        {
                            attachmentPaths.Add(remotePath);
                        }
                    }

                    _logger.LogInformation("已上傳 {Count} 個附件", attachmentPaths.Count);
                }

                // 4. 組裝 BPM 表單資料
                var bpmFormData = new
                {
                    user_id = userId,
                    type = request.Type,
                    date = request.Date.ToString("yyyy-MM-dd"),
                    start_time = request.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    end_time = request.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    location = request.Location,
                    reason = request.Reason,
                    return_to_office = request.ReturnToOffice,
                    file_paths = attachmentPaths
                };

                // 5. 呼叫 BPM API 建立外出單
                var formId = await _outingFormService.CreateOutingFormAsync(bpmFormData);

                return Ok(new
                {
                    success = true,
                    message = "外出單申請成功",
                    data = new
                    {
                        formId = formId,
                        attachmentCount = attachmentPaths.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請外出單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"申請失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢待簽核外出單列表
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForms([FromQuery] PendingOutingFormQuery query)
        {
            try
            {
                var result = await _outingFormService.GetPendingFormsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢待簽核外出單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 取得外出單詳細內容
        /// </summary>
        [HttpGet("{formId}")]
        public async Task<IActionResult> GetFormDetail(string formId)
        {
            try
            {
                if (string.IsNullOrEmpty(formId))
                    return BadRequest(new { success = false, message = "表單ID不可為空" });

                var result = await _outingFormService.GetFormDetailAsync(formId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得外出單詳細內容時發生錯誤: {FormId}", formId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 簽核外出單
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveForm([FromBody] ApproveOutingFormRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FormId))
                    return BadRequest(new { success = false, message = "表單ID為必填欄位" });

                if (string.IsNullOrEmpty(request.ApproverEmail))
                    return BadRequest(new { success = false, message = "簽核人Email為必填欄位" });

                if (string.IsNullOrEmpty(request.Action))
                    return BadRequest(new { success = false, message = "動作為必填欄位（approve/reject/return）" });

                var validActions = new[] { "approve", "reject", "return" };
                if (!validActions.Contains(request.Action.ToLower()))
                    return BadRequest(new { success = false, message = "無效的動作，僅接受 approve/reject/return" });

                var result = await _outingFormService.ApproveFormAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽核外出單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"簽核失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 批次簽核外出單
        /// </summary>
        [HttpPost("batch-approve")]
        public async Task<IActionResult> BatchApproveForms([FromBody] BatchApproveOutingFormRequest request)
        {
            try
            {
                if (request.FormIds == null || !request.FormIds.Any())
                    return BadRequest(new { success = false, message = "表單ID列表不可為空" });

                if (string.IsNullOrEmpty(request.ApproverEmail))
                    return BadRequest(new { success = false, message = "簽核人Email為必填欄位" });

                if (string.IsNullOrEmpty(request.Action))
                    return BadRequest(new { success = false, message = "動作為必填欄位（approve/reject）" });

                var validActions = new[] { "approve", "reject" };
                if (!validActions.Contains(request.Action.ToLower()))
                    return BadRequest(new { success = false, message = "批次簽核僅接受 approve/reject" });

                var result = await _outingFormService.BatchApproveFormsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次簽核外出單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"批次簽核失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 下載外出單附件
        /// </summary>
        [HttpGet("attachment/download")]
        public async Task<IActionResult> DownloadAttachment([FromQuery] string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return BadRequest(new { success = false, message = "檔案路徑不可為空" });

                var fileStream = await _ftpService.DownloadFileAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載附件時發生錯誤: {FilePath}", filePath);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"下載失敗: {ex.Message}"
                });
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 查詢我申請的外出單
        /// </summary>
        [HttpGet("my-forms")]
        public async Task<IActionResult> GetMyForms(
            [FromQuery] string email,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return BadRequest(new { success = false, message = "Email為必填欄位" });

                var userId = await _bpmService.GetUserIdByEmailAsync(email);

                var queryParams = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["pageNumber"] = pageNumber.ToString(),
                    ["pageSize"] = pageSize.ToString()
                };

                if (startDate.HasValue)
                    queryParams["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
                if (endDate.HasValue)
                    queryParams["endDate"] = endDate.Value.ToString("yyyy-MM-dd");

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await _bpmService.GetAsync($"/form/outing/my-forms?{queryString}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢我的外出單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 測試 FTP 連線
        /// </summary>
        [HttpGet("test-ftp")]
        public async Task<IActionResult> TestFtpConnection()
        {
            try
            {
                var isConnected = await _ftpService.TestConnectionAsync();
                return Ok(new
                {
                    success = isConnected,
                    message = isConnected ? "FTP 連線成功" : "FTP 連線失敗"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試 FTP 連線時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"測試失敗: {ex.Message}"
                });
            }
        }
    }
}