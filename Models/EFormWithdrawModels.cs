using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 簽核記錄撤回作業請求
    /// </summary>
    public class EFormWithdrawRequest
    {
        [Required]
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("comments")]
        public string Comments { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("eformtype")]
        public string EFormType { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("eformid")]
        public string EFormId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 簽核記錄撤回作業回應
    /// </summary>
    public class EFormWithdrawResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public EFormWithdrawData? Data { get; set; }
    }

    public class EFormWithdrawData
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
