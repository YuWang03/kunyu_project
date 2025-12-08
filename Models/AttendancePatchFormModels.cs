using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 出勤確認單申請請求 - 補登未刷卡時間
    /// </summary>
    public class CreateAttendancePatchFormRequest
    {
        /// <summary>
        /// Token標記（必填）
        /// </summary>
        [Required(ErrorMessage = "tokenid 為必填")]
        public string Tokenid { get; set; } = string.Empty;

        /// <summary>
        /// 目前所屬公司（必填）
        /// </summary>
        [Required(ErrorMessage = "cid 為必填")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號（必填）
        /// </summary>
        [Required(ErrorMessage = "uid 為必填")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 上班日期（必填）- 格式：yyyy-MM-dd
        /// </summary>
        [Required(ErrorMessage = "edate 為必填")]
        public string Edate { get; set; } = string.Empty;

        /// <summary>
        /// 未刷卡上班時間（選填）- 格式：HH:mm
        /// 與 EclockOut 二擇一
        /// </summary>
        public string? EclockIn { get; set; }

        /// <summary>
        /// 未刷卡下班時間（選填）- 格式：HH:mm
        /// 與 EclockIn 二擇一
        /// </summary>
        public string? EclockOut { get; set; }

        /// <summary>
        /// 原因（必填）
        /// A: 上班忘刷卡(臉)
        /// B: 下班忘刷卡(臉)
        /// C: 上下班忘刷卡(臉)
        /// D: 其他
        /// </summary>
        [Required(ErrorMessage = "ereason 為必填")]
        public string Ereason { get; set; } = string.Empty;

        /// <summary>
        /// 其他事由（選填）
        /// 當 ereason 為 D:其他時，才需填事由
        /// </summary>
        public string? Edetails { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 出勤確認單申請回應
    /// </summary>
    public class AttendancePatchFormResponse
    {
        /// <summary>
        /// 狀態碼
        /// 200: 請求成功
        /// 300: 授權失敗，無效token
        /// 400: 參數錯誤
        /// 500: 系統錯誤
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息說明
        /// </summary>
        public string Msg { get; set; } = string.Empty;
    }

    #endregion

    #region Internal Models

    /// <summary>
    /// 出勤確認單 BPM 請求資料
    /// </summary>
    public class AttendancePatchBpmRequest
    {
        /// <summary>
        /// 使用者工號
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 上班日期
        /// </summary>
        public string AttendanceDate { get; set; } = string.Empty;

        /// <summary>
        /// 未刷卡上班時間
        /// </summary>
        public string? ClockInTime { get; set; }

        /// <summary>
        /// 未刷卡下班時間
        /// </summary>
        public string? ClockOutTime { get; set; }

        /// <summary>
        /// 原因代碼
        /// </summary>
        public string ReasonCode { get; set; } = string.Empty;

        /// <summary>
        /// 原因說明
        /// </summary>
        public string ReasonDescription { get; set; } = string.Empty;

        /// <summary>
        /// 其他事由詳細說明
        /// </summary>
        public string? OtherDetails { get; set; }
    }

    /// <summary>
    /// 出勤確認單處理結果
    /// </summary>
    public class AttendancePatchOperationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 狀態碼
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// BPM 表單ID（如果有的話）
        /// </summary>
        public string? FormId { get; set; }
    }

    #endregion
}
