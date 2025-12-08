using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 薪資查詢 API - 測試版（不需要 Token 驗證）
    /// </summary>
    [ApiController]
    [Route("test")]
    [Produces("application/json")]
    public class SalaryTestController : ControllerBase
    {
        private readonly ISalaryService _salaryService;
        private readonly ILogger<SalaryTestController> _logger;

        public SalaryTestController(
            ISalaryService salaryService,
            ILogger<SalaryTestController> logger)
        {
            _salaryService = salaryService;
            _logger = logger;
        }

        /// <summary>
        /// 測試：發送驗證碼（無 Token 驗證）
        /// </summary>
        [HttpPost("sendcode")]
        public async Task<ActionResult<SendCodeResponse>> SendCodeTest([FromBody] SendCodeRequest request)
        {
            try
            {
                _logger.LogInformation("[TEST] 薪資查詢驗證碼寄送 - Uid={Uid}", request.Uid);

                var response = await _salaryService.SendVerificationCodeAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST] SendCode 發生錯誤");
                return StatusCode(500, new SendCodeResponse
                {
                    Code = "500",
                    Msg = $"伺服器錯誤: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 測試：驗證驗證碼（無 Token 驗證）
        /// </summary>
        [HttpPost("sendcodecheck")]
        public async Task<ActionResult<SendCodeCheckResponse>> VerifyCodeTest([FromBody] SendCodeCheckRequest request)
        {
            try
            {
                _logger.LogInformation("[TEST] 薪資查詢驗證碼驗證 - Uid={Uid}, Code={Code}", request.Uid, request.Verificationcode);

                var response = await _salaryService.VerifyCodeAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST] VerifyCode 發生錯誤");
                return StatusCode(500, new SendCodeCheckResponse
                {
                    Code = "500",
                    Msg = $"伺服器錯誤: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 測試健康檢查
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("[TEST] Health check requested");
            return Ok(new { status = "OK", timestamp = DateTime.Now });
        }
    }
}
