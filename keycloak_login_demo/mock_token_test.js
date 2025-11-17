// Mock Token Test - 在沒有真實帳號的情況下測試 API
// 此腳本會生成一個模擬的 JWT token 來測試你的 API 邏輯

const jwt = require('jsonwebtoken');

// 模擬 Keycloak 的 JWT payload
const mockPayload = {
  // 標準 JWT claims
  sub: "12345678-1234-1234-1234-123456789012",  // Subject (用戶 ID)
  iat: Math.floor(Date.now() / 1000),           // Issued At
  exp: Math.floor(Date.now() / 1000) + 3600,    // Expires (1小時後)
  iss: "https://sso.panpi.com.cn/realms/Panpi_TP",  // Issuer
  aud: "ZZ_EMPLOYEE2k7",                         // Audience (Client ID)
  
  // 用戶資訊
  email: "test.user@company.com",
  email_verified: true,
  preferred_username: "test.user",
  name: "測試用戶",
  given_name: "測試",
  family_name: "用戶",
  
  // Keycloak 特定欄位
  realm_access: {
    roles: ["employee", "user", "offline_access"]
  },
  resource_access: {
    "ZZ_EMPLOYEE2k7": {
      roles: ["employee"]
    }
  },
  scope: "openid profile email",
  
  // 自訂欄位（根據你的需求調整）
  employee_id: "EMP001",
  department: "IT",
  company: "Panpi"
};

// 使用一個假的 secret 簽名（僅供測試用）
const mockSecret = "mock-secret-key-for-testing-only";

// 生成 mock token
const mockToken = jwt.sign(mockPayload, mockSecret, {
  algorithm: 'HS256',
  header: {
    typ: 'JWT',
    alg: 'HS256'
  }
});

console.log('🎭 Mock JWT Token 生成完成\n');
console.log('=' .repeat(80));
console.log('📋 Mock Payload:');
console.log(JSON.stringify(mockPayload, null, 2));
console.log('=' .repeat(80));
console.log('\n🔑 Mock Access Token:');
console.log(mockToken);
console.log('=' .repeat(80));

// 解碼驗證
console.log('\n🔍 驗證解碼（不驗證簽名）:');
const decoded = jwt.decode(mockToken, { complete: true });
console.log('Header:', JSON.stringify(decoded.header, null, 2));
console.log('Payload:', JSON.stringify(decoded.payload, null, 2));

console.log('\n💡 使用方式:');
console.log('在 Postman 或其他 API 測試工具中：');
console.log('1. 複製上面的 Mock Access Token');
console.log('2. 在 Headers 中加入: Authorization: Bearer <token>');
console.log('3. 發送請求測試你的 API');
console.log('\n⚠️  注意：這只是 Mock Token，不能用於真實的 Keycloak 驗證');
console.log('   真實環境需要使用 Keycloak 簽發的正式 Token\n');

// 匯出供其他模組使用
module.exports = {
  mockToken,
  mockPayload,
  mockSecret
};
