using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 請假單申請 API - efleaveform
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class LeaveFormSubmitController : ControllerBase
    {
        private readonly ILeaveFormService _leaveFormService;
        private readonly ILogger<LeaveFormSubmitController> _logger;

        public LeaveFormSubmitController(
            ILeaveFormService leaveFormService,
            ILogger<LeaveFormSubmitController> logger)
        {
            _leaveFormService = leaveFormService;
            _logger = logger;
        }

        /// <summary>
        /// 提交請假單申請
        /// </summary>
        /// <remarks>
        /// 使用者提交請假申請，包含假別、起始與結束時間、請假事件發生日、事由、代理人，並可附加檔案
        /// 
        /// 輸入範例:
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "leavetype": "S0001",
        ///   "estartdate": "2025-09-16",
        ///   "estarttime": "08:55",
        ///   "eenddate": "2025-09-16",
        ///   "eendtime": "18:00",
        ///   "eleavedate": "",
        ///   "ereason": "私人事務",
        ///   "eagent": "4856",
        ///   "efiletype": "C",
        ///   "efileid": ["4", "5", "80"]
        /// }
        /// ```
        /// 
        /// 輸出範例（成功）:
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// 輸出範例（失敗）:
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("efleaveform")]
        [ProducesResponseType(typeof(LeaveFormSubmitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveFormSubmitResponse>> SubmitLeaveForm([FromBody] LeaveFormSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("提交請假單申請 API 被呼叫: {@Request}", new
                {
                    request.Tokenid,
                    request.Cid,
                    request.Uid,
                    request.Leavetype,
                    request.Estartdate,
                    request.Eenddate
                });

                // 驗證必填欄位
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Ok(new LeaveFormSubmitResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    });
                }

                // 呼叫服務層提交請假申請
                var result = await _leaveFormService.SubmitLeaveFormAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("成功提交請假單申請，FormId: {FormId}", result.FormId);
                    return Ok(new LeaveFormSubmitResponse
                    {
                        Code = "200",
                        Msg = "請求成功",
                        FormId = result.FormId,
                        FormNumber = result.FormNumber
                    });
                }
                else
                {
                    _logger.LogWarning("提交請假單申請失敗: {Message}", result.Message);
                    return Ok(new LeaveFormSubmitResponse
                    {
                        Code = "203",
                        Msg = result.Message ?? "請求失敗，主要條件不符合"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交請假單申請發生異常");
                return Ok(new LeaveFormSubmitResponse
                {
                    Code = "203",
                    Msg = $"請求失敗: {ex.Message}"
                });
            }
        }
    }
}
