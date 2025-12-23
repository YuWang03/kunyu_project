-- ============================================================
-- BPM 表單同步資料表結構 (MySQL)
-- 目標資料庫: AppHome (54.46.24.34)
-- 說明: 用於存放從 BPM 中間件同步的表單資料，並記錄本地操作
-- ============================================================

-- 使用資料庫
USE AppHome;

-- ============================================================
-- 1. BPM 表單主表 (bpm_forms)
-- 說明: 儲存所有類型表單的共通資訊
-- ============================================================
DROP TABLE IF EXISTS bpm_forms;
CREATE TABLE bpm_forms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號 (BPM流程序號，唯一碼)',
    form_code VARCHAR(50) NOT NULL COMMENT '表單代碼 (如: PI_LEAVE_001, PI_OVERTIME_001)',
    form_type VARCHAR(20) NOT NULL COMMENT '表單類型 (LEAVE=請假, OVERTIME=加班, BUSINESS_TRIP=出差, CANCEL_LEAVE=銷假)',
    form_version VARCHAR(10) DEFAULT '1.0.0' COMMENT '表單版本',
    
    -- 申請人資訊
    applicant_id VARCHAR(50) NOT NULL COMMENT '申請人工號',
    applicant_name VARCHAR(100) COMMENT '申請人姓名',
    applicant_department VARCHAR(200) COMMENT '申請人部門',
    company_id VARCHAR(50) COMMENT '公司代碼',
    
    -- 表單內容 (JSON 格式，存放完整表單資料)
    form_data JSON COMMENT '表單詳細資料 (JSON格式)',
    
    -- 狀態資訊
    status VARCHAR(30) NOT NULL DEFAULT 'PENDING' COMMENT '表單狀態 (PENDING=待審核, APPROVED=已核准, REJECTED=已拒絕, CANCELLED=已取消, WITHDRAWN=已撤回)',
    bpm_status VARCHAR(30) COMMENT 'BPM 原始狀態',
    
    -- 時間戳記
    apply_date DATETIME COMMENT '申請日期',
    submit_time DATETIME COMMENT '送出時間',
    last_sync_time DATETIME COMMENT '最後同步時間 (從BPM)',
    
    -- 審核資訊
    current_approver_id VARCHAR(50) COMMENT '目前簽核人工號',
    current_approver_name VARCHAR(100) COMMENT '目前簽核人姓名',
    approval_comment TEXT COMMENT '簽核意見',
    
    -- 本地操作記錄
    is_cancelled TINYINT(1) DEFAULT 0 COMMENT '是否已取消 (APP端)',
    cancel_reason TEXT COMMENT '取消原因',
    cancel_time DATETIME COMMENT '取消時間',
    cancelled_by VARCHAR(50) COMMENT '取消操作人',
    
    is_synced_to_bpm TINYINT(1) DEFAULT 0 COMMENT '是否已同步至BPM',
    sync_error_message TEXT COMMENT '同步錯誤訊息',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    
    -- 索引
    UNIQUE KEY uk_form_id (form_id),
    INDEX idx_form_code (form_code),
    INDEX idx_form_type (form_type),
    INDEX idx_applicant_id (applicant_id),
    INDEX idx_status (status),
    INDEX idx_apply_date (apply_date),
    INDEX idx_company_id (company_id),
    INDEX idx_is_cancelled (is_cancelled)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='BPM表單主表';

