-- 創建 API 錯誤日誌資料表
-- 資料庫: AppHome
-- 用途: 記錄 API 請求錯誤，方便後續查詢和分析

CREATE TABLE IF NOT EXISTS `api_error_logs` (
  `id` INT NOT NULL AUTO_INCREMENT COMMENT '主鍵 ID',
  `endpoint` VARCHAR(255) NOT NULL COMMENT 'API 端點',
  `http_method` VARCHAR(10) DEFAULT NULL COMMENT 'HTTP 方法',
  `request_body` TEXT DEFAULT NULL COMMENT '請求參數 (JSON)',
  `error_code` VARCHAR(10) DEFAULT NULL COMMENT '錯誤代碼',
  `error_message` TEXT DEFAULT NULL COMMENT '錯誤訊息',
  `error_details` TEXT DEFAULT NULL COMMENT '詳細錯誤資訊',
  `stack_trace` TEXT DEFAULT NULL COMMENT '堆疊追蹤',
  `user_id` VARCHAR(50) DEFAULT NULL COMMENT '使用者 ID',
  `company_id` VARCHAR(50) DEFAULT NULL COMMENT '公司 ID',
  `token_id` VARCHAR(100) DEFAULT NULL COMMENT 'Token ID',
  `client_ip` VARCHAR(50) DEFAULT NULL COMMENT '用戶端 IP',
  `user_agent` VARCHAR(500) DEFAULT NULL COMMENT '用戶端 User-Agent',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
  `additional_info` TEXT DEFAULT NULL COMMENT '附加資訊 (JSON)',
  PRIMARY KEY (`id`),
  INDEX `idx_endpoint` (`endpoint`),
  INDEX `idx_user_id` (`user_id`),
  INDEX `idx_company_id` (`company_id`),
  INDEX `idx_error_code` (`error_code`),
  INDEX `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='API 錯誤日誌';
