using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 出差表單服務介面（BPM 整合 - PI_BUSINESS_TRIP_001）
    /// </summary>
    public interface IBusinessTripFormService
    {
        /// <summary>
        /// 申請出差表單（透過 BPM API + FTP 上傳附件）- 使用 PI_BUSINESS_TRIP_001
        /// </summary>
        Task<BusinessTripFormOperationResult> CreateBusinessTripFormAsync(CreateBusinessTripFormRequest request);
    }
}
