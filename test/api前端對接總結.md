# 廣宇 HR System API 文件

## 目錄
- [1. 基本資料 API (BasicInformation)](#1-基本資料-api-basicinformation)
- [2. 考勤查詢 API (AttendanceQuery)](#2-考勤查詢-api-attendancequery)
- [3. 請假剩餘天數 API (LeaveRemain)](#3-請假剩餘天數-api-leaveremain)
- [4. 請假單 API (LeaveForm)](#4-請假單-api-leaveform)
- [5. 加班單 API (OvertimeForm)](#5-加班單-api-overtimeform)
- [6. 出差單 API (BusinessTripForm)](#6-出差單-api-businesstripform)
- [7. 出勤異常單 API (AttendanceForm)](#7-出勤異常單-api-attendanceform)

---

## 系統架構說明

本系統整合 **BPM (Business Process Management)** 系統，提供以下功能：
- **基本資料查詢**: 員工基本資訊
- **考勤管理**: 出勤記錄查詢、異常處理
- **請假管理**: 剩餘天數查詢、請假申請
- **加班管理**: 加班申請
- **出差管理**: 出差申請
- **表單簽核**: 整合 BPM 系統進行流程簽核

**重要提示:**
- 所有表單申請 API 支援附件上傳（Word, Excel, PDF, 圖片）
- 附件會自動上傳到 FTP 伺服器
- 表單會自動整合 BPM 系統建立簽核流程
- 日期格式支援 `yyyy-MM-dd` 或 `yyyy/MM/dd`
- 時間格式為 `HH:mm` 或 `yyyy-MM-dd HH:mm`

---

## 1. 基本資料 API (BasicInformation)

### 1.1 取得員工基本資料

**URL:** `GET /api/BasicInformation`

**描述:** 取得所有員工的基本資料列表。

**回應範例:**
```json
[
  {
    "employeeId": 1001,
    "employeeName": "張三",
    "employeeNo": "3536",
    "companyName": "某某科技股份有限公司",
    "departmentName": "資訊部",
    "jobTitle": "工程師",
    "joinDate": "2020-01-15T00:00:00",
    "email": "user@example.com"
  },
  {
    "employeeId": 1002,
    "employeeName": "李四",
    "employeeNo": "3537",
    "companyName": "某某科技股份有限公司",
    "departmentName": "人資部",
    "jobTitle": "專員",
    "joinDate": "2021-03-20T00:00:00",
    "email": "user2@example.com"
  }
]
```

**回應代碼:**
- `200`: 查詢成功
- `500`: 伺服器內部錯誤

---

## 2. 考勤查詢 API (AttendanceQuery)

### 2.1 查詢個人出勤記錄

**URL:** `GET /api/AttendanceQuery`

**描述:** 查詢指定員工在指定日期的出勤記錄，包含上下班刷卡時間與狀態。

**查詢參數:**
- `employeeNo` (string, 必填): 員工編號
- `date` (string, 必填): 日期 (格式: `yyyy-MM-dd`，例如: `2025-10-28`)

**回應範例 (正常打卡):**
```json
{
  "date": "2025/10/28",
  "clockInTime": "2025/10/28 08:00:00",
  "clockInStatus": "正常",
  "clockOutTime": "2025/10/28 17:30:00",
  "clockOutStatus": "正常",
  "clockInCode": "",
  "clockOutCode": ""
}
```

**回應範例 (應刷未刷):**
```json
{
  "date": "2025/10/28",
  "clockInTime": "應刷未刷",
  "clockInStatus": "應刷未刷",
  "clockOutTime": "應刷未刷",
  "clockOutStatus": "應刷未刷",
  "clockInCode": "0",
  "clockOutCode": "0"
}
```

**回應範例 (遲到 + 超時出勤):**
```json
{
  "date": "2025/10/31",
  "clockInTime": "2025/10/31 08:30:00",
  "clockInStatus": "遲到",
  "clockOutTime": "2025/10/31 21:00:00",
  "clockOutStatus": "超時出勤",
  "clockInCode": "1",
  "clockOutCode": "3"
}
```

**狀態說明:**
- `正常`: 按時打卡
- `應刷未刷`: 未打卡 (異常代碼 `0`)
- `遲到`: 上班打卡時間晚於應刷卡時間 (異常代碼 `1`)
- `早退`: 下班打卡時間早於應刷卡時間 (異常代碼 `2`)
- `超時出勤`: 打卡時間超過標準時間 (異常代碼 `3`)
- `曠職`: 未打卡且無請假 (異常代碼 `4`)

**回應代碼:**
- `200`: 查詢成功
- `400`: 請求參數錯誤
- `404`: 查無出勤記錄
- `500`: 伺服器內部錯誤

---

### 2.2 查詢所有員工的出勤記錄

**URL:** `GET /api/AttendanceQuery/all`

**描述:** 查詢指定日期所有員工的出勤記錄。

**查詢參數:**
- `date` (string, 必填): 日期 (格式: `yyyy-MM-dd`，例如: `2025-10-28`)

**回應範例:**
```json
{
  "date": "2025-10-28",
  "totalRecords": 2,
  "records": [
    {
      "date": "2025/10/28",
      "clockInTime": "2025/10/28 08:00:00",
      "clockInStatus": "正常",
      "clockOutTime": "2025/10/28 17:30:00",
      "clockOutStatus": "正常",
      "clockInCode": "",
      "clockOutCode": ""
    },
    {
      "date": "2025/10/28",
      "clockInTime": "應刷未刷",
      "clockInStatus": "曠職",
      "clockOutTime": "應刷未刷",
      "clockOutStatus": "曠職",
      "clockInCode": "4",
      "clockOutCode": "4"
    }
  ]
}
```

**回應代碼:**
- `200`: 查詢成功
- `400`: 請求參數錯誤
- `500`: 伺服器內部錯誤

---


## 3. 請假剩餘天數 API (LeaveRemain)

### 3.1 查詢個人請假剩餘天數

**URL:** `GET /api/LeaveRemain`

**描述:** 查詢指定員工在特定年度的各類假別剩餘天數。

**查詢參數:**
- `employeeNo` (string, 必填): 員工編號
- `year` (int, 選填): 查詢年度 (預設當年度，例如: `2025`)

**回應範例:**
```json
{
  "employeeNo": "3536",
  "employeeName": "張三",
  "year": "2025",
  "anniversaryStart": "2024/10/05",
  "anniversaryEnd": "2025/10/04",
  "queryTime": "2025/11/04 14:30:00",
  "leaveTypes": [
    {
      "leaveTypeName": "特休",
      "leaveTypeCode": "ANN",
      "minUnit": 0.5,
      "remainDays": 5,
      "remainHours": 4,
      "remainTotalHours": 44,
      "totalDays": 10,
      "totalHours": 80,
      "usedDays": 4,
      "usedHours": 4
    },
    {
      "leaveTypeName": "病假",
      "leaveTypeCode": "SICK",
      "minUnit": 0.5,
      "remainDays": 30,
      "remainHours": 0,
      "remainTotalHours": 240,
      "totalDays": 30,
      "totalHours": 240,
      "usedDays": 0,
      "usedHours": 0
    },
    {
      "leaveTypeName": "事假",
      "leaveTypeCode": "PERSONAL",
      "minUnit": 0.5,
      "remainDays": 14,
      "remainHours": 0,
      "remainTotalHours": 112,
      "totalDays": 14,
      "totalHours": 112,
      "usedDays": 0,
      "usedHours": 0
    },
    {
      "leaveTypeName": "補休假",
      "leaveTypeCode": "COMP",
      "minUnit": 0.5,
      "remainDays": 2,
      "remainHours": 3,
      "remainTotalHours": 19,
      "totalDays": 2,
      "totalHours": 19,
      "usedDays": 0,
      "usedHours": 0
    }
  ]
}
```

**回應代碼:**
- `200`: 查詢成功
- `400`: 參數錯誤
- `404`: 查無此員工或該年度無資料
- `500`: 伺服器錯誤

---

### 3.2 查詢個人請假剩餘天數 (包含當年及前一年)

**URL:** `GET /api/LeaveRemain/two-years`

**描述:** 查詢指定員工當年及前一年的請假剩餘天數。

**查詢參數:**
- `employeeNo` (string, 必填): 員工編號

**回應範例:**
```json
[
  {
    "employeeNo": "3536",
    "employeeName": "張三",
    "year": "2025",
    "anniversaryStart": "2024/10/05",
    "anniversaryEnd": "2025/10/04",
    "queryTime": "2025/11/04 14:30:00",
    "leaveTypes": [
      {
        "leaveTypeName": "特休",
        "leaveTypeCode": "ANN",
        "minUnit": 0.5,
        "remainDays": 5,
        "remainHours": 4,
        "remainTotalHours": 44,
        "totalDays": 10,
        "totalHours": 80,
        "usedDays": 4,
        "usedHours": 4
      }
    ]
  },
  {
    "employeeNo": "3536",
    "employeeName": "張三",
    "year": "2024",
    "anniversaryStart": "2023/10/05",
    "anniversaryEnd": "2024/10/04",
    "queryTime": "2025/11/04 14:30:00",
    "leaveTypes": [
      {
        "leaveTypeName": "特休",
        "leaveTypeCode": "ANN",
        "minUnit": 0.5,
        "remainDays": 0,
        "remainHours": 0,
        "remainTotalHours": 0,
        "totalDays": 7,
        "totalHours": 56,
        "usedDays": 7,
        "usedHours": 0
      }
    ]
  }
]
```

**回應代碼:**
- `200`: 查詢成功
- `400`: 參數錯誤
- `404`: 查無此員工
- `500`: 伺服器錯誤

---

### 3.3 查詢周年制起訖日期

**URL:** `GET /api/LeaveRemain/anniversary-period`

**描述:** 查詢員工周年制的起始日和結束日。

**查詢參數:**
- `employeeNo` (string, 必填): 員工編號
- `year` (int, 選填): 查詢年度 (預設當年度)

**回應範例:**
```json
{
  "employeeNo": "3536",
  "year": 2025,
  "anniversaryStart": "2024/10/05",
  "anniversaryEnd": "2025/10/04",
  "description": "周年制區間：2024/10/05 ~ 2025/10/04"
}
```

**回應代碼:**
- `200`: 查詢成功
- `400`: 參數錯誤
- `404`: 查無此員工或無法計算周年制
- `500`: 伺服器錯誤

---

## 4. 請假單 API (LeaveForm)

### 4.1 申請請假單

**URL:** `POST /api/LeaveForm`

**描述:** 提交請假申請並整合 BPM 系統建立簽核流程。

**請求內容 (multipart/form-data):**

**必填欄位:**
- `email` (string): 申請人 Email
- `leaveTypeId` (string): 假別代碼 (詳見下方假別代碼表)
- `leaveTypeName` (string): 假別名稱
- `startDate` (string): 開始日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `startTime` (string): 開始時間 (格式: `HH:mm`)
- `endDate` (string): 結束日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `endTime` (string): 結束時間 (格式: `HH:mm`)
- `reason` (string): 請假事由
- `agentNo` (string): 代理人工號

**選填欄位:**
- `attachments` (file[]): 附件檔案 (支援 Word, Excel, PDF, 圖片)
- `unit` (string): 單位類型 (`DAY` 或 `HOUR`，預設: `DAY`)

**假別代碼表:**
| 代碼 | 假別名稱 |
|------|---------|
| `S0001-1` | 事假 |
| `S0001-2` | 家庭照顧假 |
| `S0002-1` | 病假 |
| `S0002-2` | 生理假 |
| `S0003-1` | 婚假 |
| `S0006-1` | 八日喪假 |
| `S0006-2` | 六日喪假 |
| `S0007-2` | 體檢公假 |
| `S0008-1` | 公傷病假 |
| `S0010-1` | 遲到假 |
| `S0013-2` | 國外出差 |
| `S0014-1` | 產檢假 |
| `S0019-1` | 洽公外出 |
| `S0020-1` | 駐地休假 |
| `SLC01` | 加班 |
| `SLC01-REGL` | 例假日加班 |
| `SLC03` | 補休假 |
| `SLC04` | 特休假 |

**附件支援格式:**
- Word: `.doc`, `.docx`
- Excel: `.xls`, `.xlsx`
- PDF: `.pdf`
- 圖片: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`

**回應範例:**
```json
{
  "success": true,
  "message": "請假單申請成功",
  "data": {
    "formId": "12345",
    "formNumber": "LEAVE202511160001",
    "attachmentPaths": [
      "/uploads/leave_001_document.pdf"
    ],
    "attachmentCount": 1
  }
}
```

**回應代碼:**
- `200`: 申請成功
- `400`: 欄位驗證失敗或缺少必填欄位
- `500`: 申請失敗

---

## 5. 加班單 API (OvertimeForm)

### 5.1 申請加班單

**URL:** `POST /api/OvertimeForm`

**描述:** 提交加班申請並整合 BPM 系統建立簽核流程（使用 PI_OVERTIME_001）。

**請求內容 (multipart/form-data):**

**必填欄位:**
- `email` (string): 申請人 Email
- `applyDate` (string): 申請日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `startTimeF` (string): 開始時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `endTimeF` (string): 結束時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `startTime` (string): 開始時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `endTime` (string): 結束時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `detail` (string): 加班事由

**選填欄位:**
- `attachments` (file[]): 附件檔案 (支援 Word, Excel, PDF, 圖片)

**附件支援格式:**
- Word: `.doc`, `.docx`
- Excel: `.xls`, `.xlsx`
- PDF: `.pdf`
- 圖片: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`

**注意事項:**
- 系統會自動查詢員工資料並填充表單欄位
- 附件會自動上傳到 FTP 伺服器
- 會呼叫 BPM 表單預覽 API 取得自動計算欄位

**回應範例:**
```json
{
  "success": true,
  "message": "加班單申請成功",
  "data": {
    "formId": "12345",
    "formNumber": "OT202511160001"
  }
}
```

**回應代碼:**
- `200`: 申請成功
- `400`: 欄位驗證失敗
- `500`: 申請失敗

---

## 6. 出差單 API (BusinessTripForm)

### 6.1 申請出差單

**URL:** `POST /api/BusinessTripForm`

**描述:** 提交出差申請並整合 BPM 系統建立簽核流程（使用 PI_BUSINESS_TRIP_001）。

**請求內容 (multipart/form-data):**

**必填欄位:**
- `email` (string): 申請人 Email
- `date` (string): 申請日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `reason` (string): 出差事由
- `startDate` (string): 出差起始日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `endDate` (string): 出差結束日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `location` (string): 出差地點
- `numberOfDays` (decimal): 出差天數 (範圍: 0.5 ~ 365)
- `mainTasksOfTrip` (string): 出差主要任務
- `estimatedCosts` (string): 費用預估（津貼、機票、其他費用）

**選填欄位:**
- `applicationDateTime` (string): 申請日期時間 (未提供時使用當前時間)
- `approvalStatus` (string): 簽核狀態 (預設: `待審核`)
- `approvingPersonnel` (string): 簽核人員
- `approvalTime` (string): 簽核時間
- `remarks` (string): 備註
- `attachments` (file[]): 附件檔案 (支援 Word, Excel, PDF, 圖片)

**附件支援格式:**
- Word: `.doc`, `.docx`
- Excel: `.xls`, `.xlsx`
- PDF: `.pdf`
- 圖片: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`

**注意事項:**
- 系統會自動查詢員工資料並填充表單欄位
- 附件會自動上傳到 FTP 伺服器
- 會呼叫 BPM 表單預覽 API 取得自動計算欄位

**範例請求:**
```
Email: employee@company.com
Date: 2025-11-16
Reason: 客戶拜訪
StartDate: 2025-11-20
EndDate: 2025-11-22
Location: 台北市
NumberOfDays: 3
MainTasksOfTrip: 拜訪客戶，洽談專案合作事宜
EstimatedCosts: 津貼: 3000元, 機票: 8000元, 住宿: 5000元
Remarks: 需事先預約客戶時間
```

**回應範例:**
```json
{
  "success": true,
  "message": "出差單申請成功",
  "data": {
    "formId": "12345",
    "formNumber": "BT202511160001"
  }
}
```

**回應代碼:**
- `200`: 申請成功
- `400`: 欄位驗證失敗
- `500`: 申請失敗

---

## 7. 出勤異常單 API (AttendanceForm)

### 7.1 申請出勤異常單

**URL:** `POST /api/AttendanceForm`

**描述:** 提交出勤異常（補卡）申請並整合 BPM 系統建立簽核流程。

**請求內容 (multipart/form-data):**

**必填欄位:**
- `email` (string): 申請人 Email
- `applyDate` (string): 申請日期 (格式: `yyyy-MM-dd` 或 `yyyy/MM/dd`)
- `exceptionDescription` (string): 異常說明

**選填欄位:**
- `exceptionTime` (string): 補卡起始時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `exceptionEndTime` (string): 補卡結束時間 (格式: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm`)
- `exceptionReason` (string): 異常原因 (預設: `其他`)
- `formType` (string): 表單類型 (預設: `H1A`)
- `attachments` (file[]): 附件檔案 (支援 Word, Excel, PDF, 圖片)

**附件支援格式:**
- Word: `.doc`, `.docx`
- Excel: `.xls`, `.xlsx`
- PDF: `.pdf`
- 圖片: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`

**注意事項:**
- 系統會自動查詢員工資料並填充表單欄位
- 附件會自動上傳到 FTP 伺服器
- 整合 BPM 系統建立簽核流程

**回應範例:**
```json
{
  "success": true,
  "message": "出勤異常單申請成功",
  "data": {
    "formId": "12345",
    "formNumber": "ATT202511160001"
  }
}
```

**回應代碼:**
- `200`: 申請成功
- `400`: 欄位驗證失敗或缺少必填欄位
- `500`: 申請失敗

---

## 附錄

### A. HTTP 狀態碼說明

- `200 OK`: 請求成功
- `400 Bad Request`: 請求參數錯誤或格式不正確
- `401 Unauthorized`: 未授權或認證失敗
- `404 Not Found`: 找不到請求的資源
- `500 Internal Server Error`: 伺服器內部錯誤

---

### B. 日期時間格式

**輸入格式（API 請求）:**
- **日期格式**: `yyyy-MM-dd` 或 `yyyy/MM/dd` (例如: `2025-11-16` 或 `2025/11/16`)
- **時間格式**: `HH:mm` (例如: `09:00`)
- **日期時間格式**: `yyyy-MM-dd HH:mm` 或 `yyyy/MM/dd HH:mm` (例如: `2025-11-16 14:30`)

**輸出格式（API 回應）:**
- **日期格式**: `yyyy/MM/dd` (例如: `2025/11/16`)
- **日期時間格式**: `yyyy/MM/dd HH:mm:ss` (例如: `2025/11/16 14:30:00`)

---

### C. 附件上傳說明

**支援的附件格式:**
- **Word 文件**: `.doc`, `.docx`
- **Excel 試算表**: `.xls`, `.xlsx`
- **PDF 文件**: `.pdf`
- **圖片檔案**: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`

**上傳方式:**
- 使用 `multipart/form-data` 格式
- 可一次上傳多個附件
- 附件會自動上傳到 FTP 伺服器
- 上傳成功後會返回檔案路徑

---

### D. BPM 系統整合說明

本系統與 BPM (Business Process Management) 系統整合，提供以下功能：

**整合的表單類型:**
- **PI_OVERTIME_001**: 加班單
- **PI_BUSINESS_TRIP_001**: 出差單
- **請假單**: 各類假別申請
- **出勤異常單**: 補卡申請

**BPM 整合流程:**
1. 使用者提交表單申請
2. 系統驗證必填欄位
3. 上傳附件到 FTP 伺服器
4. 呼叫 BPM API 建立表單
5. 啟動簽核流程
6. 回傳表單 ID 和表單編號

**認證方式:**
- 使用 Header 認證
- `X-API-Key`: API 金鑰
- `X-API-Secret`: API 密鑰

---

### E. 注意事項

**表單申請:**
- 所有表單申請都需要使用員工 Email 作為識別
- 系統會自動查詢員工資料（工號、姓名、部門等）
- 日期時間必須符合指定格式
- 附件為選填項目，但建議上傳相關證明文件

**資料查詢:**
- 考勤查詢：支援單一員工或全部員工查詢
- 請假剩餘天數：採用周年制計算
- 所有查詢 API 都需要提供正確的員工編號或 Email

**錯誤處理:**
- 當 API 回應 `success: false` 時，請檢查 `message` 欄位了解錯誤原因
- `errorCode` 欄位提供更詳細的錯誤代碼（如有）
- 建議實作重試機制處理暫時性錯誤（如網路問題）

**系統環境:**
- API 伺服器監聽 Port: 5112 (HTTP)
- 支援跨網卡訪問
- 建議使用 HTTPS 進行生產環境部署

---

### F. 測試工具

**Swagger UI:**
- URL: `http://localhost:5112/swagger`
- 提供互動式 API 文件
- 可直接在瀏覽器測試所有 API
- 支援檔案上傳測試

**Postman:**
- 可使用 Postman 進行 API 測試
- 設定 `Content-Type: multipart/form-data` 進行檔案上傳
- 建議建立環境變數儲存常用參數

**HTTP 檔案:**
- 專案中包含 `api-test.http` 和 `HRSystemAPI.http`
- 可使用 VS Code 的 REST Client 擴充套件執行

---

### G. 常見問題 (FAQ)

**Q1: 如何測試 FTP 連線？**
- 目前系統尚未提供專用的 FTP 測試端點
- 可透過實際上傳附件來驗證 FTP 功能

**Q2: 表單申請後如何查詢狀態？**
- 目前查詢功能（pending, my-forms 等）暫時隱藏
- 表單申請成功後會回傳 `formId` 和 `formNumber`
- 未來版本將提供完整的查詢功能

**Q3: 日期格式可以混用嗎？**
- 可以，系統支援 `yyyy-MM-dd` 和 `yyyy/MM/dd` 兩種格式
- 建議統一使用一種格式以保持一致性

**Q4: 假別代碼從哪裡取得？**
- 請參考 [4.1 申請請假單](#41-申請請假單) 中的假別代碼表
- 必須使用正確的假別代碼，否則申請會失敗

**Q5: 附件檔案大小有限制嗎？**
- 目前文件中未說明檔案大小限制
- 建議單一附件不超過 10MB
- 多個附件總大小建議不超過 50MB

---

## 版本資訊

**API 版本:** v1  
**文件更新日期:** 2025-11-16  
**系統名稱:** 廣宇 HR System API  
**開發團隊:** 廣宇科技  

---

## 聯絡資訊

如有任何問題或建議，請聯絡開發團隊。

