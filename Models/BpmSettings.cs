namespace HRSystemAPI.Models
{
    /// <summary>
    /// BPM 系統設定
    /// </summary>
    public class BpmSettings
    {
        /// <summary>
        /// BPM API 基礎 URL
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// API Key（放在 Header 的 X-API-Key）
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API Secret（放在 Header 的 X-API-Secret）
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// 連線逾時時間（秒）
        /// </summary>
        public int Timeout { get; set; } = 30;
    }

    /// <summary>
    /// FTP 設定
    /// </summary>
    public class FtpSettings
    {
        /// <summary>
        /// FTP 主機位址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// FTP 埠號
        /// </summary>
        public int Port { get; set; } = 21;

        /// <summary>
        /// FTP 使用者名稱
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// FTP 密碼
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 上傳路徑
        /// </summary>
        public string UploadPath { get; set; } = "/uploads/attachments/";
    }
}