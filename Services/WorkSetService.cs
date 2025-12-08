using System.Text;
using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 考勤超時出勤設定服務實作
    /// </summary>
    public class WorkSetService : IWorkSetService
    {
        private readonly BpmService _bpmService;
        private readonly ILogger<WorkSetService> _logger;

        public WorkSetService(
            BpmService bpmService,
            ILogger<WorkSetService> logger)
        {
            _bpmService = bpmService;
            _logger = logger;
        }

        /// <summary>
        /// 新增或修改考勤超時出勤設定
        /// </summary>
        public async Task<WorkSetResponse> CreateOrUpdateWorkSetAsync(WorkSetRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理考勤超時出勤設定 - 工號: {Uid}, 日期: {Wdate}", 
                    request.Uid, request.Wdate);

                // 驗證日期格式
                if (!DateTime.TryParse(request.Wdate, out var parsedDate))
                {
                    _logger.LogWarning("日期格式錯誤: {Wdate}", request.Wdate);
                    return new WorkSetResponse
                    {
                        Code = "500",
                        Msg = "請求失敗"
                    };
                }

                // 轉換日期格式為 YYYY/MM/DD（BPM 要求的格式）
                var formattedDate = parsedDate.ToString("yyyy/MM/dd");

                // 準備 BPM 表單請求
                var bpmRequest = new
                {
                    processCode = "Attendance_Exception_001_PROCESS",
                    formDataMap = new
                    {
                        Attendance_Exception_001 = new
                        {
                            applyDate = formattedDate,
                            exceptionTime = "00:00",
                            exceptionEndTime = "",
                            exceptionReason = request.Reason,
                            exceptionDescription = request.Reason
                        }
                    },
                    userId = request.Uid,
                    subject = $"超時出勤異常確認：員工 {request.Uid} {parsedDate:yyyy-MM-dd}",
                    sourceSystem = "APP",
                    environment = "TEST",
                    hasAttachments = false
                };

                _logger.LogInformation("發送請求到 BPM API: bpm/invoke-process");

                // 調用 BPM API - 使用新的不轉換大小寫的方法
                var responseJson = await _bpmService.PostAsyncWithoutCamelCase("bpm/invoke-process", bpmRequest);
                _logger.LogDebug("BPM API 回應: {Response}", responseJson);

                // 解析 BPM bpm/invoke-process 端點的回應
                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    
                    // 檢查 status 欄位（BPM 標準回應格式）
                    if (root.TryGetProperty("status", out var statusElement))
                    {
                        var status = statusElement.GetString();
                        if (status == "SUCCESS")
                        {
                            // 嘗試提取表單號和流程號（只用於日誌）
                            string? formId = null;
                            string? processSerialNo = null;
                            
                            if (root.TryGetProperty("bpmProcessOid", out var bpmProcessOidElement))
                                formId = bpmProcessOidElement.GetString();
                            if (root.TryGetProperty("processSerialNo", out var processSerialNoElement))
                                processSerialNo = processSerialNoElement.GetString();
                            
                            _logger.LogInformation("✅ 超時出勤流程發起成功 - FormId: {FormId}, ProcessSerialNo: {ProcessSerialNo}", 
                                formId ?? "N/A", processSerialNo ?? "N/A");
                            
                            return new WorkSetResponse
                            {
                                Code = "200",
                                Msg = "請求成功"
                            };
                        }
                        else
                        {
                            var message = root.TryGetProperty("message", out var messageElement) 
                                ? messageElement.GetString() 
                                : "流程發起失敗";
                            _logger.LogError("❌ 超時出勤流程發起失敗: {Message}", message);
                            return new WorkSetResponse
                            {
                                Code = "500",
                                Msg = "請求失敗"
                            };
                        }
                    }

                    // 未知的回應格式
                    _logger.LogError("❌ 未知的 BPM API 回應格式: {Response}", responseJson);
                    return new WorkSetResponse
                    {
                        Code = "500",
                        Msg = "請求失敗"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP 請求錯誤 - 工號: {Uid}", request.Uid);
                return new WorkSetResponse
                {
                    Code = "500",
                    Msg = "請求超時"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON 解析錯誤 - 工號: {Uid}", request.Uid);
                return new WorkSetResponse
                {
                    Code = "500",
                    Msg = "請求失敗"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理考勤超時出勤設定時發生錯誤 - 工號: {Uid}", request.Uid);
                return new WorkSetResponse
                {
                    Code = "500",
                    Msg = "請求失敗"
                };
            }
        }
    }
}
