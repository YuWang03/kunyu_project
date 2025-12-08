using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 出勤確認單 API - 處理未刷卡補登申請
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class AttendancePatchFormController : ControllerBase
    {
        private readonly IAttendancePatchFormService _attendancePatchFormService;
        private readonly ILogger<AttendancePatchFormController> _logger;

        public AttendancePatchFormController(
            IAttendancePatchFormService attendancePatchFormService,
            ILogger<AttendancePatchFormController> logger)
        {
            _attendancePatchFormService = attendancePatchFormService;
            _logger = logger;
        }

        /// <summary>
        /// 提交出勤確認單 - 補登未刷卡時間
        /// </summary>
        /// <remarks>
        /// 使用者提交未刷卡的上班或下班時間與原因，供人事審核補登。
        /// 
        /// 必填欄位：
        /// - tokenid: Token標記
        /// - cid: 目前所屬公司
        /// - uid: 使用者工號
        /// - edate: 上班日期 (格式: yyyy-MM-dd)
        /// - ereason: 原因代碼
        ///   - A: 上班忘刷卡(臉)
        ///   - B: 下班忘刷卡(臉)
        ///   - C: 上下班忘刷卡(臉)
        ///   - D: 其他
        /// 
        /// 選填欄位（二擇一）：
        /// - eclockIn: 未刷卡上班時間 (格式: HH:mm)
        /// - eclockOut: 未刷卡下班時間 (格式: HH:mm)
        /// 
        /// 條件欄位：
        /// - edetails: 其他事由 (當 ereason 為 D 時必填)
        /// 
        /// 範例 1 - 上班忘刷卡:
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "edate": "2025-09-16",
        ///   "eclockIn": "08:55",
        ///   "eclockOut": "",
        ///   "ereason": "A",
        ///   "edetails": ""
        /// }
        /// 
        /// 範例 2 - 下班忘刷卡 (其他原因):
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "edate": "2025-09-16",
        ///   "eclockIn": "",
        ///   "eclockOut": "18:30",
        ///   "ereason": "D",
        ///   "edetails": "機器故障"
        /// }
        /// </remarks>
        /// <param name="request">出勤確認單申請資料</param>
        /// <returns>處理結果</returns>
        /// <response code="200">請求成功</response>
        /// <response code="300">授權失敗，無效token</response>
        /// <response code="400">參數錯誤</response>
        /// <response code="500">系統錯誤</response>
        [HttpPost("efpatch")]
        [ProducesResponseType(typeof(AttendancePatchFormResponse), 200)]
        [ProducesResponseType(typeof(AttendancePatchFormResponse), 500)]
        public async Task<ActionResult<AttendancePatchFormResponse>> CreateAttendancePatchForm(
            [FromBody] CreateAttendancePatchFormRequest request)
        {
            try
            {
                _logger.LogInformation("收到出勤確認單申請 - 工號: {Uid}, 日期: {Date}", request.Uid, request.Edate);

                // 呼叫服務處理
                var result = await _attendancePatchFormService.CreateAttendancePatchFormAsync(request);

                // 回傳結果
                var response = new AttendancePatchFormResponse
                {
                    Code = result.Code,
                    Msg = result.Message
                };

                _logger.LogInformation("出勤確認單處理完成 - Code: {Code}", result.Code);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理出勤確認單時發生錯誤");
                
                return Ok(new AttendancePatchFormResponse
                {
                    Code = "500",
                    Msg = "系統錯誤，請稍後再試"
                });
            }
        }
    }
}
