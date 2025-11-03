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
        /// 加班日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string OvertimeDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班開始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string PlannedStartTime { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string PlannedEndTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班開始時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string? ActualStartTime { get; set; }

        /// <summary>
        /// 實際加班結束時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string? ActualEndTime { get; set; }

        /// <summary>
        /// 事由（工作內容）
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式（補休/加班費）
        /// </summary>
        public string CompensationType { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間
        /// </summary>
        public string ApplyDateTime { get; set; } = string.Empty;

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
        /// 加班時數（自動計算）
        /// </summary>
        public decimal OvertimeHours { get; set; }

        /// <summary>
        /// 同意例休對調（週日加班必須同意）
        /// </summary>
        public bool AgreeToRestDaySwap { get; set; }

        /// <summary>
        /// 是否為週日加班
        /// </summary>
        public bool IsSundayOvertime { get; set; }

        /// <summary>
        /// 附件檔案列表
        /// </summary>
        public List<string>? Attachments { get; set; }
    }

    /// <summary>
    /// 申請加班單請求
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
        /// 加班日期 (格式: yyyy-MM-dd)
        /// </summary>
        [Required(ErrorMessage = "加班日期為必填")]
        public string OvertimeDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班開始時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "開始時間為必填")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 預計加班結束時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "結束時間為必填")]
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 事由（工作內容）
        /// </summary>
        [Required(ErrorMessage = "事由為必填")]
        [MaxLength(500, ErrorMessage = "事由不能超過500字")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式（補休/加班費）
        /// </summary>
        [Required(ErrorMessage = "處理方式為必填")]
        public string CompensationType { get; set; } = string.Empty;

        /// <summary>
        /// 同意例休對調（週日加班時必填）
        /// </summary>
        public bool? AgreeToRestDaySwap { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [MaxLength(500, ErrorMessage = "備註不能超過500字")]
        public string? Remark { get; set; }

        /// <summary>
        /// 附件檔案（選填）
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// 更新實際加班時間請求
    /// </summary>
    public class UpdateActualOvertimeRequest
    {
        /// <summary>
        /// 實際加班開始時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際開始時間為必填")]
        public string ActualStartTime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "實際結束時間為必填")]
        public string ActualEndTime { get; set; } = string.Empty;
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
        /// 加班日期
        /// </summary>
        public string OvertimeDate { get; set; } = string.Empty;

        /// <summary>
        /// 預計開始時間
        /// </summary>
        public string PlannedStartTime { get; set; } = string.Empty;

        /// <summary>
        /// 預計結束時間
        /// </summary>
        public string PlannedEndTime { get; set; } = string.Empty;

        /// <summary>
        /// 加班事由
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 處理方式
        /// </summary>
        public string CompensationType { get; set; } = string.Empty;

        /// <summary>
        /// 加班時數
        /// </summary>
        public decimal OvertimeHours { get; set; }

        /// <summary>
        /// 是否同意例休對調
        /// </summary>
        public bool AgreeToRestDaySwap { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 附件路徑列表
        /// </summary>
        public List<string>? Attachments { get; set; }
    }
}