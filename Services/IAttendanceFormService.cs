using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public interface IAttendanceFormService
    {
        Task<AttendanceFormOperationResult> CreateAttendanceFormAsync(CreateAttendanceFormRequest request);
        
        Task<PagedResponse<AttendanceFormSummary>> GetFormsAsync(GetFormsQuery query);
        
        Task<ApiResponse<AttendanceFormDetailResponse>> GetFormByIdAsync(string formId);
        
        Task<AttendanceFormOperationResult> CancelFormAsync(string formId, CancelFormRequest request);
        
        Task<AttendanceFormOperationResult> ReturnWorkItemAsync(string workItemId, ReturnWorkItemRequest request);
    }
}

