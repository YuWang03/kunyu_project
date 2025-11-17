# 廣宇科技 APP API

### 基本資料管理
- 員工基本資訊查詢（工號、姓名、部門、職稱）
- 組織架構資訊
- 員工狀態管理

### 考勤查詢系統
- 個人出勤記錄查詢
- 指定日期打卡記錄
- 上下班刷卡時間追蹤
- 異常狀態檢測與報告

### 請假剩餘天數
- 各類假別剩餘天數查詢（特休、事假、病假、補休假）
- 周年制計算支援
- 天數與小時數自動轉換

### 未來功能（開發中）
-  薪資查詢系統
-  教育訓練時數管理
-  電子表單申請與簽核流程

## 技術架構

- **框架**: ASP.NET Core 8.0
- **API 文件**: Swagger/OpenAPI 3.0
- **資料存取**: Dapper + Entity Framework Core
- **資料庫**: SQL Server
- **身份驗證**: Keycloak OIDC
- **架構模式**: Repository Pattern + Service Layer

## API 端點

### 基本資料管理
- `GET /api/BasicInformation/info` - 查詢員工基本資訊
- `GET /api/BasicInformation/all` - 查詢所有員工資訊

### 考勤查詢
- `GET /api/AttendanceQuery` - 查詢個人考勤記錄
- `GET /api/AttendanceQuery/{date}` - 查詢指定日期考勤

### 請假管理
- `GET /api/Leaveremain` - 查詢假別剩餘天數
- `POST /api/LeaveForm` - 提交請假表單
- `GET /api/LeaveForm/{formId}` - 查詢請假表單

### 加班管理
- `POST /api/OvertimeForm` - 提交加班表單
- `GET /api/OvertimeForm/{formId}` - 查詢加班表單

### 出差管理
- `POST /api/BusinessTripForm` - 提交出差表單
- `GET /api/BusinessTripForm/{formId}` - 查詢出差表單

### 考勤異常
- `POST /api/AttendanceForm` - 提交考勤異常表單
- `GET /api/AttendanceForm/{formId}` - 查詢考勤異常表單

## 環境設定

### appsettings.json 設定

```json
{
  "ConnectionStrings": {
    "HRDatabase": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "FtpSettings": {
    "Host": "YOUR_FTP_HOST",
    "Port": 21,
    "Username": "YOUR_FTP_USER",
    "Password": "YOUR_FTP_PASSWORD",
    "UploadPath": "/uploads/attachments/"
  },
  "BpmSettings": {
    "ApiBaseUrl": "YOUR_BPM_API_URL",
    "ApiKey": "YOUR_API_KEY",
    "ApiSecret": "YOUR_API_SECRET",
    "Timeout": 30
  }
}
```

### 專案結構

```
HRSystemAPI/
├── Controller/          # API 控制器
├── Services/           # 業務邏輯服務層
├── Models/             # 資料模型
├── Pages/              # Razor Pages（如需要）
├── wwwroot/            # 靜態檔案
└── appsettings.json    # 設定檔
```
