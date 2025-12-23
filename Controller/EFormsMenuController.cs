using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 電子表單選單控制器
    /// </summary>
    [ApiController]
    [Route("app")]
    public class EFormsMenuController : ControllerBase
    {
        private readonly IEFormsMenuService _eformsMenuService;
        private readonly ILogger<EFormsMenuController> _logger;

        public EFormsMenuController(
            IEFormsMenuService eformsMenuService,
            ILogger<EFormsMenuController> logger)
        {
            _eformsMenuService = eformsMenuService;
            _logger = logger;
        }

        /// <summary>
        /// 電子表單選單呈現列表
        /// </summary>
        /// <param name="request">電子表單選單請求參數</param>
        /// <returns>電子表單選單列表回應</returns>
        [HttpPost("eformslist")]
        public async Task<IActionResult> GetEFormsMenuList([FromBody] EFormsMenuRequest request)
        {
            try
            {
                _logger.LogInformation($"收到電子表單選單列表請求 - TokenId: {request.tokenid}, CID: {request.cid}, UID: {request.uid}, Language: {request.language}");

                // 驗證必填參數
                if (string.IsNullOrWhiteSpace(request.tokenid) ||
                    string.IsNullOrWhiteSpace(request.cid) ||
                    string.IsNullOrWhiteSpace(request.uid))
                {
                    _logger.LogWarning("電子表單選單列表請求參數不完整");
                    return BadRequest(new EFormsMenuResponse
                    {
                        code = "400",
                        msg = "參數不完整"
                    });
                }

                // 驗證語系參數
                if (!string.IsNullOrWhiteSpace(request.language))
                {
                    var language = request.language.ToUpper();
                    if (language != "T" && language != "C" && language != "TW" && language != "CN")
                    {
                        _logger.LogWarning($"電子表單選單列表請求語系參數無效 - Language: {request.language}");
                        return BadRequest(new EFormsMenuResponse
                        {
                            code = "400",
                            msg = "語系參數無效，只支援 T/TW (繁體) 或 C/CN (簡體)"
                        });
                    }

                    // 標準化語系代碼 (tw/cn 轉換為 T/C)
                    if (language == "TW")
                    {
                        request.language = "T";
                    }
                    else if (language == "CN")
                    {
                        request.language = "C";
                    }
                    else
                    {
                        request.language = language;
                    }
                }
                else
                {
                    // 預設為繁體中文
                    request.language = "T";
                }

                // 呼叫服務取得選單列表
                var response = await _eformsMenuService.GetEFormsMenuListAsync(request);

                if (response.code == "200")
                {
                    _logger.LogInformation($"電子表單選單列表請求成功 - TokenId: {request.tokenid}");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning($"電子表單選單列表請求失敗 - TokenId: {request.tokenid}, Code: {response.code}");
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理電子表單選單列表請求時發生錯誤 - TokenId: {request?.tokenid}");
                return StatusCode(500, new EFormsMenuResponse
                {
                    code = "500",
                    msg = "請求超時"
                });
            }
        }
    }
}
