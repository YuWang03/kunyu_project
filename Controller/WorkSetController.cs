using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 考勤超時出勤設定 API
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class WorkSetController : ControllerBase
    {
        private readonly IWorkSetService _workSetService;
        private readonly ILogger<WorkSetController> _logger;

        public WorkSetController(
            IWorkSetService workSetService,
            ILogger<WorkSetController> logger)
        {
            _workSetService = workSetService;
            _logger = logger;
        }

        /// <summary>
        /// 新增或修改考勤超時出勤設定
        /// </summary>
        /// <remarks>
        /// 用於設定員工考勤超時出勤的事由說明
        /// 
        /// **請求範例:**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "wdate": "2025-09-02",
        ///   "reason": "一時沒注意時間，工作太認真"
        /// }
        /// ```
        /// 
        /// **成功回應範例:**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// **失敗回應範例:**
        /// ```json
        /// {
        ///   "code": "500",
        ///   "msg": "請求超時"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">考勤超時出勤設定請求資料</param>
        /// <returns>操作結果</returns>
        [HttpPost("workset")]
        [ProducesResponseType(typeof(WorkSetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WorkSetResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(WorkSetResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WorkSetResponse>> CreateOrUpdateWorkSet([FromBody] WorkSetRequest request)
        {
            try
            {
                _logger.LogInformation("接收到考勤超時出勤設定請求 - 工號: {Uid}, 日期: {Wdate}", 
                    request.Uid, request.Wdate);

                // 驗證模型
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning("請求驗證失敗: {Errors}", errors);
                    
                    return BadRequest(new WorkSetResponse
                    {
                        Code = "400",
                        Msg = $"請求參數錯誤: {errors}"
                    });
                }

                // 調用服務處理
                var response = await _workSetService.CreateOrUpdateWorkSetAsync(request);

                // 根據回應代碼返回適當的 HTTP 狀態
                if (response.Code == "200")
                {
                    return Ok(response);
                }
                else if (response.Code == "400")
                {
                    return BadRequest(response);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理考勤超時出勤設定請求時發生錯誤 - 工號: {Uid}", request.Uid);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new WorkSetResponse
                {
                    Code = "500",
                    Msg = "系統內部錯誤"
                });
            }
        }
    }
}
