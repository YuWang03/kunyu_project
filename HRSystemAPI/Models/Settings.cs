namespace HRSystemAPI.Models
{
    /// <summary>
    /// BPM 系統設定
    /// </summary>
    public class BpmSettings
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// FTP 伺服器設定
    /// </summary>
    public class FtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UploadPath { get; set; } = "/uploads/attachments/";
    }

    /// <summary>
    /// Keycloak OIDC 設定
    /// </summary>
    public class KeycloakSettings
    {
        public string Authority { get; set; } = "https://待確認-keycloak-server/auth/realms/{realm}";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = "/protocol/openid-connect/token";
        public string UserInfoEndpoint { get; set; } = "/protocol/openid-connect/userinfo";
    }
}