using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using Dapper;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 中間件服務實作
    /// 與 BPM Middleware API 進行整合
    /// </summary>
    public class BpmMiddlewareService : IBpmMiddlewareService
    {
        private readonly string _connectionString;
        private readonly ILogger<BpmMiddlewareService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // BPM Middleware API 的基礎 URL - 從 appsettings 讀取
        private string _bpmMiddlewareBaseUrl = "http://60.248.158.147:8081/bpm-middleware";

        public BpmMiddlewareService(
            IConfiguration configuration,
            ILogger<BpmMiddlewareService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _connectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration));

            // 從 appsettings 讀取 BPM Middleware 的基礎 URL
            var bpmUrl = configuration["BpmMiddleware:BaseUrl"];
            if (!string.IsNullOrEmpty(bpmUrl))
            {
                _bpmMiddlewareBaseUrl = bpmUrl;
            }
        }

        /// <summary>
        /// 同步流程信息 - syncProcessInfo API
        /// </summary>
        public async Task<BpmSyncProcessInfoResponse> SyncProcessInfoAsync(
            string processSerialNo,
            string processCode,
            string environment = "TEST")
        {
            try
            {
                _logger.LogInformation(
                    "開始同步 BPM 流程信息 - 表單編號: {ProcessSerialNo}, 程序代碼: {ProcessCode}, 環境: {Environment}",
                    processSerialNo, processCode, environment);

                // 建構 API URL
                var apiUrl = $"{_bpmMiddlewareBaseUrl}/swagger-ui/index.html" +
                    $"#/BPM%20%E6%B5%81%E7%A8%8B%E7%99%BC%E8%B5%B7/syncProcessInfo" +
                    $"?processSerialNo={Uri.EscapeDataString(processSerialNo)}" +
                    $"&processCode={Uri.EscapeDataString(processCode)}" +
                    $"&environment={Uri.EscapeDataString(environment)}";

                // 實際的 API 端點應該是
                var actualApiUrl = $"{_bpmMiddlewareBaseUrl}/api/bpm/sync-process-info";

                // 建構請求參數
                var requestParameters = new
                {
                    processSerialNo = processSerialNo,
                    processCode = processCode,
                    environment = environment
                };

                // 執行 API 呼叫
                using var request = new HttpRequestMessage(HttpMethod.Post, actualApiUrl);
                request.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestParameters),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "BPM API 呼叫失敗 - 狀態碼: {StatusCode}, 回應: {Response}",
                        response.StatusCode, errorContent);

                    return new BpmSyncProcessInfoResponse
                    {
                        Code = "500",
                        Msg = $"BPM API 呼叫失敗: {response.StatusCode}",
                        ProcessInfo = null
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<BpmSyncProcessInfoResponse>(content);

                if (result == null)
                {
                    _logger.LogWarning("BPM API 回應解析失敗");
                    return new BpmSyncProcessInfoResponse
                    {
                        Code = "500",
                        Msg = "BPM API 回應解析失敗",
                        ProcessInfo = null
                    };
                }

                _logger.LogInformation("成功同步 BPM 流程信息 - 表單編號: {ProcessSerialNo}", processSerialNo);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP 請求失敗 - 無法連接到 BPM Middleware");
                return new BpmSyncProcessInfoResponse
                {
                    Code = "500",
                    Msg = "無法連接到 BPM Middleware",
                    ProcessInfo = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步 BPM 流程信息失敗");
                return new BpmSyncProcessInfoResponse
                {
                    Code = "500",
                    Msg = $"同步流程信息失敗: {ex.Message}",
                    ProcessInfo = null
                };
            }
        }

        /// <summary>
        /// 查詢請假單程序代碼
        /// 根據 BPM 規格，請假單的程序代碼是 PI_LEAVE_001_PROCESS
        /// </summary>
        public async Task<string> GetLeaveProcessCodeAsync()
        {
            try
            {
                _logger.LogInformation("查詢請假單程序代碼");
                // 從 appsettings 或資料庫讀取
                var processCode = _configuration["BpmMiddleware:LeaveProcessCode"] ?? "PI_LEAVE_001_PROCESS";
                _logger.LogInformation("請假單程序代碼: {ProcessCode}", processCode);
                return await Task.FromResult(processCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢請假單程序代碼失敗");
                // 返回預設值
                return "PI_LEAVE_001_PROCESS";
            }
        }

        /// <summary>
        /// 查詢業務出差單程序代碼
        /// </summary>
        public async Task<string> GetBusinessTripProcessCodeAsync()
        {
            try
            {
                _logger.LogInformation("查詢業務出差單程序代碼");
                // 從 appsettings 或資料庫讀取
                var processCode = _configuration["BpmMiddleware:BusinessTripProcessCode"] ?? "PI_BUSINESS_TRIP_001_PROCESS";
                _logger.LogInformation("業務出差單程序代碼: {ProcessCode}", processCode);
                return await Task.FromResult(processCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢業務出差單程序代碼失敗");
                // 返回預設值
                return "PI_BUSINESS_TRIP_001_PROCESS";
            }
        }
    }
}