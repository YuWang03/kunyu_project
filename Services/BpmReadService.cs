using HRSystemAPI.Models;
using System.Text.Json;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM Read 服務實作
    /// 處理從 BPM 中間件擷取表單資料並存入本地資料庫
    /// </summary>
    public class BpmReadService : IBpmReadService
    {
        private readonly IBpmFormRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BpmReadService> _logger;

        // 廣宇專用的 BSKEY (固定值)
        private const string VALID_BSKEY = "Kw9WyeEzUC5LkfC99XDLGkFpG615O7UD2gsX99VtSENUzwTP2FuxDj4A0UpCnJUXSCTdWPaacj2cHVZjmlcjMwaTm26oTsgiTZc7NP2nEuutq23f5gpbHLlvB914TWrUdTsRS1GHtYOsbRDu90tNceLiOQGOY7ESdKpGJlwdKSSS73tTGG52CNR8hkdbv2Svu5oAB6UX";

        // BPM 中間件基礎 URL
        private readonly string _bpmMiddlewareBaseUrl;

        public BpmReadService(
            IBpmFormRepository repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BpmReadService> logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            // 從設定讀取 BPM 中間件 URL
            _bpmMiddlewareBaseUrl = configuration["BpmMiddleware:BaseUrl"] 
                ?? "http://60.248.158.147:8081/bpm-middleware";
        }

        /// <summary>
        /// 處理 BPM Read 請求
        /// </summary>
        public async Task<BpmReadResponse> ProcessBpmReadAsync(BpmReadRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理 BPM Read 請求 - CompanyId: {CompanyId}, 資料筆數: {Count}",
                    request.CompanyId, request.BpmData?.Count ?? 0);

                // 1. 驗證 BSKEY
                if (!ValidateBskey(request.Bskey))
                {
                    _logger.LogWarning("BSKEY 驗證失敗: {Bskey}", request.Bskey);
                    return new BpmReadResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }

                // 2. 驗證必要參數
                if (string.IsNullOrWhiteSpace(request.CompanyId))
                {
                    _logger.LogWarning("缺少公司編號");
                    return new BpmReadResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }

                if (request.BpmData == null || request.BpmData.Count == 0)
                {
                    _logger.LogWarning("BPM 資料為空");
                    return new BpmReadResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }

                // 3. 處理每一筆 BPM 表單資料
                int successCount = 0;
                int failCount = 0;

                foreach (var item in request.BpmData)
                {
                    try
                    {
                        // 驗證必要欄位
                        if (string.IsNullOrWhiteSpace(item.ProcessSerialNo))
                        {
                            _logger.LogWarning("表單缺少 ProcessSerialNo，跳過");
                            failCount++;
                            continue;
                        }

                        // 從 BPM 中間件取得表單詳細資料
                        var processData = await FetchFormFromBpmMiddlewareAsync(
                            item.ProcessSerialNo, 
                            item.FormCode ?? "");

                        // 建立 BpmForm 物件
                        var bpmForm = new BpmForm
                        {
                            FormId = item.ProcessSerialNo,
                            FormCode = item.FormCode ?? "",
                            FormType = DetermineFormType(item.FormCode),
                            FormVersion = item.Version ?? "1.0.0",
                            ApplicantId = item.Uid ?? "",
                            CompanyId = request.CompanyId,
                            Status = processData?.Status ?? "PENDING",
                            BpmStatus = processData?.Status,
                            ApplicantName = processData?.ApplicantName,
                            ApplicantDepartment = processData?.ApplicantDepartment,
                            ApplyDate = processData?.CreateTime ?? DateTime.Now,
                            SubmitTime = processData?.CreateTime ?? DateTime.Now,
                            LastSyncTime = DateTime.Now,
                            IsSyncedToBpm = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        // 若有表單詳細資料，儲存為 JSON
                        if (processData?.FormData != null)
                        {
                            bpmForm.FormData = JsonSerializer.Serialize(processData.FormData);
                        }

                        // 儲存至本地資料庫
                        if (await SaveFormToLocalDbAsync(bpmForm))
                        {
                            _logger.LogInformation("成功儲存表單: {FormId}", item.ProcessSerialNo);
                            successCount++;

                            // 若有簽核歷程，也一併儲存
                            if (processData?.ApprovalHistory != null && processData.ApprovalHistory.Count > 0)
                            {
                                await SaveApprovalHistoryAsync(item.ProcessSerialNo, processData.ApprovalHistory);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("儲存表單失敗: {FormId}", item.ProcessSerialNo);
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "處理表單時發生錯誤: {FormId}", item.ProcessSerialNo);
                        failCount++;
                    }
                }

                _logger.LogInformation("BPM Read 處理完成 - 成功: {Success}, 失敗: {Fail}", successCount, failCount);

                // 4. 回傳結果
                if (successCount > 0)
                {
                    return new BpmReadResponse
                    {
                        Code = "200",
                        Msg = "成功",
                        Data = new BpmReadResponseData
                        {
                            Status = $"請求成功，已處理 {successCount} 筆資料"
                        }
                    };
                }
                else
                {
                    return new BpmReadResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 BPM Read 請求時發生錯誤");
                return new BpmReadResponse
                {
                    Code = "500",
                    Msg = $"伺服器錯誤: {ex.Message}",
                    Data = null
                };
            }
        }

        /// <summary>
        /// 驗證 BSKEY 是否有效
        /// </summary>
        public bool ValidateBskey(string? bskey)
        {
            if (string.IsNullOrWhiteSpace(bskey))
            {
                return false;
            }

            return bskey == VALID_BSKEY;
        }

        /// <summary>
        /// 從 BPM 中間件取得單一表單資料
        /// </summary>
        public async Task<BpmProcessData?> FetchFormFromBpmMiddlewareAsync(string processSerialNo, string formCode)
        {
            try
            {
                _logger.LogInformation("從 BPM 中間件取得表單: {ProcessSerialNo}, FormCode: {FormCode}",
                    processSerialNo, formCode);

                var httpClient = _httpClientFactory.CreateClient("BpmClient");

                // 呼叫 BPM 中間件 API
                var apiUrl = $"{_bpmMiddlewareBaseUrl}/api/bpm/process/{Uri.EscapeDataString(processSerialNo)}";
                
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("BPM 中間件回應失敗: {StatusCode}, {Content}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<BpmProcessInfoResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Code == "200" || result?.Code == "0")
                {
                    _logger.LogInformation("成功取得 BPM 表單資料: {ProcessSerialNo}", processSerialNo);
                    return result.Data;
                }

                _logger.LogWarning("BPM 中間件回應非成功狀態: {Code}, {Message}",
                    result?.Code, result?.Message);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "無法連線至 BPM 中間件，將使用基本資料建立表單: {ProcessSerialNo}", processSerialNo);
                // 連線失敗時回傳 null，讓呼叫端使用基本資料建立
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "從 BPM 中間件取得表單時發生錯誤: {ProcessSerialNo}", processSerialNo);
                return null;
            }
        }

        /// <summary>
        /// 將表單資料存入本地資料庫
        /// </summary>
        public async Task<bool> SaveFormToLocalDbAsync(BpmForm form)
        {
            try
            {
                // 檢查表單是否已存在
                var existingForm = await _repository.GetFormByIdAsync(form.FormId);

                if (existingForm != null)
                {
                    // 更新現有表單
                    form.Id = existingForm.Id;
                    form.CreatedAt = existingForm.CreatedAt;
                    form.UpdatedAt = DateTime.Now;
                    await _repository.UpdateFormAsync(form);
                    _logger.LogInformation("更新現有表單: {FormId}", form.FormId);
                }
                else
                {
                    // 建立新表單
                    form.CreatedAt = DateTime.Now;
                    form.UpdatedAt = DateTime.Now;
                    await _repository.CreateFormAsync(form);
                    _logger.LogInformation("建立新表單: {FormId}", form.FormId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存表單至本地資料庫時發生錯誤: {FormId}", form.FormId);
                return false;
            }
        }

        /// <summary>
        /// 儲存簽核歷程
        /// </summary>
        private async Task SaveApprovalHistoryAsync(string formId, List<BpmApprovalRecord> approvalHistory)
        {
            try
            {
                foreach (var record in approvalHistory)
                {
                    var history = new BpmFormApprovalHistory
                    {
                        FormId = formId,
                        SequenceNo = record.Sequence ?? 0,
                        ApproverId = record.ApproverId ?? "",
                        ApproverName = record.ApproverName,
                        Action = record.Action ?? "",
                        Comment = record.Comment,
                        ActionTime = record.ActionTime,
                        CreatedAt = DateTime.Now
                    };

                    await _repository.AddApprovalHistoryAsync(history);
                }

                _logger.LogInformation("儲存簽核歷程完成: {FormId}, 筆數: {Count}", formId, approvalHistory.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存簽核歷程時發生錯誤: {FormId}", formId);
            }
        }

        /// <summary>
        /// 根據表單代碼判斷表單類型
        /// </summary>
        private string DetermineFormType(string? formCode)
        {
            if (string.IsNullOrWhiteSpace(formCode))
            {
                return "OTHER";
            }

            var code = formCode.ToUpperInvariant();

            if (code.Contains("LEAVE") && !code.Contains("CANCEL"))
            {
                return "LEAVE";
            }
            else if (code.Contains("OVERTIME"))
            {
                return "OVERTIME";
            }
            else if (code.Contains("BUSINESS") || code.Contains("TRIP"))
            {
                return "BUSINESS_TRIP";
            }
            else if (code.Contains("CANCEL") && code.Contains("LEAVE"))
            {
                return "CANCEL_LEAVE";
            }
            else if (code.Contains("ATTENDANCE") || code.Contains("EXCEPTION"))
            {
                return "ATTENDANCE";
            }
            else
            {
                return "OTHER";
            }
        }
    }
}
