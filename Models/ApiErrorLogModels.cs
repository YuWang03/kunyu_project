using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// API 錯誤日誌資料表模型
    /// </summary>
    [Table("api_error_logs")]
    public class ApiErrorLog
    {
        /// <summary>
        /// 主鍵 ID
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// API 端點
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// HTTP 方法 (GET, POST, PUT, DELETE, etc.)
        /// </summary>
        [MaxLength(10)]
        [Column("http_method")]
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// 請求參數 (JSON 格式)
        /// </summary>
        [Column("request_body", TypeName = "text")]
        public string? RequestBody { get; set; }

        /// <summary>
        /// 錯誤代碼
        /// </summary>
        [MaxLength(10)]
        [Column("error_code")]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        [Column("error_message", TypeName = "text")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 詳細錯誤資訊 (Exception 詳細資訊)
        /// </summary>
        [Column("error_details", TypeName = "text")]
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// 堆疊追蹤
        /// </summary>
        [Column("stack_trace", TypeName = "text")]
        public string? StackTrace { get; set; }

        /// <summary>
        /// 使用者 ID
        /// </summary>
        [MaxLength(50)]
        [Column("user_id")]
        public string? UserId { get; set; }

        /// <summary>
        /// 公司 ID
        /// </summary>
        [MaxLength(50)]
        [Column("company_id")]
        public string? CompanyId { get; set; }

        /// <summary>
        /// Token ID
        /// </summary>
        [MaxLength(100)]
        [Column("token_id")]
        public string? TokenId { get; set; }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        [MaxLength(50)]
        [Column("client_ip")]
        public string? ClientIp { get; set; }

        /// <summary>
        /// 用戶端 User-Agent
        /// </summary>
        [MaxLength(500)]
        [Column("user_agent")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 附加資訊 (JSON 格式)
        /// </summary>
        [Column("additional_info", TypeName = "text")]
        public string? AdditionalInfo { get; set; }
    }
}
