using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
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

            // 設定 API Token 認證（如果有提供）
            if (!string.IsNullOrEmpty(_bpmSettings.ApiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bpmSettings.ApiToken);
            }
        }

        /// <summary>
        /// 透過 Email 取得 BPM UserID
        /// </summary>
        public async Task<string> GetUserIdByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("正在查詢 Email: {Email} 的 UserID", email);

                var response = await _httpClient.GetAsync($"/user/by-email?email={Uri.EscapeDataString(email)}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("BPM API 回應錯誤: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"無法取得 UserID，BPM API 回應: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var userId = result.GetProperty("userId").GetString();

                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception($"Email {email} 找不到對應的 UserID");
                }

                _logger.LogInformation("Email {Email} 對應的 UserID: {UserId}", email, userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得 UserID 時發生錯誤: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// 建立請假單
        /// </summary>
        public async Task<string> CreateLeaveFormAsync(object formData)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(formData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("正在建立請假單，資料: {Data}", jsonContent);

                var response = await _httpClient.PostAsync("/form/leave/create", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("建立請假單失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"建立請假單失敗: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var formId = result.GetProperty("formId").GetString() ?? string.Empty;
                _logger.LogInformation("請假單建立成功，FormID: {FormId}", formId);

                return formId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立請假單時發生錯誤");
                throw;
            }
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
                    throw new Exception($"GET 請求失敗: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
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
                var jsonContent = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("發送 POST 請求: {Endpoint}, 資料: {Data}", endpoint, jsonContent);

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("POST 請求失敗: {StatusCode}, {Body}", response.StatusCode, errorBody);
                    throw new Exception($"POST 請求失敗: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST 請求時發生錯誤: {Endpoint}", endpoint);
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

                var response = await _httpClient.GetAsync("/");
                var isConnected = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;

                _logger.LogInformation("BPM 連線測試結果: {Result}, StatusCode: {StatusCode}",
                    isConnected ? "成功" : "失敗", response.StatusCode);

                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試 BPM 連線時發生錯誤");
                return false;
            }
        }
    }
}