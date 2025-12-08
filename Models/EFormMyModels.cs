using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region 我的表單列表 (POST /app/eformmy)

    /// <summary>
    /// 我的表單列表請求
    /// </summary>
    public class EFormMyRequest
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
    /// 我的表單列表回應
    /// </summary>
    public class EFormMyResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public EFormMyData? Data { get; set; }
    }

    public class EFormMyData
    {
        [JsonPropertyName("eformdata")]
        public List<EFormMyItem> EFormData { get; set; } = new();
    }

    public class EFormMyItem
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

        [JsonPropertyName("eformtype ")]
        public string EFormType { get; set; } = string.Empty;

        [JsonPropertyName("eformname ")]
        public string EFormName { get; set; } = string.Empty;

        [JsonPropertyName("estarttitle ")]
        public string EStartTitle { get; set; } = string.Empty;

        [JsonPropertyName("estartdate")]
        public string EStartDate { get; set; } = string.Empty;

        [JsonPropertyName("estarttime")]
        public string EStartTime { get; set; } = string.Empty;

        [JsonPropertyName("eendtitle ")]
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
}
