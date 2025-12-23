using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using Dapper;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子名片服務實作
    /// 從 vwZZ_EMPLOYEE 視圖取得員工資料並轉換為電子名片格式
    /// </summary>
    public class BusinessCardService : IBusinessCardService
    {
        private readonly string _connectionString;
        private readonly ILogger<BusinessCardService> _logger;
        private readonly IConfiguration _configuration;

        // 電子名片網站基礎 URL
        private const string BUSINESS_CARD_BASE_URL = "https://app.panpi.com.tw/businesscard";

        // 公司設定 (根據公司代碼判斷電話、網站、地址)
        private static readonly Dictionary<string, BusinessCardCompanySettings> CompanySettingsMap = new()
        {
            // 廣宇科技 (預設)
            ["03546618"] = new BusinessCardCompanySettings
            {
                CompanyCode = "03546618",
                Phone = "+886-2-2221-3066",
                Website = "https://www.panpi.com.tw/tw",
                Address = "235新北市中和區建八路200號6樓"
            },
            // 可根據需求添加其他公司設定
        };

        public BusinessCardService(
            IConfiguration configuration,
            ILogger<BusinessCardService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration), "HRDatabase connection string is required");
        }

        /// <summary>
        /// 取得員工電子名片資料
        /// </summary>
        public async Task<BusinessCardResponse> GetBusinessCardAsync(BusinessCardRequest request)
        {
            try
            {
                _logger.LogInformation("取得電子名片 - TokenId: {TokenId}, Cid: {Cid}, Uid: {Uid}",
                    request.TokenId, request.Cid, request.Uid);

                // 1. 驗證必要參數
                if (string.IsNullOrWhiteSpace(request.Uid))
                {
                    _logger.LogWarning("缺少使用者工號");
                    return new BusinessCardResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }

                // 2. 從資料庫取得員工資料
                var employeeData = await GetEmployeeViewDataAsync(request.Uid, request.Cid);

                if (employeeData == null)
                {
                    _logger.LogWarning("找不到員工資料: Uid={Uid}, Cid={Cid}", request.Uid, request.Cid);
                    return new BusinessCardResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合",
                        Data = null
                    };
                }

                // 3. 取得公司設定
                var companySettings = GetCompanySettings(employeeData.COMPANY_CODE ?? request.Cid);

                // 4. 組合英文姓名
                var englishName = BuildEnglishName(employeeData);

                // 5. 組合公司電話 (含分機)
                var officeTel = BuildOfficeTel(employeeData, companySettings);

                // 6. 產生 QR Code URL
                var qrCodeUrl = GenerateQrCodeUrl(
                    employeeData.COMPANY_CODE ?? request.Cid, 
                    employeeData.EMPLOYEE_NO ?? request.Uid);

                // 7. 建立回應資料
                var businessCardData = new BusinessCardData
                {
                    UCompany = employeeData.COMPANY_CNAME ?? "",
                    UCompanyId = employeeData.COMPANY_CODE ?? request.Cid ?? "",
                    UName = employeeData.EMPLOYEE_CNAME ?? "",
                    UEName = englishName,
                    UTitle = employeeData.NAMECARD_JOB_CNAME ?? employeeData.JOB_CNAME ?? "",
                    UCUnit = employeeData.DEPARTMENT_CNAME ?? "",
                    UMail = employeeData.EMPLOYEE_EMAIL_1 ?? "",
                    UTel = officeTel,
                    UWebsite = companySettings.Website,
                    UAddress = companySettings.Address,
                    // 以下欄位預設為空，使用者可於 APP 端設定
                    UPhone = "", // 手機號碼 (APP 端填寫)
                    ULineId = "", // Line ID (APP 端填寫)
                    UWeChatId = "", // 微信 ID (APP 端填寫)
                    UQrCode = qrCodeUrl
                };

                _logger.LogInformation("成功取得電子名片: {Name} ({Uid})", 
                    businessCardData.UName, request.Uid);

                return new BusinessCardResponse
                {
                    Code = "200",
                    Msg = "成功",
                    Data = businessCardData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得電子名片時發生錯誤: Uid={Uid}", request.Uid);
                return new BusinessCardResponse
                {
                    Code = "500",
                    Msg = $"伺服器錯誤: {ex.Message}",
                    Data = null
                };
            }
        }

        /// <summary>
        /// 從 vwZZ_EMPLOYEE 視圖取得員工完整資料
        /// </summary>
        public async Task<EmployeeViewData?> GetEmployeeViewDataAsync(string uid, string? cid)
        {
            const string sql = @"
                SELECT 
                    EMPLOYEE_CARD_NO,
                    EMPLOYEE_CNAME,
                    EMPLOYEE_LASTNAME,
                    EMPLOYEE_FIRSTNAME,
                    EMPLOYEE_BIRTHDAY,
                    EMPLOYEE_IDC_NO,
                    EMPLOYEE_SEX,
                    EMPLOYEE_NATIONALITY_CODE,
                    EMPLOYEE_NATIONALITY_CNAME,
                    EMPLOYEE_OFFICE_TEL_1,
                    EMPLOYEE_OFFICE_TEL_2,
                    EMPLOYEE_EMAIL_1,
                    EMPLOYEE_EMAIL_2,
                    EMPLOYEE_LIVE_TEL,
                    EMPLOYEE_LIVE_ADDRESS_AREA,
                    EMPLOYEE_LIVE_ADDRESS,
                    EMPLOYEE_LIVE_POSTCODE,
                    EMPLOYEE_CONTACT_TEL,
                    EMPLOYEE_CONTACT_TEL_1,
                    EMPLOYEE_CONTACT_ADDRESS_AREA,
                    EMPLOYEE_CONTACT_ADDRESS,
                    EMPLOYEE_CONTACT_POSTCODE,
                    EMPLOYEE_PERSONAL_EMAIL,
                    EMPLOYEE_HIRE_DATE,
                    EMPLOYEE_TEST_END_DATE,
                    EMPLOYEE_WORK_STATUS,
                    EMPLOYEE_TYPE,
                    SECTION_TYPE,
                    SECTION_ID,
                    SECTION_CODE,
                    SECTION_CNAME,
                    TOPIC_FIRST_BOSS_ID,
                    TOPIC_SECOND_BOSS_ID,
                    IDENTITY_CODE,
                    IDENTITY_CNAME,
                    EMPLOYEE_ORG_START_DATE,
                    EMPLOYEE_ORG_END_DATE,
                    EMPLOYEE_QUIT_DATE,
                    EMPLOYEE_RETURN_DATE,
                    EMPLOYEE_STOP_DATE,
                    EMPLOYEE_STOP_PRE_RETURN_DATE,
                    EMPLOYEE_STOP_RETURN_DATE,
                    EMPLOYEE_RESPONSIBILITY,
                    EMPLOYEE_IS_CARD,
                    COMPANY_ID,
                    COMPANY_CODE,
                    COMPANY_CNAME,
                    DEPARTMENT_ID,
                    DEPARTMENT_CODE,
                    DEPARTMENT_CNAME,
                    JOB_CODE,
                    JOB_CNAME,
                    JOB_ENAME,
                    POSITION_CODE,
                    POSITION_CNAME,
                    POST_CLASS_CODE,
                    POST_CLASS_CNAME,
                    GRADE_CODE,
                    GRADE_CNAME,
                    LEVEL_CODE,
                    LEVEL_CNAME,
                    SITE_CODE,
                    SITE_CNAME,
                    AREA_CODE,
                    AREA_CNAME,
                    PINHOLE_PLOTTER_CODE,
                    PINHOLE_PLOTTER_CNAME,
                    INVESTIGATIO_CLASS_CODE,
                    INVESTIGATIO_CLASS_CNAME,
                    PRODUCT_CLASS_CODE,
                    PRODUCT_CLASS_CNAME,
                    FACTORY_CODE,
                    FACTORY_CNAME,
                    BENCH_CODE,
                    BENCH_CNAME,
                    COMPANY_SNO_ID,
                    COMPANY_SNO_CODE,
                    COMPANY_SNO_CNAME,
                    LABOR_NO_ID,
                    LABOR_NO,
                    LABOR_NO_NAME,
                    HEALTH_NO_ID,
                    HEALTH_NO,
                    HEALTH_NO_NAME,
                    EMPLOYEE_BANK_CODE1,
                    EMPLOYEE_BANK_CNAME1,
                    EMPLOYEE_BRANCH1,
                    EMPLOYEE_BACC1_NO,
                    EMPLOYEE_BANK_CODE2,
                    EMPLOYEE_BANK_CNAME2,
                    EMPLOYEE_BRANCH2,
                    EMPLOYEE_BACC2_NO,
                    EMPLOYEE_EDUCATIONAL_CODE,
                    EMPLOYEE_EDUCATIONAL_CNAME,
                    EMPLOYEE_SCHOOL_CNAME,
                    EMPLOYEE_REMARK,
                    EMPLOYEE_ACCOUNT,
                    EMPLOYEE_URGENCY_CONTACT,
                    EMPLOYEE_URGENCY_TEL,
                    EMPLOYEE_WORK_YEAR_STARTDATE,
                    NAMECARD_JOB_CODE,
                    NAMECARD_JOB_CNAME,
                    NAMECARD_JOB_ENAME,
                    UPDATE_DATE,
                    EMPLOYEE_PASSPORT_NAME,
                    EMPLOYEE_INSURANCE_NO,
                    EMPLOYEE_HEIGHT,
                    EMPLOYEE_WEIGHT,
                    EMPLOYEE_PICTURE_1,
                    EMPLOYEE_NO
                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                WHERE EMPLOYEE_NO = @Uid COLLATE Chinese_Taiwan_Stroke_CI_AS";

            try
            {
                _logger.LogDebug("查詢員工資料: Uid={Uid}, Cid={Cid}", uid, cid);

                using var connection = new SqlConnection(_connectionString);
                var result = await connection.QueryFirstOrDefaultAsync<EmployeeViewData>(sql, new { Uid = uid });

                if (result != null)
                {
                    _logger.LogDebug("找到員工: {Name} ({EmployeeNo})", 
                        result.EMPLOYEE_CNAME, result.EMPLOYEE_NO);
                }
                else
                {
                    _logger.LogDebug("找不到員工: Uid={Uid}", uid);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢員工資料時發生錯誤: Uid={Uid}", uid);
                throw;
            }
        }

        /// <summary>
        /// 根據公司代碼取得公司設定
        /// </summary>
        public BusinessCardCompanySettings GetCompanySettings(string? companyCode)
        {
            if (!string.IsNullOrWhiteSpace(companyCode) && 
                CompanySettingsMap.TryGetValue(companyCode, out var settings))
            {
                return settings;
            }

            // 預設使用廣宇科技的設定
            return CompanySettingsMap["03546618"];
        }

        /// <summary>
        /// 產生個人 QR Code URL
        /// 格式: https://app.panpi.com.tw/businesscard/公司代碼|員工號
        /// </summary>
        public string GenerateQrCodeUrl(string? companyCode, string? employeeNo)
        {
            var code = companyCode ?? "03546618";
            var empNo = employeeNo ?? "";

            return $"{BUSINESS_CARD_BASE_URL}/{code}|{empNo}";
        }

        /// <summary>
        /// 組合英文姓名
        /// 優先使用 EMPLOYEE_PASSPORT_NAME，其次使用 FIRSTNAME + LASTNAME
        /// </summary>
        private string BuildEnglishName(EmployeeViewData data)
        {
            // 優先使用護照名
            if (!string.IsNullOrWhiteSpace(data.EMPLOYEE_PASSPORT_NAME))
            {
                return data.EMPLOYEE_PASSPORT_NAME;
            }

            // 其次使用名片職稱英文名
            if (!string.IsNullOrWhiteSpace(data.NAMECARD_JOB_ENAME))
            {
                // 如果有 JOB_ENAME，通常是職稱，不是人名
            }

            // 組合 FirstName + LastName
            var firstName = data.EMPLOYEE_FIRSTNAME?.Trim() ?? "";
            var lastName = data.EMPLOYEE_LASTNAME?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                return $"{firstName} {lastName}";
            }
            else if (!string.IsNullOrWhiteSpace(firstName))
            {
                return firstName;
            }
            else if (!string.IsNullOrWhiteSpace(lastName))
            {
                return lastName;
            }

            return "";
        }

        /// <summary>
        /// 組合公司電話 (含分機)
        /// </summary>
        private string BuildOfficeTel(EmployeeViewData data, BusinessCardCompanySettings companySettings)
        {
            var baseTel = companySettings.Phone;

            // 如果員工有分機號碼
            if (!string.IsNullOrWhiteSpace(data.EMPLOYEE_OFFICE_TEL_1))
            {
                var ext = data.EMPLOYEE_OFFICE_TEL_1.Trim();
                
                // 如果分機看起來像是完整電話號碼
                if (ext.Contains("-") || ext.Length > 6)
                {
                    return ext;
                }
                
                // 否則附加為分機
                return $"{baseTel} # {ext}";
            }

            return baseTel;
        }
    }
}
