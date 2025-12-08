-- ========================================
-- 薪資查詢驗證碼資料表
-- ========================================
-- 用途：儲存 APP 薪資查詢功能的驗證碼
-- 時效：5 分鐘（可調整）
-- ========================================

USE [03546618];
GO

-- 檢查資料表是否存在，若不存在則建立
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SalaryVerificationCodes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SalaryVerificationCodes] (
        -- 公司代碼
        [Cid] NVARCHAR(50) NOT NULL,
        
        -- 使用者工號
        [Uid] NVARCHAR(50) NOT NULL,
        
        -- 驗證碼（4 位數）
        [Code] NVARCHAR(10) NOT NULL,
        
        -- 建立時間
        [CreatedAt] DATETIME NOT NULL,
        
        -- 過期時間
        [ExpiresAt] DATETIME NOT NULL,
        
        -- 是否已使用（0=未使用, 1=已使用）
        [IsUsed] BIT NOT NULL DEFAULT 0,
        
        -- 複合主鍵
        CONSTRAINT [PK_SalaryVerificationCodes] PRIMARY KEY CLUSTERED 
        (
            [Cid] ASC,
            [Uid] ASC
        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY];
    
    PRINT '資料表 [SalaryVerificationCodes] 已建立成功！';
END
ELSE
BEGIN
    PRINT '資料表 [SalaryVerificationCodes] 已存在。';
END
GO

-- 建立索引以提升查詢效能
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalaryVerificationCodes_CreatedAt' AND object_id = OBJECT_ID('[dbo].[SalaryVerificationCodes]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SalaryVerificationCodes_CreatedAt]
    ON [dbo].[SalaryVerificationCodes] ([CreatedAt] DESC);
    
    PRINT '索引 [IX_SalaryVerificationCodes_CreatedAt] 已建立成功！';
END
GO

-- 建立索引以提升過期時間查詢效能
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalaryVerificationCodes_ExpiresAt' AND object_id = OBJECT_ID('[dbo].[SalaryVerificationCodes]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SalaryVerificationCodes_ExpiresAt]
    ON [dbo].[SalaryVerificationCodes] ([ExpiresAt] ASC);
    
    PRINT '索引 [IX_SalaryVerificationCodes_ExpiresAt] 已建立成功！';
END
GO

-- ========================================
-- 清理過期驗證碼的預存程序（選用）
-- ========================================
-- 建議定期執行此程序清理過期的驗證碼記錄
-- 可設定 SQL Server Agent Job 每小時執行一次
-- ========================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CleanExpiredVerificationCodes]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[sp_CleanExpiredVerificationCodes];
END
GO

CREATE PROCEDURE [dbo].[sp_CleanExpiredVerificationCodes]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedCount INT;
    
    -- 刪除已過期的驗證碼（保留最近 24 小時的記錄）
    DELETE FROM [dbo].[SalaryVerificationCodes]
    WHERE ExpiresAt < DATEADD(HOUR, -24, GETDATE());
    
    SET @DeletedCount = @@ROWCOUNT;
    
    PRINT '已刪除 ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' 筆過期的驗證碼記錄。';
END
GO

PRINT '========================================';
PRINT '資料表建立完成！';
PRINT '========================================';
GO

-- 測試用 SQL 範例（請手動執行）：
-- SELECT * FROM [dbo].[SalaryVerificationCodes] ORDER BY CreatedAt DESC;
-- SELECT * FROM [dbo].[SalaryVerificationCodes] WHERE Uid = '0325';
-- EXEC [dbo].[sp_CleanExpiredVerificationCodes];

