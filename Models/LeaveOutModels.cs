using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 外出外訓申請單請求
    /// </summary>
    public class CreateLeaveOutFormRequest
    {
        /// <summary>
        /// Token標記
        /// </summary>
        [Required(ErrorMessage = "tokenid 為必填")]
        public string? Tokenid { get; set; }

        /// <summary>
        /// 目前所屬公司
        /// </summary>
        [Required(ErrorMessage = "cid 為必填")]
        public string? Cid { get; set; }

        /// <summary>
        /// 使用者工號
        /// </summary>
        [Required(ErrorMessage = "uid 為必填")]
        public string? Uid { get; set; }

        /// <summary>
        /// 表單類型 (A:外出　B:外訓)
        /// </summary>
        [Required(ErrorMessage = "etype 為必填")]
        [RegularExpression("^[AB]$", ErrorMessage = "etype 必須為 A(外出) 或 B(外訓)")]
        public string? Etype { get; set; }

        /// <summary>
        /// 外出外訓日期 (限定1天，格式: yyyy-MM-dd)
        /// </summary>
        [Required(ErrorMessage = "edate 為必填")]
        public string? Edate { get; set; }

        /// <summary>
        /// 起始時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "estarttime 為必填")]
        public string? Estarttime { get; set; }

        /// <summary>
        /// 截止時間 (格式: HH:mm)
        /// </summary>
        [Required(ErrorMessage = "eendtime 為必填")]
        public string? Eendtime { get; set; }

        /// <summary>
        /// 地點
        /// </summary>
        [Required(ErrorMessage = "elocation 為必填")]
        public string? Elocation { get; set; }

        /// <summary>
        /// 事由
        /// </summary>
        [Required(ErrorMessage = "ereason 為必填")]
        public string? Ereason { get; set; }

        /// <summary>
        /// 是否返回公司 (T: 是  F:否)
        /// </summary>
        [Required(ErrorMessage = "ereturncompany 為必填")]
        [RegularExpression("^[TF]$", ErrorMessage = "ereturncompany 必須為 T(是) 或 F(否)")]
        public string? Ereturncompany { get; set; }

        /// <summary>
        /// 附件檔案格式 (B: 外出外訓附件檔)
        /// </summary>
        public string? Efiletype { get; set; }

        /// <summary>
        /// 附件檔案編號
        /// 請先呼叫 http://54.46.24.34:5112/api/Attachment/Upload 上傳附件，
        /// 從回應中取得 tfileid（附件檔序號）後填入此欄位。
        /// </summary>
        public List<string>? Efileid { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 外出外訓申請單操作結果
    /// </summary>
    public class LeaveOutFormOperationResult
    {
        /// <summary>
        /// 狀態碼 (200: 成功, 203: 失敗)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 表單ID (成功時回傳)
        /// </summary>
        public string? FormId { get; set; }
    }

    /// <summary>
    /// 外出外訓申請單記錄
    /// </summary>
    public class LeaveOutFormRecord
    {
        /// <summary>
        /// 表單ID
        /// </summary>
        public string FormId { get; set; } = string.Empty;

        /// <summary>
        /// 員工工號
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 員工姓名
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// 表單類型 (A:外出　B:外訓)
        /// </summary>
        public string FormType { get; set; } = string.Empty;

        /// <summary>
        /// 表單類型名稱
        /// </summary>
        public string FormTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 外出外訓日期
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 起始時間
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 截止時間
        /// </summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 地點
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 事由
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 是否返回公司
        /// </summary>
        public bool ReturnToCompany { get; set; }

        /// <summary>
        /// 附件檔案編號清單
        /// </summary>
        public List<string>? FileIds { get; set; }

        /// <summary>
        /// 簽核狀態
        /// </summary>
        public string? ApprovalStatus { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion
}
