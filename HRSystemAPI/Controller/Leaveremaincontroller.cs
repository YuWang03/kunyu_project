using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 請假剩餘天數-查詢各類假別剩餘天數
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("請假剩餘天數")]
    public class LeaveRemainController : ControllerBase
    {
        private readonly ILeaveRemainService _leaveRemainService;

        public LeaveRemainController(ILeaveRemainService leaveRemainService)
        {
            _leaveRemainService = leaveRemainService ?? throw new ArgumentNullException(nameof(leaveRemainService));
        }

        /// <summary>
        /// 查詢個人請假剩餘天數
        /// </summary>
        /// <param name="employeeNo">員工編號 (必填)</param>
        /// <param name="year">查詢年度 (選填，預設當年度，例如: 2025)</param>
        /// <returns>請假剩餘天數資訊</returns>
        /// <response code="200">查詢成功</response>
        /// <response code="400">參數錯誤</response>
        /// <response code="404">查無此員工或該年度無資料</response>
        /// <response code="500">伺服器錯誤</response>
        [HttpGet]
        [ProducesResponseType(typeof(LeaveRemainResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LeaveRemainResponse>> GetLeaveRemain(
            [FromQuery, Required(ErrorMessage = "員工編號為必填")] string employeeNo,
            [FromQuery] int? year = null)
        {
            try
            {
                // 驗證員工編號
                if (string.IsNullOrWhiteSpace(employeeNo))
                {
                    return BadRequest(new { message = "員工編號不可為空" });
                }

                // 如果未指定年度，使用當年度
                int queryYear = year ?? DateTime.Now.Year;

                // 驗證年度範圍（例如：只允許查詢當年及前一年）
                int currentYear = DateTime.Now.Year;
                if (queryYear < currentYear - 1 || queryYear > currentYear + 1)
                {
                    return BadRequest(new { message = $"年度範圍錯誤，只能查詢 {currentYear - 1} ~ {currentYear + 1} 年度" });
                }

                // 查詢請假剩餘天數
                var result = await _leaveRemainService.GetLeaveRemainAsync(employeeNo, queryYear);

                if (result == null)
                {
                    return NotFound(new { message = $"查無員工編號 {employeeNo} 在 {queryYear} 年度的資料" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "查詢失敗", error = ex.Message });
            }
        }

        /// <summary>
        /// 查詢個人請假剩餘天數（包含當年及前一年）
        /// </summary>
        /// <param name="employeeNo">員工編號 (必填)</param>
        /// <returns>當年及前一年的請假剩餘天數資訊</returns>
        /// <response code="200">查詢成功</response>
        /// <response code="400">參數錯誤</response>
        /// <response code="404">查無此員工</response>
        /// <response code="500">伺服器錯誤</response>
        [HttpGet("two-years")]
        [ProducesResponseType(typeof(List<LeaveRemainResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<LeaveRemainResponse>>> GetLeaveRemainTwoYears(
            [FromQuery, Required(ErrorMessage = "員工編號為必填")] string employeeNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeNo))
                {
                    return BadRequest(new { message = "員工編號不可為空" });
                }

                int currentYear = DateTime.Now.Year;
                var results = new List<LeaveRemainResponse>();

                // 查詢當年度
                var currentYearData = await _leaveRemainService.GetLeaveRemainAsync(employeeNo, currentYear);
                if (currentYearData != null)
                {
                    results.Add(currentYearData);
                }

                // 查詢前一年度
                var previousYearData = await _leaveRemainService.GetLeaveRemainAsync(employeeNo, currentYear - 1);
                if (previousYearData != null)
                {
                    results.Add(previousYearData);
                }

                if (results.Count == 0)
                {
                    return NotFound(new { message = $"查無員工編號 {employeeNo} 的資料" });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "查詢失敗", error = ex.Message });
            }
        }

        /// <summary>
        /// 查詢周年制起訖日期
        /// </summary>
        /// <param name="employeeNo">員工編號 (必填)</param>
        /// <param name="year">查詢年度 (選填，預設當年度)</param>
        /// <returns>周年制的起始日和結束日</returns>
        /// <response code="200">查詢成功</response>
        /// <response code="400">參數錯誤</response>
        /// <response code="404">查無此員工或無法計算周年制</response>
        /// <response code="500">伺服器錯誤</response>
        [HttpGet("anniversary-period")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetAnniversaryPeriod(
            [FromQuery, Required(ErrorMessage = "員工編號為必填")] string employeeNo,
            [FromQuery] int? year = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeNo))
                {
                    return BadRequest(new { message = "員工編號不可為空" });
                }

                int queryYear = year ?? DateTime.Now.Year;

                var period = await _leaveRemainService.GetAnniversaryPeriodAsync(employeeNo, queryYear);

                if (period == null)
                {
                    return NotFound(new { message = $"查無員工編號 {employeeNo} 的資料或無法計算周年制" });
                }

                var (startDate, endDate) = period.Value;

                return Ok(new
                {
                    employeeNo = employeeNo,
                    year = queryYear,
                    anniversaryStart = startDate.ToString("yyyy/MM/dd"),
                    anniversaryEnd = endDate.ToString("yyyy/MM/dd"),
                    description = $"周年制區間：{startDate:yyyy/MM/dd} ~ {endDate:yyyy/MM/dd}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "查詢失敗", error = ex.Message });
            }
        }
    }
}