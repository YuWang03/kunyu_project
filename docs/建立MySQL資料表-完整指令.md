# ========================================
# MySQL 資料表建立指令（手動執行）
# ========================================

## 方法 1：使用 Docker exec 一次性執行（推薦）

```powershell
# 執行以下單一命令即可建立資料表
docker exec -i mysql8 mysql -u root -pHank20020926@ <<EOF
USE AppHome;

DROP TABLE IF EXISTS \`SalaryVerificationCodes\`;

CREATE TABLE \`SalaryVerificationCodes\` (
    \`Cid\` VARCHAR(50) NOT NULL,
    \`Uid\` VARCHAR(50) NOT NULL,
    \`Code\` VARCHAR(10) NOT NULL,
    \`CreatedAt\` DATETIME NOT NULL,
    \`ExpiresAt\` DATETIME NOT NULL,
    \`IsUsed\` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (\`Cid\`, \`Uid\`),
    INDEX \`idx_created_at\` (\`CreatedAt\` DESC),
    INDEX \`idx_expires_at\` (\`ExpiresAt\` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'Table created successfully!' AS Result;
DESC \`SalaryVerificationCodes\`;
EOF
```

## 方法 2：逐步執行（如果方法 1 不行）

### 步驟 1：進入 MySQL 容器
```bash
sudo docker exec -it mysql8 mysql -u root -p
```

輸入密碼：`Hank20020926@`

### 步驟 2：切換到 AppHome 資料庫
```sql
USE AppHome;
```

### 步驟 3：刪除舊資料表（如果存在）
```sql
DROP TABLE IF EXISTS `SalaryVerificationCodes`;
```

### 步驟 4：建立資料表
```sql
CREATE TABLE `SalaryVerificationCodes` (
    `Cid` VARCHAR(50) NOT NULL,
    `Uid` VARCHAR(50) NOT NULL,
    `Code` VARCHAR(10) NOT NULL,
    `CreatedAt` DATETIME NOT NULL,
    `ExpiresAt` DATETIME NOT NULL,
    `IsUsed` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`Cid`, `Uid`),
    INDEX `idx_created_at` (`CreatedAt` DESC),
    INDEX `idx_expires_at` (`ExpiresAt` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 步驟 5：驗證資料表
```sql
DESC `SalaryVerificationCodes`;
```

### 步驟 6：離開 MySQL
```sql
EXIT;
```

## 方法 3：使用 PowerShell（Windows）

```powershell
# 建立 SQL 檔案
$sql = @"
USE AppHome;
DROP TABLE IF EXISTS \`SalaryVerificationCodes\`;
CREATE TABLE \`SalaryVerificationCodes\` (
    \`Cid\` VARCHAR(50) NOT NULL,
    \`Uid\` VARCHAR(50) NOT NULL,
    \`Code\` VARCHAR(10) NOT NULL,
    \`CreatedAt\` DATETIME NOT NULL,
    \`ExpiresAt\` DATETIME NOT NULL,
    \`IsUsed\` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (\`Cid\`, \`Uid\`),
    INDEX \`idx_created_at\` (\`CreatedAt\` DESC),
    INDEX \`idx_expires_at\` (\`ExpiresAt\` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
DESC \`SalaryVerificationCodes\`;
"@

# 執行 SQL
$sql | docker exec -i mysql8 mysql -u root -pHank20020926@
```

## 驗證資料表是否建立成功

```sql
-- 查詢資料表結構
DESC `SalaryVerificationCodes`;

-- 查詢資料表資訊
SHOW CREATE TABLE `SalaryVerificationCodes`;

-- 測試插入資料
INSERT INTO `SalaryVerificationCodes` 
    (`Cid`, `Uid`, `Code`, `CreatedAt`, `ExpiresAt`, `IsUsed`)
VALUES 
    ('03546618', '3537', '1234', NOW(), DATE_ADD(NOW(), INTERVAL 5 MINUTE), 0);

-- 查詢資料
SELECT * FROM `SalaryVerificationCodes`;
```

## 測試 API

資料表建立成功後，測試發送驗證碼 API：

```bash
curl -X POST http://localhost:5112/app/sendcode \
  -H "Content-Type: application/json" \
  -d "{\"tokenid\":\"29619777\",\"uid\":\"3537\",\"cid\":\"03546618\"}"
```

或使用 PowerShell：

```powershell
Invoke-RestMethod -Uri "http://localhost:5112/app/sendcode" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"tokenid":"29619777","uid":"3537","cid":"03546618"}'
```
