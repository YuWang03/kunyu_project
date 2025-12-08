using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 簽核記錄詳細資料控制器
    /// </summary>
    [ApiController]
    [Route("app")]
    public class EFormRecordController : ControllerBase
    {
        private readonly IEFormRecordService _eformRecordService;
        private readonly ILogger<EFormRecordController> _logger;

        public EFormRecordController(
            IEFormRecordService eformRecordService,
            ILogger<EFormRecordController> logger)
        {
            _eformRecordService = eformRecordService;
            _logger = logger;
        }

        /// <summary>
        /// 簽核記錄詳細資料
        /// </summary>
        /// <param name="request">簽核記錄詳細資料請求參數</param>
        /// <returns>簽核記錄詳細資料回應</returns>
        [HttpPost("eformrecord")]
        public async Task<IActionResult> GetFormRecordDetail([FromBody] EFormRecordRequest request)
        {
            try
            {
                _logger.LogInformation($"收到簽核記錄詳細資料請求 - TokenId: {request.TokenId}, UID: {request.Uid}, FormId: {request.FormId}");

                // 驗證必填參數
                if (string.IsNullOrWhiteSpace(request.TokenId) ||
                    string.IsNullOrWhiteSpace(request.Cid) ||
                    string.IsNullOrWhiteSpace(request.Uid) ||
                    string.IsNullOrWhiteSpace(request.FormId))
                {
                    _logger.LogWarning("簽核記錄詳細資料請求參數不完整");
                    return BadRequest(new EFormRecordResponse
                    {
                        Code = "400",
                        Msg = "參數不完整"
                    });
                }

                // 呼叫服務取得簽核記錄詳細資料
                var response = await _eformRecordService.GetFormRecordDetailAsync(request);

                if (response.Code == "200")
                {
                    _logger.LogInformation($"簽核記錄詳細資料請求成功 - FormId: {request.FormId}");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"簽核記錄詳細資料請求失敗 - FormId: {request.FormId}, Code: {response.Code}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理簽核記錄詳細資料請求時發生錯誤 - FormId: {request?.FormId}");
                return StatusCode(500, new EFormRecordResponse
                {
                    Code = "500",
                    Msg = "請求超時"
                });
            }
        }
    }
}
