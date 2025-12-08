using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public interface IEFormReviewService
    {
        Task<EFormReviewResponse> GetReviewFormsAsync(EFormReviewRequest request);
    }
}
