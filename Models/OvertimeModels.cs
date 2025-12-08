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

    // ========== 新版 APP API 模型 ==========

    /// <summary>
    /// 加班單預申請請求 (API 1: /app/efotapply)
    /// </summary>
    public class EFotApplyRequest
    {
        /// <summary>
        /// 表單ID (查詢時使用，提交時不需要)
        /// </summary>
        public string? Formid { get; set; }

        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        [Required(ErrorMessage = "estartdate 為必填")]
        public string Estartdate { get; set; } = string.Empty;

        [Required(ErrorMessage = "estarttime 為必填")]
        public string Estarttime { get; set; } = string.Empty;

        [Required(ErrorMessage = "eenddate 為必填")]
        public string Eenddate { get; set; } = string.Empty;

        [Required(ErrorMessage = "eendtime 為必填")]
        public string Eendtime { get; set; } = string.Empty;

        [Required(ErrorMessage = "ereason 為必填")]
        public string Ereason { get; set; } = string.Empty;

        [Required(ErrorMessage = "eprocess 為必填")]
        public string Eprocess { get; set; } = string.Empty; // C: 补休, P: 加班費

        public string? Efiletype { get; set; } // D: 加班單預申請附件檔

        /// <summary>
        /// 附件檔案編號
        /// 請先呼叫 http://54.46.24.34:5112/api/Attachment/Upload 上傳附件，
        /// 從回應中取得 tfileid（附件檔序號）後填入此欄位。
        /// </summary>
        public List<string>? Efileid { get; set; }
    }

    /// <summary>
    /// 加班單預申請回應 (API 1: /app/efotapply)
    /// </summary>
    public class EFotApplyResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
        
        /// <summary>
        /// 申請成功的表單ID (formid)
        /// </summary>
        public string? Formid { get; set; }
    }

    /// <summary>
    /// 加班確認申請列表請求 (API 2: /app/efotconfirm GET)
    /// </summary>
    public class EFotConfirmListRequest
    {
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 加班確認申請列表回應 (API 2: /app/efotconfirm GET)
    /// </summary>
    public class EFotConfirmListResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
        public EFotConfirmListData? Data { get; set; }
    }

    public class EFotConfirmListData
    {
        public List<EFotConfirmListItem> Efotdata { get; set; } = new();
    }

    public class EFotConfirmListItem
    {
        public string Uid { get; set; } = string.Empty;
        public string Uname { get; set; } = string.Empty;
        public string Udepartment { get; set; } = string.Empty;
        public string Formid { get; set; } = string.Empty;
        public string Estartdate { get; set; } = string.Empty;
        public string Estarttime { get; set; } = string.Empty;
        public string Eenddate { get; set; } = string.Empty;
        public string Eendtime { get; set; } = string.Empty;
        public string Ereason { get; set; } = string.Empty;
        public string Eprocess { get; set; } = string.Empty;
    }

    /// <summary>
    /// 加班單詳情預覽請求 (API 3: /app/efotpreview)
    /// </summary>
    public class EFotPreviewRequest
    {
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 加班單詳情預覽回應 (API 3: /app/efotpreview)
    /// </summary>
    public class EFotPreviewResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
        public EFotPreviewData? Data { get; set; }
    }

    public class EFotPreviewData
    {
        public string Formid { get; set; } = string.Empty;
        public string Estartdate { get; set; } = string.Empty;
        public string Estarttime { get; set; } = string.Empty;
        public string Eenddate { get; set; } = string.Empty;
        public string Eendtime { get; set; } = string.Empty;
        public string Ereason { get; set; } = string.Empty;
        public string Eprocess { get; set; } = string.Empty;
        public string? Efiletype { get; set; }
        public List<EFotAttachment>? Attachments { get; set; }
    }

    public class EFotAttachment
    {
        public string Efileid { get; set; } = string.Empty;
        public string Efilename { get; set; } = string.Empty;
        public string Esfilename { get; set; } = string.Empty;
        public string Efileurl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 加班單確認申請送出請求 (API 4: /app/efotconfirm POST)
    /// </summary>
    public class EFotConfirmSubmitRequest
    {
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;

        [Required(ErrorMessage = "astartdate 為必填")]
        public string Astartdate { get; set; } = string.Empty;

        [Required(ErrorMessage = "astarttime 為必填")]
        public string Astarttime { get; set; } = string.Empty;

        [Required(ErrorMessage = "aenddate 為必填")]
        public string Aenddate { get; set; } = string.Empty;

        [Required(ErrorMessage = "aendtime 為必填")]
        public string Aendtime { get; set; } = string.Empty;

        public string? Efiletype { get; set; } // D: 加班單預申請附件檔

        /// <summary>
        /// 附件檔案編號
        /// 請先呼叫 http://54.46.24.34:5112/api/Attachment/Upload 上傳附件，
        /// 從回應中取得 tfileid（附件檔序號）後填入此欄位。
        /// </summary>
        public List<string>? Efileid { get; set; }
    }

    /// <summary>
    /// 加班單確認申請送出回應 (API 4: /app/efotconfirm POST)
    /// </summary>
    public class EFotConfirmSubmitResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
    }

    #region API: 加班確認提交 (fotconfirm POST)

    /// <summary>
    /// 加班確認提交請求 (POST /app/fotconfirm)
    /// 提交實際發生的加班申請表單，填具實際的加班時間及所需附件後送出
    /// </summary>
    public class FotConfirmSubmitRequest
    {
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班起始日期 (格式: yyyy-MM-dd)
        /// </summary>
        [Required(ErrorMessage = "astartdate 為必填")]
        public string Astartdate { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班起始時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "astarttime 為必填")]
        public string Astarttime { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束日期 (格式: yyyy-MM-dd)
        /// </summary>
        [Required(ErrorMessage = "aenddate 為必填")]
        public string Aenddate { get; set; } = string.Empty;

        /// <summary>
        /// 實際加班結束時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "aendtime 為必填")]
        public string Aendtime { get; set; } = string.Empty;

        /// <summary>
        /// 附件類型 (D: 加班單確認附件檔)
        /// </summary>
        public string? Efiletype { get; set; }

        /// <summary>
        /// 附件檔案ID列表
        /// </summary>
        public List<string>? Efileid { get; set; }
    }

    /// <summary>
    /// 加班確認提交回應 (POST /app/fotconfirm)
    /// </summary>
    public class FotConfirmSubmitResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
    }

    #endregion

    #region API 5: 代理人資料 (getagent)

    /// <summary>
    /// 代理人資料查詢請求 (API 5: /app/getagent POST)
    /// </summary>
    public class GetAgentRequest
    {
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 代理人資料
    /// </summary>
    public class AgentData
    {
        /// <summary>
        /// 代理人部門
        /// </summary>
        public string Agentdept { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號
        /// </summary>
        public string Agentid { get; set; } = string.Empty;

        /// <summary>
        /// 代理人姓名
        /// </summary>
        public string Agentname { get; set; } = string.Empty;
    }

    /// <summary>
    /// 代理人資料查詢回應 (API 5: /app/getagent POST)
    /// </summary>
    public class GetAgentResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
        public AgentDataWrapper? Data { get; set; }
    }

    /// <summary>
    /// 代理人資料包裝器
    /// </summary>
    public class AgentDataWrapper
    {
        public List<AgentData> Agentdata { get; set; } = new List<AgentData>();
    }

    #endregion
}