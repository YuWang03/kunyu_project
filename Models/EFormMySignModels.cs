using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region 我的簽核列表 (POST /app/eformmysign)

    /// <summary>
    /// 我的簽核列表請求
    /// </summary>
    public class EFormMySignRequest
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

        [JsonPropertyName("page")]
        public string Page { get; set; } = "1";

        [JsonPropertyName("pagesize")]
        public string PageSize { get; set; } = "20";

        [JsonPropertyName("eformtype")]
        public string EFormType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 我的簽核列表回應
    /// </summary>
    public class EFormMySignResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public EFormMySignData? Data { get; set; }
    }

    public class EFormMySignData
    {
        [JsonPropertyName("totalCount")]
        public string TotalCount { get; set; } = "0";

        [JsonPropertyName("page")]
        public string Page { get; set; } = "1";

        [JsonPropertyName("pageSize")]
        public string PageSize { get; set; } = "20";

        [JsonPropertyName("totalPages")]
        public string TotalPages { get; set; } = "0";

        [JsonPropertyName("eformdata")]
        public List<EFormMySignItem> EFormData { get; set; } = new();
    }

    public class EFormMySignItem
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

        [JsonPropertyName("eendtitle")]
        public string EEndTitle { get; set; } = string.Empty;

        [JsonPropertyName("eenddate")]
        public string EEndDate { get; set; } = string.Empty;

        [JsonPropertyName("eendtime")]
        public string EEndTime { get; set; } = string.Empty;

        [JsonPropertyName("estartdate")]
        public string EStartDate { get; set; } = string.Empty;

        [JsonPropertyName("estarttime")]
        public string EStartTime { get; set; } = string.Empty;

        [JsonPropertyName("ereasontitle")]
        public string EReasonTitle { get; set; } = "事由";

        [JsonPropertyName("ereason")]
        public string EReason { get; set; } = string.Empty;

        [JsonPropertyName("applicantuname")]
        public string ApplicantUName { get; set; } = string.Empty;

        [JsonPropertyName("applicantdepartment")]
        public string ApplicantDepartment { get; set; } = string.Empty;

        [JsonPropertyName("signaction")]
        public string SignAction { get; set; } = string.Empty;

        [JsonPropertyName("signdate")]
        public string SignDate { get; set; } = string.Empty;
    }

    #endregion
}
