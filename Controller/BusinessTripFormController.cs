using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 出差表單 API（BPM 整合 - PI_BUSINESS_TRIP_001）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("PI_BUSINESS_TRIP_001")]
    public class BusinessTripFormController : ControllerBase
    {
        private readonly IBusinessTripFormService _businessTripFormService;
        private readonly ILogger<BusinessTripFormController> _logger;

        public BusinessTripFormController(
            IBusinessTripFormService businessTripFormService,
            ILogger<BusinessTripFormController> logger)
        {
            _businessTripFormService = businessTripFormService;
            _logger = logger;
        }

        /// <summary>
        /// 申請出差表單（支援附件上傳）- 使用 PI_BUSINESS_TRIP_001
        /// </summary>
        /// <param name="request">出差表單申請資料</param>
        /// <returns>申請結果</returns>
        /// <remarks>
        /// 範例請求（使用 form-data）：
        /// 
        ///     POST /api/BusinessTripForm
        ///     Content-Type: multipart/form-data
        ///     
        ///     Email: employee@company.com
        ///     Date: 2025-11-16
        ///     Reason: 客戶拜訪
        ///     StartDate: 2025-11-20
        ///     EndDate: 2025-11-22
        ///     Location: 台北市
        ///     NumberOfDays: 3
        ///     MainTasksOfTrip: 拜訪客戶，洽談專案合作事宜
        ///     EstimatedCosts: 津貼: 3000元, 機票: 8000元, 住宿: 5000元
        ///     ApplicationDateTime: 2025-11-16 14:30 (選填，預設當前時間)
        ///     ApprovalStatus: 待審核 (選填，預設「待審核」)
        ///     ApprovingPersonnel: 張經理 (選填)
        ///     ApprovalTime: (選填)
        ///     Remarks: 需事先預約客戶時間 (選填)
        ///     Attachments: [file1.pdf, file2.jpg] (選填)
        /// 
        /// 必填欄位說明：
        /// - Date: 日期（申請日期）
        /// - Reason: 事由
        /// - StartDate: 出差起始日期
        /// - EndDate: 出差結束日期
        /// - Email: 員工 Email
        /// - ApplicationDateTime: 申請日期時間（未提供時使用當前時間）
        /// - ApprovalStatus: 簽核狀態（未提供時預設「待審核」）
        /// - ApprovingPersonnel: 簽核人員（選填）
        /// - ApprovalTime: 簽核時間（選填）
        /// - Remarks: 備註（選填）
        /// - Location: 地點
        /// - NumberOfDays: 天數
        /// - MainTasksOfTrip: 出差主要任務
        /// - EstimatedCosts: 費用預估（津貼、機票、其他費用）
        /// 
        /// 注意事項：
        /// - 日期格式支援 yyyy-MM-dd 或 yyyy/MM/dd
        /// - 時間格式為 yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm
        /// - 系統會自動查詢員工資料並填充表單欄位
        /// - 附件會自動上傳到 FTP 伺服器
        /// - 會呼叫 BPM 表單預覽 API 取得自動計算欄位
        /// - NumberOfDays 範圍為 0.5 到 365 天
        /// </remarks>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BusinessTripFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateBusinessTripForm([FromForm] CreateBusinessTripFormRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _businessTripFormService.CreateBusinessTripFormAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請出差表單失敗（Email: {Email}, Location: {Location}）", request.Email, request.Location);
                return StatusCode(500, new { message = "申請出差表單時發生錯誤", error = ex.Message });
            }
        }
    }
}
