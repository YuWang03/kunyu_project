using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 出勤確認單（補打卡單）申請請求 - 依照 BPM field-mappings（Attendance_Exception_001）
    /// </summary>
    public class CreateAttendanceFormRequest
    {
        /// <summary>
        /// 申請人 Email（必填）- 用於查詢員工資料
        /// </summary>
        [Required(ErrorMessage = "申請人 Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        // ===== BPM 必填欄位 (required: true) =====
        
        /// <summary>
        /// 申請日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "申請日期為必填")]
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 異常說明（必填）- exceptionDescription
        /// </summary>
        [Required(ErrorMessage = "異常說明為必填")]
        public string ExceptionDescription { get; set; } = string.Empty;

        // ===== BPM 選填欄位 (required: false) =====
        
        /// <summary>
        /// 廠區（選填）- plant，預設：PI
        /// </summary>
        public string? Plant { get; set; }

        /// <summary>
        /// 用戶ID（選填）- userId（系統自動填入）
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 檔案路徑（選填）- filePath
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 表單類型（選填）- formType，預設：H1A
        /// </summary>
        public string? FormType { get; set; }

        /// <summary>
        /// 公司編號（選填）- companyNo，預設：03546618
        /// </summary>
        public string? CompanyNo { get; set; }

        /// <summary>
        /// 員工編號（選填）- employeeId（系統自動填入）
        /// </summary>
        public string? EmployeeId { get; set; }

        /// <summary>
        /// 表單編號（選填）- formNumber
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 臨時下拉資料（選填）- tmpDrpData
        /// </summary>
        public string? TmpDrpData { get; set; }

        /// <summary>
        /// 審核人ID1（選填）- approverId1（系統自動帶入）
        /// </summary>
        public string? ApproverId1 { get; set; }

        /// <summary>
        /// 審核人ID2（選填）- approverId2（系統自動帶入）
        /// </summary>
        public string? ApproverId2 { get; set; }

        /// <summary>
        /// 申請人ID（選填）- requesterId（系統自動填入）
        /// </summary>
        public string? RequesterId { get; set; }

        /// <summary>
        /// 部門ID（選填）- departmentId（系統自動填入）
        /// </summary>
        public string? DepartmentId { get; set; }

        /// <summary>
        /// 補卡起始時間（必填）- exceptionTime
        /// </summary>
        [Required(ErrorMessage = "補卡起始時間為必填")]
        public string ExceptionTime { get; set; } = string.Empty;

        /// <summary>
        /// 申請人姓名（選填）- requesterName（系統自動帶入）
        /// </summary>
        public string? RequesterName { get; set; }

        /// <summary>
        /// 部門名稱（選填）- departmentName（系統自動帶入）
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// 異常原因（選填）- exceptionReason，預設：其他
        /// </summary>
        public string? ExceptionReason { get; set; }

        /// <summary>
        /// 補卡結束時間（選填）- exceptionEndTime（跟補卡開始時間可以擇一填）
        /// </summary>
        public string? ExceptionEndTime { get; set; }

        /// <summary>
        /// 上傳的附件檔案（選填）- Word、Excel、PDF、圖片
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 出勤異常單操作結果
    /// </summary>
    public class AttendanceFormOperationResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 表單 ID
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 表單編號
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 錯誤代碼（失敗時）
        /// </summary>
        public string? ErrorCode { get; set; }
    }

    #endregion

    #region Legacy Models (保留舊版本相容性)

    /// <summary>
    /// 申請出勤確認單請求（舊版本）- 已廢棄
    /// </summary>
    [Obsolete("請使用 CreateAttendanceFormRequest")]
    public class AttendanceFormRequest
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

    /// <summary>
    /// 查詢待簽核出勤確認單請求
    /// </summary>
    public class PendingAttendanceFormQuery
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
    public class ApproveAttendanceFormRequest
    {
        public string FormId { get; set; } = string.Empty;            // 表單ID
        public string ApproverEmail { get; set; } = string.Empty;     // 簽核人Email
        public string Action { get; set; } = string.Empty;            // approve/reject/return
        public string? Comment { get; set; }                          // 簽核意見
    }

    // 批次簽核請求
    public class BatchApproveAttendanceFormRequest
    {
        public List<string> FormIds { get; set; } = new();            // 表單ID列表
        public string ApproverEmail { get; set; } = string.Empty;     // 簽核人Email
        public string Action { get; set; } = string.Empty;            // approve/reject
        public string? Comment { get; set; }                          // 簽核意見
    }

    // 出勤確認單列表項目
    public class AttendanceFormListItem
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

    // 出勤確認單詳細內容
    public class AttendanceFormDetail
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

    // API 回應包裝
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    /// <summary>
    /// 分頁回應
    /// </summary>
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

    #endregion

    #region New Models - 2025/11/17

    /// <summary>
    /// 查詢表單請求參數
    /// </summary>
    public class GetFormsQuery
    {
        /// <summary>
        /// 申請人 Email（選填）
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 表單狀態（選填）- 例如: SUCCESS, PENDING, REJECTED
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 開始日期（選填）- 格式：yyyy-MM-dd
        /// </summary>
        public string? StartDate { get; set; }

        /// <summary>
        /// 結束日期（選填）- 格式：yyyy-MM-dd
        /// </summary>
        public string? EndDate { get; set; }

        /// <summary>
        /// 頁碼（預設：1）
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每頁筆數（預設：20）
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 表單摘要資訊（列表用）
    /// </summary>
    public class AttendanceFormSummary
    {
        /// <summary>
        /// 表單 ID (BPM ProcessOid)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string FormNumber { get; set; } = string.Empty;

        /// <summary>
        /// 未刷卡日期 (applyDate)
        /// </summary>
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 上班時間 (exceptionTime)
        /// </summary>
        public string? ExceptionTime { get; set; }

        /// <summary>
        /// 下班時間 (exceptionEndTime)
        /// </summary>
        public string? ExceptionEndTime { get; set; }

        /// <summary>
        /// 原因 (exceptionReason)
        /// </summary>
        public string? ExceptionReason { get; set; }

        /// <summary>
        /// 事由 (exceptionDescription)
        /// </summary>
        public string ExceptionDescription { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間 (createdAt)
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態 (status)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 申請人姓名
        /// </summary>
        public string? RequesterName { get; set; }

        /// <summary>
        /// 申請人工號
        /// </summary>
        public string? RequesterId { get; set; }

        /// <summary>
        /// 部門名稱
        /// </summary>
        public string? DepartmentName { get; set; }
    }

    /// <summary>
    /// 表單詳細資訊（單一表單查詢）
    /// </summary>
    public class AttendanceFormDetailResponse
    {
        /// <summary>
        /// 表單 ID (BPM ProcessOid)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string FormNumber { get; set; } = string.Empty;

        /// <summary>
        /// 未刷卡日期 (applyDate)
        /// </summary>
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 上班時間 (exceptionTime)
        /// </summary>
        public string? ExceptionTime { get; set; }

        /// <summary>
        /// 下班時間 (exceptionEndTime)
        /// </summary>
        public string? ExceptionEndTime { get; set; }

        /// <summary>
        /// 原因 (exceptionReason)
        /// </summary>
        public string? ExceptionReason { get; set; }

        /// <summary>
        /// 事由 (exceptionDescription)
        /// </summary>
        public string ExceptionDescription { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間 (createdAt)
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態 (status)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 申請人姓名
        /// </summary>
        public string? RequesterName { get; set; }

        /// <summary>
        /// 申請人工號
        /// </summary>
        public string? RequesterId { get; set; }

        /// <summary>
        /// 部門名稱
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// 簽核人員1 (approverId1)
        /// </summary>
        public string? ApproverId1 { get; set; }

        /// <summary>
        /// 簽核人員2 (approverId2)
        /// </summary>
        public string? ApproverId2 { get; set; }

        /// <summary>
        /// 附件路徑 (filePath)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 表單類型 (formType)
        /// </summary>
        public string? FormType { get; set; }

        /// <summary>
        /// 廠區 (plant)
        /// </summary>
        public string? Plant { get; set; }

        /// <summary>
        /// 公司編號 (companyNo)
        /// </summary>
        public string? CompanyNo { get; set; }

        /// <summary>
        /// 完整表單資料 (原始 JSON)
        /// </summary>
        public object? FormData { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 取消表單請求
    /// </summary>
    public class CancelFormRequest
    {
        /// <summary>
        /// 取消原因（必填）
        /// </summary>
        [Required(ErrorMessage = "取消原因為必填")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 申請人 Email（必填）- 用於驗證權限
        /// </summary>
        [Required(ErrorMessage = "申請人 Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 退回工作項目請求
    /// </summary>
    public class ReturnWorkItemRequest
    {
        /// <summary>
        /// 退回原因（必填）
        /// </summary>
        [Required(ErrorMessage = "退回原因為必填")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 審核人 Email（必填）- 用於驗證權限
        /// </summary>
        [Required(ErrorMessage = "審核人 Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}