using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 待我審核 API
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewService reviewService,
            ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        /// <summary>
        /// 1. 待我審核列表
        /// </summary>
        /// <remarks>
        /// 準備由我審核的列表資訊
        /// 
        /// **功能說明**:
        /// - 從 BPM 系統取得待辦事項 (GET /api/bpm/workitems/{userId})
        /// - 整合各表單類型的詳細資料
        /// - 返回完整的待審核列表
        /// 
        /// **表單類型**:
        /// - L: 請假單
        /// - D: 銷假單
        /// - O: 外出外訓單
        /// - A: 加班單
        /// - R: 出勤確認單
        /// - T: 出差單
        /// 
        /// **請求範例**:
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325"
        /// }
        /// ```
        /// 
        /// **回應範例 (成功)**:
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": {
        ///     "eformdata": [
        ///       {
        ///         "uname": "王大明",
        ///         "udepartment": "電子一部",
        ///         "formidtitle": "表單編號",
        ///         "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///         "eformtypetitle": "申請類別",
        ///         "eformtype": "L",
        ///         "eformname": "請假單",
        ///         "estarttitle": "起始時間",
        ///         "estartdate": "2025-09-18",
        ///         "estarttime": "08:00",
        ///         "eendtitle": "結束時間",
        ///         "eenddate": "2025-09-18",
        ///         "eendtime": "17:00",
        ///         "ereasontitle": "事由",
        ///         "ereason": "家中有事"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">待審核列表請求</param>
        /// <returns>待審核列表資料</returns>
        [HttpPost("eformreview")]
        [ProducesResponseType(typeof(ReviewListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ReviewListResponse), StatusCodes.Status203NonAuthoritative)]
        public async Task<ActionResult<ReviewListResponse>> GetReviewList([FromBody] ReviewListRequest request)
        {
            try
            {
                _logger.LogInformation("待我審核列表 API 被呼叫，UID: {Uid}", request.Uid);

                var response = await _reviewService.GetReviewListAsync(request);

                if (response.Code == "200")
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(StatusCodes.Status203NonAuthoritative, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "待我審核列表 API 發生錯誤");
                return StatusCode(StatusCodes.Status203NonAuthoritative, new ReviewListResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 2. 待我審核詳細資料
        /// </summary>
        /// <remarks>
        /// 準備由我審核的詳細資訊
        /// 
        /// **功能說明**:
        /// - 根據表單編號取得完整的表單資料
        /// - 包含申請人資訊、表單內容、附件、審核流程等
        /// 
        /// **請求範例**:
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "00123",
        ///   "formid": "PI-HR-H1A-PKG-Test0000000000053"
        /// }
        /// ```
        /// 
        /// **回應範例 (成功)**:
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": {
        ///     "uid": "0325",
        ///     "uname": "王大明",
        ///     "udepartment": "電子一部",
        ///     "formidtitle": "表單編號",
        ///     "formid": "PI-HR-H1A-PKG-Test0000000000035",
        ///     "eformtypetitle": "申請類別",
        ///     "eformtype": "L",
        ///     "eformname": "請假單",
        ///     "estarttitle": "起始時間",
        ///     "estartdate": "2025-09-18",
        ///     "estarttime": "08:00",
        ///     "eendtitle": "結束時間",
        ///     "eenddate": "2025-09-18",
        ///     "eendtime": "17:00",
        ///     "ereasontitle": "事由",
        ///     "ereason": "家中有事",
        ///     "eagenttitle": "代理人",
        ///     "eagent": "3100",
        ///     "efiletype": "C",
        ///     "attachments": [
        ///       {
        ///         "efileid": "1",
        ///         "efilename": "請假單附件檔1",
        ///         "esfilename": "20251116001.pdf",
        ///         "efileurl": "https://xxxxxx.xxxx.xx/filecenter/0325/20251116001.pdf"
        ///       }
        ///     ],
        ///     "eformflow": [
        ///       {
        ///         "workitem": "表單已送出",
        ///         "workstatus": "已完成"
        ///       },
        ///       {
        ///         "workitem": "代理人 2335 陳○○ 簽核",
        ///         "workstatus": "未完成"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">待審核詳細資料請求</param>
        /// <returns>待審核詳細資料</returns>
        [HttpPost("eformdetail")]
        [ProducesResponseType(typeof(ReviewDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ReviewDetailResponse), StatusCodes.Status203NonAuthoritative)]
        public async Task<ActionResult<ReviewDetailResponse>> GetReviewDetail([FromBody] ReviewDetailRequest request)
        {
            try
            {
                _logger.LogInformation("待我審核詳細資料 API 被呼叫，FormId: {FormId}", request.FormId);

                var response = await _reviewService.GetReviewDetailAsync(request);

                if (response.Code == "200")
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(StatusCodes.Status203NonAuthoritative, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "待我審核詳細資料 API 發生錯誤");
                return StatusCode(StatusCodes.Status203NonAuthoritative, new ReviewDetailResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }

        /// <summary>
        /// 3. 待我審核簽核作業
        /// </summary>
        /// <remarks>
        /// 由待我審核詳列表或詳細資訊頁進行多筆或單筆簽核
        /// 
        /// **功能說明**:
        /// - 支援單筆或多筆表單簽核
        /// - 支援同意/不同意
        /// - 不同意時可選擇中止流程或退回發起人
        /// 
        /// **簽核狀態**:
        /// - Y: 同意（預設）
        /// - N: 不同意
        /// 
        /// **簽核流程** (當不同意時):
        /// - S: 中止流程
        /// - R: 退回發起人
        /// - 空白: 預設（繼續流程）
        /// 
        /// **請求範例 (不同意並退回)**:
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "00123",
        ///   "comments": "客戶來訪，請調休",
        ///   "approvalstatus": "N",
        ///   "approvalflow": "R",
        ///   "approvaldata": [
        ///     {
        ///       "eformtype": "L",
        ///       "eformid": "PI-HR-H1A-PKG-Test0000000000053"
        ///     },
        ///     {
        ///       "eformtype": "O",
        ///       "eformid": "PI-HR-H1A-PKG-Test0000000000123"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **回應範例 (成功)**:
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": {
        ///     "status": "請求成功"
        ///   }
        /// }
        /// ```
        /// 
        /// **注意事項**:
        /// - 當簽核狀態為不同意時，簽核流程才會顯示
        /// - 讓使用者確認為中止流程或退回發起人
        /// </remarks>
        /// <param name="request">簽核作業請求</param>
        /// <returns>簽核結果</returns>
        [HttpPost("eformapproval")]
        [ProducesResponseType(typeof(ReviewApprovalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ReviewApprovalResponse), StatusCodes.Status203NonAuthoritative)]
        public async Task<ActionResult<ReviewApprovalResponse>> ApproveReview([FromBody] ReviewApprovalRequest request)
        {
            try
            {
                _logger.LogInformation("待我審核簽核作業 API 被呼叫，UID: {Uid}, 簽核數量: {Count}", 
                    request.Uid, request.ApprovalData.Count);

                var response = await _reviewService.ApproveReviewAsync(request);

                if (response.Code == "200")
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(StatusCodes.Status203NonAuthoritative, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "待我審核簽核作業 API 發生錯誤");
                return StatusCode(StatusCodes.Status203NonAuthoritative, new ReviewApprovalResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                });
            }
        }
    }
}
