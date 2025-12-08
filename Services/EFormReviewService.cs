using System.Text.Json;
using HRSystemAPI.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HRSystemAPI.Services
{
    public class EFormReviewService : IEFormReviewService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EFormReviewService> _logger;

        public EFormReviewService(IHttpClientFactory httpClientFactory, ILogger<EFormReviewService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<EFormReviewResponse> GetReviewFormsAsync(EFormReviewRequest request)
        {
            var response = new EFormReviewResponse { Code = "200", Msg = "請求成功", Data = new EFormReviewData() };
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("http://60.248.158.147:8081/bpm-middleware/api/");
                client.DefaultRequestHeaders.Add("X-API-Key", "APP_ACKEY");
                client.DefaultRequestHeaders.Add("X-API-Secret", "$2a$12$cB7XkVA51wUuNQeihY2NGL$dql0gNTpQpLYgxv9q3xkUhURd3oC/Cz");

                var bpmResponse = await client.GetAsync($"bpm/workitems/{request.Uid}");
                if (!bpmResponse.IsSuccessStatusCode)
                {
                    response.Code = "203";
                    response.Msg = "請求失敗，主要條件不符合";
                    return response;
                }
                var json = await bpmResponse.Content.ReadAsStringAsync();
                var bpmJson = JsonSerializer.Deserialize<JsonElement>(json);
                if (bpmJson.TryGetProperty("workItems", out var workItems) && workItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in workItems.EnumerateArray())
                    {
                        var serialNo = item.GetProperty("processSerialNumber").GetString();
                        // TODO: 根據 serialNo 取得表單詳細資料，並組裝 EFormReviewItem
                        // 這裡僅示範一筆假資料
                        response.Data.EFormData.Add(new EFormReviewItem
                        {
                            UName = "王大明",
                            UDepartment = "電子一部",
                            FormId = serialNo ?? "",
                            EFormType = "L",
                            EFormName = "請假單",
                            EStartDate = "2025-09-18",
                            EStartTime = "08:00",
                            EndTitle = "結束時間",
                            EEndDate = "2025-09-18",
                            EEndTime = "17:00",
                            EReason = "家中有事"
                        });
                    }
                }
                else
                {
                    response.Code = "203";
                    response.Msg = "請求失敗，主要條件不符合";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得簽核記錄失敗");
                response.Code = "203";
                response.Msg = "請求失敗，主要條件不符合";
            }
            return response;
        }
    }
}
