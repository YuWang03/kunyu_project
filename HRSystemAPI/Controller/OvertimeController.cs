using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 7. 加班單 API（BPM 整合）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("7. 加班單")]
    public class OvertimeController : ControllerBase
    {
        private readonly IOvertimeService _overtimeService;
        private readonly ILogger<OvertimeController> _logger;

        public OvertimeController(
            IOvertimeService overtimeService,
            ILogger<OvertimeController> logger)
        {
            _overtimeService = overtimeService;
            _logger = logger;
        }

        /// <summary>
        /// 查詢加班單記錄（支援日期區間查詢）
        /// </summary>
        /// <param name="employeeNo">員工編號（選填）</param>
        /// <param name="startDate">開始日期（選填，格式：yyyy-MM-dd）</param>
        /// <param name="endDate">結束日期（選填，格式：yyyy-MM-dd）</param>
        /// <param name="approvalStatus">簽核狀態（選填）</param>
        /// <returns>加班單記錄列表</returns>
        /// <remarks>
        /// 範例請求：
        /// 
        ///     GET /api/Overtime?employeeNo=E001234&amp;startDate=2025-09-01&amp;endDate=2025-10-31
        /// 
        /// 預設查詢近 2 個月記錄，可透過 startDate 和 endDate 指定區間
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(List<OvertimeRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOvertimeRecords(
            [FromQuery] string? employeeNo = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string? approvalStatus = null)
        {
            try
            {
                // 如果沒有指定日期區間，預設查詢近 2 個月
                if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
                {
                    endDate = DateTime.Now.ToString("yyyy-MM-dd");
                    startDate = DateTime.Now.AddMonths(-2).ToString("yyyy-MM-dd");
                }

                var request = new OvertimeQueryRequest
                {
                    EmployeeNo = employeeNo,
                    StartDate = startDate,
                    EndDate = endDate,
                    ApprovalStatus = approvalStatus
                };

                var result = await _overtimeService.GetOvertimeRecordsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢加班單記錄失敗");
                return StatusCode(500, new { message = "查詢加班單時發生錯誤", error = ex.Message });
            }
        }

        /// <summary>
        /// 取得員工近期加班記錄（預設 2 個月）
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="months">月份數（預設 2）</param>
        /// <returns>加班單記錄列表</returns>
        [HttpGet("recent/{employeeNo}")]
        [ProducesResponseType(typeof(List<OvertimeRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRecentOvertimeRecords(
            string employeeNo,
            [FromQuery] int months = 2)
        {
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                return BadRequest(new { message = "員工編號為必填" });
            }

            if (months < 1 || months > 12)
            {
                return BadRequest(new { message = "月份數必須在 1-12 之間" });
            }

            try
            {
                var result = await _overtimeService.GetRecentOvertimeRecordsAsync(employeeNo, months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢近期加班記錄失敗（員工: {EmployeeNo}）", employeeNo);
                return StatusCode(500, new { message = "查詢加班記錄時發生錯誤", error = ex.Message });
            }
        }

        /// <summary>
        /// 查詢單一加班單詳情
        /// </summary>
        /// <param name="formId">加班單ID（BPM FormID）</param>
        /// <returns>加班單詳細資訊</returns>
        [HttpGet("{formId}")]
        [ProducesResponseType(typeof(OvertimeRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOvertimeById(string formId)
        {
            try
            {
                var result = await _overtimeService.GetOvertimeByIdAsync(formId);
                
                if (result == null)
                {
                    return NotFound(new { message = "找不到該加班單" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢加班單詳情失敗（FormID: {FormId}）", formId);
                return StatusCode(500, new { message = "查詢加班單時發生錯誤", error = ex.Message });
            }
        }

        /// <summary>
        /// 申請加班單（支援附件上傳）
        /// </summary>
        /// <param name="request">加班單申請資料</param>
        /// <returns>申請結果</returns>
        /// <remarks>
        /// 範例請求（使用 form-data）：
        /// 
        ///     POST /api/Overtime
        ///     Content-Type: multipart/form-data
        ///     
        ///     employeeNo: E001234
        ///     employeeEmail: employee@company.com
        ///     overtimeDate: 2025-11-05
        ///     startTime: 18:00
        ///     endTime: 21:00
        ///     reason: 專案趕工
        ///     compensationType: 補休
        ///     agreeToRestDaySwap: false
        ///     remark: 配合客戶需求
        ///     attachments: [file1.pdf, file2.jpg]
        /// 
        /// 注意事項：
        /// - 週日加班時，agreeToRestDaySwap 必須為 true
        /// - 處理方式可選：補休、加班費
        /// - 系統會自動計算加班時數
        /// - 附件會自動上傳到 FTP 伺服器
        /// </remarks>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(OvertimeOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOvertime([FromForm] CreateOvertimeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _overtimeService.CreateOvertimeAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請加班單失敗（員工: {EmployeeNo}）", request.EmployeeNo);
                return StatusCode(500, new { message = "申請加班單時發生錯誤", error = ex.Message });
            }
        }

        /// <summary>
        /// 取消加班單
        /// </summary>
        /// <param name="formId">加班單ID（BPM FormID）</param>
        /// <param name="employeeNo">員工編號</param>
        /// <returns>取消結果</returns>
        /// <remarks>
        /// 範例請求：
        /// 
        ///     DELETE /api/Overtime/OT20251105001?employeeNo=E001234
        /// 
        /// 注意事項：
        /// - 只有「待審核」狀態的加班單可以取消
        /// - 只有申請人本人可以取消
        /// </remarks>
        [HttpDelete("{formId}")]
        [ProducesResponseType(typeof(OvertimeOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelOvertime(string formId, [FromQuery] string employeeNo)
        {
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                return BadRequest(new { message = "員工編號為必填" });
            }

            try
            {
                var result = await _overtimeService.CancelOvertimeAsync(formId, employeeNo);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消加班單失敗（FormID: {FormId}）", formId);
                return StatusCode(500, new { message = "取消加班單時發生錯誤", error = ex.Message });
            }
        }

        /// <summary>
        /// 更新實際加班時間
        /// </summary>
        /// <param name="formId">加班單ID（BPM FormID）</param>
        /// <param name="request">實際加班時間資料</param>
        /// <returns>更新結果</returns>
        [HttpPut("{formId}/actual")]
        [ProducesResponseType(typeof(OvertimeOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateActualOvertime(string formId, [FromBody] UpdateActualOvertimeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _overtimeService.UpdateActualOvertimeAsync(formId, request);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新實際加班時間失敗（FormID: {FormId}）", formId);
                return StatusCode(500, new { message = "更新實際加班時間時發生錯誤", error = ex.Message });
            }
        }
    }
}