using HRSystemAPI.Models;
using HRSystemAPI.Services;
using HRSystemAPI.Filters;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== 設定監聽所有網卡（允許外網訪問）=====
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5112); // HTTP
    // options.ListenAnyIP(7112, listenOptions => { listenOptions.UseHttps(); }); // HTTPS（如需要）
});

// Add services to the container.
builder.Services.AddControllers();

// ===== 註冊原有服務 =====
builder.Services.AddScoped<IBasicInfoService, BasicInfoService>();
// builder.Services.AddScoped<IAttendanceService, AttendanceService>(); // 舊版 AttendanceQuery，已棄用
builder.Services.AddScoped<IWorkQueryService, WorkQueryService>(); // 考勤查詢服務 (原 AttendanceService)
// builder.Services.AddScoped<ILeaveRemainService, LeaveRemainService>(); // 舊版，已棄用
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>(); // 新版請假餘額查詢服務
builder.Services.AddScoped<IOvertimeFormService, OvertimeFormService>();
builder.Services.AddScoped<ILeaveFormService, LeaveFormService>();
builder.Services.AddScoped<IAttendanceFormService, AttendanceFormService>();
builder.Services.AddScoped<IBusinessTripFormService, BusinessTripFormService>();
builder.Services.AddScoped<IAttendancePatchFormService, AttendancePatchFormService>();
builder.Services.AddScoped<ILeaveOutFormService, LeaveOutFormService>();
builder.Services.AddScoped<IWorkSetService, WorkSetService>(); // 考勤超時出勤設定服務
builder.Services.AddScoped<ICancelLeaveService, CancelLeaveService>(); // 銷假單服務
builder.Services.AddScoped<IReviewService, ReviewService>(); // 待我審核服務

// ===== 註冊 BPM Read 服務 =====
builder.Services.AddScoped<IBpmReadService, BpmReadService>(); // BPM 表單讀取服務

// ===== 註冊電子名片服務 =====
builder.Services.AddScoped<IBusinessCardService, BusinessCardService>(); // 電子名片服務

// ===== 註冊錯誤日誌服務 =====
// 暫時不連接 MySQL，使用 Mock 服務
// builder.Services.AddDbContext<ApiErrorLogDbContext>(options =>
//     options.UseMySql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         new MySqlServerVersion(new Version(8, 0, 21)) // 使用固定版本避免啟動時連線
//     )
// );
builder.Services.AddScoped<IApiErrorLogService, ApiErrorLogService>();

// ===== 註冊 BPM 表單同步服務 (MySQL - 本地和遠端) =====
// 暫時不連接 MySQL
// 本地資料庫 (localhost:3307)
// builder.Services.AddDbContext<BpmFormDbContext>(options =>
//     options.UseMySql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         new MySqlServerVersion(new Version(8, 0, 21)) // 使用固定版本避免啟動時連線
//     )
// );
// 遠端資料庫 (54.46.24.34:3306)
// builder.Services.AddDbContext<RemoteBpmFormDbContext>(options =>
//     options.UseMySql(
//         builder.Configuration.GetConnectionString("RemoteBpmDb"),
//         new MySqlServerVersion(new Version(8, 0, 21)) // 使用固定版本避免啟動時連線
//     )
// );
builder.Services.AddScoped<IBpmFormRepository, BpmFormRepository>();
builder.Services.AddScoped<IBpmFormSyncService, BpmFormSyncService>();

// ===== 註冊請假單申請記錄服務 =====
builder.Services.AddScoped<ILeaveApplicationRepository, LeaveApplicationRepository>();

// ===== 註冊薪資查詢服務 =====
builder.Services.AddScoped<ISalaryService, SalaryService>();

// ===== 註冊 Token 驗證服務 =====
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();
builder.Services.AddScoped<TokenValidationFilter>();

// ===== 註冊電子表單選單服務 =====
builder.Services.AddScoped<IEFormsMenuService, EFormsMenuService>();

// ===== 註冊電子表單類型服務 =====
builder.Services.AddScoped<IEFormTypeService, EFormTypeService>();

