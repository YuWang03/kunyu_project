namespace HRSystemAPI.Models
{
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

    /// <summary>
    /// SMTP 郵件設定
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資驗證碼設定
    /// </summary>
    public class SalaryVerificationSettings
    {
        public int CodeExpirationMinutes { get; set; } = 5;
        public string TestVerificationCode { get; set; } = "0000";
    }
}