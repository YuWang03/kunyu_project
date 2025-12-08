using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 薪資查詢服務介面
    /// </summary>
    public interface ISalaryService
    {
        /// <summary>
        /// 發送薪資查詢驗證碼至使用者信箱
        /// </summary>
        /// <param name="request">發送驗證碼請求</param>
        /// <returns>發送結果</returns>
        Task<SendCodeResponse> SendVerificationCodeAsync(SendCodeRequest request);

        /// <summary>
        /// 驗證薪資查詢驗證碼
        /// </summary>
        /// <param name="request">驗證碼驗證請求</param>
        /// <returns>驗證結果</returns>
        Task<SendCodeCheckResponse> VerifyCodeAsync(SendCodeCheckRequest request);

        /// <summary>
        /// 查詢薪資明細
        /// </summary>
        /// <param name="request">薪資查詢請求</param>
        /// <returns>薪資明細資料</returns>
        Task<SalaryViewResponse> GetSalaryDetailsAsync(SalaryViewRequest request);

        /// <summary>
        /// 查詢教育訓練明細
        /// </summary>
        /// <param name="request">教育訓練查詢請求</param>
        /// <returns>教育訓練明細資料</returns>
        Task<EduViewResponse> GetEducationDetailsAsync(EduViewRequest request);
    }
}
