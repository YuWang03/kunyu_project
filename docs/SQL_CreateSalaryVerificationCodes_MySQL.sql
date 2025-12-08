-- ========================================
-- 薪資查詢驗證碼資料表 (MySQL)
-- ========================================
-- 用途：儲存 APP 薪資查詢功能的驗證碼
-- 時效：5 分鐘（可調整）
-- ========================================

USE AppHome;

-- 檢查資料表是否存在，若存在則刪除
DROP TABLE IF EXISTS `SalaryVerificationCodes`;

-- 建立資料表
CREATE TABLE `SalaryVerificationCodes` (
    -- 公司代碼
    `Cid` VARCHAR(50) NOT NULL,
    
    -- 使用者工號
    `Uid` VARCHAR(50) NOT NULL,
    
    -- 驗證碼（4 位數）
    `Code` VARCHAR(10) NOT NULL,
    
    -- 建立時間
    `CreatedAt` DATETIME NOT NULL,
    
    -- 過期時間
    `ExpiresAt` DATETIME NOT NULL,
    
    -- 是否已使用（0=未使用, 1=已使用）
    `IsUsed` TINYINT(1) NOT NULL DEFAULT 0,
    
    -- 複合主鍵
    PRIMARY KEY (`Cid`, `Uid`),
    
    -- 索引：建立時間（降序）
    INDEX `idx_created_at` (`CreatedAt` DESC),
    
    -- 索引：過期時間（升序）
    INDEX `idx_expires_at` (`ExpiresAt` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 顯示建立結果
SELECT 'SalaryVerificationCodes 資料表已建立成功！' AS Result;

-- 查詢資料表結構
DESC `SalaryVerificationCodes`;
