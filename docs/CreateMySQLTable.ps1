# ========================================
# 自動建立 MySQL 驗證碼資料表腳本
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "開始建立 MySQL 驗證碼資料表" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# MySQL 建表 SQL
$sqlScript = @"
USE AppHome;

DROP TABLE IF EXISTS \`SalaryVerificationCodes\`;

CREATE TABLE \`SalaryVerificationCodes\` (
    \`Cid\` VARCHAR(50) NOT NULL COMMENT '公司代碼',
    \`Uid\` VARCHAR(50) NOT NULL COMMENT '使用者工號',
    \`Code\` VARCHAR(10) NOT NULL COMMENT '驗證碼（4 位數）',
    \`CreatedAt\` DATETIME NOT NULL COMMENT '建立時間',
    \`ExpiresAt\` DATETIME NOT NULL COMMENT '過期時間',
    \`IsUsed\` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否已使用',
    PRIMARY KEY (\`Cid\`, \`Uid\`),
    INDEX \`idx_created_at\` (\`CreatedAt\` DESC),
    INDEX \`idx_expires_at\` (\`ExpiresAt\` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'SalaryVerificationCodes 資料表已建立成功！' AS Result;

DESC \`SalaryVerificationCodes\`;
"@

Write-Host "正在連線到 MySQL..." -ForegroundColor Yellow

# 執行 MySQL 指令
try {
    $sqlScript | docker exec -i mysql8 mysql -u root -pHank20020926@
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "資料表建立完成！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "可以開始測試 API 了：" -ForegroundColor Cyan
    Write-Host "POST http://localhost:5112/app/sendcode" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "建立失敗！錯誤訊息：" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "請確認：" -ForegroundColor Yellow
    Write-Host "1. Docker 容器 mysql8 是否正在運行" -ForegroundColor White
    Write-Host "2. MySQL 密碼是否正確（Hank20020926@）" -ForegroundColor White
    Write-Host "3. AppHome 資料庫是否存在" -ForegroundColor White
}
