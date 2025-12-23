-- ==================================
-- 诊断脚本: 查询员工编号 3552 的数据
-- ==================================

-- 1. 检查员工编号 3552 是否存在
PRINT '====== 查询员工编号 3552 ======'
SELECT TOP 10
    EMPLOYEE_NO,
    EMPLOYEE_CNAME,
    EMPLOYEE_HIRE_DATE
FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
WHERE EMPLOYEE_NO = '3552'

-- 2. 查看该视图中的前几条记录
PRINT '====== 前5条员工记录 ======'
SELECT TOP 5
    EMPLOYEE_NO,
    EMPLOYEE_CNAME,
    EMPLOYEE_HIRE_DATE
FROM [03546618].[dbo].[vwZZ_EMPLOYEE]

-- 3. 检查员工编号 3552 是否有其他格式（有空格、前缀等）
PRINT '====== 包含 3552 的员工编号 ======'
SELECT DISTINCT
    EMPLOYEE_NO,
    LEN(EMPLOYEE_NO) as NO_LENGTH,
    EMPLOYEE_CNAME
FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
WHERE EMPLOYEE_NO LIKE '%3552%'
   OR EMPLOYEE_NO = '3552'
   OR LTRIM(RTRIM(EMPLOYEE_NO)) = '3552'

-- 4. 检查 vwZZ_EMPLOYEE 视图包含的所有列
PRINT '====== vwZZ_EMPLOYEE 视图的列信息 ======'
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_NAME = 'vwZZ_EMPLOYEE'
ORDER BY ORDINAL_POSITION

-- 5. 计算有多少条员工记录
PRINT '====== 总员工数 ======'
SELECT COUNT(*) as TOTAL_EMPLOYEES
FROM [03546618].[dbo].[vwZZ_EMPLOYEE]

-- 6. 检查特休数据 - 员工 3552 的特休余额
PRINT '====== 员工 3552 的特休数据 ======'
SELECT TOP 10
    es.EMPLOYEE_NO,
    es.EMPLOYEE_SPECIAL_YEAR,
    es.EMPLOYEE_SPECIAL_VALUE,
    es.SPECIAL_REMAIN_HOURS,
    es.IS_CLEAR
FROM [03546618].[dbo].[vwZZ_EMPLOYEE_SPECIAL] es
WHERE es.EMPLOYEE_NO = '3552'

-- 7. 检查是否有其他可能的员工编号格式（比如带空格）
PRINT '====== 所有员工编号样本（前20条）======'
SELECT TOP 20
    EMPLOYEE_NO,
    LEN(EMPLOYEE_NO) as LENGTH,
    ASCII(SUBSTRING(EMPLOYEE_NO, 1, 1)) as FIRST_CHAR_ASCII
FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
