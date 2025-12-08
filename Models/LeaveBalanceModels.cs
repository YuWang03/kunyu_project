using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region 新版 LeaveBalance API Models

    /// <summary>
    /// 請假餘額查詢 - 查詢請求
    /// </summary>
    public class LeaveBalanceRequest
    {
        /// <summary>
        /// Token ID
        /// </summary>
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 公司代碼
        /// </summary>
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者ID (員工編號)
        /// </summary>
        [Required(ErrorMessage = "使用者ID為必填")]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 查詢年度 (例如: "2025")
        /// </summary>
        [Required(ErrorMessage = "查詢年度為必填")]
        [JsonPropertyName("ryear")]
        public string RYear { get; set; } = string.Empty;
    }

    /// <summary>
    /// 請假餘額查詢 - 查詢結果
    /// </summary>
    public class LeaveBalanceResponse
    {
        /// <summary>
        /// 狀態碼 ("200" 成功, "500" 失敗)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 訊息
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 假別資料列表
        /// </summary>
        [JsonPropertyName("data")]
        public List<LeaveBalanceData>? Data { get; set; }
    }

    /// <summary>
    /// 假別餘額資料
    /// </summary>
    public class LeaveBalanceData
    {
        /// <summary>
        /// 假別名稱 (例如: 特休、事假、病假、補休假)
        /// </summary>
        [JsonPropertyName("leavetype")]
        public string LeaveType { get; set; } = string.Empty;

        /// <summary>
        /// 年度配額 (天數)
        /// </summary>
        [JsonPropertyName("annualquota")]
        public decimal AnnualQuota { get; set; }

        /// <summary>
        /// 已扣除天數
        /// </summary>
        [JsonPropertyName("deducteddays")]
        public decimal DeductedDays { get; set; }

        /// <summary>
        /// 剩餘天數
        /// </summary>
        [JsonPropertyName("remainingdays")]
        public decimal RemainingDays { get; set; }
    }

    #endregion

    #region 舊版 LeaveRemain API Models (已棄用)

    /// <summary>
    /// 請假剩餘天數 - 查詢請求 (舊版，已棄用)
    /// </summary>
    [Obsolete("已棄用，請使用 LeaveBalanceRequest")]
    public class LeaveRemainRequest
    {
        /// <summary>
        /// Token ID
        /// </summary>
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 公司代碼
        /// </summary>
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者ID (員工編號)
        /// </summary>
        [Required(ErrorMessage = "使用者ID為必填")]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 查詢年度 (例如: "2025")
        /// </summary>
        [Required(ErrorMessage = "查詢年度為必填")]
        [JsonPropertyName("ryear")]
        public string RYear { get; set; } = string.Empty;
    }

    /// <summary>
    /// 請假剩餘天數 - 查詢結果 (舊版，已棄用)
    /// </summary>
    [Obsolete("已棄用，請使用 LeaveBalanceResponse")]
    public class LeaveRemainResponse
    {
        /// <summary>
        /// 狀態碼 ("200" 成功, "500" 失敗)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 訊息
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 假別資料列表
        /// </summary>
        [JsonPropertyName("data")]
        public List<LeaveTypeData>? Data { get; set; }
    }

    /// <summary>
    /// 假別資料 (舊版，已棄用)
    /// </summary>
    [Obsolete("已棄用，請使用 LeaveBalanceData")]
    public class LeaveTypeData
    {
        /// <summary>
        /// 假別名稱 (例如: 特休、事假、病假、補休假)
        /// </summary>
        [JsonPropertyName("leavetype")]
        public string LeaveType { get; set; } = string.Empty;

        /// <summary>
        /// 年度配額 (天數)
        /// </summary>
        [JsonPropertyName("annualquota")]
        public decimal AnnualQuota { get; set; }

        /// <summary>
        /// 已扣除天數
        /// </summary>
        [JsonPropertyName("deducteddays")]
        public decimal DeductedDays { get; set; }

        /// <summary>
        /// 剩餘天數
        /// </summary>
        [JsonPropertyName("remainingdays")]
        public decimal RemainingDays { get; set; }
    }

    #endregion

    #region 舊版本模型 (保留供內部使用)

    /// <summary>
    /// 假別明細 (內部使用)
    /// </summary>
    internal class LeaveTypeDetail
    {
        /// <summary>
        /// 假別名稱 (例如: 特休、事假、病假、補休假)
        /// </summary>
        public string LeaveTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 假別代碼
        /// </summary>
        public string LeaveTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// 最小計算單位 (小時)
        /// </summary>
        public decimal MinUnit { get; set; }

        /// <summary>
        /// 剩餘天數 (例如: 5 天)
        /// </summary>
        public decimal RemainDays { get; set; }

        /// <summary>
        /// 剩餘小時數 (例如: 4 小時)
        /// </summary>
        public decimal RemainHours { get; set; }

        /// <summary>
        /// 剩餘總小時數 (例如: 44 小時 = 5天4小時)
        /// </summary>
        public decimal RemainTotalHours { get; set; }

        /// <summary>
        /// 全年假別天數
        /// </summary>
        public decimal TotalDays { get; set; }

        /// <summary>
        /// 全年假別小時數
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// 已使用天數
        /// </summary>
        public decimal UsedDays { get; set; }

        /// <summary>
        /// 已使用小時數
        /// </summary>
        public decimal UsedHours { get; set; }

        /// <summary>
        /// 已使用總小時數
        /// </summary>
        public decimal UsedTotalHours { get; set; }

        /// <summary>
        /// 顯示文字 (例如: "5 天" 或 "5 天 4 小時")
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// 特休資料 (對應 vwZZ_EMPLOYEE_SPECIAL)
    /// </summary>
    internal class EmployeeSpecialLeave
    {
        public string? EMPLOYEE_ID { get; set; }
        public string? EMPLOYEE_NO { get; set; }
        public string? EMPLOYEE_CNAME { get; set; }
        public string? EMPLOYEE_SPECIAL_YEAR { get; set; }
        public DateTime? EMPLOYEE_SPECIAL_START { get; set; }
        public DateTime? EMPLOYEE_SPECIAL_END { get; set; }
        public string? EMPLOYEE_SPECIAL_UNIT { get; set; }
        public decimal? EMPLOYEE_SPECIAL_VALUE { get; set; }
        public decimal? SPECIAL_REMAIN_HOURS { get; set; }
        public bool? IS_CLEAR { get; set; }
    }

    /// <summary>
    /// 假別定義 (對應 vwZZ_LEAVE_REFERENCE)
    /// </summary>
    internal class LeaveReference
    {
        public string? LEAVE_REFERENCE_ID { get; set; }
        public string? COMPANY_ID { get; set; }
        public string? LEAVE_REFERENCE_CLASS { get; set; }
        public string? LEAVE_REFERENCE_CODE { get; set; }
        public string? LEAVE_UNIT { get; set; }
        public decimal? LEAVE_MIN_VALUE { get; set; }
    }

    /// <summary>
    /// 請假記錄 (對應 vwZZ_ASK_LEAVE)
    /// </summary>
    internal class AskLeave
    {
        public string? ASK_LEAVE_ID { get; set; }
        public string? EMPLOYEE_ID { get; set; }
        public string? EMPLOYEE_NO { get; set; }
        public string? EMPLOYEE_CNAME { get; set; }
        public string? LEAVE_REFERENCE_ID { get; set; }
        public string? LEAVE_SETUP_CODE { get; set; }
        public string? LEAVE_SETUP_CLASS { get; set; }
        public string? LEAVE_REFERENCE_CODE { get; set; }
        public string? LEAVE_REFERENCE_CLASS { get; set; }
        public DateTime? ASK_LEAVE_START { get; set; }
        public DateTime? ASK_LEAVE_END { get; set; }
        public decimal? ASK_LEAVE_HOUR { get; set; }
        public decimal? CANCEL_HOUR { get; set; }
        public bool? IS_COUNT { get; set; }
        public DateTime? CREATE_DATE { get; set; }
    }

    /// <summary>
    /// 請假查詢參數
    /// </summary>
    public class LeaveRemainQueryParams
    {
        /// <summary>
        /// 員工編號 (必填)
        /// </summary>
        [Required(ErrorMessage = "員工編號為必填")]
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 查詢年度 (選填，預設當年度，例如: 2025)
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// 是否查詢前一年 (選填，預設 false)
        /// </summary>
        public bool IncludePreviousYear { get; set; } = false;
    }
}