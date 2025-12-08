using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 請假假別單位查詢 API - efleaveformunit
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class LeaveFormUnitController : ControllerBase
    {
        private readonly ILeaveFormService _leaveFormService;
        private readonly ILogger<LeaveFormUnitController> _logger;

        public LeaveFormUnitController(
            ILeaveFormService leaveFormService,
            ILogger<LeaveFormUnitController> logger)
        {
            _leaveFormService = leaveFormService;
            _logger = logger;
        }

        /// <summary>
        /// 查詢請假假別單位
        /// </summary>
        /// <remarks>
        /// 此 API 將同步 BPM Middleware 的流程信息，並查詢可用的假別及其最小單位
        /// 
        /// **步驟說明:**
        /// 1. 同步 BPM 流程信息 (syncProcessInfo API)
        ///    - 表單編號: PI_Leave_Test + 時間戳記
        ///    - 程序代碼: PI_LEAVE_001_PROCESS
        ///    - 環境: TEST
        /// 2. 從資料庫查詢假別資料
        ///    - 排除特定的假別代碼（已在 SQL WHERE 條件中定義）
        ///    - 根據公司代碼過濾
        /// 
        /// 輸入範例:
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "03546618",
        ///   "uid": "0325"
        /// }
        /// ```
        /// 
        /// 輸出範例（成功）:
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": [
        ///     {
        ///       "leavetype": "特休",
        ///       "leaveunit": "2.0",
        ///       "leavecode": "S0001",
        ///       "leaveunittype": "DAY"
        ///     }
        ///   ]
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
        /// 
        /// **BPM API 規格參考:**
        /// - API URL: http://60.248.158.147:8081/bpm-middleware/api/bpm/sync-process-info
        /// - processSerialNo: 表單編號（格式: PI_Leave_TestYYYYMMDDHHmmss）
        /// - processCode: PI_LEAVE_001_PROCESS
        /// - environment: TEST 或 PROD
        /// </remarks>
        [HttpPost("efleaveformunit")]
        [ProducesResponseType(typeof(LeaveTypeUnitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveTypeUnitResponse>> GetLeaveTypeUnits([FromBody] LeaveTypeUnitRequest request)
        {
            try
            {
                _logger.LogInformation("查詢請假假別單位 API 被呼叫: {@Request}", new
                {
                    request.Tokenid,
                    request.Cid,
                    request.Uid
                });

                // 驗證必填欄位
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Ok(new LeaveTypeUnitResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    });
                }

                // 呼叫服務層查詢假別單位
                var leaveTypes = await _leaveFormService.GetLeaveTypeUnitsAsync(request.Cid);

                if (leaveTypes == null || !leaveTypes.Any())
                {
                    _logger.LogWarning("查無假別資料，公司代碼: {Cid}", request.Cid);
                    return Ok(new LeaveTypeUnitResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，查無假別資料",
                        Data = null
                    });
                }

                _logger.LogInformation("成功查詢假別單位，共 {Count} 筆", leaveTypes.Count);
                return Ok(new LeaveTypeUnitResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = leaveTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假假別單位失敗");
                return Ok(new LeaveTypeUnitResponse
                {
                    Code = "203",
                    Msg = $"請求失敗: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
