# 廣宇科技 APP API

## API 端點

### 基本資料管理
- `POST /app/basiclist` - 取得基本資料選單列表

### 考勤管理
- `POST /app/WorkQuery` - 個人考勤查詢列表
- `POST /app/workset` - 考勤超時出勤設定

### 請假管理
- `POST /app/LeaveBalance` - 查詢個人請假餘額
- `POST /app/efleaveformunit` - 查詢請假假別單位
- `POST /app/efleaveform` - 提交請假單申請

### 銷假管理
- `POST /app/efleaveget` - 單筆請假資料查詢（根據 formid 查詢）
- `POST /app/efleavelist` - 銷假申請列表（查詢自己提交的請假單，用於銷假）
- `POST /app/efleavedetail` - 銷假單詳細資料
- `POST /app/efleavecancel` - 提交銷假申請

### 加班管理
- `POST /app/efotapply` - 加班單預申請
- `POST /app/efotpreview` - 加班單預覽（取得自動計算欄位）
- `POST /app/efotconfirm` - 加班單確認送出
- `POST /app/efotconfirmlist` - 加班單列表查詢
- `POST /app/getagent` - 取得加班代理人資料

### 外出外訓管理
- `POST /app/efleaveout` - 提交外出或外訓申請單
- `POST /app/efleaveout/cancel` - 提交外出外訓銷單

### 附件上傳說明
對於需要附件的 API（如 `/app/efleaveout` 和 `/app/efotapply`），請使用外部附件上傳 API：

**標準流程：**
1. 呼叫 `POST http://54.46.24.34:5112/api/Attachment/Upload` 上傳附件
2. 從回應中取得 `tfileurl`（附件 URL，例如：`/AppAttachments/3537/20251124001.docx`）
3. 將 `tfileurl` 填入對應 API 的 `efileurl` 欄位中

**範例回應：**
```json
{
    "code": "200",
    "msg": "上傳成功",
    "data": {
        "tfileid": "6",
        "afilename": "kunyu_20251118_v1.docx",
        "tfilename": "20251124001.docx",
        "tfileurl": "/AppAttachments/3537/20251124001.docx"
    }
}
```

**向後相容性：**
- 若僅提供 `efileid` 而不提供 `efileurl`，系統會自動生成 FTP 路徑（格式：`FTPTest~~/FTPShare/文件名.pdf`）
- 建議優先使用 `efileurl`，以支援更多的儲存方式

### 電子表單與審核
- `POST /app/eformslist` - 電子表單選單列表
- `POST /app/eformreview` - 待我審核列表
- `POST /app/eformdetail` - 表單詳細資料
- `POST /app/eformapproval` - 表單審核與意見提交

### 考勤異常
- `POST /app/efpatch` - 提交出勤確認單（補登未刷卡）

### appsettings.json 設定

```json
{
  "ConnectionStrings": {
    "HRDatabase": "Server=14.18.232.79,1433;Database=03546618;User Id=ehrapp;Password=***;TrustServerCertificate=True;",
    "DefaultConnection": "Server=54.46.24.34;Port=3306;Database=AppHome;Uid=root;Pwd=***;"
  },
  "FtpSettings": {
    "Host": "14.18.232.79",
    "Port": 20105,
    "Username": "ftpuser",
    "Password": "***",
    "UploadPath": "/AppAttachments/"
  },
  "BpmSettings": {
    "ApiBaseUrl": "http://60.248.158.147:8081/bpm-middleware/api/",
    "ApiKey": "APP_ACKEY",
    "ApiSecret": "***",
    "Timeout": 30
  }
}
```

**資料儲存架構：**
- **SQL Server (14.18.232.79:1433)**: 儲存 HR 主要資料 (員工資訊、考勤等)
- **MySQL (54.46.24.34:3306)**: 儲存表單同步資料 (bpm_forms 相關表)
- **SFTP (14.18.232.79:20105)**: 儲存附件檔案 (D:/FTPShare/AppAttachments)
