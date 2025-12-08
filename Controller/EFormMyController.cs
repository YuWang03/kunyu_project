using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 我的表單控制器
    /// </summary>
    [ApiController]
    [Route("app")]
    public class EFormMyController : ControllerBase
    {
        private readonly IEFormMyService _eformMyService;
        private readonly ILogger<EFormMyController> _logger;

        public EFormMyController(
            IEFormMyService eformMyService,
            ILogger<EFormMyController> logger)
        {
            _eformMyService = eformMyService;
            _logger = logger;
        }

        /// <summary>
        /// 我的表單列表（從簽核記錄中取得我的待辦表單）
        /// </summary>
        /// <param name="request">我的表單請求參數</param>
        /// <returns>我的表單列表回應</returns>
        [HttpPost("eformmy")]
        public async Task<IActionResult> GetMyForms([FromBody] EFormMyRequest request)
        {
            try
            {
                _logger.LogInformation($"收到我的表單列表請求 - TokenId: {request.TokenId}, CID: {request.Cid}, UID: {request.Uid}");

                // 驗證必填參數
                if (string.IsNullOrWhiteSpace(request.TokenId) ||
                    string.IsNullOrWhiteSpace(request.Cid) ||
                    string.IsNullOrWhiteSpace(request.Uid))
                {
                    _logger.LogWarning("我的表單列表請求參數不完整");
                    return BadRequest(new EFormMyResponse
                    {
                        Code = "400",
                        Msg = "參數不完整"
                    });
                }

                // 呼叫服務取得我的表單列表
                var response = await _eformMyService.GetMyFormsAsync(request);

                if (response.Code == "200")
                {
                    _logger.LogInformation($"我的表單列表請求成功 - TokenId: {request.TokenId}, 共 {response.Data?.EFormData?.Count ?? 0} 筆");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"我的表單列表請求失敗 - TokenId: {request.TokenId}, Code: {response.Code}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理我的表單列表請求時發生錯誤 - TokenId: {request?.TokenId}");
                return StatusCode(500, new EFormMyResponse
                {
                    Code = "500",
                    Msg = "請求超時"
                });
            }
        }
    }
}
