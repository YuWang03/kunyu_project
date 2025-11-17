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
}