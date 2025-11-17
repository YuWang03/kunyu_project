using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 出勤異常單（出勤確認單）API - 整合 BPM 系統
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AttendanceFormController : ControllerBase
    {
        private readonly IAttendanceFormService _attendanceFormService;
        private readonly ILogger<AttendanceFormController> _logger;

        public AttendanceFormController(
            IAttendanceFormService attendanceFormService,
            ILogger<AttendanceFormController> _logger)
        {
            _attendanceFormService = attendanceFormService;
            this._logger = _logger;
        }

        #region 申請與操作 API

        /// <summary>
        /// 申請出勤異常單（支援附件上傳）
        /// </summary>
        /// <remarks>
        /// 申請出勤異常單流程：
        /// 1. 填寫必填欄位（申請日期、異常說明）
        /// 2. 選填欄位（附件、補卡時間、異常原因等）
        /// 3. 系統會自動上傳附件到 FTP Server
        /// 4. 整合 BPM 系統建立簽核流程
        /// 
        /// 必填欄位：
        /// - email: 申請人 Email
        /// - applyDate: 申請日期（yyyy-MM-dd）
        /// - exceptionDescription: 異常說明
        /// 
        /// 選填欄位：
        /// - exceptionTime: 補卡起始時間
        /// - exceptionEndTime: 補卡結束時間
        /// - exceptionReason: 異常原因（預設：其他）
        /// - formType: 表單類型（預設：H1A）
        /// - attachments: 附件檔案（Word、Excel、PDF、圖片）
        /// 
        /// 附件支援格式：
        /// - Word: .doc, .docx
        /// - Excel: .xls, .xlsx
        /// - PDF: .pdf
        /// - 圖片: .jpg, .jpeg, .png, .gif, .bmp
        /// </remarks>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateOutingForm([FromForm] CreateAttendanceFormRequest request)
        {
            try
            {
                _logger.LogInformation("申請出勤異常單 API 被呼叫: {Email}, {ApplyDate}", 
                    request.Email, request.ApplyDate);

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

                var result = await _attendanceFormService.CreateAttendanceFormAsync(request);

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
                        formNumber = result.FormNumber
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "申請出勤異常單失敗");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"系統錯誤: {ex.Message}"
                });
            }
        }

        #endregion

        #region 查詢與管理 API

        /// <summary>
        /// 查詢出勤異常單列表
        /// </summary>
        /// <remarks>
        /// 查詢條件：
        /// - email: 申請人 Email（選填）
        /// - status: 表單狀態（選填）- 例如: SUCCESS, PENDING, REJECTED
        /// - startDate: 開始日期（選填）- 格式：yyyy-MM-dd
        /// - endDate: 結束日期（選填）- 格式：yyyy-MM-dd
        /// - pageNumber: 頁碼（預設：1）
        /// - pageSize: 每頁筆數（預設：20）
        /// </remarks>
        [HttpGet("forms")]
        [ProducesResponseType(typeof(PagedResponse<AttendanceFormSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetForms([FromQuery] GetFormsQuery query)
        {
            try
            {
                _logger.LogInformation("查詢出勤異常單列表: {@Query}", query);

                var result = await _attendanceFormService.GetFormsAsync(query);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤異常單列表失敗");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 查詢單一出勤異常單詳細資料
        /// </summary>
        /// <param name="id">表單 ID (BPM ProcessOid)</param>
        /// <remarks>
        /// 回傳完整表單資料，包含：
        /// - 基本資訊：未刷卡日期、上下班時間、原因、事由
        /// - 申請人資訊：姓名、工號、部門
        /// - 簽核資訊：簽核人員、簽核狀態、申請時間
        /// - 其他資訊：附件路徑、表單類型、廠區等
        /// </remarks>
        [HttpGet("forms/{id}")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceFormDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetFormById(string id)
        {
            try
            {
                _logger.LogInformation("查詢出勤異常單詳細資料: {FormId}", id);

                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "表單 ID 不可為空"
                    });
                }

                var result = await _attendanceFormService.GetFormByIdAsync(id);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤異常單詳細資料失敗: {FormId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 取消出勤異常單
        /// </summary>
        /// <param name="id">表單 ID (BPM ProcessOid)</param>
        /// <param name="request">取消請求（包含取消原因和申請人 Email）</param>
        /// <remarks>
        /// 取消流程：
        /// 1. 驗證申請人身份（Email）
        /// 2. 呼叫 BPM abort-process API 取消表單
        /// 3. 回傳取消結果
        /// 
        /// 必填欄位：
        /// - email: 申請人 Email（用於驗證權限）
        /// - reason: 取消原因
        /// </remarks>
        [HttpPost("forms/{id}/cancel")]
        [ProducesResponseType(typeof(AttendanceFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CancelForm(string id, [FromBody] CancelFormRequest request)
        {
            try
            {
                _logger.LogInformation("取消出勤異常單: {FormId}, Email: {Email}", id, request.Email);

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

                var result = await _attendanceFormService.CancelFormAsync(id, request);

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
                _logger.LogError(ex, "取消出勤異常單失敗: {FormId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"取消失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 退回工作項目（簽核退回）
        /// </summary>
        /// <param name="id">工作項目 ID (WorkItem ID)</param>
        /// <param name="request">退回請求（包含退回原因和審核人 Email）</param>
        /// <remarks>
        /// 退回流程：
        /// 1. 驗證審核人身份（Email）
        /// 2. 呼叫 BPM workitems/{id}/return API 退回工作項目
        /// 3. 回傳退回結果
        /// 
        /// 必填欄位：
        /// - email: 審核人 Email（用於驗證權限）
        /// - reason: 退回原因
        /// </remarks>
        [HttpPost("workitems/{id}/return")]
        [ProducesResponseType(typeof(AttendanceFormOperationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ReturnWorkItem(string id, [FromBody] ReturnWorkItemRequest request)
        {
            try
            {
                _logger.LogInformation("退回工作項目: {WorkItemId}, Email: {Email}", id, request.Email);

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

                var result = await _attendanceFormService.ReturnWorkItemAsync(id, request);

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
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退回工作項目失敗: {WorkItemId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"退回失敗: {ex.Message}"
                });
            }
        }

        #endregion

        #region Legacy Methods (保留舊版本相容性 - 暫時隱藏)

#if false // 暫時隱藏 - 其他端點稍後處理

        /// <summary>
        /// 查詢待簽核出勤確認單列表
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingForms([FromQuery] PendingOutingFormQuery query)
        {
            try
            {
                var result = await _attendanceFormService.GetPendingFormsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢待簽核出勤確認單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 取得出勤確認單詳細內容
        /// </summary>
        [HttpGet("{formId}")]
        public async Task<IActionResult> GetFormDetail(string formId)
        {
            try
            {
                if (string.IsNullOrEmpty(formId))
                    return BadRequest(new { success = false, message = "表單ID不可為空" });

                var result = await _attendanceFormService.GetFormDetailAsync(formId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得出勤確認單詳細內容時發生錯誤: {FormId}", formId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 簽核出勤確認單
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveForm([FromBody] ApproveOutingFormRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FormId))
                    return BadRequest(new { success = false, message = "表單ID為必填欄位" });

                if (string.IsNullOrEmpty(request.ApproverEmail))
                    return BadRequest(new { success = false, message = "簽核人Email為必填欄位" });

                if (string.IsNullOrEmpty(request.Action))
                    return BadRequest(new { success = false, message = "動作為必填欄位（approve/reject/return）" });

                var validActions = new[] { "approve", "reject", "return" };
                if (!validActions.Contains(request.Action.ToLower()))
                    return BadRequest(new { success = false, message = "無效的動作，僅接受 approve/reject/return" });

                var result = await _attendanceFormService.ApproveFormAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "簽核出勤確認單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"簽核失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 批次簽核出勤確認單
        /// </summary>
        [HttpPost("batch-approve")]
        public async Task<IActionResult> BatchApproveForms([FromBody] BatchApproveOutingFormRequest request)
        {
            try
            {
                if (request.FormIds == null || !request.FormIds.Any())
                    return BadRequest(new { success = false, message = "表單ID列表不可為空" });

                if (string.IsNullOrEmpty(request.ApproverEmail))
                    return BadRequest(new { success = false, message = "簽核人Email為必填欄位" });

                if (string.IsNullOrEmpty(request.Action))
                    return BadRequest(new { success = false, message = "動作為必填欄位（approve/reject）" });

                var validActions = new[] { "approve", "reject" };
                if (!validActions.Contains(request.Action.ToLower()))
                    return BadRequest(new { success = false, message = "批次簽核僅接受 approve/reject" });

                var result = await _attendanceFormService.BatchApproveFormsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次簽核出勤確認單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"批次簽核失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 下載出勤確認單附件
        /// </summary>
        [HttpGet("attachment/download")]
        public async Task<IActionResult> DownloadAttachment([FromQuery] string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return BadRequest(new { success = false, message = "檔案路徑不可為空" });

                var fileStream = await _ftpService.DownloadFileAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載附件時發生錯誤: {FilePath}", filePath);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"下載失敗: {ex.Message}"
                });
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 查詢我申請的出勤確認單
        /// </summary>
        [HttpGet("my-forms")]
        public async Task<IActionResult> GetMyForms(
            [FromQuery] string email,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return BadRequest(new { success = false, message = "Email為必填欄位" });

                var userId = await _bpmService.GetUserIdByEmailAsync(email);

                var queryParams = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["pageNumber"] = pageNumber.ToString(),
                    ["pageSize"] = pageSize.ToString()
                };

                if (startDate.HasValue)
                    queryParams["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
                if (endDate.HasValue)
                    queryParams["endDate"] = endDate.Value.ToString("yyyy-MM-dd");

                var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await _bpmService.GetAsync($"/form/outing/my-forms?{queryString}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢我的出勤確認單時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"查詢失敗: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 測試 FTP 連線
        /// </summary>
        [HttpGet("test-ftp")]
        public async Task<IActionResult> TestFtpConnection()
        {
            try
            {
                var isConnected = await _ftpService.TestConnectionAsync();
                return Ok(new
                {
                    success = isConnected,
                    message = isConnected ? "FTP 連線成功" : "FTP 連線失敗"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試 FTP 連線時發生錯誤");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"測試失敗: {ex.Message}"
                });
            }
        }

#endif // Legacy Methods

        #endregion
    }
}
