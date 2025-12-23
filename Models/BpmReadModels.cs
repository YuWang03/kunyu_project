using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region BPM Read API Models

    /// <summary>
    /// BPM Read API 請求模型
    /// POST /app/bpmread
    /// </summary>
    public class BpmReadRequest
    {
        /// <summary>
        /// 限定第三方識別碼 (廣宇專用)
        /// </summary>
        [JsonPropertyName("bskey")]
        public string? Bskey { get; set; }

        /// <summary>
        /// BPM 來源公司編號
        /// </summary>
        [JsonPropertyName("companyid")]
        public string? CompanyId { get; set; }

        /// <summary>
        /// BPM 表單資料陣列
        /// </summary>
        [JsonPropertyName("bpmdata")]
        public List<BpmDataItem>? BpmData { get; set; }
    }

    /// <summary>
    /// BPM 表單資料項目
    /// </summary>
    public class BpmDataItem
    {
        /// <summary>
        /// 員工工號
        /// </summary>
        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        /// <summary>
        /// 表單格式代碼
        /// </summary>
        [JsonPropertyName("formCode")]
        public string? FormCode { get; set; }

        /// <summary>
        /// 表單版本
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// 表單處理單號
        /// </summary>
        [JsonPropertyName("processSerialNo")]
        public string? ProcessSerialNo { get; set; }
    }

    /// <summary>
    /// BPM Read API 回應模型
    /// </summary>
    public class BpmReadResponse
    {
        /// <summary>
        /// 是否成功 (依 code 定義內容)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 失敗訊息 (依 code 回覆對應訊息)
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = "成功";

        /// <summary>
        /// 數據返回區 (成功:有此區 / 失敗:無此區)
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BpmReadResponseData? Data { get; set; }
    }

    /// <summary>
    /// BPM Read 回應資料
    /// </summary>
    public class BpmReadResponseData
    {
        /// <summary>
        /// 狀態訊息
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "請求成功";
    }

    #endregion

    #region BPM Middleware Response Models

    /// <summary>
    /// BPM 中間件流程資訊回應
    /// </summary>
    public class BpmProcessInfoResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public BpmProcessData? Data { get; set; }
    }

    /// <summary>
    /// BPM 流程資料
    /// </summary>
    public class BpmProcessData
    {
        [JsonPropertyName("processSerialNo")]
        public string? ProcessSerialNo { get; set; }

        [JsonPropertyName("processCode")]
        public string? ProcessCode { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("applicantId")]
        public string? ApplicantId { get; set; }

        [JsonPropertyName("applicantName")]
        public string? ApplicantName { get; set; }

        [JsonPropertyName("applicantDepartment")]
        public string? ApplicantDepartment { get; set; }

        [JsonPropertyName("createTime")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("formData")]
        public Dictionary<string, object>? FormData { get; set; }

        [JsonPropertyName("approvalHistory")]
        public List<BpmApprovalRecord>? ApprovalHistory { get; set; }
    }

    /// <summary>
    /// BPM 簽核記錄
    /// </summary>
    public class BpmApprovalRecord
    {
        [JsonPropertyName("sequence")]
        public int? Sequence { get; set; }

        [JsonPropertyName("approverId")]
        public string? ApproverId { get; set; }

        [JsonPropertyName("approverName")]
        public string? ApproverName { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("actionTime")]
        public DateTime? ActionTime { get; set; }
    }

    #endregion
}
