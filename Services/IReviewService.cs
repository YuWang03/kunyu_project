using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 待我審核服務介面
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// 取得待我審核列表
        /// </summary>
        Task<ReviewListResponse> GetReviewListAsync(ReviewListRequest request);

        /// <summary>
        /// 取得待我審核詳細資料
        /// </summary>
        Task<ReviewDetailResponse> GetReviewDetailAsync(ReviewDetailRequest request);

        /// <summary>
        /// 執行簽核作業
        /// </summary>
        Task<ReviewApprovalResponse> ApproveReviewAsync(ReviewApprovalRequest request);
    }
}
