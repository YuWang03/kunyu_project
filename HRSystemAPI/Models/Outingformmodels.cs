using Microsoft.AspNetCore.Http;

namespace HRSystemAPI.Models
{
    // 申請外出單請求
    public class OutingFormRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;              // 外出/外訓
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;          // 地點
        public string Reason { get; set; } = string.Empty;            // 事由
        public bool ReturnToOffice { get; set; }                      // 是否返回公司
        public List<IFormFile>? Attachments { get; set; }
    }

    // 查詢待簽核外出單請求
    public class PendingOutingFormQuery
    {
        public string? ApproverEmail { get; set; }                    // 簽核人Email
        public int? Year { get; set; }                                // 西元年
        public int? Month { get; set; }                               // 月
        public int? Day { get; set; }                                 // 日
        public DateTime? StartDate { get; set; }                      // 區間起始
        public DateTime? EndDate { get; set; }                        // 區間結束
        public string? EmployeeName { get; set; }                     // 員工姓名
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // 簽核請求
    public class ApproveOutingFormRequest
    {
        public string FormId { get; set; } = string.Empty;            // 表單ID
        public string ApproverEmail { get; set; } = string.Empty;     // 簽核人Email
        public string Action { get; set; } = string.Empty;            // approve/reject/return
        public string? Comment { get; set; }                          // 簽核意見
    }

    // 批次簽核請求
    public class BatchApproveOutingFormRequest
    {
        public List<string> FormIds { get; set; } = new();            // 表單ID列表
        public string ApproverEmail { get; set; } = string.Empty;     // 簽核人Email
        public string Action { get; set; } = string.Empty;            // approve/reject
        public string? Comment { get; set; }                          // 簽核意見
    }

    // 外出單列表項目
    public class OutingFormListItem
    {
        public string FormId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;      // 員工姓名
        public string EmployeeId { get; set; } = string.Empty;        // 員工編號
        public string Type { get; set; } = string.Empty;              // 外出/外訓
        public DateTime Date { get; set; }                            // 外出日期
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;            // 簽核狀態
        public DateTime CreatedAt { get; set; }                       // 申請時間
    }

    // 外出單詳細內容
    public class OutingFormDetail
    {
        public string FormId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;        // 部門
        public string Type { get; set; } = string.Empty;              // 外出/外訓
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool ReturnToOffice { get; set; }
        public string Status { get; set; } = string.Empty;            // 簽核狀態
        public DateTime CreatedAt { get; set; }                       // 申請日期時間
        public List<ApprovalHistory> ApprovalHistory { get; set; } = new(); // 簽核歷程
        public List<AttachmentInfo> Attachments { get; set; } = new(); // 附件
        public string? Note { get; set; }                             // 備註
    }

    // 簽核歷程
    public class ApprovalHistory
    {
        public string ApproverName { get; set; } = string.Empty;      // 簽核人員
        public string Action { get; set; } = string.Empty;            // 動作
        public string? Comment { get; set; }                          // 意見
        public DateTime ApprovedAt { get; set; }                      // 簽核時間
    }

    // 附件資訊
    public class AttachmentInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;          // Word/Excel/PDF/Image
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // API 回應包裝
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    // 分頁回應
    public class PagedResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}