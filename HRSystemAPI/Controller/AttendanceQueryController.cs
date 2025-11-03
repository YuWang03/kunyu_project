using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 考勤查詢 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("考勤查詢")]
    public class AttendanceQueryController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceQueryController> _logger;

        public AttendanceQueryController(
            IAttendanceService attendanceService,
            ILogger<AttendanceQueryController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        /// <summary>
        /// 查詢個人出勤記錄
        /// </summary>
        /// <param name="employeeNo">員工編號</param>
        /// <param name="date">日期 (格式: yyyy-MM-dd，例如: 2025-10-28)</param>
        /// <returns>當日出勤記錄</returns>
        /// <remarks>
        /// 查詢指定員工在指定日期的出勤記錄，包含：
        /// - 上班刷卡時間與狀態
        /// - 下班刷卡時間與狀態
        /// 
        /// 狀態說明：
        /// - 正常：按時打卡
        /// - 應刷未刷：未打卡（異常代碼 0）
        /// - 遲到：上班打卡時間晚於應刷卡時間（異常代碼 1）
        /// - 早退：下班打卡時間早於應刷卡時間（異常代碼 2）
        /// - 超時出勤：打卡時間超過標準時間（異常代碼 3）
        /// - 曠職：未打卡且無請假（異常代碼 4）
        /// 
        /// 範例請求：
        ///     GET /api/AttendanceQuery?employeeNo=3536&amp;date=2025-10-28
        /// 
        /// 範例回應（正常打卡）：
        /// 
        ///     {
        ///       "date": "2025/10/28",
        ///       "clockInTime": "2025/10/28 08:00:00",
        ///       "clockInStatus": "正常",
        ///       "clockOutTime": "2025/10/28 17:30:00",
        ///       "clockOutStatus": "正常",
        ///       "clockInCode": "",
        ///       "clockOutCode": ""
        ///     }
        /// 
        /// 範例回應（應刷未刷）：
        /// 
        ///     {
        ///       "date": "2025/10/28",
        ///       "clockInTime": "應刷未刷",
        ///       "clockInStatus": "應刷未刷",
        ///       "clockOutTime": "應刷未刷",
        ///       "clockOutStatus": "應刷未刷",
        ///       "clockInCode": "0",
        ///       "clockOutCode": "0"
        ///     }
        /// 
        /// 範例回應（遲到 + 超時出勤）：
        /// 
        ///     {
        ///       "date": "2025/10/31",
        ///       "clockInTime": "2025/10/31 08:30:00",
        ///       "clockInStatus": "遲到",
        ///       "clockOutTime": "2025/10/31 21:00:00",
        ///       "clockOutStatus": "超時出勤",
        ///       "clockInCode": "1",
        ///       "clockOutCode": "3"
        ///     }
        /// </remarks>
        /// <response code="200">查詢成功，返回出勤記錄</response>
        /// <response code="400">請求參數錯誤</response>
        /// <response code="404">查無出勤記錄</response>
        /// <response code="500">伺服器內部錯誤</response>
        [HttpGet]
        [ProducesResponseType(typeof(AttendanceRecord), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAttendanceRecord(
            [FromQuery] string employeeNo,
            [FromQuery] string date)
        {
            // 參數驗證
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                return BadRequest(new { message = "員工編號不可為空" });
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest(new { message = "日期不可為空" });
            }

            // 驗證日期格式
            if (!DateTime.TryParse(date, out _))
            {
                return BadRequest(new { message = "日期格式不正確，請使用 yyyy-MM-dd 格式（例如: 2025-10-28）" });
            }

            try
            {
                var record = await _attendanceService.GetAttendanceRecordAsync(employeeNo, date);

                if (record == null)
                {
                    return NotFound(new
                    {
                        message = $"查無出勤記錄",
                        employeeNo = employeeNo,
                        date = date
                    });
                }

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢出勤記錄失敗: 員工編號={EmployeeNo}, 日期={Date}", employeeNo, date);
                return StatusCode(500, new { message = "查詢出勤記錄時發生錯誤" });
            }
        }

        /// <summary>
        /// 查詢所有員工的出勤記錄（依日期）
        /// </summary>
        /// <param name="date">日期 (格式: yyyy-MM-dd，例如: 2025-10-28)</param>
        /// <returns>所有員工的出勤記錄清單</returns>
        /// <remarks>
        /// 查詢指定日期所有員工的出勤記錄。
        /// 
        /// 範例請求：
        ///     GET /api/AttendanceQuery/all?date=2025-10-28
        /// 
        /// 範例回應：
        /// 
        ///     [
        ///       {
        ///         "date": "2025/10/28",
        ///         "clockInTime": "2025/10/28 08:00:00",
        ///         "clockInStatus": "正常",
        ///         "clockOutTime": "2025/10/28 17:30:00",
        ///         "clockOutStatus": "正常",
        ///         "clockInCode": "",
        ///         "clockOutCode": ""
        ///       },
        ///       {
        ///         "date": "2025/10/28",
        ///         "clockInTime": "應刷未刷",
        ///         "clockInStatus": "曠職",
        ///         "clockOutTime": "應刷未刷",
        ///         "clockOutStatus": "曠職",
        ///         "clockInCode": "4",
        ///         "clockOutCode": "4"
        ///       }
        ///     ]
        /// </remarks>
        /// <response code="200">查詢成功，返回出勤記錄清單</response>
        /// <response code="400">請求參數錯誤</response>
        /// <response code="500">伺服器內部錯誤</response>
        [HttpGet("all")]
        [ProducesResponseType(typeof(List<AttendanceRecord>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAttendanceRecords([FromQuery] string date)
        {
            // 參數驗證
            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest(new { message = "日期不可為空" });
            }

            // 驗證日期格式
            if (!DateTime.TryParse(date, out _))
            {
                return BadRequest(new { message = "日期格式不正確，請使用 yyyy-MM-dd 格式（例如: 2025-10-28）" });
            }

            try
            {
                var records = await _attendanceService.GetAllAttendanceRecordsAsync(date);

                return Ok(new
                {
                    date = date,
                    totalRecords = records.Count,
                    records = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢所有員工出勤記錄失敗: 日期={Date}", date);
                return StatusCode(500, new { message = "查詢出勤記錄時發生錯誤" });
            }
        }
    }
}