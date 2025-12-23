using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 加班表單 API（BPM 整合 - PI_OVERTIME_001）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("PI_OVERTIME_001")]
    [ServiceFilter(typeof(TokenValidationFilter))]
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

        // /// <summary>
        // /// 申請加班表單（支援附件上傳）- 使用 PI_OVERTIME_001
        // /// </summary>
        // /// <param name="request">加班表單申請資料</param>
        // /// <returns>申請結果</returns>
        // /// <remarks>
        // /// 範例請求（使用 form-data）：
        // /// 
        // ///     POST /api/OvertimeForm
        // ///     Content-Type: multipart/form-data
        // ///     
        // ///     Email: employee@company.com
        // ///     ApplyDate: 2025-11-16
        // ///     StartTimeF: 2025-11-16 18:00
        // ///     EndTimeF: 2025-11-16 21:00
        // ///     StartTime: 2025-11-16 18:00
        // ///     EndTime: 2025-11-16 21:00
        // ///     Detail: 專案趕工
        // ///     Attachments: [file1.pdf, file2.jpg]
        // /// 
        // /// 注意事項：
        // /// - 日期格式支援 yyyy-MM-dd 或 yyyy/MM/dd
        // /// - 時間格式為 yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm
        // /// - 系統會自動查詢員工資料並填充表單欄位
        // /// - 附件會自動上傳到 FTP 伺服器
        // /// - 會呼叫 BPM 表單預覽 API 取得自動計算欄位
        // /// </remarks>
        // [HttpPost]
        // [Consumes("multipart/form-data")]
        // [ProducesResponseType(typeof(OvertimeFormOperationResult), StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // public async Task<IActionResult> CreateOvertimeForm([FromForm] CreateOvertimeFormRequest request)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         return BadRequest(ModelState);
        //     }

        //     try
        //     {
        //         var result = await _overtimeFormService.CreateOvertimeFormAsync(request);
                
        //         if (!result.Success)
        //         {
        //             return BadRequest(result);
        //         }

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "申請加班表單失敗（Email: {Email}）", request.Email);
        //         return StatusCode(500, new { message = "申請加班表單時發生錯誤", error = ex.Message });
        //     }
        // }

        /// <summary>
        /// API 1: 加班單預申請
        /// </summary>
        /// <remarks>
        /// **附件上傳說明:**
        /// - 若需附加檔案，請先呼叫附件上傳 API: `POST http://54.46.24.34:5112/api/Attachment/Upload`
        /// - 從上傳 API 的回應中取得 `tfileid`（附件檔序號）
        /// - 將取得的 `tfileid` 放入本 API 的 `efileid` 欄位中
        /// - `efileid` 為字串陣列，可包含多個附件檔序號
        /// - `efiletype` 固定為 "D"（加班單預申請附件檔）
        /// 
        /// **請求範例:**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "estartdate": "2025-09-18",
        ///   "estarttime": "18:00",
        ///   "eenddate": "2025-09-18",
        ///   "eendtime": "21:00",
        ///   "ereason": "專案趕工",
        ///   "eprocess": "C",
        ///   "efiletype": "D",
        ///   "efileid": ["1", "2"]
        /// }
        /// ```
        /// 
        /// **注意:** efileid 值應從附件上傳 API 的回應中動態取得，而非固定值。
        /// </remarks>
        /// <param name="request">預申請資料</param>
        /// <returns>申請結果</returns>
        [HttpPost("~/app/efotapply")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(EFotApplyResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> EFotApply([FromBody] EFotApplyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new EFotApplyResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.EFotApplyAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班單預申請失敗（uid: {Uid}）", request.Uid);
                return Ok(new EFotApplyResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                });
            }
        }

        /// <summary>
        /// API: 加班確認提交（POST /app/efotconfirmsubmit）
        /// 提交實際發生的加班申請表單，填具實際的加班時間及所需附件後送出
        /// </summary>
        /// <param name="request">加班確認提交資料</param>
        /// <returns>提交結果</returns>
        /// <remarks>
        /// 輸入範例：
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "formid": "PI-HR-H1A-PKG-Test0000000000053",
        ///   "astartdate": "2025-09-18",
        ///   "astarttime": "19:00",
        ///   "aenddate": "2025-09-18",
        ///   "aendtime": "22:00",
        ///   "efiletype": "D",
        ///   "efileid": ["4", "5", "80"]
        /// }
        /// ```
        /// 
        /// 輸出範例（成功）：
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// 輸出範例（失敗）：
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("~/app/efotconfirmsubmit")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(FotConfirmSubmitResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> FotConfirm([FromBody] FotConfirmSubmitRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new FotConfirmSubmitResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.FotConfirmSubmitAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班確認提交失敗（formid: {Formid}）", request.Formid);
                return Ok(new FotConfirmSubmitResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                });
            }
        }

        /// <summary>
        /// API 2: 加班確認申請列表
        /// </summary>
        /// <param name="request">查詢條件</param>
        /// <returns>加班單列表</returns>
        [HttpPost("~/app/efotconfirmlist")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(EFotConfirmListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> EFotConfirmList([FromBody] EFotConfirmListRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new EFotConfirmListResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.EFotConfirmListAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得加班確認申請列表失敗（uid: {Uid}）", request.Uid);
                return Ok(new EFotConfirmListResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                });
            }
        }

        /// <summary>
        /// API 3: 加班單詳情預覽
        /// </summary>
        /// <param name="request">查詢條件</param>
        /// <returns>加班單詳情</returns>
        [HttpPost("~/app/efotpreview")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(EFotPreviewResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> EFotPreview([FromBody] EFotPreviewRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new EFotPreviewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.EFotPreviewAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得加班單詳情失敗（formid: {FormId}）", request.Formid);
                return Ok(new EFotPreviewResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                });
            }
        }

        /// <summary>
        /// API 4: 加班單確認申請送出
        /// </summary>
        /// <param name="request">確認申請資料</param>
        /// <returns>申請結果</returns>
        [HttpPost("~/app/efotconfirm")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(EFotConfirmSubmitResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> EFotConfirmSubmit([FromBody] EFotConfirmSubmitRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new EFotConfirmSubmitResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.EFotConfirmSubmitAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加班單確認申請送出失敗（formid: {FormId}）", request.Formid);
                return Ok(new EFotConfirmSubmitResponse
                {
                    Code = "500",
                    Msg = "系統錯誤"
                });
            }
        }

        /// <summary>
        /// API 5: 代理人資料查詢
        /// </summary>
        /// <param name="request">查詢參數</param>
        /// <returns>代理人資料列表</returns>
        /// <remarks>
        /// 查詢全公司的員工資料作為代理人選項
        /// 
        /// 輸入範例：
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "03546618",
        ///   "uid": "0325"
        /// }
        /// ```
        /// 
        /// 輸出範例（成功）：
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": {
        ///     "agentdata": [
        ///       {
        ///         "agentdept": "資訊處",
        ///         "agentid": "3537",
        ///         "agentname": "曹○○"
        ///       },
        ///       {
        ///         "agentdept": "PWB-製造處",
        ///         "agentid": "3396",
        ///         "agentname": "許○○"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// 
        /// 輸出範例（失敗）：
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("~/app/getagent")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(GetAgentResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAgent([FromBody] GetAgentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new GetAgentResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }

            try
            {
                var result = await _overtimeFormService.GetAgentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢代理人資料失敗（cid: {Cid}）", request.Cid);
                return Ok(new GetAgentResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }
    }
}
