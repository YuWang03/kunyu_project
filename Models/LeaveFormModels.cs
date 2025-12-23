using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 請假假別單位查詢請求 - efleaveformunit API
    /// </summary>
    public class LeaveTypeUnitRequest
    {
        /// <summary>
        /// Token標記（必填）
        /// </summary>
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司（必填）
        /// </summary>
        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號（必填）
        /// </summary>
        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 請假假別單位查詢回應 - efleaveformunit API
    /// </summary>
    public class LeaveTypeUnitResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 失敗訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 數據返回區（成功時有此區，失敗時無此區）
        /// </summary>
        public List<LeaveTypeUnitData>? Data { get; set; }
    }

    /// <summary>
    /// 假別單位資料
    /// </summary>
    public class LeaveTypeUnitData
    {
        /// <summary>
        /// 假別名稱
        /// </summary>
        public string LeaveType { get; set; } = string.Empty;

        /// <summary>
        /// 請假最小單位
        /// </summary>
        public string LeaveUnit { get; set; } = string.Empty;

        /// <summary>
        /// 假別代碼
        /// </summary>
        public string LeaveCode { get; set; } = string.Empty;

        /// <summary>
        /// 假別單位類型
        /// </summary>
        public string LeaveUnitType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 請假單申請請求 - efleaveform API
    /// </summary>
    public class LeaveFormSubmitRequest
    {
        /// <summary>
        /// Token標記（必填）
        /// </summary>
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司（必填）
        /// </summary>
        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號（必填）
        /// </summary>
        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 假別代碼（必填）
        /// </summary>
        [Required(ErrorMessage = "leavetype 為必填")]
        public string Leavetype { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "estartdate 為必填")]
        public string Estartdate { get; set; } = string.Empty;

        /// <summary>
        /// 起始時間（必填）- 格式：HH:mm
        /// </summary>
        [Required(ErrorMessage = "estarttime 為必填")]
        public string Estarttime { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "eenddate 為必填")]
        public string Eenddate { get; set; } = string.Empty;

        /// <summary>
        /// 截止時間（必填）- 格式：HH:mm
        /// </summary>
        [Required(ErrorMessage = "eendtime 為必填")]
        public string Eendtime { get; set; } = string.Empty;

        /// <summary>
        /// 請假事件發生日（選填，婚假、產假必填）- 格式：yyyy-MM-dd
        /// </summary>
        public string? Eleavedate { get; set; }

        /// <summary>
        /// 事由（必填）
        /// </summary>
        [Required(ErrorMessage = "ereason 為必填")]
        public string Ereason { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號（必填）
        /// </summary>
        [Required(ErrorMessage = "eagent 為必填")]
        public string Eagent { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案格式（選填）- C: 請假附件檔
        /// </summary>
        public string? Efiletype { get; set; }

        /// <summary>
        /// 附件檔案編號（選填）
        /// </summary>
        public List<string>? Efileid { get; set; }

        /// <summary>
        /// 附件檔案 URL（選填）- 從附件上傳 API 取得的實際檔案路徑
        /// </summary>
        public string? Efileurl { get; set; }
    }

    /// <summary>
    /// 請假單申請回應 - efleaveform API
    /// </summary>
    public class LeaveFormSubmitResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號（成功時回傳）
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 表單請求編號（成功時回傳）
        /// </summary>
        public string? FormNumber { get; set; }
    }

    /// <summary>
    /// 請假單查詢條件
    /// </summary>
    public class LeaveFormQueryRequest
    {
        /// <summary>
        /// 員工工號
        /// </summary>
        public string? EmployeeNo { get; set; }

        /// <summary>
        /// 員工 Email
        /// </summary>
        public string? EmployeeEmail { get; set; }

        /// <summary>
        /// 查詢年份（西元年，例如：2025）
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// 查詢月份（1-12）
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// 查詢開始日期 (yyyy-MM-dd)
        /// </summary>
        public string? StartDate { get; set; }

        /// <summary>
        /// 查詢結束日期 (yyyy-MM-dd)
        /// </summary>
        public string? EndDate { get; set; }

        /// <summary>
        /// 簽核狀態 (pending/approved/rejected/cancelled)
        /// </summary>
        public string? ApprovalStatus { get; set; }

        /// <summary>
        /// 假別類型 ID
        /// </summary>
        public string? LeaveType { get; set; }

        /// <summary>
        /// 頁碼（預設：1）
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每頁筆數（預設：20）
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 建立請假單請求 - 完整欄位版本（依照 BPM field-mappings）
    /// </summary>
    public class CreateLeaveFormRequest
    {
        /// <summary>
        /// 申請人 Email（必填）- 用於查詢員工資料
        /// </summary>
        [Required(ErrorMessage = "申請人 Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        // ===== BPM 必填欄位 (required: true) =====
        
        /// <summary>
        /// 假別代碼（必填）
        /// </summary>
        [Required(ErrorMessage = "假別代碼為必填")]
        public string LeaveTypeId { get; set; } = string.Empty;

        /// <summary>
        /// 假別名稱（必填）
        /// </summary>
        [Required(ErrorMessage = "假別名稱為必填")]
        public string LeaveTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 開始日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "開始日期為必填")]
        public string StartDate { get; set; } = string.Empty;

        /// <summary>
        /// 開始時間（必填）- 格式：HH:mm
        /// </summary>
        [Required(ErrorMessage = "開始時間為必填")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 結束日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "結束日期為必填")]
        public string EndDate { get; set; } = string.Empty;

        /// <summary>
        /// 結束時間（必填）- 格式：HH:mm
        /// </summary>
        [Required(ErrorMessage = "結束時間為必填")]
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 請假事由（必填）
        /// </summary>
        [Required(ErrorMessage = "請假事由為必填")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號（必填）
        /// </summary>
        [Required(ErrorMessage = "代理人工號為必填")]
        public string AgentNo { get; set; } = string.Empty;

        // ===== BPM 選填欄位 (required: false) =====

        /// <summary>
        /// 組織代碼（選填）- txtOrg
        /// </summary>
        public string? Org { get; set; }

        /// <summary>
        /// 表單代碼（選填）- snuForm
        /// </summary>
        public string? Form { get; set; }

        /// <summary>
        /// cpf01 欄位（選填）- txtcpf01
        /// </summary>
        public string? Cpf01 { get; set; }

        /// <summary>
        /// 是否需要文件（選填）- hdnIsDoc
        /// </summary>
        public string? IsDoc { get; set; }

        /// <summary>
        /// 請假單位數量（選填）- txbunits
        /// </summary>
        public decimal? Units { get; set; }

        /// <summary>
        /// 是否為部分天數請假（選填）- hdnIsPart
        /// </summary>
        public string? IsPart { get; set; }

        /// <summary>
        /// 申請人姓名（選填）- labApplier（系統自動帶入）
        /// </summary>
        public string? Applier { get; set; }

        /// <summary>
        /// 結束打卡記錄（選填）- txbEndCard
        /// </summary>
        public string? EndCard { get; set; }

        /// <summary>
        /// 組織名稱（選填）- txtOrgName（系統自動帶入）
        /// </summary>
        public string? OrgName { get; set; }

        /// <summary>
        /// 組織單位（選填）- txtOrgUnit（系統自動帶入）
        /// </summary>
        public string? OrgUnit { get; set; }

        /// <summary>
        /// 附件檔案路徑（選填）- hdnFilePath
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 簽核類型（選填）- txtSignType
        /// </summary>
        public string? SignType { get; set; }

        /// <summary>
        /// 文字資料（選填）- tmpTextData
        /// </summary>
        public string? TextData { get; set; }

        /// <summary>
        /// 總請假天數（選填）- txbTotalDay
        /// </summary>
        public decimal? TotalDay { get; set; }

        /// <summary>
        /// 請假單位類型（選填）- txbunitType (DAY/HOUR)
        /// </summary>
        public string? UnitType { get; set; }

        /// <summary>
        /// 公司代碼（選填）- txbCompanyNo（系統自動帶入）
        /// </summary>
        public string? CompanyNo { get; set; }

        /// <summary>
        /// 事件發生日（選填）- dateEventDate
        /// </summary>
        public string? EventDate { get; set; }

        /// <summary>
        /// 是否為假日（選填）- hdnIsHoliday
        /// </summary>
        public string? IsHoliday { get; set; }

        /// <summary>
        /// 簽核層級 01（選填）- txtSignlvl01
        /// </summary>
        public string? Signlvl01 { get; set; }

        /// <summary>
        /// 簽核層級 02（選填）- txtSignlvl02
        /// </summary>
        public string? Signlvl02 { get; set; }

        /// <summary>
        /// 簽核層級 03（選填）- txtSignlvl03
        /// </summary>
        public string? Signlvl03 { get; set; }

        /// <summary>
        /// 簽核層級 04（選填）- txtSignlvl04
        /// </summary>
        public string? Signlvl04 { get; set; }

        /// <summary>
        /// 簽核層級 05（選填）- txtSignlvl05
        /// </summary>
        public string? Signlvl05 { get; set; }

        /// <summary>
        /// 簽核層級 06（選填）- txtSignlvl06
        /// </summary>
        public string? Signlvl06 { get; set; }

        /// <summary>
        /// 簽核層級 07（選填）- txtSignlvl07
        /// </summary>
        public string? Signlvl07 { get; set; }

        /// <summary>
        /// 簽核層級 08（選填）- txtSignlvl08
        /// </summary>
        public string? Signlvl08 { get; set; }

        /// <summary>
        /// 簽核層級 09（選填）- txtSignlvl09
        /// </summary>
        public string? Signlvl09 { get; set; }

        /// <summary>
        /// 開始打卡記錄（選填）- txbStartCard
        /// </summary>
        public string? StartCard { get; set; }

        /// <summary>
        /// 總請假時數（選填）- txbTotalHour
        /// </summary>
        public decimal? TotalHour { get; set; }

        /// <summary>
        /// 發送通知對象（選填）- mulSendNotice
        /// </summary>
        public string? SendNotice { get; set; }

        /// <summary>
        /// 請假單位類型中文（選填）- txbunitTypeTW
        /// </summary>
        public string? UnitTypeTW { get; set; }

        /// <summary>
        /// 申請人姓名（選填）- hdnApplierName（系統自動帶入）
        /// </summary>
        public string? ApplierName { get; set; }

        /// <summary>
        /// 申請人部門（選填）- labApplierUnit（系統自動帶入）
        /// </summary>
        public string? ApplierUnit { get; set; }

        /// <summary>
        /// 是否有事件日期（選填）- hdnIsEventDate
        /// </summary>
        public string? IsEventDate { get; set; }

        /// <summary>
        /// 代填人員 ID（選填）- txtKeyInUserId
        /// </summary>
        public string? KeyInUserId { get; set; }

        /// <summary>
        /// 每日工作時數（選填）- hdnDayWorkHours
        /// </summary>
        public decimal? DayWorkHours { get; set; }

        /// <summary>
        /// 是否發送通知（選填）- hdnIsSendNotice
        /// </summary>
        public string? IsSendNotice { get; set; }

        /// <summary>
        /// 代理人 ID（選填）- hdnSubstituteId
        /// </summary>
        public string? SubstituteId { get; set; }

        /// <summary>
        /// 假別備註（選填）- txbLeaveTypeMemo
        /// </summary>
        public string? LeaveTypeMemo { get; set; }

        /// <summary>
        /// 簽核總天數（選填）- txbSignTotalDays
        /// </summary>
        public decimal? SignTotalDays { get; set; }

        /// <summary>
        /// 申請人部門名稱（選填）- hdnApplierDeptName（系統自動帶入）
        /// </summary>
        public string? ApplierDeptName { get; set; }

        /// <summary>
        /// 代理人列表（選填）- substituteId[] 陣列
        /// </summary>
        public List<SubstituteInfo>? Substitutes { get; set; }

        /// <summary>
        /// 上傳的附件檔案（選填）
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// 代理人資訊
    /// </summary>
    public class SubstituteInfo
    {
        /// <summary>
        /// 代理人 ID
        /// </summary>
        public string SubId { get; set; } = string.Empty;

        /// <summary>
        /// 代理順序
        /// </summary>
        public string SubSeq { get; set; } = string.Empty;

        /// <summary>
        /// 代理人姓名
        /// </summary>
        public string SubName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 簽核層級設定
    /// </summary>
    public class SignLevelSettings
    {
        public string? Level01 { get; set; }
        public string? Level02 { get; set; }
        public string? Level03 { get; set; }
        public string? Level04 { get; set; }
        public string? Level05 { get; set; }
        public string? Level06 { get; set; }
        public string? Level07 { get; set; }
        public string? Level08 { get; set; }
        public string? Level09 { get; set; }
    }

    /// <summary>
    /// 簽核請假單請求
    /// </summary>
    public class ApproveLeaveFormRequest
    {
        /// <summary>
        /// 表單 ID（必填）
        /// </summary>
        [Required(ErrorMessage = "表單 ID 為必填")]
        public string FormId { get; set; } = string.Empty;

        /// <summary>
        /// 簽核人 Email（必填）
        /// </summary>
        [Required(ErrorMessage = "簽核人 Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string ApproverEmail { get; set; } = string.Empty;

        /// <summary>
        /// 簽核動作（必填）- approve(核准) / reject(拒絕) / return(退回重簽)
        /// </summary>
        [Required(ErrorMessage = "簽核動作為必填")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 簽核意見（選填）
        /// </summary>
        public string? Comment { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 請假單記錄
    /// </summary>
    public class LeaveFormRecord
    {
        /// <summary>
        /// 表單 ID
        /// </summary>
        public string FormId { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string FormNumber { get; set; } = string.Empty;

        /// <summary>
        /// 申請人工號
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 申請人姓名
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// 申請人部門
        /// </summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// 假別 ID
        /// </summary>
        public string LeaveTypeId { get; set; } = string.Empty;

        /// <summary>
        /// 假別名稱（請假類別）
        /// </summary>
        public string LeaveTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 開始日期
        /// </summary>
        public string StartDate { get; set; } = string.Empty;

        /// <summary>
        /// 開始時間
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 結束日期
        /// </summary>
        public string EndDate { get; set; } = string.Empty;

        /// <summary>
        /// 結束時間
        /// </summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 起始時間（完整格式：yyyy-MM-dd HH:mm）
        /// </summary>
        public string StartDateTime { get; set; } = string.Empty;

        /// <summary>
        /// 截止時間（完整格式：yyyy-MM-dd HH:mm）
        /// </summary>
        public string EndDateTime { get; set; } = string.Empty;

        /// <summary>
        /// 請假時數
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// 請假天數
        /// </summary>
        public decimal TotalDays { get; set; }

        /// <summary>
        /// 單位類型（DAY=天 / HOUR=時）
        /// </summary>
        public string UnitType { get; set; } = string.Empty;

        /// <summary>
        /// 單位數量
        /// </summary>
        public decimal Units { get; set; }

        /// <summary>
        /// 請假事由
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號
        /// </summary>
        public string AgentNo { get; set; } = string.Empty;

        /// <summary>
        /// 代理人姓名
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// 申請日期時間
        /// </summary>
        public string ApplyDateTime { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態 - pending(待簽核) / approved(已核准) / rejected(已拒絕) / cancelled(已取消)
        /// </summary>
        public string ApprovalStatus { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態顯示文字
        /// </summary>
        public string ApprovalStatusText { get; set; } = string.Empty;

        /// <summary>
        /// 當前簽核人員
        /// </summary>
        public string? CurrentApprover { get; set; }

        /// <summary>
        /// 當前簽核人員姓名
        /// </summary>
        public string? CurrentApproverName { get; set; }

        /// <summary>
        /// 簽核歷程
        /// </summary>
        public List<ApprovalHistory> ApprovalHistories { get; set; } = new();

        /// <summary>
        /// 事件發生日（參考資料庫）
        /// </summary>
        public string? EventDate { get; set; }

        /// <summary>
        /// 附件路徑（多個用 || 分隔）
        /// </summary>
        public string? AttachmentPaths { get; set; }

        /// <summary>
        /// 附件列表（Word、Excel、PDF、圖片）
        /// </summary>
        public List<AttachmentInfo> Attachments { get; set; } = new();

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 簽核歷程
    /// </summary>
    public class ApprovalHistory
    {
        /// <summary>
        /// 簽核層級
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 簽核人員工號
        /// </summary>
        public string ApproverNo { get; set; } = string.Empty;

        /// <summary>
        /// 簽核人員姓名
        /// </summary>
        public string ApproverName { get; set; } = string.Empty;

        /// <summary>
        /// 簽核動作 - approved / rejected / returned
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 簽核動作文字
        /// </summary>
        public string ActionText { get; set; } = string.Empty;

        /// <summary>
        /// 簽核意見
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// 簽核時間
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// 簽核時間（字串格式）
        /// </summary>
        public string? ApprovedAtText { get; set; }
    }

    /// <summary>
    /// 附件資訊
    /// </summary>
    public class AttachmentInfo
    {
        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 檔案路徑
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 檔案類型（Word / Excel / PDF / Image）
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 檔案副檔名
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>
        /// 檔案大小（bytes）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 上傳時間
        /// </summary>
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// 請假單操作結果
    /// </summary>
    public class LeaveFormOperationResult
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
        /// 表單 ID
        /// </summary>
        public string? FormId { get; set; }

        /// <summary>
        /// 表單編號
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 附件路徑列表
        /// </summary>
        public List<string>? AttachmentPaths { get; set; }

        /// <summary>
        /// 錯誤代碼
        /// </summary>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// 請假單查詢結果（分頁）
    /// </summary>
    public class LeaveFormQueryResult
    {
        /// <summary>
        /// 請假單列表
        /// </summary>
        public List<LeaveFormRecord> Records { get; set; } = new();

        /// <summary>
        /// 總筆數
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當前頁碼
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages { get; set; }
    }

    #endregion

    #region BPM API Models

    /// <summary>
    /// BPM 表單建立請求
    /// </summary>
    public class BpmCreateFormRequest
    {
        /// <summary>
        /// 流程代碼（必填）
        /// </summary>
        public string ProcessCode { get; set; } = string.Empty;

        /// <summary>
        /// 表單代碼
        /// </summary>
        public string FormCode { get; set; } = "PI_LEAVE_001";

        /// <summary>
        /// 表單版本
        /// </summary>
        public string FormVersion { get; set; } = "1.0.0";

        /// <summary>
        /// 使用者 ID（BPM UserID）
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 表單主旨
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 來源系統
        /// </summary>
        public string SourceSystem { get; set; } = "HRSystemAPI";

        /// <summary>
        /// 是否有附件
        /// </summary>
        public bool HasAttachments { get; set; }

        /// <summary>
        /// 表單資料（動態欄位）
        /// </summary>
        public Dictionary<string, object?> FormData { get; set; } = new();
    }

    /// <summary>
    /// BPM 表單建立回應
    /// </summary>
    public class BpmCreateFormResponse
    {
        public bool Success { get; set; }
        public string? FormId { get; set; }
        public string? FormNumber { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    #endregion
}