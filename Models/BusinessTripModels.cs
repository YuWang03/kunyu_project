using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 出差單記錄
    /// </summary>
    public class BusinessTripRecord
    {
        /// <summary>
        /// 出差單ID（BPM FormID）
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
        /// 日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 出差起始日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string StartDate { get; set; } = string.Empty;

        /// <summary>
        /// 出差結束日期 (格式: yyyy/MM/dd)
        /// </summary>
        public string EndDate { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間 (格式: yyyy/MM/dd HH:mm)
        /// </summary>
        public string ApplicationDateTime { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態（待審核/已核准/已拒絕/已取消）
        /// </summary>
        public string ApprovalStatus { get; set; } = string.Empty;

        /// <summary>
        /// 簽核人員
        /// </summary>
        public string? ApprovingPersonnel { get; set; }

        /// <summary>
        /// 簽核時間
        /// </summary>
        public string? ApprovalTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 地點
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 天數
        /// </summary>
        public decimal NumberOfDays { get; set; }

        /// <summary>
        /// 出差主要任務
        /// </summary>
        public string MainTasksOfTrip { get; set; } = string.Empty;

        /// <summary>
        /// 費用預估（津貼、機票、其他費用）
        /// </summary>
        public string EstimatedCosts { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案路徑（多個附件用 || 連接）
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 附件檔案列表（由 FilePath 解析而來）
        /// </summary>
        public List<string>? Attachments { get; set; }
    }

    /// <summary>
    /// 申請出差表單請求（使用 PI_BUSINESS_TRIP_001）
    /// </summary>
    public class CreateBusinessTripFormRequest
    {
        /// <summary>
        /// 員工 Email（用於查詢員工資料和取得 BPM UserID）
        /// </summary>
        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 日期 (格式: yyyy-MM-dd 或 yyyy/MM/dd)
        /// </summary>
        [Required(ErrorMessage = "日期為必填")]
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        [Required(ErrorMessage = "事由為必填")]
        [MaxLength(500, ErrorMessage = "事由不能超過500字")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 出差起始日期 (格式: yyyy-MM-dd 或 yyyy/MM/dd)
        /// </summary>
        [Required(ErrorMessage = "出差起始日期為必填")]
        public string StartDate { get; set; } = string.Empty;

        /// <summary>
        /// 出差結束日期 (格式: yyyy-MM-dd 或 yyyy/MM/dd)
        /// </summary>
        [Required(ErrorMessage = "出差結束日期為必填")]
        public string EndDate { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間 (格式: yyyy-MM-dd HH:mm 或 yyyy/MM/dd HH:mm)
        /// 如果未提供則使用當前時間
        /// </summary>
        public string? ApplicationDateTime { get; set; }

        /// <summary>
        /// 簽核狀態（待審核/已核准/已拒絕/已取消）
        /// 預設為「待審核」
        /// </summary>
        public string? ApprovalStatus { get; set; }

        /// <summary>
        /// 簽核人員
        /// </summary>
        public string? ApprovingPersonnel { get; set; }

        /// <summary>
        /// 簽核時間
        /// </summary>
        public string? ApprovalTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [MaxLength(1000, ErrorMessage = "備註不能超過1000字")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 地點
        /// </summary>
        [Required(ErrorMessage = "地點為必填")]
        [MaxLength(200, ErrorMessage = "地點不能超過200字")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 天數
        /// </summary>
        [Required(ErrorMessage = "天數為必填")]
        [Range(0.5, 365, ErrorMessage = "天數必須在0.5到365之間")]
        public decimal NumberOfDays { get; set; }

        /// <summary>
        /// 出差主要任務
        /// </summary>
        [Required(ErrorMessage = "出差主要任務為必填")]
        [MaxLength(1000, ErrorMessage = "出差主要任務不能超過1000字")]
        public string MainTasksOfTrip { get; set; } = string.Empty;

        /// <summary>
        /// 費用預估（津貼、機票、其他費用）
        /// 格式範例：津貼: 3000元, 機票: 8000元, 其他: 2000元
        /// </summary>
        [Required(ErrorMessage = "費用預估為必填")]
        [MaxLength(500, ErrorMessage = "費用預估不能超過500字")]
        public string EstimatedCosts { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案上傳（選填，用於上傳至FTP）
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// 出差表單操作結果
    /// </summary>
    public class BusinessTripFormOperationResult
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
        /// 表單ID（BPM 流程實例 OID）
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 表單編號（流程序號）
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 錯誤代碼（失敗時）
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// BPM 回應資訊
        /// </summary>
        public object? BpmResponse { get; set; }
    }
}