// ===== 註冊我的表單服務 =====
builder.Services.AddScoped<IEFormMyService, EFormMyService>();

// ===== 註冊簽核記錄數據庫服務 (54.46.24.34) =====
builder.Services.AddDbContext<EFormApprovalDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))
    )
);
builder.Services.AddScoped<IEFormApprovalRepository, EFormApprovalRepository>();

// ===== 註冊我的簽核列表服務 =====
builder.Services.AddScoped<IEFormMySignService, EFormMySignService>();

// ===== 註冊簽核記錄詳細資料服務 =====
builder.Services.AddScoped<IEFormRecordService, EFormRecordService>();

// ===== 註冊簽核記錄撤回服務 =====
builder.Services.AddScoped<IEFormWithdrawService, EFormWithdrawService>();

// ===== 註冊 FTP 和 BPM 設定 =====
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("FtpSettings"));
builder.Services.Configure<BpmSettings>(builder.Configuration.GetSection("BpmSettings"));
builder.Services.Configure<AttachmentSettings>(builder.Configuration.GetSection("AttachmentSettings"));

// ===== 註冊 SMTP 和薪資驗證設定 =====
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<SalaryVerificationSettings>(builder.Configuration.GetSection("SalaryVerification"));

// ===== 註冊 FTP 服務 =====
builder.Services.AddScoped<FtpService>();

