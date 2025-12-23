-- ============================================
-- 在遠程資料庫 (54.46.24.34) 創建 bpm_forms 表
-- ============================================

USE AppHome;

-- 刪除已存在的表（如有）
DROP TABLE IF EXISTS bpm_forms;

-- 建立表結構
CREATE TABLE bpm_forms (
    id INT AUTO_INCREMENT PRIMARY KEY,
    form_id VARCHAR(255) NOT NULL UNIQUE COMMENT '表單編號 (BPM ProcessSerialNo)',
    form_type VARCHAR(50) NOT NULL COMMENT '表單類型 (PI_LEAVE_001, PI_OVERTIME_001 等)',
    applicant_id VARCHAR(50) NOT NULL COMMENT '申請人工號',
    applicant_name VARCHAR(100) NOT NULL COMMENT '申請人姓名',
    leave_type_code VARCHAR(50) COMMENT '假別代碼 (S0001-1, SLC01 等)',
    leave_type_name VARCHAR(100) COMMENT '假別名稱 (事假, 加班 等)',
    status VARCHAR(50) NOT NULL COMMENT '表單狀態 (SUBMITTED, APPROVED, REJECTED, WITHDRAWN)',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    form_data LONGTEXT COMMENT '完整的表單資料 (JSON 格式)',
    INDEX idx_form_id (form_id),
    INDEX idx_applicant_id (applicant_id),
    INDEX idx_form_type (form_type),
    INDEX idx_status (status),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='BPM 表單記錄表';

-- 驗證表是否建立成功
SELECT 'bpm_forms table created successfully' AS result;
SHOW CREATE TABLE bpm_forms;
