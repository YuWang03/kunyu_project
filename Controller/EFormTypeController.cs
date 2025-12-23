using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 電子表單類型控制器
    /// </summary>
    [ApiController]
    [Route("app")]
    public class EFormTypeController : ControllerBase
    {
        private readonly IEFormTypeService _eformTypeService;
        private readonly ILogger<EFormTypeController> _logger;

        public EFormTypeController(
            IEFormTypeService eformTypeService,
            ILogger<EFormTypeController> logger)
        {
            _eformTypeService = eformTypeService;
            _logger = logger;
        }

        /// <summary>
        /// 取得電子表單類型列表
        /// </summary>
        /// <param name="request">電子表單類型請求參數</param>
        /// <returns>電子表單類型列表回應</returns>
        [HttpPost("eformtype")]
        public async Task<IActionResult> GetEFormTypes([FromBody] EFormTypeRequest request)
        {
            try
            {
                _logger.LogInformation($"收到電子表單類型列表請求 - TokenId: {request.tokenid}, CID: {request.cid}, UID: {request.uid}");

                // 驗證必填參數
                if (string.IsNullOrWhiteSpace(request.tokenid) ||
                    string.IsNullOrWhiteSpace(request.cid) ||
                    string.IsNullOrWhiteSpace(request.uid))
                {
                    _logger.LogWarning("電子表單類型列表請求參數不完整");
                    return BadRequest(new EFormTypeResponse
                    {
                        code = "400",
                        msg = "參數不完整"
                    });
                }

                // 呼叫服務取得電子表單類型列表
                var response = await _eformTypeService.GetEFormTypesAsync(request);

                if (response.code == "200")
                {
                    _logger.LogInformation($"成功回應電子表單類型列表 - UID: {request.uid}");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"電子表單類型列表查詢失敗 - Code: {response.code}, Message: {response.msg}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"電子表單類型列表請求處理異常 - TokenId: {request.tokenid}");

                return StatusCode(500, new EFormTypeResponse
                {
                    code = "500",
                    msg = "請求超時"
                });
            }
        }
    }
}
