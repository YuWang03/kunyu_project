using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    #region Business Card API Models

    /// <summary>
    /// 電子名片 API 請求模型
    /// POST /app/businesscard
    /// </summary>
    public class BusinessCardRequest
    {
        /// <summary>
        /// Token 標記
        /// </summary>
        [JsonPropertyName("tokenid")]
        public string? TokenId { get; set; }

        /// <summary>
        /// 目前所屬公司
        /// </summary>
        [JsonPropertyName("cid")]
        public string? Cid { get; set; }

        /// <summary>
        /// 使用者工號
        /// </summary>
        [JsonPropertyName("uid")]
        public string? Uid { get; set; }
    }

    /// <summary>
    /// 電子名片 API 回應模型
    /// </summary>
    public class BusinessCardResponse
    {
        /// <summary>
        /// 是否成功 (依 code 定義內容)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 失敗訊息 (依 code 回覆對應訊息)
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = "成功";

        /// <summary>
        /// 數據返回區 (成功:有此區 / 失敗:無此區)
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BusinessCardData? Data { get; set; }
    }

    /// <summary>
    /// 電子名片資料
    /// </summary>
    public class BusinessCardData
    {
        /// <summary>
        /// 公司名稱
        /// </summary>
        [JsonPropertyName("ucompany")]
        public string? UCompany { get; set; }

        /// <summary>
        /// 公司代碼
        /// </summary>
        [JsonPropertyName("ucompanyid")]
        public string? UCompanyId { get; set; }

        /// <summary>
        /// 員工中文姓名
        /// </summary>
        [JsonPropertyName("uname")]
        public string? UName { get; set; }

        /// <summary>
        /// 員工英文姓名
        /// </summary>
        [JsonPropertyName("uename")]
        public string? UEName { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        [JsonPropertyName("utitle")]
        public string? UTitle { get; set; }

        /// <summary>
        /// 所屬部門
        /// </summary>
        [JsonPropertyName("ucunit")]
        public string? UCUnit { get; set; }

        /// <summary>
        /// 電子信箱
        /// </summary>
        [JsonPropertyName("umail")]
        public string? UMail { get; set; }

        /// <summary>
        /// 公司電話
        /// </summary>
        [JsonPropertyName("utel")]
        public string? UTel { get; set; }

        /// <summary>
        /// 公司網站
        /// </summary>
        [JsonPropertyName("uwebsite")]
        public string? UWebsite { get; set; }

        /// <summary>
        /// 公司地址
        /// </summary>
        [JsonPropertyName("uaddress")]
        public string? UAddress { get; set; }

        /// <summary>
        /// 手機號碼
        /// </summary>
        [JsonPropertyName("uphone")]
        public string? UPhone { get; set; }

        /// <summary>
        /// Line ID
        /// </summary>
        [JsonPropertyName("ulineid")]
        public string? ULineId { get; set; }

        /// <summary>
        /// 微信 ID
        /// </summary>
        [JsonPropertyName("uwechatid")]
        public string? UWeChatId { get; set; }

        /// <summary>
        /// 個人 QR Code 位置
        /// </summary>
        [JsonPropertyName("uqrcode")]
        public string? UQrCode { get; set; }
    }

    #endregion

    #region Employee View Models

    /// <summary>
    /// vwZZ_EMPLOYEE 視圖完整欄位模型
    /// 用於電子名片查詢
    /// </summary>
    public class EmployeeViewData
    {
        public string? EMPLOYEE_CARD_NO { get; set; }
        public string? EMPLOYEE_CNAME { get; set; }
        public string? EMPLOYEE_LASTNAME { get; set; }
        public string? EMPLOYEE_FIRSTNAME { get; set; }
        public DateTime? EMPLOYEE_BIRTHDAY { get; set; }
        public string? EMPLOYEE_IDC_NO { get; set; }
        public string? EMPLOYEE_SEX { get; set; }
        public string? EMPLOYEE_NATIONALITY_CODE { get; set; }
        public string? EMPLOYEE_NATIONALITY_CNAME { get; set; }
        public string? EMPLOYEE_OFFICE_TEL_1 { get; set; }
        public string? EMPLOYEE_OFFICE_TEL_2 { get; set; }
        public string? EMPLOYEE_EMAIL_1 { get; set; }
        public string? EMPLOYEE_EMAIL_2 { get; set; }
        public string? EMPLOYEE_LIVE_TEL { get; set; }
        public string? EMPLOYEE_LIVE_ADDRESS_AREA { get; set; }
        public string? EMPLOYEE_LIVE_ADDRESS { get; set; }
        public string? EMPLOYEE_LIVE_POSTCODE { get; set; }
        public string? EMPLOYEE_CONTACT_TEL { get; set; }
        public string? EMPLOYEE_CONTACT_TEL_1 { get; set; }
        public string? EMPLOYEE_CONTACT_ADDRESS_AREA { get; set; }
        public string? EMPLOYEE_CONTACT_ADDRESS { get; set; }
        public string? EMPLOYEE_CONTACT_POSTCODE { get; set; }
        public string? EMPLOYEE_PERSONAL_EMAIL { get; set; }
        public DateTime? EMPLOYEE_HIRE_DATE { get; set; }
        public DateTime? EMPLOYEE_TEST_END_DATE { get; set; }
        public string? EMPLOYEE_WORK_STATUS { get; set; }
        public string? EMPLOYEE_TYPE { get; set; }
        public string? SECTION_TYPE { get; set; }
        public string? SECTION_ID { get; set; }
        public string? SECTION_CODE { get; set; }
        public string? SECTION_CNAME { get; set; }
        public string? TOPIC_FIRST_BOSS_ID { get; set; }
        public string? TOPIC_SECOND_BOSS_ID { get; set; }
        public string? IDENTITY_CODE { get; set; }
        public string? IDENTITY_CNAME { get; set; }
        public DateTime? EMPLOYEE_ORG_START_DATE { get; set; }
        public DateTime? EMPLOYEE_ORG_END_DATE { get; set; }
        public DateTime? EMPLOYEE_QUIT_DATE { get; set; }
        public DateTime? EMPLOYEE_RETURN_DATE { get; set; }
        public DateTime? EMPLOYEE_STOP_DATE { get; set; }
        public DateTime? EMPLOYEE_STOP_PRE_RETURN_DATE { get; set; }
        public DateTime? EMPLOYEE_STOP_RETURN_DATE { get; set; }
        public string? EMPLOYEE_RESPONSIBILITY { get; set; }
        public string? EMPLOYEE_IS_CARD { get; set; }
        public string? COMPANY_ID { get; set; }
        public string? COMPANY_CODE { get; set; }
        public string? COMPANY_CNAME { get; set; }
        public string? DEPARTMENT_ID { get; set; }
        public string? DEPARTMENT_CODE { get; set; }
        public string? DEPARTMENT_CNAME { get; set; }
        public string? JOB_CODE { get; set; }
        public string? JOB_CNAME { get; set; }
        public string? JOB_ENAME { get; set; }
        public string? POSITION_CODE { get; set; }
        public string? POSITION_CNAME { get; set; }
        public string? POST_CLASS_CODE { get; set; }
        public string? POST_CLASS_CNAME { get; set; }
        public string? GRADE_CODE { get; set; }
        public string? GRADE_CNAME { get; set; }
        public string? LEVEL_CODE { get; set; }
        public string? LEVEL_CNAME { get; set; }
        public string? SITE_CODE { get; set; }
        public string? SITE_CNAME { get; set; }
        public string? AREA_CODE { get; set; }
        public string? AREA_CNAME { get; set; }
        public string? PINHOLE_PLOTTER_CODE { get; set; }
        public string? PINHOLE_PLOTTER_CNAME { get; set; }
        public string? INVESTIGATIO_CLASS_CODE { get; set; }
        public string? INVESTIGATIO_CLASS_CNAME { get; set; }
        public string? PRODUCT_CLASS_CODE { get; set; }
        public string? PRODUCT_CLASS_CNAME { get; set; }
        public string? FACTORY_CODE { get; set; }
        public string? FACTORY_CNAME { get; set; }
        public string? BENCH_CODE { get; set; }
        public string? BENCH_CNAME { get; set; }
        public string? COMPANY_SNO_ID { get; set; }
        public string? COMPANY_SNO_CODE { get; set; }
        public string? COMPANY_SNO_CNAME { get; set; }
        public string? LABOR_NO_ID { get; set; }
        public string? LABOR_NO { get; set; }
        public string? LABOR_NO_NAME { get; set; }
        public string? HEALTH_NO_ID { get; set; }
        public string? HEALTH_NO { get; set; }
        public string? HEALTH_NO_NAME { get; set; }
        public string? EMPLOYEE_BANK_CODE1 { get; set; }
        public string? EMPLOYEE_BANK_CNAME1 { get; set; }
        public string? EMPLOYEE_BRANCH1 { get; set; }
        public string? EMPLOYEE_BACC1_NO { get; set; }
        public string? EMPLOYEE_BANK_CODE2 { get; set; }
        public string? EMPLOYEE_BANK_CNAME2 { get; set; }
        public string? EMPLOYEE_BRANCH2 { get; set; }
        public string? EMPLOYEE_BACC2_NO { get; set; }
        public string? EMPLOYEE_EDUCATIONAL_CODE { get; set; }
        public string? EMPLOYEE_EDUCATIONAL_CNAME { get; set; }
        public string? EMPLOYEE_SCHOOL_CNAME { get; set; }
        public string? EMPLOYEE_REMARK { get; set; }
        public string? EMPLOYEE_ACCOUNT { get; set; }
        public string? EMPLOYEE_URGENCY_CONTACT { get; set; }
        public string? EMPLOYEE_URGENCY_TEL { get; set; }
        public DateTime? EMPLOYEE_WORK_YEAR_STARTDATE { get; set; }
        public string? NAMECARD_JOB_CODE { get; set; }
        public string? NAMECARD_JOB_CNAME { get; set; }
        public string? NAMECARD_JOB_ENAME { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? EMPLOYEE_PASSPORT_NAME { get; set; }
        public string? EMPLOYEE_INSURANCE_NO { get; set; }
        public decimal? EMPLOYEE_HEIGHT { get; set; }
        public decimal? EMPLOYEE_WEIGHT { get; set; }
        public string? EMPLOYEE_PICTURE_1 { get; set; }
        public string? EMPLOYEE_NO { get; set; }
    }

    #endregion

    #region Business Card Settings

    /// <summary>
    /// 電子名片公司設定
    /// 根據公司別判斷電話、公司網站 URL、地址
    /// </summary>
    public class BusinessCardCompanySettings
    {
        /// <summary>
        /// 公司代碼
        /// </summary>
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// 公司電話
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 公司網站
        /// </summary>
        public string Website { get; set; } = string.Empty;

        /// <summary>
        /// 公司地址
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }

    #endregion
}
