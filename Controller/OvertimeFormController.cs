using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 加班表單 API（BPM 整合 - PI_OVERTIME_001）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("PI_OVERTIME_001")]
    public class OvertimeFormController : ControllerBase
    {
        private readonly IOvertimeFormService _overtimeFormService;
        private readonly ILogger<OvertimeFormController> _logger;

        public OvertimeFormController(
            IOvertimeFormService overtimeFormService,
            ILogger<OvertimeFormController> logger)
        {
            _overtimeFormService = overtimeFormService;
            _logger = logger;
        }

        /// <summary>
        /// 申請加班表單（支援附件上傳）- 使用 PI_OVERTIME_001
        /// </summary>
        /// <param name="request">加班表單申請資料</param>
        /// <returns>申請結果</returns>
        /// <remarks>
        /// 範例請求（使用 form-data）：
        /// 
        ///     POST /api/OvertimeForm
        ///     Content-Type: multipart/form-data
        ///     
        ///     Email: employee@company.com
        ///     ApplyDate: 2025-11-16
        ///     StartTimeF: 2025-11-16 18:00
        ///     EndTimeF: 2025-11-16 21:00
        ///     StartTime: 2025-11-16 18:00
        ///     EndTime: 2025-11-16 21:00
        ///     Detail: 專案趕工
        ///     Attachments: [file1.pdf, file2.jpg]
        /// 
        /// 注意事項：
        /// - 日期格式支援 yyyy-MM-dd 或 yyyy/MM/dd
        /// - 時間格式為 yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm
        /// - 系統會自動查詢員工資料並填充表單欄位
        /// - 附件會自動上傳到 FTP 伺服器
        /// - 會呼叫 BPM 表單預覽 API 取得自動計算欄位
        /// </remarks>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(OvertimeFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOvertimeForm([FromForm] CreateOvertimeFormRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _overtimeFormService.CreateOvertimeFormAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請加班表單失敗（Email: {Email}）", request.Email);
                return StatusCode(500, new { message = "申請加班表單時發生錯誤", error = ex.Message });
            }
        }
    }
}
