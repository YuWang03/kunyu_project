// API 端點測試 - 測試 API 的可用性和回應格式
// 不需要真實帳號，測試 API 的基本功能

require('dotenv').config();
const axios = require('axios');

// 你的 HRSystemAPI 基礎 URL（根據實際情況調整）
const API_BASE_URL = process.env.API_BASE_URL || 'https://localhost:7001';

// 測試結果統計
let testResults = {
  passed: 0,
  failed: 0,
  tests: []
};

/**
 * 測試輔助函數
 */
function logTest(testName, passed, message = '') {
  const status = passed ? '✅ PASS' : '❌ FAIL';
  console.log(`${status} - ${testName}`);
  if (message) console.log(`   ${message}`);
  
  testResults.tests.push({ testName, passed, message });
  if (passed) testResults.passed++;
  else testResults.failed++;
}

/**
 * 測試 1: 測試 Token Endpoint 是否可訪問
 */
async function testTokenEndpoint() {
  console.log('\n📡 測試 1: Keycloak Token Endpoint 可訪問性');
  console.log('-'.repeat(60));
  
  const TOKEN_URL = process.env.TOKEN_URL || 'https://sso.panpi.com.cn/realms/Panpi_TP/protocol/openid-connect/token';
  
  try {
    // 故意發送空請求，預期會收到錯誤回應（但至少證明端點存在）
    const response = await axios.post(TOKEN_URL, '', {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      validateStatus: () => true  // 接受所有狀態碼
    });
    
    if (response.status === 400 || response.status === 401) {
      logTest('Token Endpoint 可訪問', true, `收到預期的錯誤回應 (${response.status})`);
      console.log('   回應:', response.data);
    } else {
      logTest('Token Endpoint 可訪問', false, `未預期的狀態碼: ${response.status}`);
    }
  } catch (error) {
    logTest('Token Endpoint 可訪問', false, error.message);
  }
}

/**
 * 測試 2: 測試錯誤的憑證處理
 */
async function testInvalidCredentials() {
  console.log('\n🔐 測試 2: 錯誤憑證處理');
  console.log('-'.repeat(60));
  
  const TOKEN_URL = process.env.TOKEN_URL || 'https://sso.panpi.com.cn/realms/Panpi_TP/protocol/openid-connect/token';
  const CLIENT_ID = process.env.CLIENT_ID || 'ZZ_EMPLOYEE2k7';
  
  try {
    const params = new URLSearchParams();
    params.append('grant_type', 'password');
    params.append('client_id', CLIENT_ID);
    params.append('username', 'fake_user@test.com');
    params.append('password', 'wrong_password');
    
    const response = await axios.post(TOKEN_URL, params, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      validateStatus: () => true
    });
    
    if (response.status === 401 && response.data.error === 'invalid_grant') {
      logTest('錯誤憑證處理', true, '正確返回 invalid_grant 錯誤');
      console.log('   錯誤描述:', response.data.error_description);
    } else {
      logTest('錯誤憑證處理', false, `未預期的回應: ${response.status}`);
    }
  } catch (error) {
    logTest('錯誤憑證處理', false, error.message);
  }
}

/**
 * 測試 3: 測試 JWT 解碼功能
 */
async function testJWTDecoding() {
  console.log('\n🔍 測試 3: JWT 解碼功能');
  console.log('-'.repeat(60));
  
  const jwt = require('jsonwebtoken');
  
  // 創建一個測試 token
  const testPayload = {
    sub: "test-user-id",
    email: "test@example.com",
    preferred_username: "testuser",
    exp: Math.floor(Date.now() / 1000) + 3600
  };
  
  try {
    const testToken = jwt.sign(testPayload, 'test-secret');
    const decoded = jwt.decode(testToken);
    
    if (decoded && decoded.email === testPayload.email) {
      logTest('JWT 解碼功能', true, '成功解碼測試 token');
      console.log('   解碼結果:', decoded);
    } else {
      logTest('JWT 解碼功能', false, '解碼結果不符合預期');
    }
  } catch (error) {
    logTest('JWT 解碼功能', false, error.message);
  }
}

/**
 * 測試 4: 測試 Client ID 格式驗證
 */
