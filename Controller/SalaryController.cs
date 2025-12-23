using Microsoft.AspNetCore.Mvc;
using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;

namespace HRSystemAPI.Controllers
{
    /// <summary>
    /// 薪資查詢 API
    /// </summary>
    [ApiController]
    [Route("app")]
    [Produces("application/json")]
    [ServiceFilter(typeof(TokenValidationFilter))]
    public class SalaryController : ControllerBase
    {
        private readonly ISalaryService _salaryService;
        private readonly ILogger<SalaryController> _logger;

        public SalaryController(
            ISalaryService salaryService,
            ILogger<SalaryController> logger)
        {
            _salaryService = salaryService;
            _logger = logger;
        }

        /// <summary>
        /// 薪資查詢驗證碼寄送
        /// </summary>
        /// <remarks>
        /// 提交4位數字驗證碼至指定測試信箱（ian888.chen@gmail.com）
        /// 
        /// **功能說明：**
        /// - 系統會產生一組 4 位數字驗證碼
        /// - 驗證碼將發送至測試信箱 ian888.chen@gmail.com
        /// - 驗證碼具有時效性，5 分鐘後失效
        /// 
        /// **Input JSON 範例：**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（失敗）：**
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">驗證碼寄送請求</param>
        /// <returns>寄送結果</returns>
        [HttpPost("sendcode")]
        [ProducesResponseType(typeof(SendCodeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SendCodeResponse>> SendCode([FromBody] SendCodeRequest request)
        {
            try
            {
                _logger.LogInformation("薪資查詢驗證碼寄送 API 被呼叫");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗");
                    return BadRequest(new SendCodeResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，參數驗證不通過"
                    });
                }

                var response = await _salaryService.SendVerificationCodeAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation($"驗證碼寄送成功 - 使用者：{request.Uid}");
                }
                else
                {
                    _logger.LogWarning($"驗證碼寄送失敗 - 使用者：{request.Uid}, 原因：{response.Msg}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "薪資查詢驗證碼寄送 API 發生例外");
                return Ok(new SendCodeResponse
                {
                    Code = "203",
                    Msg = "請求失敗，系統發生錯誤"
                });
            }
        }

        /// <summary>
        /// 薪資查詢驗證碼驗證
        /// </summary>
        /// <remarks>
        /// 提交4位數字驗證前次發送驗證碼至使用者個人信箱
        /// 
        /// **功能說明：**
        /// - 驗證使用者輸入的 4 位數驗證碼是否正確
        /// - 驗證碼具有時效性，15 分鐘後失效
        /// - 驗證碼僅能使用一次
        /// 
        /// **Input JSON 範例：**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "verificationcode": "1234"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（失敗）：**
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">驗證碼驗證請求</param>
        /// <returns>驗證結果</returns>
        [HttpPost("sendcodecheck")]
        [ProducesResponseType(typeof(SendCodeCheckResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SendCodeCheckResponse>> SendCodeCheck([FromBody] SendCodeCheckRequest request)
        {
            try
            {
                _logger.LogInformation("薪資查詢驗證碼驗證 API 被呼叫");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗");
                    return BadRequest(new SendCodeCheckResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，參數驗證不通過"
                    });
                }

                var response = await _salaryService.VerifyCodeAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation($"驗證碼驗證成功 - 使用者：{request.Uid}");
                }
                else
                {
                    _logger.LogWarning($"驗證碼驗證失敗 - 使用者：{request.Uid}, 原因：{response.Msg}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "薪資查詢驗證碼驗證 API 發生例外");
                return Ok(new SendCodeCheckResponse
                {
                    Code = "203",
                    Msg = "請求失敗，系統發生錯誤"
                });
            }
        }

        /// <summary>
        /// 薪資查詢明細
        /// </summary>
        /// <remarks>
        /// 將個人薪資總表及明細呈現
        /// 
        /// **功能說明：**
        /// - 查詢指定年月的薪資明細
        /// - 包含薪資結構、加項、減項、出勤記錄等完整資訊
        /// - 提供應稅工資及實際發放金額
        /// 
        /// **Input JSON 範例：**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "querydate": "2025-09"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "請求成功",
        ///   "data": {
        ///     "uid": "0325",
        ///     "uname": "王大明",
        ///     "attendance": "2025年08月26日～2025年09月25日",
        ///     "salary": "2025年09月01日～2025年09月30日",
        ///     "structure": [
        ///       { "sitem": "基本薪資", "samount": "30,000" },
        ///       { "sitem": "職務加給", "samount": "5,000" }
        ///     ],
        ///     "structuretotal": "35,000",
        ///     "additional": [
        ///       { "aitem": "績效獎金", "aamount": "8,000" },
        ///       { "aitem": "交通津貼", "aamount": "2,000" }
        ///     ],
        ///     "aitemcount": "薪資加項合計",
        ///     "aamountcount": "10,000",
        ///     "reduction": [
        ///       { "ritem": "勞保扣款", "ramount": "1,200" },
        ///       { "ritem": "健保扣款", "ramount": "800" }
        ///     ],
        ///     "ritemcount": "薪資減項合計",
        ///     "ramountcount": "2,000",
        ///     "record": [
        ///       { "fake": "事假", "fakehours": "8" },
        ///       { "fake": "病假", "fakehours": "4" }
        ///     ],
        ///     "taxabletitle": "應稅工資",
        ///     "taxablepaid": "45,000",
        ///     "dtaxabletitle": "扣個人所得稅",
        ///     "dtaxablepaid": "3,000",
        ///     "actualtitle": "實際發放薪資",
        ///     "actualpaid": "42,000",
        ///     "notes1": "含獎金與津貼",
        ///     "notes2": "已扣除勞健保與所得稅"
        ///   }
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（失敗）：**
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">薪資查詢請求</param>
        /// <returns>薪資明細資料</returns>
        [HttpPost("salaryview")]
        [ProducesResponseType(typeof(SalaryViewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SalaryViewResponse>> SalaryView([FromBody] SalaryViewRequest request)
        {
            try
            {
                _logger.LogInformation("薪資查詢明細 API 被呼叫");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗");
                    return BadRequest(new SalaryViewResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，參數驗證不通過"
                    });
                }

                var response = await _salaryService.GetSalaryDetailsAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation($"薪資明細查詢成功 - 使用者：{request.Uid}, 查詢年月：{request.Querydate}");
                }
                else
                {
                    _logger.LogWarning($"薪資明細查詢失敗 - 使用者：{request.Uid}, 原因：{response.Msg}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "薪資查詢明細 API 發生例外");
                return Ok(new SalaryViewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，系統發生錯誤"
                });
            }
        }

        /// <summary>
        /// 教育訓練查詢明細
        /// </summary>
        /// <remarks>
        /// 依年度查詢教育訓練明細的結果
        /// 
        /// **功能說明：**
        /// - 查詢指定年份的教育訓練課程明細
        /// - 只能查詢近三年的資料
        /// - 包含課程類別、課程名稱、課程時數
        /// - 提供年度總時數統計
        /// 
        /// **Input JSON 範例：**
        /// ```json
        /// {
        ///   "tokenid": "53422421",
        ///   "cid": "45624657",
        ///   "uid": "0325",
        ///   "queryyear": "2025"
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "code": "200",
        ///   "msg": "成功",
        ///   "data": {
        ///     "edudata": [
        ///       {
        ///         "classtype": "專業",
        ///         "classname": "高階程式設計",
        ///         "classhours": "3"
        ///       },
        ///       {
        ///         "classtype": "一般",
        ///         "classname": "商用英文會話",
        ///         "classhours": "3"
        ///       },
        ///       {
        ///         "classtype": "專業",
        ///         "classname": "專案管理實務",
        ///         "classhours": "2"
        ///       }
        ///     ],
        ///     "yearhourstitle": "學分合計",
        ///     "yearhours": "8"
        ///   }
        /// }
        /// ```
        /// 
        /// **Output JSON 範例（失敗）：**
        /// ```json
        /// {
        ///   "code": "203",
        ///   "msg": "請求失敗，主要條件不符合"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">教育訓練查詢請求</param>
        /// <returns>教育訓練明細資料</returns>
        [HttpPost("eduview")]
        [ProducesResponseType(typeof(EduViewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EduViewResponse>> EduView([FromBody] EduViewRequest request)
        {
            try
            {
                _logger.LogInformation("教育訓練查詢明細 API 被呼叫");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("請求參數驗證失敗");
                    return BadRequest(new EduViewResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，參數驗證不通過"
                    });
                }

                var response = await _salaryService.GetEducationDetailsAsync(request);
                
                if (response.Code == "200")
                {
                    _logger.LogInformation($"教育訓練明細查詢成功 - 使用者：{request.Uid}, 查詢年份：{request.Queryyear}");
                }
                else
                {
                    _logger.LogWarning($"教育訓練明細查詢失敗 - 使用者：{request.Uid}, 原因：{response.Msg}");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "教育訓練查詢明細 API 發生例外");
                return Ok(new EduViewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，系統發生錯誤"
                });
            }
        }
    }
}
