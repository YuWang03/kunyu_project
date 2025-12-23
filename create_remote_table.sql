CREATE TABLE IF NOT EXISTS `EFormApprovalRecords` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TokenId` VARCHAR(100) NOT NULL,
    `Cid` VARCHAR(50) NOT NULL,
    `Uid` VARCHAR(50) NOT NULL,
    `UName` VARCHAR(100),
    `UDepartment` VARCHAR(200),
    `EFormType` VARCHAR(10) NOT NULL,
    `EFormId` VARCHAR(200) NOT NULL,
    `ApprovalStatus` VARCHAR(10) NOT NULL,
    `ApprovalFlow` VARCHAR(10) NOT NULL,
    `Comments` TEXT,
    `ApprovalDate` DATETIME NOT NULL,
    `CreatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_uid` (`Uid`),
    INDEX `idx_eformid` (`EFormId`),
    INDEX `idx_eformtype` (`EFormType`),
    INDEX `idx_approval_date` (`ApprovalDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 插入测试数据
INSERT INTO `EFormApprovalRecords` 
(`TokenId`, `Cid`, `Uid`, `UName`, `UDepartment`, `EFormType`, `EFormId`, `ApprovalStatus`, `ApprovalFlow`, `Comments`, `ApprovalDate`, `CreatedAt`)
VALUES 
('94065980', '03546618', '3100', '測試員工', '測試部門', 'L', 'FORM-2024-001', 'Y', 'A', '同意', NOW(), NOW()),
('94065980', '03546618', '3100', '測試員工', '測試部門', 'A', 'FORM-2024-002', 'Y', 'A', '同意', NOW(), NOW()),
('94065980', '03546618', '3100', '測試員工', '測試部門', 'R', 'FORM-2024-003', 'N', 'R', '退回', NOW(), NOW());
