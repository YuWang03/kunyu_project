using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 出勤確認單服務 - 處理未刷卡補登申請
    /// </summary>
    public class AttendancePatchFormService : IAttendancePatchFormService
    {
        private readonly BpmService _bpmService;
        private readonly IBasicInfoService _basicInfoService;
        private readonly ILogger<AttendancePatchFormService> _logger;
        private const string PROCESS_CODE = "Attendance_Exception_001_PROCESS"; // BPM 流程代碼
        private const string FORM_CODE = "Attendance_Exception_001"; // BPM 表單代碼
        private const string FORM_VERSION = "1.0.0";

        public AttendancePatchFormService(
            BpmService bpmService,
            IBasicInfoService basicInfoService,
            ILogger<AttendancePatchFormService> logger)
        {
            _bpmService = bpmService;
            _basicInfoService = basicInfoService;
            _logger = logger;
        }

        public async Task<AttendancePatchOperationResult> CreateAttendancePatchFormAsync(CreateAttendancePatchFormRequest request)
        {
            try
            {
                _logger.LogInformation("開始處理出勤確認單: 工號={Uid}, 日期={Date}", request.Uid, request.Edate);

                // 簡化驗證 - 只檢查必填欄位
                if (string.IsNullOrWhiteSpace(request.Tokenid) ||
                    string.IsNullOrWhiteSpace(request.Uid) ||
                    string.IsNullOrWhiteSpace(request.Cid) ||
                    string.IsNullOrWhiteSpace(request.Edate) ||
                    string.IsNullOrWhiteSpace(request.Ereason))
                {
                    _logger.LogWarning("缺少必填參數");
                    return new AttendancePatchOperationResult
                    {
                        Success = false,
                        Code = "400",
                        Message = "缺少必填參數"
                    };
                }

                // 記錄請求資訊
                _logger.LogInformation("處理出勤確認單 - 工號: {Uid}, 公司: {Cid}, 日期: {Date}", 
                    request.Uid, request.Cid, request.Edate);

                // 建立 BPM 表單資料
                var reasonText = GetReasonText(request.Ereason);
                // 轉換日期格式為 BPM 期望的格式 (yyyy/MM/dd)
                var formattedDate = request.Edate.Replace("-", "/");
                
                // exceptionDescription 為必填，如果沒有提供則使用原因作為預設值
                var exceptionDescription = string.IsNullOrWhiteSpace(request.Edetails) 
                    ? $"異常原因：{reasonText}" 
                    : request.Edetails;
                
                var formData = new Dictionary<string, object?>
                {
                    { "applyDate", formattedDate },
                    { "exceptionReason", reasonText },
                    { "exceptionDescription", exceptionDescription }
                };

                // 根據原因代碼選擇性地添加時間欄位
                // A: 上班忘刷卡(臉) - 只填 exceptionTime
                // B: 下班忘刷卡(臉) - 只填 exceptionEndTime
                // C: 上下班忘刷卡(臉) - 填兩個時間
                // D: 其他 - 根據 eclockIn/eclockOut 判斷
                switch (request.Ereason.ToUpper())
                {
                    case "A": // 上班忘刷卡
                        if (!string.IsNullOrWhiteSpace(request.EclockIn))
                        {
                            formData["exceptionTime"] = request.EclockIn;
                        }
                        break;
                    case "B": // 下班忘刷卡
                        if (!string.IsNullOrWhiteSpace(request.EclockOut))
                        {
                            formData["exceptionEndTime"] = request.EclockOut;
                        }
                        break;
                    case "C": // 上下班忘刷卡
                        if (!string.IsNullOrWhiteSpace(request.EclockIn))
                        {
                            formData["exceptionTime"] = request.EclockIn;
                        }
                        if (!string.IsNullOrWhiteSpace(request.EclockOut))
                        {
                            formData["exceptionEndTime"] = request.EclockOut;
                        }
                        break;
                    case "D": // 其他
                    default:
                        if (!string.IsNullOrWhiteSpace(request.EclockIn))
                        {
                            formData["exceptionTime"] = request.EclockIn;
                        }
                        if (!string.IsNullOrWhiteSpace(request.EclockOut))
                        {
                            formData["exceptionEndTime"] = request.EclockOut;
                        }
                        break;
                }

                var bpmRequest = new
                {
                    processCode = PROCESS_CODE,
                    formDataMap = new Dictionary<string, object>
                    {
                        { FORM_CODE, formData }
                    },
                    userId = request.Uid,
                    subject = $"出勤異常確認：{request.Uid} {reasonText} {request.Edate}",
                    sourceSystem = "APP",
                    environment = "TEST",
                    hasAttachments = false
                };

                // 呼叫 BPM API
                _logger.LogInformation("呼叫 BPM API 處理出勤異常申請: {UserId}, {Date}", request.Uid, request.Edate);
                var response = await _bpmService.PostAsync("bpm/invoke-process", bpmRequest);
                
                // 解析回應
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                var formId = ExtractFormId(jsonResponse);

                _logger.LogInformation("出勤確認單處理成功 - FormId: {FormId}", formId);

                return new AttendancePatchOperationResult
                {
                    Success = true,
                    Code = "200",
                    Message = "請求成功",
                    FormId = formId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理出勤確認單失敗");
                return new AttendancePatchOperationResult
                {
                    Success = false,
                    Code = "500",
                    Message = $"系統錯誤: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 取得原因文字
        /// </summary>
        private string GetReasonText(string reasonCode)
        {
            return reasonCode.ToUpper() switch
            {
                "A" => "上班忘刷卡(臉)",
                "B" => "下班忘刷卡(臉)",
                "C" => "上下班忘刷卡(臉)",
                "D" => "其他",
                _ => "未知原因"
            };
        }

        /// <summary>
        /// 從 BPM 回應中提取表單 ID
        /// </summary>
        private string? ExtractFormId(JsonElement jsonResponse)
        {
            try
            {
                if (jsonResponse.TryGetProperty("bpmProcessOid", out var oid))
                    return oid.GetString();
                if (jsonResponse.TryGetProperty("formId", out var formId))
                    return formId.GetString();
                if (jsonResponse.TryGetProperty("id", out var id))
                    return id.GetString();
                if (jsonResponse.TryGetProperty("processInstanceId", out var processId))
                    return processId.GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取表單 ID 失敗");
            }
            return null;
        }
    }
}
