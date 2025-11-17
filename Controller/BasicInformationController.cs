using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 2. 基本資料 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("2. 基本資料")]
    public class BasicInformationController : ControllerBase
    {
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<BasicInformationController> _logger;

        public BasicInformationController(
            IBasicInfoService basicInfoService,
            ILogger<BasicInformationController> logger)
        {
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        /// <summary>
        /// 取得員工基本資料
        /// </summary>
        /// <returns>員工基本資料列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<EmployeeBasicInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBasicInformation()
        {
            try
            {
                var result = await _basicInfoService.GetAllBasicInfoAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得員工基本資料失敗");
                return StatusCode(500, new { message = "取得員工資料時發生錯誤" });
            }
        }
    }
}
