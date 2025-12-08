using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region Request Models

    /// <summary>
    /// 銷假申請列表請求 - efleaveget API
    /// </summary>
    public class CancelLeaveListRequest
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
        /// 表單編號（選填）
        /// </summary>
        public string? Formid { get; set; }
    }

    /// <summary>
    /// 銷假申請詳細資料請求 - efleavedetail API
    /// </summary>
    public class CancelLeaveDetailRequest
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
        /// 請假單編號（必填）
        /// </summary>
        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 銷假申請送出請求 - efleavecancel API
    /// </summary>
    public class CancelLeaveSubmitRequest
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
        /// 請假單編號（必填）
        /// </summary>
        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;

        /// <summary>
        /// 原因說明（必填）
        /// </summary>
        [Required(ErrorMessage = "reasons 為必填")]
        public string Reasons { get; set; } = string.Empty;
    }

    /// <summary>
    /// 銷假申請預覽請求 - efleavepreview API
    /// </summary>
    public class EFleavePreviewRequest
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
        /// 請假單編號（必填）
        /// </summary>
        [Required(ErrorMessage = "formid 為必填")]
        public string Formid { get; set; } = string.Empty;
    }

    #endregion

    #region Response Models

    /// <summary>
    /// 銷假申請列表回應 - efleaveget API
    /// </summary>
    public class CancelLeaveListResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 失敗訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 數據返回區（成功時有此區，失敗時無此區）
        /// </summary>
        public CancelLeaveListData? Data { get; set; }
    }

    /// <summary>
    /// 銷假申請列表數據
    /// </summary>
    public class CancelLeaveListData
    {
        /// <summary>
        /// 可銷假的請假單列表
        /// </summary>
        public List<CancelLeaveItem> Efleveldata { get; set; } = new List<CancelLeaveItem>();
    }

    /// <summary>
    /// 銷假申請項目
    /// </summary>
    public class CancelLeaveItem
    {
        /// <summary>
        /// 使用者工號
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者姓名
        /// </summary>
        public string Uname { get; set; } = string.Empty;

        /// <summary>
        /// 使用者單位
        /// </summary>
        public string Udepartment { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string Formid { get; set; } = string.Empty;

        /// <summary>
        /// 請假類別
        /// </summary>
        public string Leavetype { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始日期 (YYYY-MM-DD)
        /// </summary>
        public string Estartdate { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始時間 (HH:mm)
        /// </summary>
        public string Estarttime { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束日期 (YYYY-MM-DD)
        /// </summary>
        public string Eenddate { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束時間 (HH:mm)
        /// </summary>
        public string Eendtime { get; set; } = string.Empty;

        /// <summary>
        /// 請假事由
        /// </summary>
        public string Ereason { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號
        /// </summary>
        public string Eagent { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案格式 (C: 請假附件檔)
        /// </summary>
        public string Efiletype { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案列表
        /// </summary>
        public List<CancelLeaveAttachment> Attachments { get; set; } = new List<CancelLeaveAttachment>();
    }

    /// <summary>
    /// 銷假申請詳細資料回應 - efleavedetail API
    /// </summary>
    public class CancelLeaveDetailResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 失敗訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 數據返回區（成功時有此區，失敗時無此區）
        /// </summary>
        public CancelLeaveDetailData? Data { get; set; }
    }

    /// <summary>
    /// 銷假申請詳細數據
    /// </summary>
    public class CancelLeaveDetailData
    {
        /// <summary>
        /// 可銷假的請假單詳細列表
        /// </summary>
        public List<CancelLeaveDetailItem> Efleveldata { get; set; } = new List<CancelLeaveDetailItem>();
    }

    /// <summary>
    /// 銷假申請詳細項目
    /// </summary>
    public class CancelLeaveDetailItem
    {
        /// <summary>
        /// 使用者工號
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者姓名
        /// </summary>
        public string Uname { get; set; } = string.Empty;

        /// <summary>
        /// 使用者單位
        /// </summary>
        public string Udepartment { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string Formid { get; set; } = string.Empty;

        /// <summary>
        /// 請假類別
        /// </summary>
        public string Leavetype { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始日期 (YYYY-MM-DD)
        /// </summary>
        public string Estartdate { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始時間 (HH:mm)
        /// </summary>
        public string Estarttime { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束日期 (YYYY-MM-DD)
        /// </summary>
        public string Eenddate { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束時間 (HH:mm)
        /// </summary>
        public string Eendtime { get; set; } = string.Empty;

        /// <summary>
        /// 請假事由
        /// </summary>
        public string Ereason { get; set; } = string.Empty;

        /// <summary>
        /// 代理人工號
        /// </summary>
        public string Eagent { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案格式 (C: 請假附件檔)
        /// </summary>
        public string Efiletype { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案列表
        /// </summary>
        public List<CancelLeaveAttachment> Attachments { get; set; } = new List<CancelLeaveAttachment>();
    }

    /// <summary>
    /// 銷假申請附件
    /// </summary>
    public class CancelLeaveAttachment
    {
        /// <summary>
        /// 附件檔案編號
        /// </summary>
        public string Efileid { get; set; } = string.Empty;

        /// <summary>
        /// 附件標體名稱
        /// </summary>
        public string Efilename { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案名稱
        /// </summary>
        public string Esfilename { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案網址
        /// </summary>
        public string Efileurl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 銷假申請送出回應 - efleavecancel API
    /// </summary>
    public class CancelLeaveSubmitResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 失敗訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;
    }

    /// <summary>
    /// 銷假申請預覽回應 - efleavepreview API
    /// </summary>
    public class EFleavePreviewResponse
    {
        /// <summary>
        /// 是否成功（200=成功, 203=失敗）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 數據返回區（成功時有此區，失敗時無此區）
        /// </summary>
        public EFleavePreviewData? Data { get; set; }
    }

    /// <summary>
    /// 銷假申請預覽數據
    /// </summary>
    public class EFleavePreviewData
    {
        /// <summary>
        /// 表單編號
        /// </summary>
        public string Formid { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始日期 (YYYY-MM-DD)
        /// </summary>
        public string Estartdate { get; set; } = string.Empty;

        /// <summary>
        /// 請假起始時間 (HH:mm)
        /// </summary>
        public string Estarttime { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束日期 (YYYY-MM-DD)
        /// </summary>
        public string Eenddate { get; set; } = string.Empty;

        /// <summary>
        /// 請假結束時間 (HH:mm)
        /// </summary>
        public string Eendtime { get; set; } = string.Empty;

        /// <summary>
        /// 請假事由
        /// </summary>
        public string Ereason { get; set; } = string.Empty;

        /// <summary>
        /// 流程狀態（如：補休）
        /// </summary>
        public string Eprocess { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案格式 (D: 銷假附件檔)
        /// </summary>
        public string? Efiletype { get; set; }

        /// <summary>
        /// 附件檔案列表
        /// </summary>
        public List<EFleaveAttachment>? Attachments { get; set; }
    }

    /// <summary>
    /// 銷假申請附件
    /// </summary>
    public class EFleaveAttachment
    {
        /// <summary>
        /// 附件檔案編號
        /// </summary>
        public string Efileid { get; set; } = string.Empty;

        /// <summary>
        /// 附件標題名稱
        /// </summary>
        public string Efilename { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案名稱
        /// </summary>
        public string Esfilename { get; set; } = string.Empty;

        /// <summary>
        /// 附件檔案網址
        /// </summary>
        public string Efileurl { get; set; } = string.Empty;
    }

    #endregion

    #region BPM Integration Models

    /// <summary>
    /// BPM 銷假單預覽請求
    /// </summary>
    public class BpmCancelLeavePreviewRequest
    {
        /// <summary>
        /// 環境 (TEST/PROD)
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// 表單代碼
        /// </summary>
        public string FormCode { get; set; } = string.Empty;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 基本資料
        /// </summary>
        public BpmCancelLeaveBasicData BasicData { get; set; } = new BpmCancelLeaveBasicData();
    }

    /// <summary>
    /// BPM 銷假單基本資料
    /// </summary>
    public class BpmCancelLeaveBasicData
    {
        /// <summary>
        /// 使用者 ID (員工工號)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 原請假單編號
        /// </summary>
        public string OriginalFormId { get; set; } = string.Empty;

        /// <summary>
        /// 銷假原因
        /// </summary>
        public string CancelReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// BPM 銷假單提交請求
    /// </summary>
    public class BpmCancelLeaveSubmitRequest
    {
        /// <summary>
        /// 環境 (TEST/PROD)
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// 表單代碼
        /// </summary>
        public string FormCode { get; set; } = string.Empty;

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 表單資料
        /// </summary>
        public Dictionary<string, object> FormData { get; set; } = new Dictionary<string, object>();
    }

    #endregion
}
