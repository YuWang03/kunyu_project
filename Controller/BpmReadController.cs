using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// BPM Read API 控制器
    /// 接收 BPM 表單資料，從 BPM 中間件擷取詳細資訊並存入資料庫
    /// </summary>
    [ApiController]
    [Route("app/bpmread")]
    [Tags("BPM 表單同步")]
    public class BpmReadController : ControllerBase
    {
        private readonly IBpmReadService _bpmReadService;
        private readonly ILogger<BpmReadController> _logger;

        public BpmReadController(
            IBpmReadService bpmReadService,
            ILogger<BpmReadController> logger)
        {
            _bpmReadService = bpmReadService;
            _logger = logger;
        }

        /// <summary>
        /// 接收 BPM 表單資料並同步至本地資料庫
        /// </summary>
        /// <remarks>
        /// 此 API 用於接收來自第三方的 BPM 表單通知，
        /// 系統會從 BPM 中間件 (http://60.248.158.147:8081/bpm-middleware) 擷取詳細資料，
        /// 並將資料存入本地資料庫 (54.46.24.34)。
        /// 
        /// 必須提供正確的 bskey (第三方識別碼) 才能存取此 API。
        /// </remarks>
        /// <param name="request">BPM Read 請求資料</param>
        /// <returns>處理結果</returns>
        /// <response code="200">請求成功</response>
        /// <response code="203">請求失敗，主要條件不符合</response>
        /// <response code="500">伺服器錯誤</response>
        [HttpPost]
        [ProducesResponseType(typeof(BpmReadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BpmReadResponse), StatusCodes.Status200OK)] // 203 也回傳 200 但內容不同
        [ProducesResponseType(typeof(BpmReadResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BpmReadResponse>> ProcessBpmRead([FromBody] BpmReadRequest request)
        {
            try
            {
                _logger.LogInformation("收到 BPM Read 請求 - CompanyId: {CompanyId}, 資料筆數: {Count}",
                    request.CompanyId, request.BpmData?.Count ?? 0);

                // 呼叫服務處理請求
                var response = await _bpmReadService.ProcessBpmReadAsync(request);

                // 記錄處理結果
                _logger.LogInformation("BPM Read 處理完成 - Code: {Code}, Msg: {Msg}",
                    response.Code, response.Msg);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 BPM Read 請求時發生未預期的錯誤");
                
                return Ok(new BpmReadResponse
                {
                    Code = "500",
                    Msg = "伺服器發生錯誤，請稍後再試",
                    Data = null
                });
            }
        }
    }
}
