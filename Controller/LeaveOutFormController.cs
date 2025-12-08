using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 外出外訓申請單 API (整合 BPM 系統)
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class LeaveOutFormController : ControllerBase
    {
        private readonly ILeaveOutFormService _leaveOutFormService;
        private readonly ILogger<LeaveOutFormController> _logger;

        public LeaveOutFormController(
            ILeaveOutFormService leaveOutFormService,
            ILogger<LeaveOutFormController> logger)
        {
            _leaveOutFormService = leaveOutFormService;
            _logger = logger;
        }

        #region 申請外出外訓單

        /// <summary>
        /// 提交外出或外訓申請單
        /// </summary>
        /// <remarks>
        /// 使用者提交外出或外訓申請，包含時間、地點、事由與是否返回公司，並可附加檔案。
        /// 
        /// **表單類型 (etype):**
        /// - A: 外出
        /// - B: 外訓
        /// 
        /// **是否返回公司 (ereturncompany):**
        /// - T: 是
        /// - F: 否
        /// 
        /// **附件檔案格式 (efiletype):**
        /// - B: 外出外訓附件檔
        /// 
        /// **附件上傳說明:**
        /// - 若需附加檔案，請先呼叫附件上傳 API: `POST http://54.46.24.34:5112/api/Attachment/Upload`
        /// - 從上傳 API 的回應中取得 `tfileid`（附件檔序號）
        /// - 將取得的 `tfileid` 放入本 API 的 `efileid` 欄位中
        /// - `efileid` 為字串陣列，可包含多個附件檔序號
        /// 
        /// **請求範例:**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "etype": "A",
        ///   "edate": "2025-09-16",
        ///   "estarttime": "08:55",
        ///   "eendtime": "18:00",
        ///   "elocation": "新北市三重區重新路四段97號20樓",
        ///   "ereason": "拜訪客戶",
        ///   "ereturncompany": "F",
        ///   "efiletype": "B",
        ///   "efileid": ["4", "5", "80"]
        /// }
        /// ```
        /// 
        /// **注意:** 上述範例中的 efileid 值應從附件上傳 API 的回應中動態取得，而非固定值。
        /// 
        /// **成功回應:**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// **失敗回應:**
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">外出外訓申請單請求資料</param>
        /// <returns>操作結果</returns>
        [HttpPost("efleaveout")]
        [ProducesResponseType(typeof(LeaveOutFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LeaveOutFormOperationResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveOutFormOperationResult>> CreateLeaveOutForm(
            [FromBody] CreateLeaveOutFormRequest request)
        {
            try
            {
                _logger.LogInformation("收到外出外訓申請，員工工號: {Uid}, 類型: {Type}, 日期: {Date}", 
                    request.Uid, request.Etype, request.Edate);

                // 驗證 ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    var errorMessage = string.Join("; ", errors);
                    _logger.LogWarning("外出外訓申請驗證失敗: {Errors}", errorMessage);

                    return Ok(new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errorMessage}"
                    });
                }

                // 呼叫服務處理申請
                var result = await _leaveOutFormService.CreateLeaveOutFormAsync(request);

                // 根據 code 決定 HTTP 狀態碼
                if (result.Code == "200")
                {
                    _logger.LogInformation("外出外訓申請成功，員工工號: {Uid}, 表單ID: {FormId}", 
                        request.Uid, result.FormId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("外出外訓申請失敗，員工工號: {Uid}, 原因: {Msg}", 
                        request.Uid, result.Msg);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理外出外訓申請時發生錯誤");
                return Ok(new LeaveOutFormOperationResult
                {
                    Code = "203",
                    Msg = "系統錯誤，請稍後再試"
                });
            }
        }

        #endregion

        #region 查詢外出外訓單

#if false // 已註銷 - 2024-11-24

        /// <summary>
        /// 查詢外出外訓單記錄
        /// </summary>
        /// <remarks>
        /// 查詢指定員工的外出外訓單記錄，可選擇性指定日期區間。
        /// 
        /// **查詢參數:**
        /// - employeeNo: 員工工號 (必填)
        /// - startDate: 開始日期 (選填，格式: yyyy-MM-dd)
        /// - endDate: 結束日期 (選填，格式: yyyy-MM-dd)
        /// 
        /// **範例:**
        /// ```
        /// GET /app/efleaveout?employeeNo=0325&startDate=2025-09-01&endDate=2025-09-30
        /// ```
        /// </remarks>
        [HttpGet("efleaveout")]
        [ProducesResponseType(typeof(List<LeaveOutFormRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<LeaveOutFormRecord>>> GetLeaveOutForms(
            [FromQuery, Required] string employeeNo,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                _logger.LogInformation("查詢外出外訓單記錄，員工工號: {EmployeeNo}", employeeNo);

                var records = await _leaveOutFormService.GetLeaveOutFormsAsync(employeeNo, startDate, endDate);
                
                _logger.LogInformation("查詢到 {Count} 筆外出外訓單記錄", records.Count);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢外出外訓單記錄時發生錯誤");
                return StatusCode(500, new { message = "查詢失敗" });
            }
        }

        /// <summary>
        /// 查詢單一外出外訓單詳情
        /// </summary>
        /// <remarks>
        /// 根據表單ID查詢外出外訓單的詳細資訊。
        /// 
        /// **範例:**
        /// ```
        /// GET /app/efleaveout/{formId}
        /// ```
        /// </remarks>
        [HttpGet("efleaveout/{formId}")]
        [ProducesResponseType(typeof(LeaveOutFormRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveOutFormRecord>> GetLeaveOutFormById(string formId)
        {
            try
            {
                _logger.LogInformation("查詢外出外訓單詳情，表單ID: {FormId}", formId);

                var record = await _leaveOutFormService.GetLeaveOutFormByIdAsync(formId);
                
                if (record == null)
                {
                    _logger.LogWarning("找不到外出外訓單，表單ID: {FormId}", formId);
                    return NotFound(new { message = "找不到指定的外出外訓單" });
                }

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢外出外訓單詳情時發生錯誤");
                return StatusCode(500, new { message = "查詢失敗" });
            }
        }

#endif // 已註銷 - GET /app/efleaveout, GET /app/efleaveout/{formId}

        #endregion

        #region 取消外出外訓單

        /// <summary>
        /// 取消外出外訓單
        /// </summary>
        /// <remarks>
        /// 取消指定的外出外訓單，需提供取消原因。
        /// 
        /// **請求範例:**
        /// ```json
        /// {
        ///   "formId": "FORM123456",
        ///   "cancelReason": "行程異動"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("efleaveout/cancel")]
        [ProducesResponseType(typeof(LeaveOutFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveOutFormOperationResult>> CancelLeaveOutForm(
            [FromBody] CancelLeaveOutFormRequest request)
        {
            try
            {
                _logger.LogInformation("取消外出外訓單，表單ID: {FormId}", request.FormId);

                if (string.IsNullOrWhiteSpace(request.FormId))
                {
                    return Ok(new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = "表單ID不可為空"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CancelReason))
                {
                    return Ok(new LeaveOutFormOperationResult
                    {
                        Code = "203",
                        Msg = "取消原因不可為空"
                    });
                }

                var result = await _leaveOutFormService.CancelLeaveOutFormAsync(
                    request.FormId, 
                    request.CancelReason);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消外出外訓單時發生錯誤");
                return Ok(new LeaveOutFormOperationResult
                {
                    Code = "203",
                    Msg = "系統錯誤，請稍後再試"
                });
            }
        }

        #endregion
    }

    #region 輔助 Request Models

    /// <summary>
    /// 取消外出外訓單請求
    /// </summary>
    public class CancelLeaveOutFormRequest
    {
        /// <summary>
        /// 表單ID
        /// </summary>
        [Required(ErrorMessage = "formId 為必填")]
        public string FormId { get; set; } = string.Empty;

        /// <summary>
        /// 取消原因
        /// </summary>
        [Required(ErrorMessage = "cancelReason 為必填")]
        public string CancelReason { get; set; } = string.Empty;
    }

    #endregion
}
