using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 服務 - 使用 Header 認證（X-API-Key 和 X-API-Secret）
    /// </summary>
    public class BpmService
    {
        private readonly HttpClient _httpClient;
        private readonly BpmSettings _bpmSettings;
        private readonly ILogger<BpmService> _logger;

        public BpmService(
            IHttpClientFactory httpClientFactory,
            IOptions<BpmSettings> bpmSettings,
            ILogger<BpmService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BpmClient");
            _bpmSettings = bpmSettings.Value;
            _logger = logger;

            _logger.LogInformation("BPM Service 已初始化，BaseUrl: {BaseUrl}", _bpmSettings.ApiBaseUrl);
        }

        /// <summary>
        /// <summary>
        /// ⚠️ 已棄用：透過 Email 取得 BPM UserID
        /// BPM 中間件不支援此端點，請改用 IBasicInfoService.GetEmployeeByEmailAsync() 查詢員工工號
        /// </summary>
        [Obsolete("BPM 中間件不支援 user/by-email 端點。請改用 IBasicInfoService.GetEmployeeByEmailAsync() 查詢員工工號。")]
        public async Task<string> GetUserIdByEmailAsync(string email)
        {
            _logger.LogWarning("⚠️ GetUserIdByEmailAsync 已棄用。BPM 中間件不支援 user/by-email 端點。");
            _logger.LogWarning("請改用 IBasicInfoService.GetEmployeeByEmailAsync() 查詢員工工號。");
            
            throw new NotSupportedException(
                "BPM 中間件不支援 user/by-email 端點。" +
                "請改用 IBasicInfoService.GetEmployeeByEmailAsync() 查詢員工工號。"
            );
        }

        /// <summary>
        /// GET 請求（通用方法）
        /// </summary>
        public async Task<string> GetAsync(string endpoint)
        {
            try
            {
                _logger.LogInformation("發送 GET 請求: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GET 請求失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"GET 請求失敗: {response.StatusCode} - {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("GET 請求成功: {Response}", responseBody);
                
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET 請求時發生錯誤: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// POST 請求（通用方法）
        /// </summary>
        public async Task<string> PostAsync(string endpoint, object data)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("發送 POST 請求: {Endpoint}, 資料: {Data}", endpoint, jsonContent);

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("POST 請求失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"POST 請求失敗: {response.StatusCode} - {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("POST 請求成功: {Response}", responseBody);
                
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST 請求時發生錯誤: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// POST 請求（不使用 CamelCase - 用於特殊格式如 formDataMap）
        /// </summary>
        public async Task<string> PostAsyncWithoutCamelCase(string endpoint, object data)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null,  // 不轉換大小寫，保持原始名稱
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("發送 POST 請求: {Endpoint}, 資料: {Data}", endpoint, jsonContent);

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("POST 請求失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"POST 請求失敗: {response.StatusCode} - {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("POST 請求成功: {Response}", responseBody);
                
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST 請求時發生錯誤: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// PUT 請求（通用方法）
        /// </summary>
        public async Task<string> PutAsync(string endpoint, object data)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("發送 PUT 請求: {Endpoint}, 資料: {Data}", endpoint, jsonContent);

                var response = await _httpClient.PutAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("PUT 請求失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"PUT 請求失敗: {response.StatusCode} - {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("PUT 請求成功: {Response}", responseBody);
                
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUT 請求時發生錯誤: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// 測試 BPM 連線
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("測試 BPM 連線: {BaseUrl}", _bpmSettings.ApiBaseUrl);

                // 使用提供的測試端點
                var testEndpoint = "system-configs/APP/test-auth";

                var response = await _httpClient.GetAsync(testEndpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                var isConnected = response.IsSuccessStatusCode;

                _logger.LogInformation("BPM 連線測試結果: {Result}, StatusCode: {StatusCode}, Response: {Response}",
                    isConnected ? "成功" : "失敗", response.StatusCode, responseBody);

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試 BPM 連線時發生錯誤");
                return false;
            }
        }

        /// <summary>
        /// 取得 BPM API 的完整 URL（用於除錯）
        /// </summary>
        public string GetFullUrl(string endpoint)
        {
            return $"{_bpmSettings.ApiBaseUrl}{endpoint}";
        }

        /// <summary>
        /// 查詢公司啟用的表單列表
        /// </summary>
        public async Task<string> GetCompanyFormsAsync(string companyCode)
        {
            try
            {
                _logger.LogInformation("查詢公司啟用表單: {CompanyCode}", companyCode);

                var endpoint = $"forms/by-company/{companyCode}";
                return await GetAsync(endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢公司表單時發生錯誤: {CompanyCode}", companyCode);
                throw;
            }
        }

        /// <summary>
        /// 查詢表單欄位映射
        /// </summary>
        public async Task<string> GetFormFieldMappingsAsync(string formCode, string version)
        {
            try
            {
                _logger.LogInformation("查詢表單欄位映射: {FormCode}, Version: {Version}", formCode, version);

                var endpoint = $"forms/{formCode}/versions/{version}/field-mappings";
                return await GetAsync(endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢表單欄位映射時發生錯誤: {FormCode}", formCode);
                throw;
            }
        }
    }
}