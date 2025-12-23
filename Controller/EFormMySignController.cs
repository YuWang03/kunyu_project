using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 我的簽核列表控制器
    /// </summary>
    [ApiController]
    [Route("app")]
    public class EFormMySignController : ControllerBase
    {
        private readonly IEFormMySignService _eformMySignService;
        private readonly ILogger<EFormMySignController> _logger;

        public EFormMySignController(
            IEFormMySignService eformMySignService,
            ILogger<EFormMySignController> logger)
        {
            _eformMySignService = eformMySignService;
            _logger = logger;
        }

        /// <summary>
        /// 我的簽核列表（從簽核記錄中取得我簽過的表單）
        /// </summary>
        /// <param name="request">我的簽核請求參數</param>
        /// <returns>我的簽核列表回應</returns>
        [HttpPost("eformmysign")]
        public async Task<IActionResult> GetMySignForms([FromBody] EFormMySignRequest request)
        {
            try
            {
                _logger.LogInformation($"收到我的簽核列表請求 - TokenId: {request.TokenId}, CID: {request.Cid}, UID: {request.Uid}");

                // 驗證必填參數
                if (string.IsNullOrWhiteSpace(request.TokenId) ||
                    string.IsNullOrWhiteSpace(request.Cid) ||
                    string.IsNullOrWhiteSpace(request.Uid))
                {
                    _logger.LogWarning("我的簽核列表請求參數不完整");
                    return BadRequest(new EFormMySignResponse
                    {
                        Code = "400",
                        Msg = "參數不完整"
                    });
                }

                // 呼叫服務取得我的簽核列表
                var response = await _eformMySignService.GetMySignFormsAsync(request);

                if (response.Code == "200")
                {
                    _logger.LogInformation($"我的簽核列表請求成功 - TokenId: {request.TokenId}, 共 {response.Data?.EFormData?.Count ?? 0} 筆");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"我的簽核列表請求失敗 - TokenId: {request.TokenId}, Code: {response.Code}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理我的簽核列表請求時發生錯誤 - TokenId: {request?.TokenId}");
                return StatusCode(500, new EFormMySignResponse
                {
                    Code = "500",
                    Msg = "請求超時"
                });
            }
        }
    }
}
