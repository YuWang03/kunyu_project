using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controller
{
    /// <summary>
    /// SFTP 連線測試控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SftpTestController : ControllerBase
    {
        private readonly FtpService _ftpService;
        private readonly ILogger<SftpTestController> _logger;

        public SftpTestController(
            FtpService ftpService,
            ILogger<SftpTestController> logger)
        {
            _ftpService = ftpService;
            _logger = logger;
        }

        /// <summary>
        /// 測試 SFTP 連線
        /// </summary>
        /// <remarks>
        /// 測試與 SFTP 伺服器的連線
        /// 
        /// IP: 14.18.232.79
        /// Port: 20105
        /// Username: ftpuser
        /// Password: Panpi3066
        /// </remarks>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("開始測試 SFTP 連線");
                var result = await _ftpService.TestConnectionAsync();

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SFTP 連線成功",
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "SFTP 連線失敗",
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP 連線測試異常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"SFTP 連線測試異常: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 測試上傳檔案
        /// </summary>
        [HttpPost("test-upload")]
        public async Task<IActionResult> TestUpload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("請提供要上傳的檔案");
                }

                _logger.LogInformation("開始測試檔案上傳，檔案名: {FileName}, 大小: {Size}", file.FileName, file.Length);

                var fileName = $"test_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
                var remotePath = $"/AppAttachments/{fileName}";

                using var stream = file.OpenReadStream();
                var result = await _ftpService.UploadFileAsync(stream, remotePath);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "檔案上傳成功",
                        remoteFile = remotePath,
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "檔案上傳失敗",
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案上傳測試異常");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"檔案上傳失敗: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 檢查 SFTP 伺服器設定
        /// </summary>
        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            return Ok(new
            {
                message = "SFTP 伺服器設定",
                configuration = new
                {
                    ip = "14.18.232.79",
                    port = 20105,
                    username = "ftpuser",
                    protocol = "SFTP",
                    uploadPath = "/AppAttachments/",
                    note = "密碼已存儲在 appsettings.json"
                }
            });
        }
    }
}
