using HRSystemAPI.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Microsoft.Extensions.Options;
using Dapper;
using MailKit.Net.Smtp;
using MimeKit;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 薪資查詢服務實作
    /// </summary>
    public class SalaryService : ISalaryService
    {
        private readonly ILogger<SalaryService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBasicInfoService _basicInfoService;
        private readonly string _hrConnectionString;
        private readonly string _mysqlConnectionString;
        private readonly SmtpSettings _smtpSettings;
        private readonly SalaryVerificationSettings _verificationSettings;

        public SalaryService(
            ILogger<SalaryService> logger,
            IConfiguration configuration,
            IBasicInfoService basicInfoService,
            IOptions<SmtpSettings> smtpSettings,
            IOptions<SalaryVerificationSettings> verificationSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _basicInfoService = basicInfoService;
            _hrConnectionString = configuration.GetConnectionString("HRDatabase")
                ?? throw new ArgumentNullException(nameof(configuration), "HRDatabase connection string not found");
            _mysqlConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "DefaultConnection connection string not found");
            _smtpSettings = smtpSettings.Value;
            _verificationSettings = verificationSettings.Value;
        }

        /// <summary>
        /// 發送薪資查詢驗證碼至指定測試信箱
        /// </summary>
        public async Task<SendCodeResponse> SendVerificationCodeAsync(SendCodeRequest request)
        {
            try
            {
                _logger.LogInformation($"發送驗證碼至使用者 {request.Uid}");

                // 1. 從資料庫查詢使用者資料（用於取得使用者名稱）
                var employee = await _basicInfoService.GetEmployeeByIdAsync(request.Uid);
                if (employee == null)
                {
                    _logger.LogWarning($"找不到使用者：{request.Uid}");
                    return new SendCodeResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，找不到使用者資料"
                    };
                }

                // 2. 產生 4 位數隨機驗證碼
                var verificationCode = GenerateVerificationCode();
                var now = DateTime.Now;
                var expiresAt = now.AddMinutes(_verificationSettings.CodeExpirationMinutes);

                // 3. 將驗證碼與過期時間存入資料庫
                try
                {
                    await SaveVerificationCodeAsync(request.Cid, request.Uid, verificationCode, now, expiresAt);
                    _logger.LogInformation($"驗證碼已產生: {verificationCode}，將於 {expiresAt:yyyy-MM-dd HH:mm:ss} 過期（{_verificationSettings.CodeExpirationMinutes} 分鐘）");
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"儲存驗證碼到 MySQL 失敗，將使用測試驗證碼代替 - {dbEx.GetType().Name}: {dbEx.Message}");
                    // MySQL 連接失敗，但仍然允許使用測試驗證碼進行測試
                }

                // 4. 透過 SMTP 發送驗證碼到指定測試信箱 ian888.chen@gmail.com
                const string testEmail = "ian888.chen@gmail.com";
                try
                {
                    await SendEmailAsync(testEmail, employee.EmployeeName, verificationCode, _verificationSettings.CodeExpirationMinutes);
                    _logger.LogInformation($"驗證碼已發送至測試信箱：{testEmail}（使用者：{request.Uid} - {employee.EmployeeName}）");
                    
                    // 確認信件發送成功後立即回傳成功
                    return new SendCodeResponse
                    {
                        Code = "200",
                        Msg = "請求成功"
                    };
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"發送 Email 失敗：{testEmail}");
                    return new SendCodeResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，Email 發送失敗"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送驗證碼時發生錯誤");
                return new SendCodeResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 驗證薪資查詢驗證碼
        /// </summary>
        public async Task<SendCodeCheckResponse> VerifyCodeAsync(SendCodeCheckRequest request)
        {
            try
            {
                _logger.LogInformation($"驗證使用者 {request.Uid} 的驗證碼");

                // 檢查是否使用測試萬用驗證碼
                if (request.Verificationcode == _verificationSettings.TestVerificationCode)
                {
                    _logger.LogInformation($"使用者 {request.Uid} 使用測試萬用驗證碼驗證成功");
                    return new SendCodeCheckResponse
                    {
                        Code = "200",
                        Msg = "請求成功"
                    };
                }

                // 從資料庫查詢驗證碼
                var verificationRecord = await GetVerificationCodeAsync(request.Cid, request.Uid);
                
                if (verificationRecord == null)
                {
                    _logger.LogWarning($"找不到使用者 {request.Uid} 的驗證碼");
                    return new SendCodeCheckResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，找不到驗證碼或驗證碼已過期"
                    };
                }

                // 檢查驗證碼是否已使用
                if (verificationRecord.IsUsed)
                {
                    _logger.LogWarning($"使用者 {request.Uid} 的驗證碼已被使用");
                    return new SendCodeCheckResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，驗證碼已被使用"
                    };
                }

                // 檢查驗證碼是否過期（5分鐘）
                if (DateTime.Now > verificationRecord.ExpiresAt)
                {
                    _logger.LogWarning($"使用者 {request.Uid} 的驗證碼已過期");
                    return new SendCodeCheckResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，驗證碼已過期"
                    };
                }

                // 驗證驗證碼是否正確
                if (verificationRecord.Code != request.Verificationcode)
                {
                    _logger.LogWarning($"使用者 {request.Uid} 的驗證碼不正確");
                    return new SendCodeCheckResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，驗證碼不正確"
                    };
                }

                // 驗證成功，標記驗證碼為已使用（一次性使用）
                await MarkVerificationCodeAsUsedAsync(request.Cid, request.Uid);
                _logger.LogInformation($"使用者 {request.Uid} 驗證碼驗證成功");

                return new SendCodeCheckResponse
                {
                    Code = "200",
                    Msg = "請求成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證驗證碼時發生錯誤");
                return new SendCodeCheckResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 查詢薪資明細（目前為 Mock 實作，直接回傳範例資料）
        /// </summary>
        public async Task<SalaryViewResponse> GetSalaryDetailsAsync(SalaryViewRequest request)
        {
            try
            {
                _logger.LogInformation($"查詢使用者 {request.Uid} 的薪資明細，查詢年月：{request.Querydate}");

                // Mock 實作：直接回傳固定範例資料（因為目前沒有真實 DB 資料）
                var salaryData = new SalaryViewData
                {
                    Uid = "0325",
                    Uname = "王大明",
                    Attendance = "2025年08月26日～2025年09月25日",
                    Salary = "2025年09月01日～2025年09月30日",
                    Structure = new List<SalaryStructureItem>
                    {
                        new SalaryStructureItem { Sitem = "基本薪資", Samount = "30,000" },
                        new SalaryStructureItem { Sitem = "職務加給", Samount = "5,000" }
                    },
                    Structuretotal = "35,000",
                    Additional = new List<SalaryAdditionalItem>
                    {
                        new SalaryAdditionalItem { Aitem = "績效獎金", Aamount = "8,000" },
                        new SalaryAdditionalItem { Aitem = "交通津貼", Aamount = "2,000" }
                    },
                    Aitemcount = "薪資加項合計",
                    Aamountcount = "10,000",
                    Reduction = new List<SalaryReductionItem>
                    {
                        new SalaryReductionItem { Ritem = "勞保扣款", Ramount = "1,200" },
                        new SalaryReductionItem { Ritem = "健保扣款", Ramount = "800" }
                    },
                    Ritemcount = "薪資減項合計",
                    Ramountcount = "2,000",
                    Record = new List<AttendanceRecordItem>
                    {
                        new AttendanceRecordItem { Fake = "事假", Fakehours = "8" },
                        new AttendanceRecordItem { Fake = "病假", Fakehours = "4" }
                    },
                    Taxabletitle = "應稅工資",
                    Taxablepaid = "45,000",
                    Dtaxabletitle = "扣個人所得稅",
                    Dtaxablepaid = "3,000",
                    Actualtitle = "實際發放薪資",
                    Actualpaid = "42,000",
                    Notes1 = "含獎金與津貼",
                    Notes2 = "已扣除勞健保與所得稅"
                };

                _logger.LogInformation($"成功取得使用者 {request.Uid} 的薪資明細（Mock 資料）");

                return new SalaryViewResponse
                {
                    Code = "200",
                    Msg = "請求成功",
                    Data = salaryData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢薪資明細時發生錯誤");
                return new SalaryViewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 查詢教育訓練明細（先查詢資料庫，若無資料則回傳 Mock 資料）
        /// </summary>
        public async Task<EduViewResponse> GetEducationDetailsAsync(EduViewRequest request)
        {
            try
            {
                // 驗證查詢年份（只能查詢近三年）
                if (!ValidateQueryYear(request.Queryyear))
                {
                    _logger.LogWarning($"查詢年份不符合規則：{request.Queryyear}");
                    return new EduViewResponse
                    {
                        Code = "203",
                        Msg = "請求失敗，主要條件不符合"
                    };
                }

                _logger.LogInformation($"查詢使用者 {request.Uid} 的教育訓練明細，查詢年份：{request.Queryyear}");

                // 先嘗試從資料庫查詢
                var eduCourses = await QueryEducationDataFromDatabaseAsync(request);

                // 如果資料庫無資料，使用 Mock 資料
                if (eduCourses == null || eduCourses.Count == 0)
                {
                    _logger.LogInformation($"資料庫無資料，使用 Mock 資料");
                    eduCourses = GetMockEducationData();
                }

                // 計算總時數
                var totalHours = CalculateTotalHours(eduCourses);

                var eduData = new EduViewData
                {
                    Edudata = eduCourses,
                    Yearhourstitle = "學分合計",
                    Yearhours = totalHours.ToString()
                };

                _logger.LogInformation($"成功取得使用者 {request.Uid} 的教育訓練明細");

                return new EduViewResponse
                {
                    Code = "200",
                    Msg = "成功",
                    Data = eduData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢教育訓練明細時發生錯誤");
                return new EduViewResponse
                {
                    Code = "203",
                    Msg = "請求失敗，主要條件不符合"
                };
            }
        }

        /// <summary>
        /// 驗證查詢年份（只能查詢近三年）
        /// </summary>
        private bool ValidateQueryYear(string queryYear)
        {
            if (!int.TryParse(queryYear, out int year))
            {
                return false;
            }

            int currentYear = DateTime.Now.Year;
            return year >= (currentYear - 2) && year <= currentYear;
        }

        /// <summary>
        /// 從資料庫查詢教育訓練資料
        /// </summary>
        private async Task<List<EduCourseItem>> QueryEducationDataFromDatabaseAsync(EduViewRequest request)
        {
            try
            {
                using (var connection = new MySqlConnection(_mysqlConnectionString))
                {
                    string query = @"
                        SELECT classtype AS Classtype, classname AS Classname, classhours AS Classhours 
                        FROM education_training 
                        WHERE uid = @uid 
                        AND cid = @cid 
                        AND queryyear = @queryyear
                        ORDER BY classtype, classname";

                    var result = await connection.QueryAsync<EduCourseItem>(query, new
                    {
                        uid = request.Uid,
                        cid = request.Cid,
                        queryyear = request.Queryyear
                    });

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢資料庫時發生錯誤");
                // 發生錯誤時返回空列表，將使用假資料
                return new List<EduCourseItem>();
            }
        }

        /// <summary>
        /// 取得假資料（當資料庫無資料時使用）
        /// </summary>
        private List<EduCourseItem> GetMockEducationData()
        {
            return new List<EduCourseItem>
            {
                new EduCourseItem
                {
                    Classtype = "專業",
                    Classname = "高階程式設計",
                    Classhours = "3"
                },
                new EduCourseItem
                {
                    Classtype = "一般",
                    Classname = "商用英文會話",
                    Classhours = "3"
                },
                new EduCourseItem
                {
                    Classtype = "專業",
                    Classname = "專案管理實務",
                    Classhours = "2"
                }
            };
        }

        /// <summary>
        /// 計算課程總時數
        /// </summary>
        private int CalculateTotalHours(List<EduCourseItem> eduCourses)
        {
            int totalHours = 0;
            foreach (var course in eduCourses)
            {
                if (int.TryParse(course.Classhours, out int hours))
                {
                    totalHours += hours;
                }
            }
            return totalHours;
        }

        /// <summary>
        /// 產生 4 位數隨機驗證碼
        /// </summary>
        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        /// <summary>
        /// 儲存驗證碼到資料庫（MySQL）
        /// </summary>
        private async Task SaveVerificationCodeAsync(string cid, string uid, string code, DateTime createdAt, DateTime expiresAt)
        {
            const string deleteSql = @"
                DELETE FROM `SalaryVerificationCodes`
                WHERE `Cid` = @Cid AND `Uid` = @Uid;";

            const string insertSql = @"
                INSERT INTO `SalaryVerificationCodes` 
                    (`Cid`, `Uid`, `Code`, `CreatedAt`, `ExpiresAt`, `IsUsed`)
                VALUES 
                    (@Cid, @Uid, @Code, @CreatedAt, @ExpiresAt, 0);";

            try
            {
                _logger.LogInformation($"[SaveVerificationCode] 開始儲存驗證碼 - Cid={cid}, Uid={uid}, Code={code}");
                _logger.LogInformation($"[SaveVerificationCode] MySQL 連接字串: {_mysqlConnectionString}");

                using var connection = new MySqlConnection(_mysqlConnectionString);
                await connection.OpenAsync();
                _logger.LogInformation($"[SaveVerificationCode] MySQL 連接成功");

                // 先刪除該使用者的舊驗證碼
                var deleteResult = await connection.ExecuteAsync(deleteSql, new { Cid = cid, Uid = uid });
                _logger.LogInformation($"[SaveVerificationCode] 已刪除 {deleteResult} 筆舊驗證碼");

                // 插入新的驗證碼
                var insertResult = await connection.ExecuteAsync(insertSql, new
                {
                    Cid = cid,
                    Uid = uid,
                    Code = code,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                });
                _logger.LogInformation($"[SaveVerificationCode] 已插入 {insertResult} 筆新驗證碼");
                _logger.LogInformation($"驗證碼已儲存到 MySQL 資料庫：Uid={uid}, Code={code}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SaveVerificationCode] 儲存驗證碼到 MySQL 資料庫失敗 - {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 從資料庫查詢驗證碼（MySQL）
        /// </summary>
        private async Task<SalaryVerificationCode?> GetVerificationCodeAsync(string cid, string uid)
        {
            const string sql = @"
                SELECT 
                    `Cid`, `Uid`, `Code`, `CreatedAt`, `ExpiresAt`, `IsUsed`
                FROM `SalaryVerificationCodes`
                WHERE `Cid` = @Cid AND `Uid` = @Uid
                ORDER BY `CreatedAt` DESC
                LIMIT 1;";

            try
            {
                _logger.LogInformation($"[GetVerificationCode] 開始查詢驗證碼 - Cid={cid}, Uid={uid}");
                _logger.LogInformation($"[GetVerificationCode] MySQL 連接字串: {_mysqlConnectionString}");

                using var connection = new MySqlConnection(_mysqlConnectionString);
                await connection.OpenAsync();
                _logger.LogInformation($"[GetVerificationCode] MySQL 連接成功");

                var result = await connection.QueryFirstOrDefaultAsync<SalaryVerificationCode>(sql, new
                {
                    Cid = cid,
                    Uid = uid
                });

                if (result == null)
                {
                    _logger.LogWarning($"[GetVerificationCode] 找不到驗證碼 - Cid={cid}, Uid={uid}");
                }
                else
                {
                    _logger.LogInformation($"[GetVerificationCode] 查詢成功 - Code={result.Code}, IsUsed={result.IsUsed}, ExpiresAt={result.ExpiresAt}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetVerificationCode] 從 MySQL 資料庫查詢驗證碼失敗 - {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 標記驗證碼為已使用（MySQL）
        /// </summary>
        private async Task MarkVerificationCodeAsUsedAsync(string cid, string uid)
        {
            const string sql = @"
                UPDATE `SalaryVerificationCodes`
                SET `IsUsed` = 1
                WHERE `Cid` = @Cid AND `Uid` = @Uid;";

            try
            {
                using var connection = new MySqlConnection(_mysqlConnectionString);
                await connection.ExecuteAsync(sql, new
                {
                    Cid = cid,
                    Uid = uid
                });

                _logger.LogInformation($"驗證碼已標記為已使用：Uid={uid}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "標記驗證碼為已使用失敗");
                throw;
            }
        }

        /// <summary>
        /// 發送驗證碼 Email
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string userName, string verificationCode, int expirationMinutes)
        {
            try
            {
                _logger.LogInformation($"準備發送 Email - Host: {_smtpSettings.Host}, Port: {_smtpSettings.Port}, From: {_smtpSettings.FromEmail}, To: {toEmail}");
                
                // 建立郵件訊息
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "【廣宇科技】薪資查詢驗證碼";

                // 設定郵件內容
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>薪資查詢驗證碼</h2>
    <p>親愛的 {userName}，您好：</p>
    <p>您的薪資查詢驗證碼為：</p>
    <h1 style='color: #0066cc; font-size: 32px; letter-spacing: 5px;'>{verificationCode}</h1>
    <p>此驗證碼將於 <strong>{expirationMinutes} 分鐘</strong>後失效，請盡快使用。</p>
    <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>
        此為系統自動發送的郵件，請勿直接回覆。<br>
        如有疑問，請聯繫人力資源部門。
    </p>
    <p style='color: #666; font-size: 12px;'>
        廣宇科技人力資源系統<br>
        {DateTime.Now:yyyy-MM-dd HH:mm:ss}
    </p>
</body>
</html>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                // 使用 MailKit 的 SmtpClient
                using var client = new MailKit.Net.Smtp.SmtpClient();
                
                _logger.LogInformation("正在連接 SMTP 伺服器...");
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.None);
                
                _logger.LogInformation("正在進行 SMTP 驗證...");
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                
                _logger.LogInformation("正在發送 Email...");
                await client.SendAsync(message);
                
                _logger.LogInformation("正在中斷 SMTP 連接...");
                await client.DisconnectAsync(true);
                
                _logger.LogInformation($"驗證碼 Email 已成功發送至：{toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"發送驗證碼 Email 失敗：{toEmail} - {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
}
