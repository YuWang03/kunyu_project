using Microsoft.Data.SqlClient;
using System.Text.Json;
using Dapper;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 簽核記錄撤回服務實作
    /// </summary>
    public class EFormWithdrawService : IEFormWithdrawService
    {
        private readonly BpmService _bpmService;
        private readonly ILogger<EFormWithdrawService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _hrDatabaseConnectionString;

        public EFormWithdrawService(
            BpmService bpmService, 
            ILogger<EFormWithdrawService> logger,
            IConfiguration configuration)
        {
            _bpmService = bpmService;
            _logger = logger;
            _configuration = configuration;
            _hrDatabaseConnectionString = configuration.GetConnectionString("HRDatabase") ?? "";
        }

        public async Task<EFormWithdrawResponse> WithdrawFormAsync(EFormWithdrawRequest request)
        {
            try
            {
                _logger.LogInformation("開始執行表單撤回，UID: {Uid}, FormId: {FormId}", request.Uid, request.EFormId);

                // 呼叫 BPM batch/abort-processes API
                var abortRequest = new
                {
                    items = new[]
                    {
                        new
                        {
                            processInstanceSerialNo = request.EFormId,
                            userId = request.Uid,
                            abortComment = request.Comments,
                            environment = "TEST"
                        }
                    }
                };

                _logger.LogInformation("呼叫 BPM batch/abort-processes API");
                var abortResponseJson = await _bpmService.PostAsync("bpm/batch/abort-processes", abortRequest);
                _logger.LogDebug("BPM batch abort 回應: {Response}", abortResponseJson);

                var abortResponse = JsonSerializer.Deserialize<JsonElement>(abortResponseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 檢查回應是否成功
                bool isSuccess = false;
                string errorMessage = "撤回失敗";

                if (abortResponse.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    var firstResult = results.EnumerateArray().FirstOrDefault();
                    if (firstResult.ValueKind != JsonValueKind.Undefined)
                    {
                        if (firstResult.TryGetProperty("success", out var successElement))
                        {
                            isSuccess = successElement.GetBoolean();
                        }
                        
                        if (firstResult.TryGetProperty("message", out var msgElement))
                        {
                            errorMessage = msgElement.GetString() ?? errorMessage;
                        }
                    }
                }
                else if (abortResponse.TryGetProperty("status", out var statusElement))
                {
                    isSuccess = statusElement.GetString() == "SUCCESS";
                    if (abortResponse.TryGetProperty("message", out var msgElement))
                    {
                        errorMessage = msgElement.GetString() ?? errorMessage;
                    }
                }

                if (isSuccess)
                {
                    _logger.LogInformation("表單撤回成功: {FormId}", request.EFormId);
                    return new EFormWithdrawResponse 
                    { 
                        Code = "200", 
                        Msg = "請求成功", 
                        Data = new EFormWithdrawData { Status = "請求成功" } 
                    };
                }
                else
                {
                    _logger.LogWarning("表單撤回失敗: {FormId}, 原因: {Message}", request.EFormId, errorMessage);
                    
                    // 檢查是否為流程已結束的錯誤
                    if (errorMessage.Contains("terminated") || errorMessage.Contains("aborted") || 
                        errorMessage.Contains("已結束") || errorMessage.Contains("已撤回"))
                    {
                        return new EFormWithdrawResponse 
                        { 
                            Code = "203", 
                            Msg = "請求失敗，此表單已結束或已撤回" 
                        };
                    }
                    
                    return new EFormWithdrawResponse 
                    { 
                        Code = "203", 
                        Msg = $"請求失敗: {errorMessage}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "表單撤回失敗: {FormId}", request.EFormId);
                return new EFormWithdrawResponse 
                { 
                    Code = "500", 
                    Msg = "請求超時或查無資料" 
                };
            }
        }

    }
}
