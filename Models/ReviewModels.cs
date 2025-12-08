using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region 待我審核列表 (1. POST /app/eformreview)

    /// <summary>
    /// 待我審核列表請求
    /// </summary>
    public class ReviewListRequest
    {
        [Required]
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 待我審核列表回應
    /// </summary>
    public class ReviewListResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public ReviewListData? Data { get; set; }
    }

    public class ReviewListData
    {
        [JsonPropertyName("eformdata")]
        public List<ReviewListItem> EFormData { get; set; } = new();
    }

    public class ReviewListItem
    {
        [JsonPropertyName("uname")]
        public string UName { get; set; } = string.Empty;

        [JsonPropertyName("udepartment")]
        public string UDepartment { get; set; } = string.Empty;

        [JsonPropertyName("formidtitle")]
        public string FormIdTitle { get; set; } = "表單編號";

        [JsonPropertyName("formid")]
        public string FormId { get; set; } = string.Empty;

        [JsonPropertyName("eformtypetitle")]
        public string EFormTypeTitle { get; set; } = "申請類別";

        [JsonPropertyName("eformtype")]
        public string EFormType { get; set; } = string.Empty;

        [JsonPropertyName("eformname")]
        public string EFormName { get; set; } = string.Empty;

        [JsonPropertyName("estarttitle")]
        public string EStartTitle { get; set; } = string.Empty;

        [JsonPropertyName("estartdate")]
        public string EStartDate { get; set; } = string.Empty;

        [JsonPropertyName("estarttime")]
        public string EStartTime { get; set; } = string.Empty;

        [JsonPropertyName("eendtitle")]
        public string EEndTitle { get; set; } = string.Empty;

        [JsonPropertyName("eenddate")]
        public string EEndDate { get; set; } = string.Empty;

        [JsonPropertyName("eendtime")]
        public string EEndTime { get; set; } = string.Empty;

        [JsonPropertyName("ereasontitle")]
        public string EReasonTitle { get; set; } = "事由";

        [JsonPropertyName("ereason")]
        public string EReason { get; set; } = string.Empty;
    }

    #endregion

    #region 待我審核詳細資料 (2. POST /app/eformdetail)

    /// <summary>
    /// 待我審核詳細資料請求
    /// </summary>
    public class ReviewDetailRequest
    {
        [Required]
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("formid")]
        public string FormId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 待我審核詳細資料回應
    /// </summary>
    public class ReviewDetailResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public ReviewDetailData? Data { get; set; }
    }

    public class ReviewDetailData
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("uname")]
        public string UName { get; set; } = string.Empty;

        [JsonPropertyName("udepartment")]
        public string UDepartment { get; set; } = string.Empty;

        [JsonPropertyName("formidtitle")]
        public string FormIdTitle { get; set; } = "表單編號";

        [JsonPropertyName("formid")]
        public string FormId { get; set; } = string.Empty;

        [JsonPropertyName("eformtypetitle")]
        public string EFormTypeTitle { get; set; } = "申請類別";

        [JsonPropertyName("eformtype")]
        public string EFormType { get; set; } = string.Empty;

        [JsonPropertyName("eformname")]
        public string EFormName { get; set; } = string.Empty;

        [JsonPropertyName("estarttitle")]
        public string EStartTitle { get; set; } = string.Empty;

        [JsonPropertyName("estartdate")]
        public string EStartDate { get; set; } = string.Empty;

        [JsonPropertyName("estarttime")]
        public string EStartTime { get; set; } = string.Empty;

        [JsonPropertyName("eendtitle")]
        public string EEndTitle { get; set; } = string.Empty;

        [JsonPropertyName("eenddate")]
        public string EEndDate { get; set; } = string.Empty;

        [JsonPropertyName("eendtime")]
        public string EEndTime { get; set; } = string.Empty;

        [JsonPropertyName("ereasontitle")]
        public string EReasonTitle { get; set; } = "事由";

        [JsonPropertyName("ereason")]
        public string EReason { get; set; } = string.Empty;

        [JsonPropertyName("eagenttitle")]
        public string EAgentTitle { get; set; } = "代理人";

        [JsonPropertyName("eagent")]
        public string EAgent { get; set; } = string.Empty;

        [JsonPropertyName("efiletype")]
        public string EFileType { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<ReviewAttachment> Attachments { get; set; } = new();

        [JsonPropertyName("eformflow")]
        public List<ReviewFormFlow> EFormFlow { get; set; } = new();
    }

    public class ReviewAttachment
    {
        [JsonPropertyName("efileid")]
        public string EFileId { get; set; } = string.Empty;

        [JsonPropertyName("efilename")]
        public string EFileName { get; set; } = string.Empty;

        [JsonPropertyName("esfilename")]
        public string ESFileName { get; set; } = string.Empty;

        [JsonPropertyName("efileurl")]
        public string EFileUrl { get; set; } = string.Empty;
    }

    public class ReviewFormFlow
    {
        [JsonPropertyName("workitem")]
        public string WorkItem { get; set; } = string.Empty;

        [JsonPropertyName("workstatus")]
        public string WorkStatus { get; set; } = string.Empty;
    }

    #endregion

    #region 待我審核簽核作業 (3. POST /app/eformapproval)

    /// <summary>
    /// 待我審核簽核作業請求
    /// </summary>
    public class ReviewApprovalRequest
    {
        [Required]
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// 簽核狀態：同意 Y / 不同意 N（預設同意）
        /// </summary>
        [JsonPropertyName("approvalstatus")]
        public string ApprovalStatus { get; set; } = "Y";

        /// <summary>
        /// 簽核流程：中止流程 S / 退回發起人 R（預設空白）
        /// </summary>
        [JsonPropertyName("approvalflow")]
        public string ApprovalFlow { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("approvaldata")]
        public List<ReviewApprovalData> ApprovalData { get; set; } = new();
    }

    public class ReviewApprovalData
    {
        /// <summary>
        /// 表單類型：請假單 L / 銷假單 D / 外出外訓單 O / 加班單 A / 出勤單 R / 出差單 T
        /// </summary>
        [Required]
        [JsonPropertyName("eformtype")]
        public string EFormType { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("eformid")]
        public string EFormId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 待我審核簽核作業回應
    /// </summary>
    public class ReviewApprovalResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public ReviewApprovalResponseData? Data { get; set; }
    }

    public class ReviewApprovalResponseData
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    #endregion

    #region BPM API 相關 Models

    /// <summary>
    /// BPM 待辦事項清單回應
    /// </summary>
    public class BpmWorkItemsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("responseTimeMs")]
        public int? ResponseTimeMs { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("workItems")]
        public List<BpmWorkItem> WorkItems { get; set; } = new();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    public class BpmWorkItem
    {
        [JsonPropertyName("processSerialNumber")]
        public string ProcessSerialNumber { get; set; } = string.Empty;

        [JsonPropertyName("activityId")]
        public string ActivityId { get; set; } = string.Empty;

        [JsonPropertyName("workItemOID")]
        public string WorkItemOID { get; set; } = string.Empty;
    }

    #endregion
}
