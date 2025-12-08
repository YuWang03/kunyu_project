using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    [ApiController]
    [Route("app/eformwithdraw")]
    public class EFormWithdrawController : ControllerBase
    {
        private readonly IEFormWithdrawService _service;
        private readonly ILogger<EFormWithdrawController> _logger;

        public EFormWithdrawController(IEFormWithdrawService service, ILogger<EFormWithdrawController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 簽核記錄撤回作業
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Withdraw([FromBody] EFormWithdrawRequest request)
        {
            _logger.LogInformation("收到表單撤回請求 - TokenId: {TokenId}, UID: {Uid}, FormId: {FormId}", 
                request.TokenId, request.Uid, request.EFormId);

            if (!ModelState.IsValid)
            {
                return Ok(new EFormWithdrawResponse
                {
                    Code = "203",
                    Msg = "請求失敗，參數無效"
                });
            }

            var response = await _service.WithdrawFormAsync(request);
            return Ok(response);
        }
    }
}
