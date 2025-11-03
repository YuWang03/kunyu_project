using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controller
{
    /// <summary>
    /// 5. 請假單 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("5. 請假單")]
    public class LeaveFormController : ControllerBase
    {
        private readonly FtpService _ftpService;
        private readonly BpmService _bpmService;
        private readonly ILogger<LeaveFormController> _logger;

        public LeaveFormController(
            FtpService ftpService,
            BpmService bpmService,
            ILogger<LeaveFormController> logger)
        {
            _ftpService = ftpService;
            _bpmService = bpmService;
            _logger = logger;
        }

        /// <summary>
        /// 測試FTP連線
        /// </summary>
        [HttpGet("test-ftp")]
        public async Task<IActionResult> TestFtp()
        {
            var result = await _ftpService.TestConnectionAsync();

            if (result)
            {
                return Ok(new { success = true, message = "FTP連線成功" });
            }
            else
            {
                return BadRequest(new { success = false, message = "FTP連線失敗" });
            }
        }

        /// <summary>
        /// 申請請假單
        /// </summary>
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave([FromForm] LeaveFormRequest request)
        {
            try
            {
                // 1. 驗證必填欄位
                if (string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.LeaveType) ||
                    request.StartTime == default ||
                    request.EndTime == default)
                {
                    return BadRequest(new { success = false, message = "缺少必填欄位" });
                }

                // 2. Email轉UserID
                var userId = await _bpmService.GetUserIdByEmailAsync(request.Email);

                // 3. 上傳附件到FTP
                var attachmentPaths = new List<string>();
                if (request.Attachments != null && request.Attachments.Any())
                {
                    attachmentPaths = await _ftpService.UploadFilesAsync(request.Attachments);
                }

                // 4. 組裝BPM表單資料
                var bpmFormData = new
                {
                    user_id = userId,
                    leave_type_code = request.LeaveType,
                    start_datetime = request.StartTime,
                    end_datetime = request.EndTime,
                    reason_text = request.Reason,
                    file_paths = attachmentPaths
                };

                // 5. 呼叫BPM API
                var formId = await _bpmService.CreateLeaveFormAsync(bpmFormData);

                return Ok(new
                {
                    success = true,
                    formId = formId,
                    message = "請假單申請成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請請假單發生錯誤");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 請假單請求模型
    /// </summary>
    public class LeaveFormRequest
    {
        public string Email { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<IFormFile>? Attachments { get; set; }
    }
}