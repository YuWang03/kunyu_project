# POST /app/eformmy - 我的表單列表 API

## 功能說明
於簽核記錄中，取得我的表單列表。此 API 會從 BPM 系統中取得使用者的待辦事項（代辦事項），並回傳所有相關的表單資訊。

## API 端點
```
POST /app/eformmy
```

## 請求參數 (Request Body)
```json
{
    "tokenid": "53422421",
    "cid": "45624657",
    "uid": "0325"
}
```

| 參數名稱 | 類型 | 必填 | 說明 |
|---------|------|------|------|
| tokenid | string | 是 | Token ID |
| cid | string | 是 | 公司識別碼 |
| uid | string | 是 | 使用者工號 |

## 回應格式 (Response)

### 成功回應
```json
{
    "code": "200",
    "msg": "請求成功",
    "data": {
        "eformdata": [
            {
                "uname": "王大明",
                "udepartment": "電子一部",
                "formidtitle": "表單編號",
                "formid": "PI-HR-H1A-PKG-Test0000000000035",
                "eformtypetitle": "申請類別",
                "eformtype": "L",
                "eformname": "請假單",
                "estarttitle": "起始時間",
                "estartdate": "2025-09-18",
                "estarttime": "08:00",
                "eendtitle": "結束時間",
                "eenddate": "2025-09-18",
                "eendtime": "17:00",
                "ereasontitle": "事由",
                "ereason": "家中有事"
            },
            {
                "uname": "王大明",
                "udepartment": "電子一部",
                "formidtitle": "表單編號",
                "formid": "PI-HR-H1A-PKG-Test0000000000045",
                "eformtypetitle": "申請類別",
                "eformtype": "R",
                "eformname": "出勤確認單",
                "estarttitle": "上班時間",
                "estartdate": "2025-09-18",
                "estarttime": "08:00",
                "eendtitle": "",
                "eenddate": "",
                "eendtime": "",
                "ereasontitle": "事由",
                "ereason": "忘記刷卡(臉)"
            }
        ]
    }
}
```

### 失敗回應
```json
{
    "code": "203",
    "msg": "請求失敗，主要條件不符合"
}
```

## 表單類型對照表

| 表單類型代碼 | 表單名稱 | 說明 |
|------------|---------|------|
| L | 請假單 | 員工請假申請 |
| R | 出勤確認單 | 出勤異常確認 |
| A | 加班單 | 加班申請 |
| T | 出差單 | 出差申請 |
| O | 外出外訓單 | 外出/外訓申請 |
| D | 銷假單 | 銷假申請 |

## BPM 整合說明

此 API 會呼叫以下 BPM 中間件端點：

1. **取得待辦事項清單**
   ```
   GET http://60.248.158.147:8081/bpm-middleware/api/bpm/workitems/{userId}
   Headers:
     X-API-Key: APP_ACKEY
     X-API-Secret: $2a$12$cB7XkVA51wUuNQeihY2NGL$dql0gNTpQpLYgxv9q3xkUhURd3oC/Cz
   ```

2. **取得表單詳細資料**
   ```
   GET http://60.248.158.147:8081/bpm-middleware/api/bpm/forms/{processSerialNumber}
   Headers:
     X-API-Key: APP_ACKEY
     X-API-Secret: $2a$12$cB7XkVA51wUuNQeihY2NGL$dql0gNTpQpLYgxv9q3xkUhURd3oC/Cz
   ```

## 實作檔案

- **Models**: `Models/EFormMyModels.cs`
- **Service Interface**: `Services/IEFormMyService.cs`
- **Service Implementation**: `Services/EFormMyService.cs`
- **Controller**: `Controller/EFormMyController.cs`
- **DI Registration**: `Program.cs` (已註冊服務)

## 使用範例

### 使用 cURL
```bash
curl -X POST http://localhost:5112/app/eformmy \
  -H "Content-Type: application/json" \
  -d '{
    "tokenid": "53422421",
    "cid": "45624657",
    "uid": "3552"
  }'
```

### 使用 Postman
1. 選擇 POST 方法
2. 輸入 URL: `http://localhost:5112/app/eformmy`
3. Headers 設定: `Content-Type: application/json`
4. Body 選擇 raw (JSON) 並輸入請求參數
5. 點擊 Send

## 注意事項

1. **表單類型自動識別**: 系統會根據表單編號（processSerialNumber）自動識別表單類型
2. **日期格式**: 日期欄位統一使用 `yyyy-MM-dd` 格式
3. **時間格式**: 時間欄位使用 `HH:mm` 格式（24小時制）
4. **出勤確認單特殊處理**: 出勤確認單（R類型）不顯示結束時間相關欄位
5. **員工資訊**: 系統會自動從基本資料服務中取得員工姓名和部門資訊

## 錯誤處理

| 錯誤代碼 | 說明 | 處理方式 |
|---------|------|---------|
| 400 | 參數不完整 | 檢查必填欄位是否都有提供 |
| 203 | 請求失敗 | 檢查 BPM 服務是否正常運作 |
| 500 | 請求超時 | 系統內部錯誤，請聯絡管理員 |

## 測試建議

1. 使用有待辦事項的使用者 UID 進行測試
2. 確認 BPM 中間件服務正常運作
3. 驗證回傳的表單資料是否完整且正確
4. 測試不同表單類型的顯示是否正確
