using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region 簽核記錄詳細資料 (POST /app/eformrecord)

    /// <summary>
    /// 簽核記錄詳細資料請求
    /// </summary>
    public class EFormRecordRequest
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
    /// 簽核記錄詳細資料回應
    /// </summary>
    public class EFormRecordResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public EFormRecordData? Data { get; set; }
    }

    /// <summary>
    /// 簽核記錄詳細資料
    /// </summary>
    public class EFormRecordData
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
        public List<EFormRecordAttachment> Attachments { get; set; } = new();

        [JsonPropertyName("eformflow")]
        public List<EFormRecordFlow> EFormFlow { get; set; } = new();
    }

    /// <summary>
    /// 表單附件
    /// </summary>
    public class EFormRecordAttachment
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

    /// <summary>
    /// 表單流程
    /// </summary>
    public class EFormRecordFlow
    {
        [JsonPropertyName("workitem")]
        public string WorkItem { get; set; } = string.Empty;

        [JsonPropertyName("workstatus")]
        public string WorkStatus { get; set; } = string.Empty;
    }

    #endregion
}
