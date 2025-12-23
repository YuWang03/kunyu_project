using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

class DiagnoseEmployee
{
    static async Task Main()
    {
        string connectionString = "Server=14.18.232.79,1433;Database=03546618;User Id=ehrapp;Password=fz@r3g#Zzar8sN1MfnTy;TrustServerCertificate=True;Connection Timeout=30;";
        string employeeNo = "3552";

        Console.WriteLine("=== 诊断员工编号 3552 ===\n");

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                Console.WriteLine("✓ 数据库连接成功\n");

                // 查询1: 查找员工 3552
                Console.WriteLine("--- 查询1: 查找员工 3552 ---");
                string query1 = @"
                    SELECT TOP 10
                        EMPLOYEE_NO,
                        EMPLOYEE_CNAME,
                        EMPLOYEE_HIRE_DATE
                    FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                    WHERE EMPLOYEE_NO = @EmployeeNo COLLATE Chinese_Taiwan_Stroke_CI_AS
                ";

                using (SqlCommand cmd = new SqlCommand(query1, connection))
                {
                    cmd.Parameters.AddWithValue("@EmployeeNo", employeeNo);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                Console.WriteLine($"员工编号: {reader["EMPLOYEE_NO"]}");
                                Console.WriteLine($"姓名: {reader["EMPLOYEE_CNAME"]}");
                                Console.WriteLine($"到职日期: {reader["EMPLOYEE_HIRE_DATE"]}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("✗ 未找到员工 3552\n");

                            // 查询2: 查看有没有类似的编号
                            Console.WriteLine("--- 查询2: 查找包含 '3552' 的编号 ---");
                            string query2 = @"
                                SELECT TOP 10
                                    EMPLOYEE_NO,
                                    LEN(EMPLOYEE_NO) as NO_LENGTH,
                                    EMPLOYEE_CNAME
                                FROM [03546618].[dbo].[vwZZ_EMPLOYEE]
                                WHERE EMPLOYEE_NO LIKE '%3552%'
                                   OR EMPLOYEE_NO = '3552'
                            ";
                            cmd.CommandText = query2;
                            cmd.Parameters.Clear();
                            using (SqlDataReader reader2 = await cmd.ExecuteReaderAsync())
                            {
                                if (reader2.HasRows)
                                {
                                    while (await reader2.ReadAsync())
                                    {
                                        Console.WriteLine($"员工编号: '{reader2["EMPLOYEE_NO"]}' (长度: {reader2["NO_LENGTH"]})");
                                        Console.WriteLine($"姓名: {reader2["EMPLOYEE_CNAME"]}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("✗ 未找到包含 '3552' 的编号");
                                }
                            }
                        }
                    }
                }

                // 查询3: 统计总员工数
                Console.WriteLine("\n--- 查询3: 员工总数 ---");
                string query3 = "SELECT COUNT(*) as TOTAL FROM [03546618].[dbo].[vwZZ_EMPLOYEE]";
                using (SqlCommand cmd = new SqlCommand(query3, connection))
                {
                    object result = await cmd.ExecuteScalarAsync();
                    Console.WriteLine($"总员工数: {result}");
                }

                // 查询4: 查看前5个员工的编号样本
                Console.WriteLine("\n--- 查询4: 前5个员工编号样本 ---");
                string query4 = "SELECT TOP 5 EMPLOYEE_NO, EMPLOYEE_CNAME FROM [03546618].[dbo].[vwZZ_EMPLOYEE]";
                using (SqlCommand cmd = new SqlCommand(query4, connection))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine($"  {reader["EMPLOYEE_NO"]} - {reader["EMPLOYEE_CNAME"]}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 错误: {ex.Message}");
            Console.WriteLine($"内部异常: {ex.InnerException?.Message}");
        }
    }
}
