using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 考勤超時出勤設定請求 - POST /app/workset
    /// </summary>
    public class WorkSetRequest
    {
        /// <summary>
        /// Token標記（必填）
        /// </summary>
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司（必填）
        /// </summary>
        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號（必填）
        /// </summary>
        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 設定年月日（必填）格式：YYYY-MM-DD
        /// </summary>
        [Required(ErrorMessage = "wdate 為必填")]
        public string Wdate { get; set; } = string.Empty;

        /// <summary>
        /// 事由（必填）
        /// </summary>
        [Required(ErrorMessage = "reason 為必填")]
        public string Reason { get; set; } = string.Empty;
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 考勤超時出勤設定回應 - POST /app/workset
    /// </summary>
    public class WorkSetResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 500=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息內容
        /// </summary>
        public string Msg { get; set; } = string.Empty;
    }

    #endregion
}
