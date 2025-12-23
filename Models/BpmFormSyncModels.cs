using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 表單類型列舉
    /// </summary>
    public enum BpmFormType
    {
        /// <summary>請假單</summary>
        LEAVE,
        /// <summary>加班單</summary>
        OVERTIME,
        /// <summary>出差單</summary>
        BUSINESS_TRIP,
        /// <summary>銷假單</summary>
        CANCEL_LEAVE,
        /// <summary>其他</summary>
        OTHER
    }

    /// <summary>
    /// 表單狀態列舉
    /// </summary>
    public enum BpmFormStatus
    {
        /// <summary>待審核</summary>
        PENDING,
        /// <summary>審核中</summary>
        PROCESSING,
        /// <summary>已核准</summary>
        APPROVED,
        /// <summary>已拒絕</summary>
        REJECTED,
        /// <summary>已取消</summary>
        CANCELLED,
        /// <summary>已撤回</summary>
        WITHDRAWN,
        /// <summary>已結案</summary>
        COMPLETED
    }

    /// <summary>
    /// 同步類型列舉
    /// </summary>
    public enum SyncType
    {
        /// <summary>從 BPM 拉取</summary>
        FETCH,
        /// <summary>推送至 BPM</summary>
        PUSH,
        /// <summary>取消同步</summary>
        CANCEL
    }

    /// <summary>
    /// 同步方向列舉
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>進入 (從 BPM 到本地)</summary>
        IN,
        /// <summary>送出 (從本地到 BPM)</summary>
        OUT
    }

    /// <summary>
    /// 同步狀態列舉
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>成功</summary>
        SUCCESS,
        /// <summary>失敗</summary>
        FAILED,
        /// <summary>部分成功</summary>
        PARTIAL
    }

    #region 主表 - BPM 表單

    /// <summary>
    /// BPM 表單主表實體
    /// 對應資料表: bpm_forms
    /// </summary>
    [Table("bpm_forms")]
    public class BpmForm
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號 (BPM流程序號，唯一碼)</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>表單代碼 (如: PI_LEAVE_001)</summary>
        [Column("form_code")]
        [Required]
        [MaxLength(50)]
        public string FormCode { get; set; } = string.Empty;

        /// <summary>表單類型</summary>
        [Column("form_type")]
        [Required]
        [MaxLength(20)]
        public string FormType { get; set; } = string.Empty;

        /// <summary>表單版本</summary>
        [Column("form_version")]
        [MaxLength(10)]
        public string FormVersion { get; set; } = "1.0.0";

        /// <summary>申請人工號</summary>
        [Column("applicant_id")]
        [Required]
        [MaxLength(50)]
        public string ApplicantId { get; set; } = string.Empty;

        /// <summary>申請人姓名</summary>
        [Column("applicant_name")]
        [MaxLength(100)]
        public string? ApplicantName { get; set; }

        /// <summary>申請人部門</summary>
        [Column("applicant_department")]
        [MaxLength(200)]
        public string? ApplicantDepartment { get; set; }

        /// <summary>公司代碼</summary>
        [Column("company_id")]
        [MaxLength(50)]
        public string? CompanyId { get; set; }

        /// <summary>表單詳細資料 (JSON格式)</summary>
        [Column("form_data")]
        public string? FormData { get; set; }

        /// <summary>表單狀態</summary>
        [Column("status")]
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING";

        /// <summary>BPM 原始狀態</summary>
        [Column("bpm_status")]
        [MaxLength(30)]
        public string? BpmStatus { get; set; }

        /// <summary>申請日期</summary>
        [Column("apply_date")]
        public DateTime? ApplyDate { get; set; }

        /// <summary>送出時間</summary>
        [Column("submit_time")]
        public DateTime? SubmitTime { get; set; }

        /// <summary>最後同步時間</summary>
        [Column("last_sync_time")]
        public DateTime? LastSyncTime { get; set; }

        /// <summary>目前簽核人工號</summary>
        [Column("current_approver_id")]
        [MaxLength(50)]
        public string? CurrentApproverId { get; set; }

        /// <summary>目前簽核人姓名</summary>
        [Column("current_approver_name")]
        [MaxLength(100)]
        public string? CurrentApproverName { get; set; }

        /// <summary>簽核意見</summary>
        [Column("approval_comment")]
        public string? ApprovalComment { get; set; }

        /// <summary>是否已取消 (APP端)</summary>
        [Column("is_cancelled")]
        public bool IsCancelled { get; set; } = false;

        /// <summary>取消原因</summary>
        [Column("cancel_reason")]
        public string? CancelReason { get; set; }

        /// <summary>取消時間</summary>
        [Column("cancel_time")]
        public DateTime? CancelTime { get; set; }

        /// <summary>取消操作人</summary>
        [Column("cancelled_by")]
        [MaxLength(50)]
        public string? CancelledBy { get; set; }

        /// <summary>是否已同步至BPM</summary>
        [Column("is_synced_to_bpm")]
        public bool IsSyncedToBpm { get; set; } = false;

        /// <summary>同步錯誤訊息</summary>
        [Column("sync_error_message")]
        public string? SyncErrorMessage { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新時間</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        public virtual BpmLeaveForm? LeaveForm { get; set; }
        public virtual BpmOvertimeForm? OvertimeForm { get; set; }
        public virtual BpmBusinessTripForm? BusinessTripForm { get; set; }
        public virtual BpmCancelLeaveForm? CancelLeaveForm { get; set; }
        public virtual ICollection<BpmFormApprovalHistory>? ApprovalHistory { get; set; }

        /// <summary>
        /// 取得表單類型列舉值
        /// </summary>
        public BpmFormType GetFormTypeEnum()
        {
            return FormType.ToUpperInvariant() switch
            {
                "LEAVE" => BpmFormType.LEAVE,
                "OVERTIME" => BpmFormType.OVERTIME,
                "BUSINESS_TRIP" => BpmFormType.BUSINESS_TRIP,
                "CANCEL_LEAVE" => BpmFormType.CANCEL_LEAVE,
                _ => BpmFormType.OTHER
            };
        }

        /// <summary>
        /// 取得表單狀態列舉值
        /// </summary>
        public BpmFormStatus GetStatusEnum()
        {
            return Status.ToUpperInvariant() switch
            {
                "PENDING" => BpmFormStatus.PENDING,
                "PROCESSING" => BpmFormStatus.PROCESSING,
                "APPROVED" => BpmFormStatus.APPROVED,
                "REJECTED" => BpmFormStatus.REJECTED,
                "CANCELLED" => BpmFormStatus.CANCELLED,
                "WITHDRAWN" => BpmFormStatus.WITHDRAWN,
                "COMPLETED" => BpmFormStatus.COMPLETED,
                _ => BpmFormStatus.PENDING
            };
        }

        /// <summary>
        /// 取得表單詳細資料物件
        /// </summary>
        public T? GetFormDataAs<T>() where T : class
        {
            if (string.IsNullOrEmpty(FormData))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(FormData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 設定表單詳細資料
        /// </summary>
        public void SetFormData<T>(T data) where T : class
        {
            FormData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
    }

    #endregion

    #region 請假單詳細資料

    /// <summary>
    /// 請假單詳細資料實體
    /// 對應資料表: bpm_leave_forms
    /// </summary>
    [Table("bpm_leave_forms")]
    public class BpmLeaveForm
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>假別代碼</summary>
        [Column("leave_type_code")]
        [MaxLength(20)]
        public string? LeaveTypeCode { get; set; }

        /// <summary>假別名稱</summary>
        [Column("leave_type_name")]
        [MaxLength(50)]
        public string? LeaveTypeName { get; set; }

        /// <summary>請假起始日期</summary>
        [Column("start_date")]
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>起始時間</summary>
        [Column("start_time")]
        public TimeSpan? StartTime { get; set; }

        /// <summary>請假結束日期</summary>
        [Column("end_date")]
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>結束時間</summary>
        [Column("end_time")]
        public TimeSpan? EndTime { get; set; }

        /// <summary>請假時數</summary>
        [Column("leave_hours")]
        public decimal? LeaveHours { get; set; }

        /// <summary>請假天數</summary>
        [Column("leave_days")]
        public decimal? LeaveDays { get; set; }

        /// <summary>請假事由</summary>
        [Column("reason")]
        public string? Reason { get; set; }

        /// <summary>代理人工號</summary>
        [Column("agent_id")]
        [MaxLength(50)]
        public string? AgentId { get; set; }

        /// <summary>代理人姓名</summary>
        [Column("agent_name")]
        [MaxLength(100)]
        public string? AgentName { get; set; }

        /// <summary>請假事件發生日</summary>
        [Column("leave_event_date")]
        public DateTime? LeaveEventDate { get; set; }

        /// <summary>是否有附件</summary>
        [Column("has_attachments")]
        public bool HasAttachments { get; set; } = false;

        /// <summary>附件資訊 (JSON)</summary>
        [Column("attachments")]
        public string? Attachments { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新時間</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        [ForeignKey("FormId")]
        public virtual BpmForm? BpmForm { get; set; }
    }

    #endregion

    #region 加班單詳細資料

    /// <summary>
    /// 加班單詳細資料實體
    /// 對應資料表: bpm_overtime_forms
    /// </summary>
    [Table("bpm_overtime_forms")]
    public class BpmOvertimeForm
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>加班日期</summary>
        [Column("overtime_date")]
        [Required]
        public DateTime OvertimeDate { get; set; }

        /// <summary>預計加班起始時間</summary>
        [Column("planned_start_time")]
        public DateTime? PlannedStartTime { get; set; }

        /// <summary>預計加班結束時間</summary>
        [Column("planned_end_time")]
        public DateTime? PlannedEndTime { get; set; }

        /// <summary>實際加班起始時間</summary>
        [Column("actual_start_time")]
        public DateTime? ActualStartTime { get; set; }

        /// <summary>實際加班結束時間</summary>
        [Column("actual_end_time")]
        public DateTime? ActualEndTime { get; set; }

        /// <summary>加班時數</summary>
        [Column("overtime_hours")]
        public decimal? OvertimeHours { get; set; }

        /// <summary>處理方式 (0=轉補休, 1=加班費)</summary>
        [Column("process_type")]
        public int ProcessType { get; set; } = 0;

        /// <summary>加班事由</summary>
        [Column("reason")]
        public string? Reason { get; set; }

        /// <summary>是否有附件</summary>
        [Column("has_attachments")]
        public bool HasAttachments { get; set; } = false;

        /// <summary>附件資訊 (JSON)</summary>
        [Column("attachments")]
        public string? Attachments { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新時間</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        [ForeignKey("FormId")]
        public virtual BpmForm? BpmForm { get; set; }
    }

    #endregion

    #region 出差單詳細資料

    /// <summary>
    /// 出差單詳細資料實體
    /// 對應資料表: bpm_business_trip_forms
    /// </summary>
    [Table("bpm_business_trip_forms")]
    public class BpmBusinessTripForm
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>出差日期</summary>
        [Column("trip_date")]
        [Required]
        public DateTime TripDate { get; set; }

        /// <summary>出差起始日期</summary>
        [Column("start_date")]
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>出差結束日期</summary>
        [Column("end_date")]
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>出差天數</summary>
        [Column("trip_days")]
        public decimal? TripDays { get; set; }

        /// <summary>出差地點</summary>
        [Column("location")]
        [MaxLength(200)]
        public string? Location { get; set; }

        /// <summary>出差主要任務</summary>
        [Column("main_tasks")]
        public string? MainTasks { get; set; }

        /// <summary>出差事由</summary>
        [Column("reason")]
        public string? Reason { get; set; }

        /// <summary>費用預估</summary>
        [Column("estimated_costs")]
        public string? EstimatedCosts { get; set; }

        /// <summary>是否有附件</summary>
        [Column("has_attachments")]
        public bool HasAttachments { get; set; } = false;

        /// <summary>附件資訊 (JSON)</summary>
        [Column("attachments")]
        public string? Attachments { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新時間</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        [ForeignKey("FormId")]
        public virtual BpmForm? BpmForm { get; set; }
    }

    #endregion

    #region 銷假單詳細資料

    /// <summary>
    /// 銷假單詳細資料實體
    /// 對應資料表: bpm_cancel_leave_forms
    /// </summary>
    [Table("bpm_cancel_leave_forms")]
    public class BpmCancelLeaveForm
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>原請假單編號</summary>
        [Column("original_leave_form_id")]
        [MaxLength(100)]
        public string? OriginalLeaveFormId { get; set; }

        /// <summary>銷假原因</summary>
        [Column("cancel_reason")]
        public string? CancelReason { get; set; }

        /// <summary>原假別</summary>
        [Column("original_leave_type")]
        [MaxLength(50)]
        public string? OriginalLeaveType { get; set; }

        /// <summary>原請假起始日期</summary>
        [Column("original_start_date")]
        public DateTime? OriginalStartDate { get; set; }

        /// <summary>原起始時間</summary>
        [Column("original_start_time")]
        public TimeSpan? OriginalStartTime { get; set; }

        /// <summary>原請假結束日期</summary>
        [Column("original_end_date")]
        public DateTime? OriginalEndDate { get; set; }

        /// <summary>原結束時間</summary>
        [Column("original_end_time")]
        public TimeSpan? OriginalEndTime { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新時間</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        [ForeignKey("FormId")]
        public virtual BpmForm? BpmForm { get; set; }
    }

    #endregion

    #region 簽核歷程

    /// <summary>
    /// 表單簽核歷程實體
    /// 對應資料表: bpm_form_approval_history
    /// </summary>
    [Table("bpm_form_approval_history")]
    public class BpmFormApprovalHistory
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [Required]
        [MaxLength(100)]
        public string FormId { get; set; } = string.Empty;

        /// <summary>簽核順序</summary>
        [Column("sequence_no")]
        [Required]
        public int SequenceNo { get; set; }

        /// <summary>簽核人工號</summary>
        [Column("approver_id")]
        [Required]
        [MaxLength(50)]
        public string ApproverId { get; set; } = string.Empty;

        /// <summary>簽核人姓名</summary>
        [Column("approver_name")]
        [MaxLength(100)]
        public string? ApproverName { get; set; }

        /// <summary>簽核人部門</summary>
        [Column("approver_department")]
        [MaxLength(200)]
        public string? ApproverDepartment { get; set; }

        /// <summary>簽核動作 (APPROVE/REJECT/RETURN)</summary>
        [Column("action")]
        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        /// <summary>簽核意見</summary>
        [Column("comment")]
        public string? Comment { get; set; }

        /// <summary>簽核時間</summary>
        [Column("action_time")]
        public DateTime? ActionTime { get; set; }

        /// <summary>建立時間</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 導航屬性
        [ForeignKey("FormId")]
        public virtual BpmForm? BpmForm { get; set; }
    }

    #endregion

    #region 同步日誌

    /// <summary>
    /// 表單同步日誌實體
    /// 對應資料表: bpm_form_sync_logs
    /// </summary>
    [Table("bpm_form_sync_logs")]
    public class BpmFormSyncLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>表單編號</summary>
        [Column("form_id")]
        [MaxLength(100)]
        public string? FormId { get; set; }

        /// <summary>同步類型 (FETCH/PUSH/CANCEL)</summary>
        [Column("sync_type")]
        [Required]
        [MaxLength(20)]
        public string SyncType { get; set; } = string.Empty;

        /// <summary>同步方向 (IN/OUT)</summary>
        [Column("sync_direction")]
        [Required]
        [MaxLength(10)]
        public string SyncDirection { get; set; } = string.Empty;

        /// <summary>同步狀態 (SUCCESS/FAILED/PARTIAL)</summary>
        [Column("sync_status")]
        [Required]
        [MaxLength(20)]
        public string SyncStatus { get; set; } = string.Empty;

        /// <summary>請求資料</summary>
        [Column("request_data")]
        public string? RequestData { get; set; }

        /// <summary>回應資料</summary>
        [Column("response_data")]
        public string? ResponseData { get; set; }

        /// <summary>錯誤訊息</summary>
        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>操作人工號</summary>
        [Column("operator_id")]
        [MaxLength(50)]
        public string? OperatorId { get; set; }

        /// <summary>同步時間</summary>
        [Column("sync_time")]
        public DateTime SyncTime { get; set; } = DateTime.Now;
    }

    #endregion

    #region DTO 類別

    /// <summary>
    /// 表單同步請求
    /// </summary>
    public class FormSyncRequest
    {
        /// <summary>表單編號</summary>
        public string FormId { get; set; } = string.Empty;

        /// <summary>表單類型</summary>
        public string? FormType { get; set; }

        /// <summary>操作人工號</summary>
        public string? OperatorId { get; set; }

        /// <summary>是否強制同步</summary>
        public bool ForceSync { get; set; } = false;
    }

    /// <summary>
    /// 表單同步結果
    /// </summary>
    public class FormSyncResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>訊息</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>表單編號</summary>
        public string? FormId { get; set; }

        /// <summary>同步的表單</summary>
        public BpmForm? Form { get; set; }

        /// <summary>錯誤代碼</summary>
        public string? ErrorCode { get; set; }

        /// <summary>是否為新建立的表單</summary>
        public bool IsNewForm { get; set; } = false;

        /// <summary>是否已更新</summary>
        public bool IsUpdated { get; set; } = false;
    }

    /// <summary>
    /// 表單取消請求
    /// </summary>
    public class FormCancelRequest
    {
        /// <summary>表單編號</summary>
        [Required]
        public string FormId { get; set; } = string.Empty;

        /// <summary>取消原因</summary>
        [Required]
        public string CancelReason { get; set; } = string.Empty;

        /// <summary>操作人工號</summary>
        [Required]
        public string OperatorId { get; set; } = string.Empty;

        /// <summary>是否同步至 BPM</summary>
        public bool SyncToBpm { get; set; } = true;
    }

    /// <summary>
    /// 表單取消結果
    /// </summary>
    public class FormCancelResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>訊息</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>表單編號</summary>
        public string? FormId { get; set; }

        /// <summary>是否已同步至 BPM</summary>
        public bool SyncedToBpm { get; set; } = false;

        /// <summary>錯誤代碼</summary>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// 表單查詢條件
    /// </summary>
    public class BpmFormQueryRequest
    {
        /// <summary>表單編號</summary>
        public string? FormId { get; set; }

        /// <summary>表單類型</summary>
        public string? FormType { get; set; }

        /// <summary>申請人工號</summary>
        public string? ApplicantId { get; set; }

        /// <summary>公司代碼</summary>
        public string? CompanyId { get; set; }

        /// <summary>表單狀態</summary>
        public string? Status { get; set; }

        /// <summary>是否已取消</summary>
        public bool? IsCancelled { get; set; }

        /// <summary>申請日期起始</summary>
        public DateTime? ApplyDateFrom { get; set; }

        /// <summary>申請日期結束</summary>
        public DateTime? ApplyDateTo { get; set; }

        /// <summary>頁碼</summary>
        public int Page { get; set; } = 1;

        /// <summary>每頁筆數</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 表單查詢結果
    /// </summary>
    public class BpmFormQueryResult
    {
        /// <summary>總筆數</summary>
        public int TotalCount { get; set; }

        /// <summary>總頁數</summary>
        public int TotalPages { get; set; }

        /// <summary>目前頁碼</summary>
        public int Page { get; set; }

        /// <summary>每頁筆數</summary>
        public int PageSize { get; set; }

        /// <summary>表單列表</summary>
        public List<BpmForm> Forms { get; set; } = new();
    }

    #endregion
}
