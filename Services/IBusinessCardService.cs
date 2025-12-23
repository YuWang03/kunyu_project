using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子名片服務介面
    /// 處理 POST /app/businesscard 的業務邏輯
    /// </summary>
    public interface IBusinessCardService
    {
        /// <summary>
        /// 取得員工電子名片資料
        /// </summary>
        /// <param name="request">電子名片請求</param>
        /// <returns>電子名片回應</returns>
        Task<BusinessCardResponse> GetBusinessCardAsync(BusinessCardRequest request);

        /// <summary>
        /// 從 vwZZ_EMPLOYEE 視圖取得員工完整資料
        /// </summary>
        /// <param name="uid">員工工號</param>
        /// <param name="cid">公司代碼</param>
        /// <returns>員工視圖資料</returns>
        Task<EmployeeViewData?> GetEmployeeViewDataAsync(string uid, string? cid);

        /// <summary>
        /// 根據公司代碼取得公司設定 (電話、網站、地址)
        /// </summary>
        /// <param name="companyCode">公司代碼</param>
        /// <returns>公司設定</returns>
        BusinessCardCompanySettings GetCompanySettings(string? companyCode);

        /// <summary>
        /// 產生個人 QR Code URL
        /// </summary>
        /// <param name="companyCode">公司代碼</param>
        /// <param name="employeeNo">員工工號</param>
        /// <returns>QR Code URL</returns>
        string GenerateQrCodeUrl(string? companyCode, string? employeeNo);
    }
}
