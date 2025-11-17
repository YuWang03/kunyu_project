# Keycloak OIDC Login Demo

這是一個簡單的 Keycloak OpenID Connect (OIDC) 登入示範專案，使用 Resource Owner Password Credentials Grant 流程。

## 📋 專案結構

```
keycloak_login_demo/
├── .env                    # 環境變數設定檔（包含敏感資訊，不提交至 Git）
├── .gitignore             # Git 忽略檔案清單
├── keycloak_login.js      # 主程式：實作 Keycloak 登入邏輯
├── package.json           # Node.js 專案設定檔
└── README.md              # 專案說明文件
```

## 🚀 快速開始

### 1. 安裝相依套件

```bash
npm install
```

### 2. 設定環境變數

編輯 `.env` 檔案，填入你的 Keycloak 設定：

```env
REALM=Panpi_TP
TOKEN_URL=https://sso.panpi.com.cn/realms/Panpi_TP/protocol/openid-connect/token
CLIENT_ID=ZZ_EMPLOYEE2k7
USERNAME=你的公司信箱
PASSWORD=你的公司密碼
```

⚠️ **注意**：請將 `USERNAME` 和 `PASSWORD` 替換為你的實際帳號密碼

### 3. 執行程式

```bash
node keycloak_login.js
```

## 📦 使用的套件

- **axios**: HTTP 客戶端，用於發送 API 請求
- **jsonwebtoken**: JWT 解碼工具，用於解析 access token
- **dotenv**: 環境變數管理工具

## 🔐 程式功能

1. **發送登入請求**：使用 axios 向 Keycloak Token Endpoint 發送 POST 請求
2. **取得 Access Token**：接收並顯示 access_token、refresh_token 等資訊
3. **解析 JWT**：使用 jsonwebtoken 解碼 access_token
4. **顯示使用者資訊**：印出使用者的 email、name、roles 等資料

## 📄 輸出範例

```
🔐 Starting Keycloak OIDC Login...
📍 Realm: Panpi_TP
📍 Token URL: https://sso.panpi.com.cn/realms/Panpi_TP/protocol/openid-connect/token
📍 Client ID: ZZ_EMPLOYEE2k7
📍 Username: user@example.com

📡 Sending authentication request...
✅ Login successful!

📊 Token Response:
   Token Type: Bearer
   Expires In: 300 seconds
   Refresh Expires In: 1800 seconds
   Scope: openid profile email

🔑 Access Token:
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...

🔍 Decoding JWT Token...

👤 JWT Payload (User Info):
{
  "sub": "...",
  "email": "user@example.com",
  "preferred_username": "user",
  "name": "User Name",
  ...
}
```

## 🛠️ 支援的 Realms

- **Panpi_HM**
  - Token URL: `https://sso.panpi.com.cn/realms/Panpi_HM/protocol/openid-connect/token`
  
- **Panpi_TP**
  - Token URL: `https://sso.panpi.com.cn/realms/Panpi_TP/protocol/openid-connect/token`

## 🔒 安全性注意事項

1. ✅ `.env` 檔案已加入 `.gitignore`，不會被提交至版本控制
2. ✅ 請勿將帳號密碼寫入程式碼或提交至 Git
3. ✅ 生產環境建議使用 Authorization Code Flow 而非 Password Grant
4. ✅ Access Token 應妥善保管，不要暴露於公開環境

## 📚 相關文件

- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [OpenID Connect Specification](https://openid.net/connect/)
- [RFC 6749 - OAuth 2.0](https://datatracker.ietf.org/doc/html/rfc6749)

## 🧪 沒有測試帳號？這樣測試！

### 方法一：使用 Mock Token 測試

```bash
# 生成 Mock Token
node mock_token_test.js
```

這會生成一個模擬的 JWT token，你可以：
1. 複製生成的 token
2. 在 Postman 或 API 測試工具中使用
3. 測試你的 API 是否正確處理 token

### 方法二：執行自動化測試

```bash
# 執行完整的 API 測試套件
node api_test.js
```

這個測試腳本會：
- ✅ 測試 Keycloak 端點可訪問性
- ✅ 測試錯誤處理邏輯
- ✅ 驗證環境變數設定
- ✅ 測試 JWT 解碼功能
- ✅ 檢查 API 連線狀態

### 方法三：使用 Postman Collection

1. 匯入 `Keycloak-Mock-Tests.postman_collection.json` 到 Postman
2. 資料夾說明：
   - **1. 測試 Keycloak 連線**：不需要帳號，測試端點是否正常
   - **2. 使用 Mock Token 測試 API**：使用預設 Mock Token 測試你的 API
   - **3. 真實登入測試**：需要真實帳號才能執行

### 測試優先順序

```
無帳號 → 執行 api_test.js
      → 生成 mock_token_test.js
      → 使用 Mock Token 測試 API

有帳號 → 執行 keycloak_login.js
      → 使用真實 Token 測試 API
```

## 💡 常見問題

### Q: 登入失敗怎麼辦？
A: 請檢查：
- USERNAME 和 PASSWORD 是否正確
- CLIENT_ID 是否對應正確的 Realm
- 網路連線是否正常
- Keycloak 伺服器是否運行中

### Q: Client ID 從哪裡取得？
A: CLIENT_ID 對應到資料庫 `ZZ_EMPLOYEE` 表中的 `EMPLOYEE_EMAIL_1l` 欄位

### Q: 如何切換到不同的 Realm？
A: 修改 `.env` 檔案中的 `REALM` 和 `TOKEN_URL` 即可

### Q: Mock Token 可以用於生產環境嗎？
A: ❌ 不行！Mock Token 只用於開發測試。生產環境必須使用 Keycloak 簽發的真實 Token

### Q: 如何驗證我的 API 是否正確處理 Token？
A: 
1. 執行 `node mock_token_test.js` 生成 Mock Token
2. 在你的 API 中加入 token 解碼邏輯
3. 使用 Postman 測試 API 是否正確解析 token 中的用戶資訊

---

建立日期：2025-11-04
