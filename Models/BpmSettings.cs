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
    /// BPM 同步流程信息請求
    /// </summary>
    public class BpmSyncProcessInfoRequest
    {
        /// <summary>
        /// 表單編號（processSerialNo）
        /// 例如: PI_Leave_Test00000225
        /// </summary>
        public string ProcessSerialNo { get; set; } = string.Empty;

        /// <summary>
        /// 處理程序代碼（processCode）
        /// 例如: PI_LEAVE_001_PROCESS
        /// </summary>
        public string ProcessCode { get; set; } = string.Empty;

        /// <summary>
        /// 環境代碼（environment）
        /// 可選值: TEST, PROD
        /// </summary>
        public string Environment { get; set; } = "TEST";
    }

    /// <summary>
    /// BPM 流程信息
    /// </summary>
    public class BpmProcessInfo
    {
        /// <summary>
        /// 流程 ID
        /// </summary>
        public string ProcessId { get; set; } = string.Empty;

        /// <summary>
        /// 流程名稱
        /// 例如: 請假單/掛號流程
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 流程代碼
        /// 例如: PI_LEAVE_001_PROCESS
        /// </summary>
        public string ProcessCode { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string FormSerialNo { get; set; } = string.Empty;

        /// <summary>
        /// 表單狀態
        /// </summary>
        public string FormStatus { get; set; } = string.Empty;

        /// <summary>
        /// 當前節點
        /// </summary>
        public string CurrentNode { get; set; } = string.Empty;

        /// <summary>
        /// 流程進度（百分比）
        /// </summary>
        public int Progress { get; set; } = 0;
    }

    /// <summary>
    /// BPM 同步流程信息回應
    /// </summary>
    public class BpmSyncProcessInfoResponse
    {
        /// <summary>
        /// 回應狀態碼（200=成功, 其他=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 回應訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 流程信息
        /// </summary>
        public BpmProcessInfo? ProcessInfo { get; set; }
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