// ===== 註冊 HttpClient（用於附件服務查詢）=====
builder.Services.AddHttpClient<IAttachmentService, AttachmentService>(client =>
{
    // 設定較長的超時時間以處理大型檔案查詢
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ===== 註冊 HttpClient（使用 Header 認證）=====
// ⭐⭐⭐ 重要:BPM API 使用 X-API-Key 和 X-API-Secret Header 進行認證 ⭐⭐⭐
builder.Services.AddHttpClient("BpmClient", (serviceProvider, client) =>
{
    var bpmSettings = builder.Configuration.GetSection("BpmSettings").Get<BpmSettings>();
    if (bpmSettings != null && !string.IsNullOrEmpty(bpmSettings.ApiBaseUrl))
    {
        // 設定 BaseAddress（必須以 "/" 結尾）
        var baseUrl = bpmSettings.ApiBaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        
        // ⭐ 重要：設定 Header 認證（這兩行是關鍵！）
        client.DefaultRequestHeaders.Add("X-API-Key", bpmSettings.ApiKey);
        client.DefaultRequestHeaders.Add("X-API-Secret", bpmSettings.ApiSecret);
        
        // 設定 Content-Type
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        
        // 設定 Timeout
        client.Timeout = TimeSpan.FromSeconds(bpmSettings.Timeout);

        // 記錄日誌（方便除錯）
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("====================================");
        logger.LogInformation("BPM HttpClient 已設定");
        logger.LogInformation("  - BaseAddress: {BaseUrl}", baseUrl);
        logger.LogInformation("  - X-API-Key: {ApiKey}", bpmSettings.ApiKey);
        logger.LogInformation("  - X-API-Secret: {ApiSecret}", 
            string.IsNullOrEmpty(bpmSettings.ApiSecret) ? "(未設定)" : "******");
        logger.LogInformation("  - Timeout: {Timeout} 秒", bpmSettings.Timeout);
        logger.LogInformation("====================================");
    }
});

// ===== 註冊 Token 驗證專用 HttpClient =====
builder.Services.AddHttpClient("TokenValidationClient", client =>
{
    // 設定較短的超時時間以加快故障檢測（Token 驗證 API 應該快速回應）
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ===== 註冊服務 =====
builder.Services.AddSingleton<FtpService>();
builder.Services.AddScoped<BpmService>();
builder.Services.AddScoped<IBpmMiddlewareService, BpmMiddlewareService>();
builder.Services.AddScoped<AttendanceFormService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "廣宇科技 HR System API",
        Version = "v1",
        Description = @"## API 架構說明

### 帳號整合
* 提供 Keycloak OIDC 登入整合
* 支援 Access Token 和 Refresh Token
* 查詢使用者資訊

### 基本資料
* 查詢員工基本資訊（工號、姓名、部門、職稱等）

### 考勤查詢
* 查看個人出勤記錄
* 支援查詢指定日期的打卡記錄
* 顯示上下班刷卡時間與異常狀態

### 請假剩餘天數
* 查詢各類假別剩餘天數（特休、事假、病假、補休假）
* 支援周年制計算
* 自動轉換天數與小時數

### 電子表單功能（整合 BPM 系統）
* **請假單申請與簽核**
  - 支援年、月、日、區間查詢
  - 預設顯示近 2 個月請假記錄
  - 詳細內容包含：請假類別、起始時間、截止時間、事由、申請日期時間、簽核狀態、簽核人員、簽核時間、備註
  - 支援事件發生日（參考資料庫）
  - 附件上傳（Word、Excel、PDF、圖片）
  - 功能：申請、取消、檢視、退回（全部重簽）
* 外出單申請與簽核
* 加班單申請與簽核
* 出勤確認單申請與簽核
* 出差單申請與簽核
* 附件上傳至 FTP Server
* 整合 BPM 系統流程
* 支援批次簽核

### 未來功能（開發中）
* 薪資查詢
* 教育訓練時數",
        Contact = new OpenApiContact
        {
            Name = "廣宇科技 HR System Team"
        }
    });

    // 啟用 XML 註解（如果有的話）
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // 設定 API 分組顯示
    c.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return new[] { api.GroupName };
        }

        var controllerActionDescriptor = api.ActionDescriptor as
            Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

        if (controllerActionDescriptor != null)
        {
            return new[] { controllerActionDescriptor.ControllerName };
        }

        return new[] { "其他" };
    });

    // 設定標籤顯示順序
    c.OrderActionsBy((apiDesc) =>
    {
        // 獲取控制器名稱
        var controllerActionDescriptor = apiDesc.ActionDescriptor as
            Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        
        var controllerName = controllerActionDescriptor?.ControllerName ?? "";
        
        var order = controllerName switch
        {
            "Auth" => "1",
            "BasicList" => "2",
            "LeaveBalance" => "3", // 新版請假餘額查詢
            // "LeaveRemain" => "3", // 舊版，已棄用
            // "AttendanceQuery" => "4", // 舊版，已由 WorkQuery 取代
            "WorkQuery" => "4", // 考勤查詢 (取代舊版 AttendanceQuery)
            "LeaveForm" => "5",
            "LeaveFormTest" => "5.1",
            "OutingForm" => "6",
            "OvertimeForm" => "7",
            "AttendanceForm" => "8",
            "BusinessTripForm" => "9",
            _ => "99"
        };
        return $"{order}_{apiDesc.RelativePath}";
    });
});

// 加入 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ===== 自動建立資料庫和遷移表 =====
try
{
    using (var scope = app.Services.CreateScope())
    {
        var eformApprovalDbContext = scope.ServiceProvider.GetRequiredService<EFormApprovalDbContext>();
        
        // 確保資料庫存在並執行遷移
        eformApprovalDbContext.Database.EnsureCreated();
        
        var initLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        initLogger.LogInformation("EFormApprovalRecords 資料表初始化完成");
    }
}
catch (Exception ex)
{
    var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
    appLogger.LogError(ex, "資料表初始化失敗");
}

// ===== Swagger 設定（所有環境都可用,包含外網訪問）=====
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "廣宇科技 HR System API v1");
    c.RoutePrefix = "swagger";  // 訪問路徑: http://你的IP:5112/swagger
    c.DocumentTitle = "廣宇科技 HR System API";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DefaultModelsExpandDepth(-1);
});

// ⚠️ 如果不需要 HTTPS 重定向,可以註解掉這行
// app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// 記錄啟動資訊
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("====================================");
logger.LogInformation("廣宇科技 HR System API 已啟動");
logger.LogInformation("環境: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("====================================");

app.Run();