-- ============================================================
-- 2. 請假單詳細資料表 (bpm_leave_forms)
-- 說明: 儲存請假單特有欄位
-- ============================================================
DROP TABLE IF EXISTS bpm_leave_forms;
CREATE TABLE bpm_leave_forms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號 (對應 bpm_forms.form_id)',
    
    -- 請假資訊
    leave_type_code VARCHAR(20) COMMENT '假別代碼',
    leave_type_name VARCHAR(50) COMMENT '假別名稱',
    start_date DATE NOT NULL COMMENT '請假起始日期',
    start_time TIME COMMENT '起始時間',
    end_date DATE NOT NULL COMMENT '請假結束日期',
    end_time TIME COMMENT '結束時間',
    leave_hours DECIMAL(10,2) COMMENT '請假時數',
    leave_days DECIMAL(10,2) COMMENT '請假天數',
    
    -- 其他資訊
    reason TEXT COMMENT '請假事由',
    agent_id VARCHAR(50) COMMENT '代理人工號',
    agent_name VARCHAR(100) COMMENT '代理人姓名',
    leave_event_date DATE COMMENT '請假事件發生日 (婚假、產假)',
    
    -- 附件
    has_attachments TINYINT(1) DEFAULT 0 COMMENT '是否有附件',
    attachments JSON COMMENT '附件資訊 (JSON)',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    
    -- 索引
    UNIQUE KEY uk_form_id (form_id),
    INDEX idx_leave_type_code (leave_type_code),
    INDEX idx_start_date (start_date),
    INDEX idx_end_date (end_date),
    INDEX idx_agent_id (agent_id),
    
    -- 外鍵
    CONSTRAINT fk_leave_form_id FOREIGN KEY (form_id) REFERENCES bpm_forms(form_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='請假單詳細資料';

-- ============================================================
-- 3. 加班單詳細資料表 (bpm_overtime_forms)
-- 說明: 儲存加班單特有欄位
-- ============================================================
DROP TABLE IF EXISTS bpm_overtime_forms;
CREATE TABLE bpm_overtime_forms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號 (對應 bpm_forms.form_id)',
    
    -- 加班資訊
    overtime_date DATE NOT NULL COMMENT '加班日期',
    planned_start_time DATETIME COMMENT '預計加班起始時間',
    planned_end_time DATETIME COMMENT '預計加班結束時間',
    actual_start_time DATETIME COMMENT '實際加班起始時間',
    actual_end_time DATETIME COMMENT '實際加班結束時間',
    overtime_hours DECIMAL(10,2) COMMENT '加班時數',
    
    -- 處理方式
    process_type INT DEFAULT 0 COMMENT '處理方式 (0=轉補休, 1=加班費)',
    
    -- 事由
    reason TEXT COMMENT '加班事由',
    
    -- 附件
    has_attachments TINYINT(1) DEFAULT 0 COMMENT '是否有附件',
    attachments JSON COMMENT '附件資訊 (JSON)',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    
    -- 索引
    UNIQUE KEY uk_form_id (form_id),
    INDEX idx_overtime_date (overtime_date),
    INDEX idx_process_type (process_type),
    
    -- 外鍵
    CONSTRAINT fk_overtime_form_id FOREIGN KEY (form_id) REFERENCES bpm_forms(form_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='加班單詳細資料';

-- ============================================================
-- 4. 出差單詳細資料表 (bpm_business_trip_forms)
-- 說明: 儲存出差單特有欄位
-- ============================================================
DROP TABLE IF EXISTS bpm_business_trip_forms;
CREATE TABLE bpm_business_trip_forms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號 (對應 bpm_forms.form_id)',
    
    -- 出差資訊
    trip_date DATE NOT NULL COMMENT '出差日期',
    start_date DATE NOT NULL COMMENT '出差起始日期',
    end_date DATE NOT NULL COMMENT '出差結束日期',
    trip_days DECIMAL(10,2) COMMENT '出差天數',
    
    -- 地點與任務
    location VARCHAR(200) COMMENT '出差地點',
    main_tasks TEXT COMMENT '出差主要任務',
    reason TEXT COMMENT '出差事由',
    
    -- 費用
    estimated_costs TEXT COMMENT '費用預估',
    
    -- 附件
    has_attachments TINYINT(1) DEFAULT 0 COMMENT '是否有附件',
    attachments JSON COMMENT '附件資訊 (JSON)',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    
    -- 索引
    UNIQUE KEY uk_form_id (form_id),
    INDEX idx_trip_date (trip_date),
    INDEX idx_start_date (start_date),
    INDEX idx_end_date (end_date),
    INDEX idx_location (location),
    
    -- 外鍵
    CONSTRAINT fk_business_trip_form_id FOREIGN KEY (form_id) REFERENCES bpm_forms(form_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='出差單詳細資料';

-- ============================================================
-- 5. 銷假單詳細資料表 (bpm_cancel_leave_forms)
-- 說明: 儲存銷假單特有欄位
-- ============================================================
DROP TABLE IF EXISTS bpm_cancel_leave_forms;
CREATE TABLE bpm_cancel_leave_forms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號 (對應 bpm_forms.form_id)',
    
    -- 銷假資訊
    original_leave_form_id VARCHAR(100) COMMENT '原請假單編號',
    cancel_reason TEXT COMMENT '銷假原因',
    
    -- 原請假單資訊
    original_leave_type VARCHAR(50) COMMENT '原假別',
    original_start_date DATE COMMENT '原請假起始日期',
    original_start_time TIME COMMENT '原起始時間',
    original_end_date DATE COMMENT '原請假結束日期',
    original_end_time TIME COMMENT '原結束時間',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新時間',
    
    -- 索引
    UNIQUE KEY uk_form_id (form_id),
    INDEX idx_original_leave_form_id (original_leave_form_id),
    
    -- 外鍵
    CONSTRAINT fk_cancel_leave_form_id FOREIGN KEY (form_id) REFERENCES bpm_forms(form_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='銷假單詳細資料';

-- ============================================================
-- 6. 表單簽核歷程表 (bpm_form_approval_history)
-- 說明: 記錄表單的簽核歷程
-- ============================================================
DROP TABLE IF EXISTS bpm_form_approval_history;
CREATE TABLE bpm_form_approval_history (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) NOT NULL COMMENT '表單編號',
    
    -- 簽核資訊
    sequence_no INT NOT NULL COMMENT '簽核順序',
    approver_id VARCHAR(50) NOT NULL COMMENT '簽核人工號',
    approver_name VARCHAR(100) COMMENT '簽核人姓名',
    approver_department VARCHAR(200) COMMENT '簽核人部門',
    
    -- 簽核結果
    action VARCHAR(20) NOT NULL COMMENT '簽核動作 (APPROVE=核准, REJECT=拒絕, RETURN=退回)',
    comment TEXT COMMENT '簽核意見',
    action_time DATETIME COMMENT '簽核時間',
    
    -- 系統欄位
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '建立時間',
    
    -- 索引
    INDEX idx_form_id (form_id),
    INDEX idx_approver_id (approver_id),
    INDEX idx_action_time (action_time),
    
    -- 外鍵
    CONSTRAINT fk_approval_history_form_id FOREIGN KEY (form_id) REFERENCES bpm_forms(form_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='表單簽核歷程';

-- ============================================================
-- 7. 表單同步日誌表 (bpm_form_sync_logs)
-- 說明: 記錄與 BPM 中間件的同步操作
-- ============================================================
DROP TABLE IF EXISTS bpm_form_sync_logs;
CREATE TABLE bpm_form_sync_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '自增主鍵',
    form_id VARCHAR(100) COMMENT '表單編號',
    
    -- 同步資訊
    sync_type VARCHAR(20) NOT NULL COMMENT '同步類型 (FETCH=從BPM拉取, PUSH=推送至BPM, CANCEL=取消同步)',
    sync_direction VARCHAR(10) NOT NULL COMMENT '同步方向 (IN=進入, OUT=送出)',
    sync_status VARCHAR(20) NOT NULL COMMENT '同步狀態 (SUCCESS=成功, FAILED=失敗, PARTIAL=部分成功)',
    
    -- 請求/回應
    request_data JSON COMMENT '請求資料',
    response_data JSON COMMENT '回應資料',
    error_message TEXT COMMENT '錯誤訊息',
    
    -- 操作者
    operator_id VARCHAR(50) COMMENT '操作人工號',
    
    -- 時間
    sync_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '同步時間',
    
    -- 索引
    INDEX idx_form_id (form_id),
    INDEX idx_sync_type (sync_type),
    INDEX idx_sync_status (sync_status),
    INDEX idx_sync_time (sync_time)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='表單同步日誌';

-- ============================================================
-- 8. 建立觸發器 - 自動更新 updated_at
-- ============================================================

-- bpm_forms 的觸發器已由 ON UPDATE CURRENT_TIMESTAMP 處理

-- ============================================================
-- 9. 建立檢視表 - 方便查詢
-- ============================================================

-- 請假單完整檢視
DROP VIEW IF EXISTS v_leave_forms;
CREATE VIEW v_leave_forms AS
SELECT 
    f.id,
    f.form_id,
    f.form_code,
    f.applicant_id,
    f.applicant_name,
    f.applicant_department,
    f.company_id,
    f.status,
    f.apply_date,
    f.is_cancelled,
    f.cancel_reason,
    f.cancel_time,
    l.leave_type_code,
    l.leave_type_name,
    l.start_date,
    l.start_time,
    l.end_date,
    l.end_time,
    l.leave_hours,
    l.leave_days,
    l.reason,
    l.agent_id,
    l.agent_name,
    l.has_attachments,
    f.created_at,
    f.updated_at
FROM bpm_forms f
LEFT JOIN bpm_leave_forms l ON f.form_id = l.form_id
WHERE f.form_type = 'LEAVE';

-- 加班單完整檢視
DROP VIEW IF EXISTS v_overtime_forms;
CREATE VIEW v_overtime_forms AS
SELECT 
    f.id,
    f.form_id,
    f.form_code,
    f.applicant_id,
    f.applicant_name,
    f.applicant_department,
    f.company_id,
    f.status,
    f.apply_date,
    f.is_cancelled,
    f.cancel_reason,
    f.cancel_time,
    o.overtime_date,
    o.planned_start_time,
    o.planned_end_time,
    o.actual_start_time,
    o.actual_end_time,
    o.overtime_hours,
    o.process_type,
    o.reason,
    o.has_attachments,
    f.created_at,
    f.updated_at
FROM bpm_forms f
LEFT JOIN bpm_overtime_forms o ON f.form_id = o.form_id
WHERE f.form_type = 'OVERTIME';

-- 出差單完整檢視
DROP VIEW IF EXISTS v_business_trip_forms;
CREATE VIEW v_business_trip_forms AS
SELECT 
    f.id,
    f.form_id,
    f.form_code,
    f.applicant_id,
    f.applicant_name,
    f.applicant_department,
    f.company_id,
    f.status,
    f.apply_date,
    f.is_cancelled,
    f.cancel_reason,
    f.cancel_time,
    b.trip_date,
    b.start_date,
    b.end_date,
    b.trip_days,
    b.location,
    b.main_tasks,
    b.reason,
    b.estimated_costs,
    b.has_attachments,
    f.created_at,
    f.updated_at
FROM bpm_forms f
LEFT JOIN bpm_business_trip_forms b ON f.form_id = b.form_id
WHERE f.form_type = 'BUSINESS_TRIP';

-- ============================================================
-- 完成訊息
-- ============================================================
SELECT 'BPM 表單同步資料表建立完成!' AS Message;
