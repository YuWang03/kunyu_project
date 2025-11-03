using HRSystemAPI.Models;
using HRSystemAPI.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ===== 註冊原有服務 =====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBasicInfoService, BasicInfoService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveRemainService, LeaveRemainService>();
builder.Services.AddScoped<IOvertimeService, OvertimeService>();

// ===== 註冊 FTP 和 BPM 設定 =====
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("FtpSettings"));
builder.Services.Configure<BpmSettings>(builder.Configuration.GetSection("BpmSettings"));

// ===== 註冊 HttpClient（正確方式）=====
builder.Services.AddHttpClient("BpmClient", client =>
{
    var bpmSettings = builder.Configuration.GetSection("BpmSettings").Get<BpmSettings>();
    if (bpmSettings != null && !string.IsNullOrEmpty(bpmSettings.ApiBaseUrl))
    {
        client.BaseAddress = new Uri(bpmSettings.ApiBaseUrl);
    }
});

// ===== 註冊服務 =====
builder.Services.AddSingleton<FtpService>();
builder.Services.AddScoped<BpmService>();
builder.Services.AddScoped<OutingFormService>();

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

### 電子表單功能 
* 請假單申請與簽核
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
        var order = apiDesc.GroupName switch
        {
            "Auth" => "1",
            "帳號整合" => "1",
            "BasicInformation" => "2",
            "基本資料" => "2",
            "AttendanceQuery" => "3",
            "考勤查詢" => "3",
            "LeaveRemain" => "4",
            "請假剩餘天數" => "4",
            "LeaveForm" => "5",
            "請假單" => "5",
            "OutingForm" => "6",
            "外出單" => "6",
            "OvertimeForm" => "7",
            "加班單" => "7",
            "AttendanceForm" => "8",
            "出勤確認單" => "8",
            "BusinessTripForm" => "9",
            "出差單" => "9",
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "廣宇科技 HR System API v1");
        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "廣宇科技 HR System API";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();