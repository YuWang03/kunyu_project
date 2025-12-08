# MySQL 資料表建立指南

## 連線到 MySQL

```bash
sudo docker exec -it mysql8 mysql -u root -p
```

輸入密碼：`Hank20020926@`

## 切換到 AppHome 資料庫

```sql
USE AppHome;
```

## 建立 SalaryVerificationCodes 資料表

```sql
-- 檢查資料表是否存在，若存在則刪除
DROP TABLE IF EXISTS `SalaryVerificationCodes`;

-- 建立資料表
CREATE TABLE `SalaryVerificationCodes` (
    `Cid` VARCHAR(50) NOT NULL COMMENT '公司代碼',
    `Uid` VARCHAR(50) NOT NULL COMMENT '使用者工號',
    `Code` VARCHAR(10) NOT NULL COMMENT '驗證碼（4 位數）',
    `CreatedAt` DATETIME NOT NULL COMMENT '建立時間',
    `ExpiresAt` DATETIME NOT NULL COMMENT '過期時間',
    `IsUsed` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否已使用（0=未使用, 1=已使用）',
    PRIMARY KEY (`Cid`, `Uid`),
    INDEX `idx_created_at` (`CreatedAt` DESC),
    INDEX `idx_expires_at` (`ExpiresAt` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci 
COMMENT='薪資查詢驗證碼資料表';
```

## 驗證資料表是否建立成功

```sql
-- 查詢資料表結構
DESC `SalaryVerificationCodes`;

-- 查詢資料表資訊
SHOW CREATE TABLE `SalaryVerificationCodes`;

-- 查詢所有資料
SELECT * FROM `SalaryVerificationCodes`;
```

## 測試用 SQL

### 插入測試資料
```sql
INSERT INTO `SalaryVerificationCodes` 
    (`Cid`, `Uid`, `Code`, `CreatedAt`, `ExpiresAt`, `IsUsed`)
VALUES 
    ('03546618', '3537', '1234', NOW(), DATE_ADD(NOW(), INTERVAL 5 MINUTE), 0);
```

### 查詢特定使用者的驗證碼
```sql
SELECT * FROM `SalaryVerificationCodes` 
WHERE `Uid` = '3537' 
ORDER BY `CreatedAt` DESC 
LIMIT 1;
```

### 刪除過期的驗證碼
```sql
DELETE FROM `SalaryVerificationCodes` 
WHERE `ExpiresAt` < NOW();
```

### 清空資料表
```sql
TRUNCATE TABLE `SalaryVerificationCodes`;
```

## 離開 MySQL

```sql
EXIT;
```

或按 `Ctrl + D`
