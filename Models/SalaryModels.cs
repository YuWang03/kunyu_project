using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    #region 薪資查詢驗證碼寄送 (POST /app/sendcode)

    /// <summary>
    /// 薪資查詢驗證碼寄送請求
    /// </summary>
    public class SendCodeRequest
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
    }

    /// <summary>
    /// 薪資查詢驗證碼寄送回應
    /// </summary>
    public class SendCodeResponse
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

    #endregion

    #region 薪資查詢驗證碼驗證 (POST /app/sendcodecheck)

    /// <summary>
    /// 薪資查詢驗證碼驗證請求
    /// </summary>
    public class SendCodeCheckRequest
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
        /// 驗證碼（必填，4位數字）
        /// </summary>
        [Required(ErrorMessage = "verificationcode 為必填")]
        public string Verificationcode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資查詢驗證碼驗證回應
    /// </summary>
    public class SendCodeCheckResponse
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

    #endregion

    #region 薪資查詢明細 (POST /app/salaryview)

    /// <summary>
    /// 薪資查詢明細請求
    /// </summary>
    public class SalaryViewRequest
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
        /// 查詢年月（必填，格式：YYYY-MM）
        /// </summary>
        [Required(ErrorMessage = "querydate 為必填")]
        public string Querydate { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資查詢明細回應
    /// </summary>
    public class SalaryViewResponse
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
        public SalaryViewData? Data { get; set; }
    }

    /// <summary>
    /// 薪資查詢明細資料
    /// </summary>
    public class SalaryViewData
    {
        /// <summary>
        /// 工號
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 姓名
        /// </summary>
        public string Uname { get; set; } = string.Empty;

        /// <summary>
        /// 考勤週期
        /// </summary>
        public string Attendance { get; set; } = string.Empty;

        /// <summary>
        /// 薪資週期
        /// </summary>
        public string Salary { get; set; } = string.Empty;

        /// <summary>
        /// 薪資結構
        /// </summary>
        public List<SalaryStructureItem> Structure { get; set; } = new List<SalaryStructureItem>();

        /// <summary>
        /// 薪資結構合計
        /// </summary>
        public string Structuretotal { get; set; } = string.Empty;

        /// <summary>
        /// 薪資加項
        /// </summary>
        public List<SalaryAdditionalItem> Additional { get; set; } = new List<SalaryAdditionalItem>();

        /// <summary>
        /// 薪資加項標題（預設值：薪資加項合計）
        /// </summary>
        public string Aitemcount { get; set; } = "薪資加項合計";

        /// <summary>
        /// 薪資加項總計
        /// </summary>
        public string Aamountcount { get; set; } = string.Empty;

        /// <summary>
        /// 薪資減項
        /// </summary>
        public List<SalaryReductionItem> Reduction { get; set; } = new List<SalaryReductionItem>();

        /// <summary>
        /// 薪資減項標題（預設值：薪資減項合計）
        /// </summary>
        public string Ritemcount { get; set; } = "薪資減項合計";

        /// <summary>
        /// 薪資減項總計
        /// </summary>
        public string Ramountcount { get; set; } = string.Empty;

        /// <summary>
        /// 出勤記錄
        /// </summary>
        public List<AttendanceRecordItem> Record { get; set; } = new List<AttendanceRecordItem>();

        /// <summary>
        /// 應稅工資標題（大陸用，預設值：應稅工資）
        /// </summary>
        public string Taxabletitle { get; set; } = "應稅工資";

        /// <summary>
        /// 應稅工資金額（大陸用）
        /// </summary>
        public string Taxablepaid { get; set; } = string.Empty;

        /// <summary>
        /// 扣個人所得稅標題（大陸用，預設值：扣個人所得稅）
        /// </summary>
        public string Dtaxabletitle { get; set; } = "扣個人所得稅";

        /// <summary>
        /// 扣個人所得稅金額（大陸用）
        /// </summary>
        public string Dtaxablepaid { get; set; } = string.Empty;

        /// <summary>
        /// 實際發放薪資標題（預設值：實際發放薪資）
        /// </summary>
        public string Actualtitle { get; set; } = "實際發放薪資";

        /// <summary>
        /// 實際發放薪資金額
        /// </summary>
        public string Actualpaid { get; set; } = string.Empty;

        /// <summary>
        /// 薪資備註1
        /// </summary>
        public string Notes1 { get; set; } = string.Empty;

        /// <summary>
        /// 薪資備註2
        /// </summary>
        public string Notes2 { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資結構明細
    /// </summary>
    public class SalaryStructureItem
    {
        /// <summary>
        /// 結構明細名稱
        /// </summary>
        public string Sitem { get; set; } = string.Empty;

        /// <summary>
        /// 結構明細金額
        /// </summary>
        public string Samount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資加項明細
    /// </summary>
    public class SalaryAdditionalItem
    {
        /// <summary>
        /// 加項明細名稱
        /// </summary>
        public string Aitem { get; set; } = string.Empty;

        /// <summary>
        /// 加項明細金額
        /// </summary>
        public string Aamount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 薪資減項明細
    /// </summary>
    public class SalaryReductionItem
    {
        /// <summary>
        /// 減項明細名稱
        /// </summary>
        public string Ritem { get; set; } = string.Empty;

        /// <summary>
        /// 減項明細金額
        /// </summary>
        public string Ramount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 出勤記錄明細
    /// </summary>
    public class AttendanceRecordItem
    {
        /// <summary>
        /// 假別
        /// </summary>
        public string Fake { get; set; } = string.Empty;

        /// <summary>
        /// 假別時數
        /// </summary>
        public string Fakehours { get; set; } = string.Empty;
    }

    #endregion

    #region 教育訓練查詢明細 (POST /app/eduview)

    /// <summary>
    /// 教育訓練查詢明細請求
    /// </summary>
    public class EduViewRequest
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
        /// 查詢年份（必填，只能查詢近三年）
        /// </summary>
        [Required(ErrorMessage = "queryyear 為必填")]
        public string Queryyear { get; set; } = string.Empty;
    }

    /// <summary>
    /// 教育訓練查詢明細回應
    /// </summary>
    public class EduViewResponse
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
        public EduViewData? Data { get; set; }
    }

    /// <summary>
    /// 教育訓練查詢明細資料
    /// </summary>
    public class EduViewData
    {
        /// <summary>
        /// 課程資料
        /// </summary>
        public List<EduCourseItem> Edudata { get; set; } = new List<EduCourseItem>();

        /// <summary>
        /// 課程年度總時數標題（預設值：學分合計）
        /// </summary>
        public string Yearhourstitle { get; set; } = "學分合計";

        /// <summary>
        /// 課程年度總時數
        /// </summary>
        public string Yearhours { get; set; } = string.Empty;
    }

    /// <summary>
    /// 教育訓練課程明細
    /// </summary>
    public class EduCourseItem
    {
        /// <summary>
        /// 課程類別
        /// </summary>
        public string Classtype { get; set; } = string.Empty;

        /// <summary>
        /// 課程名稱
        /// </summary>
        public string Classname { get; set; } = string.Empty;

        /// <summary>
        /// 課程時數
        /// </summary>
        public string Classhours { get; set; } = string.Empty;
    }

    #endregion

    #region 驗證碼資料表 Model

    /// <summary>
    /// 薪資查詢驗證碼記錄（資料庫用）
    /// </summary>
    public class SalaryVerificationCode
    {
        /// <summary>
        /// 公司代碼
        /// </summary>
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者工號
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 驗證碼
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 過期時間
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool IsUsed { get; set; }
    }

    #endregion
}
