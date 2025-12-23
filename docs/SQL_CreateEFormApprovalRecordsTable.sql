-- 创建签核记录表
-- 用于存储 eformapproval 的签核数据

CREATE TABLE IF NOT EXISTS EFormApprovalRecords (
    Id INT AUTO_INCREMENT PRIMARY KEY COMMENT '主键ID',
    TokenId VARCHAR(100) NOT NULL COMMENT 'Token ID',
    Cid VARCHAR(50) NOT NULL COMMENT '公司ID',
    Uid VARCHAR(50) NOT NULL COMMENT '签核者工号',
    UName VARCHAR(100) COMMENT '签核者姓名',
    UDepartment VARCHAR(200) COMMENT '签核者部门',
    EFormType VARCHAR(10) NOT NULL COMMENT '表单类型 (L=请假, A=加班, R=出勤, T=出差, O=外出, D=销假)',
    EFormId VARCHAR(200) NOT NULL COMMENT '表单编号',
    ApprovalStatus VARCHAR(10) NOT NULL COMMENT '签核状态 (Y=同意, N=不同意, R=退回)',
    ApprovalFlow VARCHAR(10) NOT NULL COMMENT '签核流程 (A=核准, R=退回, T=终止, J=驳回)',
    Comments TEXT COMMENT '签核意见',
    ApprovalDate DATETIME NOT NULL COMMENT '签核日期时间',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    INDEX idx_uid (Uid),
    INDEX idx_eformid (EFormId),
    INDEX idx_eformtype (EFormType),
    INDEX idx_approval_date (ApprovalDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='电子表单签核记录';

-- 查询示例
-- SELECT * FROM EFormApprovalRecords WHERE Uid = '00123' ORDER BY ApprovalDate DESC;
-- SELECT * FROM EFormApprovalRecords WHERE EFormId = 'PI-HR-H1A-PKG-Test0000000000053';
