using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 2. 基本資料選單列表 API
    /// </summary>
    [ApiController]
    [Route("app/basiclist")]
    [Tags("2. 基本資料選單列表")]
    [ServiceFilter(typeof(TokenValidationFilter))] // ✅ 套用 Token 驗證
    public class BasicListController : ControllerBase
    {
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<BasicListController> _logger;

        public BasicListController(
            IBasicInfoService basicInfoService,
            ILogger<BasicListController> logger)
        {
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        /// <summary>
        /// 取得基本資料選單列表
        /// </summary>
        /// <param name="request">查詢請求 (包含 tokenid, cid, uid, language)</param>
        /// <returns>選單列表</returns>
        /// <response code="200">請求成功</response>
        /// <response code="500">請求超時或伺服器錯誤</response>
        [HttpPost]
        [ProducesResponseType(typeof(MenuListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MenuListResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MenuListResponse>> GetMenuList([FromBody] MenuListRequest request)
        {
            try
            {
                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    return Ok(new MenuListResponse
                    {
                        Code = "500",
                        Msg = "使用者ID不可為空",
                        Data = null
                    });
                }

                _logger.LogInformation($"取得選單列表 - TokenId: {request.TokenId}, Cid: {request.Cid}, Uid: {request.Uid}, Language: {request.Language}");

                // 驗證語系參數
                if (!string.IsNullOrWhiteSpace(request.Language))
                {
                    var language = request.Language.ToUpper();
                    if (language != "T" && language != "C" && language != "TW" && language != "CN")
                    {
                        _logger.LogWarning($"選單列表請求語系參數無效 - Language: {request.Language}");
                        return Ok(new MenuListResponse
                        {
                            Code = "400",
                            Msg = "語系參數無效，只支援 T/TW (繁體) 或 C/CN (簡體)",
                            Data = null
                        });
                    }

                    // 標準化語系代碼 (tw/cn/TW/CN 轉換為 T/C)
                    if (language == "TW")
                    {
                        request.Language = "T";
                    }
                    else if (language == "CN")
                    {
                        request.Language = "C";
                    }
                    else
                    {
                        request.Language = language;
                    }
                }
                else
                {
                    // 預設為繁體中文
                    request.Language = "T";
                }

                // 取得選單列表
                var result = await _basicInfoService.GetMenuListAsync(request.Uid, request.Language);

                return Ok(result);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning($"取得選單列表超時 - Uid: {request.Uid}");
                return Ok(new MenuListResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得選單列表失敗 - Uid: {request.Uid}");
                return Ok(new MenuListResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                });
            }
        }
    }
}