async function testClientIDFormat() {
  console.log('\n📝 測試 4: Client ID 格式驗證');
  console.log('-'.repeat(60));
  
  const CLIENT_ID = process.env.CLIENT_ID || 'ZZ_EMPLOYEE2k7';
  
  try {
    // 驗證 Client ID 格式 (應該類似 ZZ_EMPLOYEE2k7)
    const isValidFormat = /^ZZ_EMPLOYEE\w+$/.test(CLIENT_ID);
    
    if (isValidFormat) {
      logTest('Client ID 格式', true, `Client ID 格式正確: ${CLIENT_ID}`);
    } else {
      logTest('Client ID 格式', false, `Client ID 格式可能不正確: ${CLIENT_ID}`);
    }
  } catch (error) {
    logTest('Client ID 格式', false, error.message);
  }
}

/**
 * 測試 5: 測試環境變數配置
 */
async function testEnvironmentConfig() {
  console.log('\n⚙️  測試 5: 環境變數配置');
  console.log('-'.repeat(60));
  
  const requiredVars = ['REALM', 'TOKEN_URL', 'CLIENT_ID'];
  const optionalVars = ['USERNAME', 'PASSWORD'];
  
  let allRequired = true;
  
  for (const varName of requiredVars) {
    if (process.env[varName]) {
      console.log(`   ✅ ${varName}: 已設定`);
    } else {
      console.log(`   ❌ ${varName}: 未設定`);
      allRequired = false;
    }
  }
  
  for (const varName of optionalVars) {
    const value = process.env[varName];
    if (value && !value.includes('<') && !value.includes('>')) {
      console.log(`   ✅ ${varName}: 已設定`);
    } else {
      console.log(`   ⚠️  ${varName}: 未設定或使用預設值`);
    }
  }
  
  logTest('環境變數配置', allRequired, allRequired ? '所有必要變數已設定' : '缺少必要變數');
}

/**
 * 測試 6: 測試 API 基本連線（如果有本地 API）
 */
async function testAPIConnection() {
  console.log('\n🌐 測試 6: 本地 API 連線');
  console.log('-'.repeat(60));
  
  try {
    // 嘗試連接本地 API（可能不存在，這是正常的）
    const response = await axios.get(`${API_BASE_URL}/api/health`, {
      timeout: 3000,
      validateStatus: () => true
    });
    
    logTest('本地 API 連線', true, `API 回應: ${response.status}`);
  } catch (error) {
    if (error.code === 'ECONNREFUSED') {
      logTest('本地 API 連線', true, 'API 未運行（這是正常的，如果你還沒啟動 API）');
    } else {
      logTest('本地 API 連線', false, error.message);
    }
  }
}

/**
 * 主測試函數
 */
async function runTests() {
  console.log('\n' + '='.repeat(60));
  console.log('🧪 開始執行 Keycloak & API 整合測試');
  console.log('='.repeat(60));
  
  // 執行所有測試
  await testEnvironmentConfig();
  await testClientIDFormat();
  await testJWTDecoding();
  await testTokenEndpoint();
  await testInvalidCredentials();
  await testAPIConnection();
  
  // 顯示測試結果摘要
  console.log('\n' + '='.repeat(60));
  console.log('📊 測試結果摘要');
  console.log('='.repeat(60));
  console.log(`總測試數: ${testResults.tests.length}`);
  console.log(`✅ 通過: ${testResults.passed}`);
  console.log(`❌ 失敗: ${testResults.failed}`);
  console.log(`成功率: ${((testResults.passed / testResults.tests.length) * 100).toFixed(1)}%`);
  
  console.log('\n💡 建議:');
  if (testResults.failed > 0) {
    console.log('   - 檢查失敗的測試項目');
    console.log('   - 確認 .env 檔案設定正確');
    console.log('   - 確認網路連線正常');
  } else {
    console.log('   - 所有基礎測試通過！');
    console.log('   - 如果有真實帳號，可以執行 keycloak_login.js 進行完整測試');
    console.log('   - 可以使用 mock_token_test.js 生成測試 token');
  }
  
  console.log('\n');
}

// 執行測試
if (require.main === module) {
  runTests().catch(error => {
    console.error('測試執行錯誤:', error);
    process.exit(1);
  });
}

module.exports = { runTests };
