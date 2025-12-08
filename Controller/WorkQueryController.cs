using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 個人考勤查詢 API
    /// </summary>
    [ApiController]
    [Route("app/[controller]")]
    [Tags("個人考勤查詢")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class WorkQueryController : ControllerBase
    {
        private readonly IWorkQueryService _workQueryService;
        private readonly ILogger<WorkQueryController> _logger;

        public WorkQueryController(
            IWorkQueryService workQueryService,
            ILogger<WorkQueryController> logger)
        {
            _workQueryService = workQueryService;
            _logger = logger;
        }

        /// <summary>
        /// 個人考勤查詢列表
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns>考勤查詢結果</returns>
        /// <remarks>
        /// 查詢指定員工在指定月份的考勤記錄
        /// 
        /// 範例請求：
        /// 
        ///     POST /app/workquery
        ///     {
        ///       "tokenid": "53422421",
        ///       "cid": "45624657",
        ///       "uid": "0325",
        ///       "wyearmonth": "2025-09"
        ///     }
        /// 
        /// 範例回應（成功）：
        /// 
        ///     {
        ///       "code": "200",
        ///       "msg": "查詢成功",
        ///       "data": {
        ///         "ryear": "2025",
        ///         "rmonth": "09",
        ///         "records": [
        ///           {
        ///             "date": "2025-09-01",
        ///             "onduty": "T",
        ///             "clockin": "08:00:00",
        ///             "checkin": "07:59:38",
        ///             "statusin": "T",
        ///             "clockout": "18:00:00",
        ///             "checkout": "18:05:06",
        ///             "statusout": "T"
        ///           },
        ///           {
        ///             "date": "2025-09-02",
        ///             "onduty": "F",
        ///             "clockin": "08:00:00",
        ///             "checkin": "07:55:38",
        ///             "statusin": "T",
        ///             "clockout": "18:00:00",
        ///             "checkout": "19:55:00",
        ///             "statusout": "F"
        ///           },
        ///           {
        ///             "date": "2025-09-03",
        ///             "onduty": "T",
        ///             "clockin": "08:00:00",
        ///             "checkin": "07:55:38",
        ///             "statusin": "T",
        ///             "clockout": "18:00:00",
        ///             "checkout": "",
        ///             "statusout": ""
        ///           }
        ///         ]
        ///       }
        ///     }
        /// 
        /// 範例回應（失敗）：
        /// 
        ///     {
        ///       "code": "500",
        ///       "msg": "請求超時"
        ///     }
        /// </remarks>
        /// <response code="200">查詢成功</response>
        /// <response code="400">請求參數錯誤</response>
        /// <response code="500">伺服器內部錯誤</response>
        [HttpPost]
        [ProducesResponseType(typeof(WorkQueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueryWorkRecords([FromBody] WorkQueryRequest request)
        {
            try
            {
                // 參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return Ok(new WorkQueryResponse
                    {
                        Code = "400",
                        Msg = string.Join("; ", errors)
                    });
                }

                _logger.LogInformation("查詢考勤記錄: TokenId={TokenId}, Cid={Cid}, Uid={Uid}, YearMonth={YearMonth}", 
                    request.TokenId, request.Cid, request.Uid, request.WYearMonth);

                // 查詢考勤記錄
                var data = await _workQueryService.GetMonthlyWorkRecordsAsync(request.Uid, request.WYearMonth);

                if (data == null)
                {
                    return Ok(new WorkQueryResponse
                    {
                        Code = "500",
                        Msg = "查無資料"
                    });
                }

                return Ok(new WorkQueryResponse
                {
                    Code = "200",
                    Msg = "查詢成功",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢考勤記錄時發生錯誤: Uid={Uid}, YearMonth={YearMonth}", 
                    request?.Uid, request?.WYearMonth);
                
                return Ok(new WorkQueryResponse
                {
                    Code = "500",
                    Msg = "請求超時"
                });
            }
        }
    }
}
