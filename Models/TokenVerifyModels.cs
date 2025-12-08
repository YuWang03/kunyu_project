using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// Token 驗證請求模型
    /// </summary>
    public class TokenVerifyRequest
    {
        /// <summary>
        /// Token標記
        /// </summary>
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 員工工號
        /// </summary>
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司
        /// </summary>
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token 驗證回應模型
    /// </summary>
    public class TokenVerifyResponse
    {
        /// <summary>
        /// 成功/失敗代碼 (200: 驗證成功, 300: 授權失敗)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息內容
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;
    }
}
