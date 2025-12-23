using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 銷假單 API (整合 BPM 系統)
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class CancelLeaveController : ControllerBase
    {
        private readonly ICancelLeaveService _cancelLeaveService;
        private readonly ILogger<CancelLeaveController> _logger;

        public CancelLeaveController(
            ICancelLeaveService cancelLeaveService,
            ILogger<CancelLeaveController> logger)
        {
            _cancelLeaveService = cancelLeaveService;
            _logger = logger;
        }

        /// <summary>
        /// 1. 銷假申請列表 API
        /// </summary>
        /// <remarks>
        /// 查詢可銷假的請假單列表
        /// - 返回使用者自己提交的請假表單
        /// - 用於顯示可以申請銷假的請假單列表（已提交但因故需要取消）
        /// 
        /// **範例請求：**
        /// ```json
        /// {
        ///     "tokenid": "53422421",
        ///     "cid": "45624657",
        ///     "uid": "0325"
        /// }
        /// ```
        /// 
        /// **範例回應（成功）：**
        /// ```json
        /// {
        ///     "code": "200",
        ///     "msg": "請求成功",
        ///     "data": {
        ///         "efleveldata": [
        ///             {
        ///                 "uid": "00123",
        ///                 "uname": "王大明",
        ///                 "udepartment": "電子一部",
        ///                 "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///                 "leavetype": "病假",
        ///                 "estartdate": "2025-09-18",
        ///                 "estarttime": "08:00",
        ///                 "eenddate": "2025-09-20",
        ///                 "eendtime": "22:00",
        ///                 "ereason": "生病調養"
        ///             }
        ///         ]
        ///     }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">銷假申請列表請求</param>
        /// <returns>可銷假的請假單列表</returns>
        [HttpPost("efleavelist")] // 修改路徑
        [ProducesResponseType(typeof(CancelLeaveListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CancelLeaveListResponse>> GetCancelLeaveList(
            [FromBody] CancelLeaveListRequest request)
        {
            try
            {
                _logger.LogInformation("銷假申請列表 API 被呼叫，使用者工號: {Uid}", request.Uid);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return Ok(new CancelLeaveListResponse
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errors}"
                    });
                }

                var response = await _cancelLeaveService.GetCancelLeaveListAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation("銷假申請列表查詢成功，使用者工號: {Uid}，記錄數: {Count}", 
                        request.Uid, response.Data?.Efleveldata?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("銷假申請列表查詢失敗: {Msg}", response.Msg);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "銷假申請列表 API 發生錯誤");
                return Ok(new CancelLeaveListResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 2. 單筆請假資料查詢 API
        /// </summary>
        /// <remarks>
        /// 根據 formid 查詢單筆請假資料
        /// - 返回指定表單編號的請假資訊
        /// - 用於查詢特定請假單的基本資料
        /// 
        /// **範例請求：**
        /// ```json
        /// {
        ///     "tokenid": "53422421",
        ///     "cid": "45624657",
        ///     "uid": "0325",
        ///     "formid": "PI_Leave_IDL_BG_PI_HRM00000018"
        /// }
        /// ```
        /// 
        /// **範例回應（成功）：**
        /// ```json
        /// {
        ///     "code": "200",
        ///     "msg": "請求成功",
        ///     "data": {
        ///         "efleveldata": {
        ///             "uid": "3552",
        ///             "uname": "王大明",
        ///             "udepartment": "電子一部",
        ///             "formid": "PI_Leave_IDL_BG_PI_HRM00000018",
        ///             "leavetype": "病假",
        ///             "estartdate": "2025-09-18",
        ///             "estarttime": "08:00",
        ///             "eenddate": "2025-09-20",
        ///             "eendtime": "22:00",
        ///             "ereason": "生病調養"
        ///         }
        ///     }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">單筆請假資料查詢請求</param>
        /// <returns>單筆請假資料</returns>
        [HttpPost("efleaveget")]
        [ProducesResponseType(typeof(CancelLeaveSingleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CancelLeaveSingleResponse>> GetCancelLeaveSingle(
            [FromBody] CancelLeaveSingleRequest request)
        {
            try
            {
                _logger.LogInformation("單筆請假資料查詢 API 被呼叫，表單編號: {FormId}", request.Formid);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return Ok(new CancelLeaveSingleResponse
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errors}"
                    });
                }

                var response = await _cancelLeaveService.GetCancelLeaveSingleAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation("單筆請假資料查詢成功，表單編號: {FormId}", request.Formid);
                }
                else
                {
                    _logger.LogWarning("單筆請假資料查詢失敗: {Msg}", response.Msg);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "單筆請假資料查詢 API 發生錯誤");
                return Ok(new CancelLeaveSingleResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 3. 銷假申請詳細資料 API
        /// </summary>
        /// <remarks>
        /// 查詢單一請假單的詳細資料
        /// - 包含完整的請假資訊
        /// - 包含附件檔案列表
        /// - 包含代理人資訊
        /// 
        /// **範例請求：**
        /// ```json
        /// {
        ///     "tokenid": "53422421",
        ///     "cid": "45624657",
        ///     "uid": "0325",
        ///     "formid": "PI-HR-H1A-PKG-Test0000000000053"
        /// }
        /// ```
        /// 
        /// **範例回應（成功）：**
        /// ```json
        /// {
        ///     "code": "200",
        ///     "msg": "請求成功",
        ///     "data": {
        ///         "efleveldata": [
        ///             {
        ///                 "uid": "00123",
        ///                 "uname": "王大明",
        ///                 "udepartment": "電子一部",
        ///                 "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///                 "leavetype": "病假",
        ///                 "estartdate": "2025-09-18",
        ///                 "estarttime": "08:00",
        ///                 "eenddate": "2025-09-20",
        ///                 "eendtime": "22:00",
        ///                 "ereason": "生病調養",
        ///                 "eagent": "3100",
        ///                 "efiletype": "C",
        ///                 "attachments": [
        ///                     {
        ///                         "efileid": "1",
        ///                         "efilename": "請假單附件檔1",
        ///                         "esfilename": "20251116001.pdf",
        ///                         "efileurl": "https://xxxxxx.xxxx.xx/filecenter/0325/20251116001.pdf"
        ///                     }
        ///                 ]
        ///             }
        ///         ]
        ///     }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">銷假申請詳細資料請求</param>
        /// <returns>請假單詳細資料</returns>
        [HttpPost("efleavedetail")]
        [ProducesResponseType(typeof(CancelLeaveDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CancelLeaveDetailResponse>> GetCancelLeaveDetail(
            [FromBody] CancelLeaveDetailRequest request)
        {
            try
            {
                _logger.LogInformation("銷假申請詳細資料 API 被呼叫，表單編號: {FormId}", request.Formid);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return Ok(new CancelLeaveDetailResponse
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errors}"
                    });
                }

                var response = await _cancelLeaveService.GetCancelLeaveDetailAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation("銷假申請詳細資料查詢成功，表單編號: {FormId}", request.Formid);
                }
                else
                {
                    _logger.LogWarning("銷假申請詳細資料查詢失敗: {Msg}", response.Msg);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "銷假申請詳細資料 API 發生錯誤");
                return Ok(new CancelLeaveDetailResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 4. 銷假申請預覽 API
        /// </summary>
        /// <remarks>
        /// 查詢請假單預覽（用於銷假申請時顯示原請假單詳細資訊）
        /// - 返回原請假單的所有信息
        /// - 包含附件檔案列表
        /// - 用於銷假表單的初始化
        /// 
        /// **範例請求：**
        /// ```json
        /// {
        ///     "tokenid": "53422421",
        ///     "cid": "45624657",
        ///     "uid": "0325",
        ///     "formid": "PI-HR-H1A-PKG-Test0000000000053"
        /// }
        /// ```
        /// 
        /// **範例回應（成功）：**
        /// ```json
        /// {
        ///     "code": "200",
        ///     "msg": "請求成功",
        ///     "data": {
        ///         "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///         "estartdate": "2025-09-18",
        ///         "estarttime": "18:00",
        ///         "eenddate": "2025-09-18",
        ///         "eendtime": "22:00",
        ///         "ereason": "因配合客戶結案時間，需進行加班",
        ///         "eprocess": "補休",
        ///         "efiletype": "D",
        ///         "attachments": [
        ///             {
        ///                 "efileid": "1",
        ///                 "efilename": "加班確認附件檔1",
        ///                 "esfilename": "20251116001.pdf",
        ///                 "efileurl": "https://xxxxxx.xxxx.xx/filecenter/0325/20251116001.pdf"
        ///             }
        ///         ]
        ///     }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">銷假申請預覽請求</param>
        /// <returns>請假單預覽資料</returns>
        [HttpPost("efleavepreview")]
        [ProducesResponseType(typeof(EFleavePreviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EFleavePreviewResponse>> GetLeavePreview(
            [FromBody] EFleavePreviewRequest request)
        {
            try
            {
                _logger.LogInformation("銷假申請預覽 API 被呼叫，表單編號: {FormId}", request.Formid);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return Ok(new EFleavePreviewResponse
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errors}"
                    });
                }

                var response = await _cancelLeaveService.GetLeavePreviewAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation("銷假申請預覽查詢成功，表單編號: {FormId}", request.Formid);
                }
                else
                {
                    _logger.LogWarning("銷假申請預覽查詢失敗: {Msg}", response.Msg);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "銷假申請預覽 API 發生錯誤");
                return Ok(new EFleavePreviewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 5. 銷假申請送出 API
        /// </summary>
        /// <remarks>
        /// 提交銷假申請
        /// - 取消已申請的請假單
        /// - 需要填寫銷假原因
        /// - 會觸發 BPM 銷假流程
        /// 
        /// **範例請求：**
        /// ```json
        /// {
        ///     "tokenid": "53422421",
        ///     "cid": "45624657",
        ///     "uid": "0325",
        ///     "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///     "reasons": "計劃趕不上變化"
        /// }
        /// ```
        /// 
        /// **範例回應（成功）：**
        /// ```json
        /// {
        ///     "code": "200",
        ///     "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// **範例回應（失敗）：**
        /// ```json
        /// {
        ///     "code": "203",
        ///     "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">銷假申請送出請求</param>
        /// <returns>送出結果</returns>
        [HttpPost("efleavecancel")]
        [ProducesResponseType(typeof(CancelLeaveSubmitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CancelLeaveSubmitResponse>> SubmitCancelLeave(
            [FromBody] CancelLeaveSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("銷假申請送出 API 被呼叫，表單編號: {FormId}，原因: {Reasons}", 
                    request.Formid, request.Reasons);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return Ok(new CancelLeaveSubmitResponse
                    {
                        Code = "203",
                        Msg = $"請求失敗，{errors}"
                    });
                }

                var response = await _cancelLeaveService.SubmitCancelLeaveAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation("銷假申請送出成功，表單編號: {FormId}", request.Formid);
                }
                else
                {
                    _logger.LogWarning("銷假申請送出失敗: {Msg}", response.Msg);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "銷假申請送出 API 發生錯誤");
                return Ok(new CancelLeaveSubmitResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }
    }
}
