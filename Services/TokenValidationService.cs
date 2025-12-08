using System.Text;
using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// Token 驗證服務實作
    /// </summary>
    public class TokenValidationService : ITokenValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenValidationService> _logger;
        private readonly string _tokenVerifyUrl;

        public TokenValidationService(
            IHttpClientFactory httpClientFactory,
            ILogger<TokenValidationService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            
            // 從設定檔讀取 Token 驗證 API 的 URL，如果沒有設定則使用預設值
            _tokenVerifyUrl = configuration["TokenValidation:VerifyUrl"] 
                ?? "http://54.46.24.34:5112/api/Tokenid/Verify";
        }

        /// <summary>
        /// 驗證 Token 是否有效
        /// </summary>
        public async Task<TokenVerifyResponse> ValidateTokenAsync(string tokenId, string uid, string cid)
        {
            try
            {
                // 使用專用的 TokenValidationClient（已設定 30 秒超時）
                var client = _httpClientFactory.CreateClient("TokenValidationClient");

                var request = new TokenVerifyRequest
                {
                    TokenId = tokenId,
                    Uid = uid,
                    Cid = cid
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("呼叫 Token 驗證 API: {Url}", _tokenVerifyUrl);
                _logger.LogDebug("驗證請求內容: TokenId={TokenId}, Uid={Uid}, Cid={Cid}", 
                    tokenId, uid, cid);

                var response = await client.PostAsync(_tokenVerifyUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Token 驗證 API 回應: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TokenVerifyResponse>(responseContent);
                    
                    if (result != null)
                    {
                        _logger.LogInformation("Token 驗證結果: Code={Code}, Msg={Msg}", 
                            result.Code, result.Msg);
                        return result;
                    }
                }

                _logger.LogWarning("Token 驗證 API 呼叫失敗: StatusCode={StatusCode}, Response={Response}", 
                    response.StatusCode, responseContent);

                return new TokenVerifyResponse
                {
                    Code = "300",
                    Msg = "Token 驗證服務無法連線或回應異常"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token 驗證過程發生例外");
                return new TokenVerifyResponse
                {
                    Code = "300",
                    Msg = $"Token 驗證失敗: {ex.Message}"
                };
            }
        }
    }
}
