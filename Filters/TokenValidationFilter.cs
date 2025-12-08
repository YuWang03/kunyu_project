using HRSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace HRSystemAPI.Filters
{
    /// <summary>
    /// Token 驗證過濾器 - 自動驗證每個 API 請求的 Token
    /// </summary>
    public class TokenValidationFilter : IAsyncActionFilter
    {
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<TokenValidationFilter> _logger;

        public TokenValidationFilter(
            ITokenValidationService tokenValidationService,
            ILogger<TokenValidationFilter> logger)
        {
            _tokenValidationService = tokenValidationService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                // 從請求 Body 中取得 tokenid, uid, cid
                string? tokenId = null;
                string? uid = null;
                string? cid = null;

                // 檢查是否有 Body 參數
                if (context.ActionArguments.Count > 0)
                {
                    var requestBody = context.ActionArguments.FirstOrDefault().Value;
                    
                    if (requestBody != null)
                    {
                        // 使用反射取得 tokenid, uid, cid 屬性
                        var type = requestBody.GetType();
                        
                        var tokenIdProp = type.GetProperty("TokenId") ?? type.GetProperty("Tokenid") ?? type.GetProperty("tokenid");
                        var uidProp = type.GetProperty("Uid") ?? type.GetProperty("uid");
                        var cidProp = type.GetProperty("Cid") ?? type.GetProperty("cid");

                        tokenId = tokenIdProp?.GetValue(requestBody)?.ToString();
                        uid = uidProp?.GetValue(requestBody)?.ToString();
                        cid = cidProp?.GetValue(requestBody)?.ToString();
                    }
                }

                // 檢查必要參數是否存在
                if (string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(uid) || 
                    string.IsNullOrWhiteSpace(cid))
                {
                    _logger.LogWarning("Token 驗證失敗: 缺少必要參數 (tokenId={TokenId}, uid={Uid}, cid={Cid})", 
                        tokenId, uid, cid);

                    context.Result = new JsonResult(new
                    {
                        code = "400",
                        msg = "缺少必要參數: tokenid, uid, cid"
                    })
                    {
                        StatusCode = 400
                    };
                    return;
                }

                // 呼叫 Token 驗證服務
                var verifyResult = await _tokenValidationService.ValidateTokenAsync(tokenId, uid, cid);

                // 檢查驗證結果
                if (verifyResult.Code != "200")
                {
                    _logger.LogWarning("Token 驗證失敗: {Msg} (uid={Uid})", verifyResult.Msg, uid);

                    context.Result = new JsonResult(new
                    {
                        code = verifyResult.Code,
                        msg = verifyResult.Msg
                    })
                    {
                        StatusCode = 401 // Unauthorized
                    };
                    return;
                }

                _logger.LogInformation("Token 驗證成功: uid={Uid}, cid={Cid}", uid, cid);

                // Token 驗證通過，繼續執行 Action
                await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token 驗證過程發生例外");

                context.Result = new JsonResult(new
                {
                    code = "500",
                    msg = $"Token 驗證過程發生錯誤: {ex.Message}"
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
