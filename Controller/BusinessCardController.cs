using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 電子名片 API 控制器
    /// 從 vwZZ_EMPLOYEE 視圖取得員工資料並轉換為電子名片格式
    /// </summary>
    [ApiController]
    [Route("app/businesscard")]
    [Tags("電子名片")]
    [ServiceFilter(typeof(TokenValidationFilter))] // 套用 Token 驗證
    public class BusinessCardController : ControllerBase
    {
        private readonly IBusinessCardService _businessCardService;
        private readonly ILogger<BusinessCardController> _logger;

        public BusinessCardController(
            IBusinessCardService businessCardService,
            ILogger<BusinessCardController> logger)
        {
            _businessCardService = businessCardService;
            _logger = logger;
        }

        /// <summary>
        /// 取得員工電子名片資料
        /// </summary>
        /// <remarks>
        /// 此 API 從 vwZZ_EMPLOYEE 視圖取得員工資料，並轉換為電子名片格式。
        /// 
        /// 回傳資料包括:
        /// - 公司資訊 (名稱、代碼、電話、網站、地址)
        /// - 個人資訊 (中英文姓名、職稱、部門、Email)
        /// - QR Code 位置
        /// 
        /// 手機號碼、Line ID、微信 ID 可於 APP 端填寫並設定是否顯示。
        /// </remarks>
        /// <param name="request">電子名片請求資料</param>
        /// <returns>電子名片資料</returns>
        /// <response code="200">請求成功</response>
        /// <response code="203">請求失敗，主要條件不符合</response>
        /// <response code="500">伺服器錯誤</response>
        [HttpPost]
        [ProducesResponseType(typeof(BusinessCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BusinessCardResponse), StatusCodes.Status200OK)] // 203 也回傳 200 但內容不同
        [ProducesResponseType(typeof(BusinessCardResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BusinessCardResponse>> GetBusinessCard([FromBody] BusinessCardRequest request)
        {
            try
            {
                _logger.LogInformation("收到電子名片請求 - TokenId: {TokenId}, Cid: {Cid}, Uid: {Uid}",
                    request.TokenId, request.Cid, request.Uid);

                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    _logger.LogWarning("電子名片請求缺少使用者工號");
                    return Ok(new BusinessCardResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    });
                }

                // 呼叫服務取得電子名片
                var response = await _businessCardService.GetBusinessCardAsync(request);

                // 記錄處理結果
                _logger.LogInformation("電子名片請求處理完成 - Code: {Code}, Msg: {Msg}",
                    response.Code, response.Msg);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理電子名片請求時發生未預期的錯誤 - Uid: {Uid}", request.Uid);
                
                return Ok(new BusinessCardResponse
                {
                    Code = "500",
                    Msg = "伺服器發生錯誤，請稍後再試",
                    Data = null
                });
            }
        }
    }
}
