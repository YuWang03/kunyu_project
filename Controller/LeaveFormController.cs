using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 請假單 API(整合 BPM 系統)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class LeaveFormController : ControllerBase
    {
        private readonly ILeaveFormService _leaveFormService;
        private readonly ILogger<LeaveFormController> _logger;

        public LeaveFormController(
            ILeaveFormService leaveFormService,
            ILogger<LeaveFormController> logger)
        {
            _leaveFormService = leaveFormService;
            _logger = logger;
        }

        #region 查詢相關 API

#if false // 暫時隱藏 - BPM 中間件不支援查詢 API
        /// <summary>
        /// 查詢所有請假單（不限制條件）
        /// </summary>
        /// <remarks>
        /// 查詢所有表單，不受時間限制
        /// - 適合用於測試或查看所有資料
        /// - ⚠️ 注意：生產環境建議加上分頁或時間限制
        /// </remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetAllLeaveForms()
        {
            try
            {
                _logger.LogInformation("查詢所有請假單 API 被呼叫");

                var request = new LeaveFormQueryRequest
                {
                    // 不設定任何日期限制
                    PageSize = 1000 // 設定較大的分頁數
                };

                var records = await _leaveFormService.GetLeaveFormsAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = records,
                    totalCount = records.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢所有請假單失敗");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢請假單列表（支援多種條件篩選）
        /// </summary>
        /// <remarks>
        /// 查詢條件說明：
        /// - 支援依員工工號、Email、西元年、月、日期區間、簽核狀態、假別類型查詢
        /// - 預設查詢近 2 個月的記錄
        /// - 支援分頁查詢
        /// 
        /// 查詢方式範例：
        /// 1. 按年查詢：year=2025
        /// 2. 按年月查詢：year=2025&amp;month=11
        /// 3. 按日期區間查詢：startDate=2025-01-01&amp;endDate=2025-01-31
        /// 4. 按員工查詢：employeeEmail=user@example.com
        /// 
        /// 簽核狀態選項：
        /// - pending: 待簽核
        /// - approved: 已核准
        /// - rejected: 已拒絕
        /// - cancelled: 已取消
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetLeaveForms([FromQuery] LeaveFormQueryRequest request)
        {
            try
            {
                _logger.LogInformation("查詢請假單列表 API 被呼叫");

                // 如果沒有指定任何查詢條件，預設查詢近 2 個月
                if (string.IsNullOrEmpty(request.StartDate) && 
                    string.IsNullOrEmpty(request.EndDate) && 
                    !request.Year.HasValue && 
                    !request.Month.HasValue)
                {
                    var endDate = DateTime.Now;
                    var startDate = endDate.AddMonths(-2);
                    request.StartDate = startDate.ToString("yyyy-MM-dd");
                    request.EndDate = endDate.ToString("yyyy-MM-dd");
                    _logger.LogInformation("使用預設查詢區間（近 2 個月）: {StartDate} ~ {EndDate}", 
                        request.StartDate, request.EndDate);
                }

                var records = await _leaveFormService.GetLeaveFormsAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = records,
                    totalCount = records.Count,
                    page = request.Page,
                    pageSize = request.PageSize,
                    queryConditions = new
                    {
                        request.Year,
                        request.Month,
                        request.StartDate,
                        request.EndDate,
                        request.EmployeeEmail,
                        request.ApprovalStatus,
                        request.LeaveType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單列表失敗");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢單一請假單詳情
        /// </summary>
        /// <param name="formId">表單 ID</param>
        /// <remarks>
        /// 回傳詳細內容包含：
        /// - 請假類別
        /// - 起始時間、截止時間（時間單位視假別決定）
        /// - 事由
        /// - 申請日期時間
        /// - 簽核狀態、簽核人員、簽核時間
        /// - 備註
        /// - 事件發生日（參考資料庫）
        /// - 附件（Word、Excel、PDF、圖片）
        /// </remarks>
        [HttpGet("{formId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetLeaveFormById([Required] string formId)
        {
            try
            {
                _logger.LogInformation("查詢請假單詳情: {FormId}", formId);

                var record = await _leaveFormService.GetLeaveFormByIdAsync(formId);

                if (record == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"找不到表單 ID: {formId}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = record
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單詳情失敗: {FormId}", formId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢我的請假單（預設查詢近 2 個月）
        /// </summary>
        /// <param name="email">員工 Email（必填）</param>
        /// <param name="startDate">查詢開始日期（選填，格式：yyyy-MM-dd）</param>
        /// <param name="endDate">查詢結束日期（選填，格式：yyyy-MM-dd）</param>
        [HttpGet("my-forms")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetMyLeaveForms(
            [FromQuery][Required][EmailAddress] string email,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("查詢我的請假單: {Email}", email);

                // 預設查詢近 2 個月
                if (!startDate.HasValue && !endDate.HasValue)
                {
                    endDate = DateTime.Now;
                    startDate = endDate.Value.AddMonths(-2);
                }

                var records = await _leaveFormService.GetMyLeaveFormsAsync(email, startDate, endDate);

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = records,
                    totalCount = records.Count,
                    queryPeriod = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢我的請假單失敗: {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢待簽核請假單
        /// </summary>
        /// <param name="approverEmail">簽核人 Email（必填）</param>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetPendingLeaveForms(
            [FromQuery][Required][EmailAddress] string approverEmail)
        {
            try
            {
                _logger.LogInformation("查詢待簽核請假單: {ApproverEmail}", approverEmail);

                var records = await _leaveFormService.GetPendingLeaveFormsAsync(approverEmail);

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = records,
                    totalCount = records.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢待簽核請假單失敗: {ApproverEmail}", approverEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢近期請假記錄
        /// </summary>
        /// <param name="email">員工 Email（必填）</param>
        /// <param name="months">查詢近幾個月（預設：2）</param>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetRecentLeaveForms(
            [FromQuery][Required][EmailAddress] string email,
            [FromQuery][Range(1, 12)] int months = 2)
        {
            try
            {
                _logger.LogInformation("查詢近 {Months} 個月請假記錄: {Email}", months, email);

                var records = await _leaveFormService.GetRecentLeaveFormsAsync(email, months);

                return Ok(new
                {
                    success = true,
                    message = "查詢成功",
                    data = records,
                    totalCount = records.Count,
                    queryMonths = months
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢近期請假記錄失敗: {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

#endif // 查詢相關 API 暫時隱藏

        #endregion

        #region 申請與操作 API

#if false // 暫時隱藏 - BPM 中間件不支援申請 API
        /// <summary>
        /// 申請請假單（支援附件上傳）
        /// </summary>
        /// <remarks>
        /// 申請請假單流程：
        /// 1. 填寫必填欄位（假別、日期時間、事由、代理人）
        /// 2. 選填欄位（附件、代理人列表、簽核層級等）
        /// 3. 系統會自動上傳附件到 FTP Server
        /// 4. 整合 BPM 系統建立請假流程
        /// 
        /// 必填欄位：
        /// - email: 申請人 Email
        /// - leaveTypeId: 假別代碼（⚠️ 必須使用正確格式）
        ///   常用假別：
        ///   * S0001-1: 事假
        ///   * S0001-2: 家庭照顧假
        ///   * S0002-1: 病假
        ///   * S0002-2: 生理假
        ///   * S0003-1: 婚假
        ///   * S0006-1: 八日喪假
        ///   * S0006-2: 六日喪假
        ///   * S0007-2: 體檢公假
        ///   * S0008-1: 公傷病假
        ///   * S0010-1: 遲到假
        ///   * S0013-2: 國外出差
        ///   * S0014-1: 產檢假
        ///   * S0019-1: 洽公外出
        ///   * S0020-1: 駐地休假
        ///   * SLC01: 加班
        ///   * SLC01-REGL: 例假日加班
        ///   * SLC03: 補休假
        ///   * SLC04: 特休假
        /// - leaveTypeName: 假別名稱（對應上述代碼）
        /// - startDate: 開始日期（yyyy-MM-dd 或 yyyy/MM/dd）
        /// - startTime: 開始時間（HH:mm）
        /// - endDate: 結束日期（yyyy-MM-dd 或 yyyy/MM/dd）
        /// - endTime: 結束時間（HH:mm）
        /// - reason: 請假事由
        /// - agentNo: 代理人工號
        /// 
        /// 單位類型：
        /// - DAY: 以天為單位
        /// - HOUR: 以小時為單位
        /// 
        /// 附件支援格式：
        /// - Word: .doc, .docx
        /// - Excel: .xls, .xlsx
        /// - PDF: .pdf
        /// - 圖片: .jpg, .jpeg, .png, .gif, .bmp
        /// 
        /// 使用 Swagger 測試：
        /// 1. 點擊 "Try it out"
        /// 2. 填寫必填欄位
        /// 3. 選擇附件檔案（可多選）
        /// 4. 點擊 "Execute"
        /// </remarks>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateLeaveForm([FromForm] CreateLeaveFormRequest request)
        {
            try
            {
                _logger.LogInformation("申請請假單 API 被呼叫: {Email}, {LeaveTypeName}", 
                    request.Email, request.LeaveTypeName);

                // 驗證必填欄位
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        success = false,
                        message = "欄位驗證失敗",
                        errors = errors
                    });
                }

                var result = await _leaveFormService.CreateLeaveFormAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errorCode = result.ErrorCode
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        formId = result.FormId,
                        formNumber = result.FormNumber,
                        attachmentPaths = result.AttachmentPaths,
                        attachmentCount = result.AttachmentPaths?.Count ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請請假單失敗");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"申請失敗: {ex.Message}"
                });
            }
        }
#endif // 申請 API 暫時隱藏

#if false // 暫時隱藏 - BPM 中間件不支援簽核和取消 API
        /// <summary>
        /// 取消請假單
        /// </summary>
        /// <param name="formId">表單 ID（必填）</param>
        /// <param name="email">申請人 Email（必填）</param>
        [HttpPost("{formId}/cancel")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CancelLeaveForm(
            [Required] string formId,
            [FromQuery][Required][EmailAddress] string email)
        {
            try
            {
                _logger.LogInformation("取消請假單: {FormId}, {Email}", formId, email);

                var result = await _leaveFormService.CancelLeaveFormAsync(formId, email);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errorCode = result.ErrorCode
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        formId = result.FormId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消請假單失敗: {FormId}", formId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"取消失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 簽核請假單（支援核准/拒絕/退回重簽）
        /// </summary>
        /// <remarks>
        /// 簽核動作說明：
        /// - approve: 核准此請假單
        /// - reject: 拒絕此請假單
        /// - return: 退回重簽（全部重簽）
        /// 
        /// 範例請求：
        /// ```json
        /// {
        ///   "formId": "FORM123456",
        ///   "approverEmail": "manager@example.com",
        ///   "action": "approve",
        ///   "comment": "同意請假"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{formId}/approve")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ApproveLeaveForm(
            [Required] string formId,
            [FromBody] ApproveLeaveFormRequest request)
        {
            try
            {
                // 確保 FormId 一致
                if (!string.IsNullOrEmpty(request.FormId) && request.FormId != formId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "URL 中的 FormId 與請求內容中的 FormId 不一致"
                    });
                }

                request.FormId = formId;

                _logger.LogInformation("簽核請假單: {FormId}, Action: {Action}, Approver: {Email}", 
                    formId, request.Action, request.ApproverEmail);

                // 驗證必填欄位
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        success = false,
                        message = "欄位驗證失敗",
                        errors = errors
                    });
                }

                var result = await _leaveFormService.ApproveLeaveFormAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errorCode = result.ErrorCode
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        formId = result.FormId,
                        action = request.Action,
                        comment = request.Comment
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽核請假單失敗: {FormId}", formId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"簽核失敗: {ex.Message}"
                });
            }
        }

#endif // 簽核和取消 API 暫時隱藏

        #endregion
    }
}