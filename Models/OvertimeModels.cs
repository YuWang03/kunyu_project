using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 加班單記錄
    /// </summary>
    public class OvertimeRecord
    {
        /// <summary>
        /// 加班單ID（BPM FormID）
        /// </summary>
        public string FormId { get; set; } = string.Empty;

        /// <summary>
        /// 員工編號
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 員工姓名
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string StartTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string EndTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 表單填寫日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string FillFormDate { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式：轉補休=0 / 加班費=1
        /// </summary>
        public int ProcessType { get; set; }

        /// <summary>
        /// 附件檔案路徑（多個附件用 || 連接）
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 簽核狀態（待審核/已核准/已拒絕/已取消）
        /// </summary>
        public string ApprovalStatus { get; set; } = string.Empty;

        /// <summary>
        /// 簽核人員
        /// </summary>
        public string? ApproverName { get; set; }

        /// <summary>
        /// 簽核時間
        /// </summary>
        public string? ApprovalDateTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 附件檔案列表（由 FilePath 解析而來）
        /// </summary>
        public List<string>? Attachments { get; set; }
    }

    /// <summary>
    /// 申請加班單請求（舊版）
    /// </summary>
    public class CreateOvertimeRequest
    {
        /// <summary>
        /// 員工編號
        /// </summary>
        [Required(ErrorMessage = "員工編號為必填")]
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 員工 Email（用於取得 BPM UserID）
        /// </summary>
        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string EmployeeEmail { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期 (格式: yyyy/MM/dd)
        /// </summary>
        [Required(ErrorMessage = "申請日期為必填")]
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "預計加班起始時間為必填")]
        public string StartTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "預計加班結束時間為必填")]
        public string EndTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 表單填寫日期 (格式: yyyy/MM/dd)，預設當日
        /// </summary>
        public string? FillFormDate { get; set; }

        /// <summary>
        /// 實際加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// 必填，預申請時無值（隨便帶一個時間）
        /// </summary>
        [Required(ErrorMessage = "實際加班起始時間為必填")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// 必填，預申請時無值（隨便帶一個時間）
        /// </summary>
        [Required(ErrorMessage = "實際加班結束時間為必填")]
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        [Required(ErrorMessage = "事由為必填")]
        [MaxLength(500, ErrorMessage = "事由不能超過500字")]
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式：轉補休=0 / 加班費=1
        /// </summary>
        [Required(ErrorMessage = "處理方式為必填")]
        public int ProcessType { get; set; }

        /// <summary>
        /// 附件檔案路徑（多個附件用 || 連接）
        /// 範例：FTPTest~~/FTPShare/20250915.txt||FTPTest~~/FTPShare/A01.txt
        /// FTP目錄在 d:/FTPShare
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 附件檔案上傳（選填，用於上傳至FTP）
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// 申請加班表單請求（新版 - 使用 PI_OVERTIME_001）
    /// </summary>
    public class CreateOvertimeFormRequest
    {
        /// <summary>
        /// 員工 Email（用於查詢員工資料和取得 BPM UserID）
        /// </summary>
        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期 (格式: yyyy-MM-dd 或 yyyy/MM/dd)
        /// </summary>
        [Required(ErrorMessage = "申請日期為必填")]
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班起始時間 (格式: yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "預計加班起始時間為必填")]
        public string StartTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "預計加班結束時間為必填")]
        public string EndTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班起始時間 (格式: yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際加班起始時間為必填")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際加班結束時間為必填")]
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 事由（工作內容）
        /// </summary>
        [Required(ErrorMessage = "事由為必填")]
        [MaxLength(500, ErrorMessage = "事由不能超過500字")]
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式：轉補休=0 / 加班費=1
        /// </summary>
        [Required(ErrorMessage = "處理方式為必填")]
        public string ProcessType { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案上傳（選填，用於上傳至FTP）
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// 更新實際加班時間請求
    /// </summary>
    public class UpdateActualOvertimeRequest
    {
        /// <summary>
        /// 實際加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際起始時間為必填")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際結束時間為必填")]
        public string EndTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 加班單查詢條件
    /// </summary>
    public class OvertimeQueryRequest
    {
        /// <summary>
        /// 員工編號（選填）
        /// </summary>
        public string? EmployeeNo { get; set; }

        /// <summary>
        /// 開始日期 (格式: yyyy-MM-dd)
        /// </summary>
        public string? StartDate { get; set; }

        /// <summary>
        /// 結束日期 (格式: yyyy-MM-dd)
        /// </summary>
        public string? EndDate { get; set; }

        /// <summary>
        /// 簽核狀態（選填）
        /// </summary>
        public string? ApprovalStatus { get; set; }
    }

    /// <summary>
    /// 加班單操作結果
    /// </summary>
    public class OvertimeOperationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 加班單ID（BPM FormID）
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 上傳的附件路徑列表
        /// </summary>
        public List<string>? AttachmentPaths { get; set; }
    }

    /// <summary>
    /// 加班表單操作結果（新版）
    /// </summary>
    public class OvertimeFormOperationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 加班表單ID（BPM ProcessOid）
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 加班表單編號（BPM ProcessSerialNo）
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 錯誤代碼
        /// </summary>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// BPM 加班單資料格式
    /// </summary>
    public class BpmOvertimeFormData
    {
        /// <summary>
        /// BPM 使用者 ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 員工編號
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 員工姓名
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string ApplyDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string StartTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string EndTimeF { get; set; } = string.Empty;

        /// <summary>
        /// 表單填寫日期 (格式: yyyy/MM/dd)，預設當日
        /// </summary>
        public string FillFormDate { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班起始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式：轉補休=0 / 加班費=1
        /// </summary>
        public int ProcessType { get; set; }

        /// <summary>
        /// 附件檔案路徑（多個附件用 || 連接）
        /// 範例：FTPTest~~/FTPShare/20250915.txt||FTPTest~~/FTPShare/A01.txt
        /// FTP目錄在 d:/FTPShare
        /// </summary>
        public string? FilePath { get; set; }
    }
}