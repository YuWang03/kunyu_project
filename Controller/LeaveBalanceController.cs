using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 請假餘額查詢 - 查詢各類假別剩餘天數
    /// </summary>
    [ApiController]
    [Route("app/[controller]")]
    [Tags("請假餘額查詢")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ILeaveBalanceService _leaveBalanceService;
        private readonly ILogger<LeaveBalanceController> _logger;

        public LeaveBalanceController(ILeaveBalanceService leaveBalanceService, ILogger<LeaveBalanceController> logger)
        {
            _leaveBalanceService = leaveBalanceService ?? throw new ArgumentNullException(nameof(leaveBalanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 查詢個人請假餘額
        /// </summary>
        /// <param name="request">查詢請求 (包含 tokenid, cid, uid, ryear)</param>
        /// <returns>請假餘額資訊</returns>
        /// <response code="200">查詢成功</response>
        /// <response code="500">請求超時或伺服器錯誤</response>
        [HttpPost]
        [ProducesResponseType(typeof(LeaveBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LeaveBalanceResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveBalanceResponse>> GetLeaveBalance([FromBody] LeaveBalanceRequest request)
        {
            try
            {
                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    return Ok(new LeaveBalanceResponse
                    {
                        Code = "500",
                        Msg = "使用者ID不可為空",
                        Data = null
                    });
                }

                if (string.IsNullOrWhiteSpace(request.RYear))
                {
                    return Ok(new LeaveBalanceResponse
                    {
                        Code = "500",
                        Msg = "查詢年度不可為空",
                        Data = null
                    });
                }

                // 驗證年度格式
                if (!int.TryParse(request.RYear, out int queryYear))
                {
                    return Ok(new LeaveBalanceResponse
                    {
                        Code = "500",
                        Msg = "查詢年度格式錯誤",
                        Data = null
                    });
                }

                _logger.LogInformation($"查詢請假餘額 - TokenId: {request.TokenId}, Cid: {request.Cid}, Uid: {request.Uid}, Year: {request.RYear}");

                // 查詢請假餘額
                var result = await _leaveBalanceService.GetLeaveBalanceAsync(request.Uid, queryYear);

                return Ok(result);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning($"查詢請假餘額超時 - Uid: {request.Uid}, Year: {request.RYear}");
                return Ok(new LeaveBalanceResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查詢請假餘額失敗 - Uid: {request.Uid}, Year: {request.RYear}");
                return Ok(new LeaveBalanceResponse
                {
                    Code = "500",
                    Msg = "請求超時",
                    Data = null
                });
            }
        }
    }
}
