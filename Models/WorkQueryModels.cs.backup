using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 個人考勤查詢請求
    /// </summary>
    public class WorkQueryRequest
    {
        /// <summary>
        /// Token標記
        /// </summary>
        [Required(ErrorMessage = "tokenid 不可為空")]
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司
        /// </summary>
        [Required(ErrorMessage = "cid 不可為空")]
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號
        /// </summary>
        [Required(ErrorMessage = "uid 不可為空")]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 查詢年月份 (格式: yyyy-MM，例如: 2025-09)
        /// </summary>
        [Required(ErrorMessage = "wyearmonth 不可為空")]
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "wyearmonth 格式不正確，請使用 yyyy-MM 格式")]
        [JsonPropertyName("wyearmonth")]
        public string WYearMonth { get; set; } = string.Empty;
    }

    /// <summary>
    /// 個人考勤查詢回應
    /// </summary>
    public class WorkQueryResponse
    {
        /// <summary>
        /// 回應代碼 (200: 成功, 500: 錯誤)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 回應訊息
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 考勤資料
        /// </summary>
        [JsonPropertyName("data")]
        public WorkQueryData? Data { get; set; }
    }

    /// <summary>
    /// 考勤資料
    /// </summary>
    public class WorkQueryData
    {
        /// <summary>
        /// 考勤資料查詢年度
        /// </summary>
        [JsonPropertyName("ryear")]
        public string RYear { get; set; } = string.Empty;

        /// <summary>
        /// 考勤資料查詢月份
        /// </summary>
        [JsonPropertyName("rmonth")]
        public string RMonth { get; set; } = string.Empty;

        /// <summary>
        /// 當月考勤資料
        /// </summary>
        [JsonPropertyName("records")]
        public List<WorkQueryRecord> Records { get; set; } = new List<WorkQueryRecord>();
    }

    /// <summary>
    /// 考勤記錄
    /// </summary>
    public class WorkQueryRecord
    {
        /// <summary>
        /// 考勤日期 (格式: yyyy-MM-dd)
        /// </summary>
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 全天考勤狀況 (T: 正常（綠燈）, F: 異常（紅燈）)
        /// </summary>
        [JsonPropertyName("onduty")]
        public string OnDuty { get; set; } = string.Empty;

        /// <summary>
        /// 標準上班時間 (格式: HH:mm:ss)
        /// </summary>
        [JsonPropertyName("clockin")]
        public string ClockIn { get; set; } = string.Empty;

        /// <summary>
        /// 實際上班時間 (格式: HH:mm:ss，空白表示未打卡)
        /// </summary>
        [JsonPropertyName("checkin")]
        public string CheckIn { get; set; } = string.Empty;

        /// <summary>
        /// 上班狀態 (T: 正常, F: 異常, 空白: 未打卡)
        /// </summary>
        [JsonPropertyName("statusin")]
        public string StatusIn { get; set; } = string.Empty;

        /// <summary>
        /// 標準下班時間 (格式: HH:mm:ss)
        /// </summary>
        [JsonPropertyName("clockout")]
        public string ClockOut { get; set; } = string.Empty;

        /// <summary>
        /// 實際下班時間 (格式: HH:mm:ss，空白表示未打卡)
        /// </summary>
        [JsonPropertyName("checkout")]
        public string CheckOut { get; set; } = string.Empty;

        /// <summary>
        /// 下班狀態 (T: 正常, F: 異常, 空白: 未打卡)
        /// </summary>
        [JsonPropertyName("statusout")]
        public string StatusOut { get; set; } = string.Empty;
    }
}
