using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    public class EFormReviewRequest
    {
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;
    }

    public class EFormReviewResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = "請求成功";
        [JsonPropertyName("data")]
        public EFormReviewData? Data { get; set; }
    }

    public class EFormReviewData
    {
        [JsonPropertyName("eformdata")]
        public List<EFormReviewItem> EFormData { get; set; } = new();
    }

    public class EFormReviewItem
    {
        [JsonPropertyName("uname")]
        public string UName { get; set; } = "";
        [JsonPropertyName("udepartment")]
        public string UDepartment { get; set; } = "";
        [JsonPropertyName("formidtitle")]
        public string FormIdTitle { get; set; } = "表單編號";
        [JsonPropertyName("formid")]
        public string FormId { get; set; } = "";
        [JsonPropertyName("eformtypetitle")]
        public string EFormTypeTitle { get; set; } = "申請類別";
        [JsonPropertyName("eformtype ")]
        public string EFormType { get; set; } = "";
        [JsonPropertyName("eformname ")]
        public string EFormName { get; set; } = "";
        [JsonPropertyName("estarttitle ")]
        public string EStartTitle { get; set; } = "起始時間";
        [JsonPropertyName("estartdate")]
        public string EStartDate { get; set; } = "";
        [JsonPropertyName("estarttime")]
        public string EStartTime { get; set; } = "";
        [JsonPropertyName("endtitle ")]
        public string EndTitle { get; set; } = "結束時間";
        [JsonPropertyName("eenddate")]
        public string EEndDate { get; set; } = "";
        [JsonPropertyName("eendtime")]
        public string EEndTime { get; set; } = "";
        [JsonPropertyName("ereasontitle")]
        public string EReasonTitle { get; set; } = "事由";
        [JsonPropertyName("ereason")]
        public string EReason { get; set; } = "";
    }
}
