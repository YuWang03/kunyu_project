using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 帳號整合 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("帳號整合")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.LoginAsync(request);
                if (response == null)
                    return Unauthorized(new { message = "Email 或密碼錯誤，或帳號已停用" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登入失敗");
                return StatusCode(500, new { message = "登入處理時發生錯誤" });
            }
        }

        /// <summary>
        /// 刷新 Token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.RefreshTokenAsync(request.RefreshToken);
                if (response == null)
                    return Unauthorized(new { message = "Refresh Token 無效或已過期" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新 Token 失敗");
                return StatusCode(500, new { message = "刷新 Token 時發生錯誤" });
            }
        }

        /// <summary>
        /// 取得使用者資訊
        /// </summary>
        [HttpGet("userinfo")]
        public async Task<IActionResult> GetUserInfo([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email 為必填" });

            try
            {
                var userInfo = await _authService.GetUserInfoByEmailAsync(email);
                if (userInfo == null)
                    return NotFound(new { message = "找不到使用者資料" });

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者資訊失敗");
                return StatusCode(500, new { message = "取得使用者資訊時發生錯誤" });
            }
        }

        /// <summary>
        /// 驗證 Token ID 是否有效（需求1）
        /// </summary>
        [HttpPost("validate-token")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var isValid = await _authService.ValidateTokenAsync(request.TokenId);
                return Ok(new
                {
                    success = true,
                    isValid = isValid,
                    message = isValid ? "Token 有效" : "Token 無效或已過期"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證 Token 失敗");
                return StatusCode(500, new { message = "驗證 Token 時發生錯誤" });
            }
        }

        /// <summary>
        /// 檢核帳號狀態（需求4：檢核目前帳號狀態）
        /// </summary>
        [HttpGet("check-status")]
        [ProducesResponseType(typeof(EmployeeStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckAccountStatus([FromQuery] string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return BadRequest(new { message = "員工工號 (uid) 為必填" });

            try
            {
                var status = await _authService.CheckEmployeeStatusAsync(uid);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢核帳號狀態失敗");
                return StatusCode(500, new { message = "檢核帳號狀態時發生錯誤" });
            }
        }
    }
}
