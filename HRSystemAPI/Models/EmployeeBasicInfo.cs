using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 員工基本資料模型
    /// </summary>
    public class EmployeeBasicInfo
    {
        /// <summary>
        /// 員工ID
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// 工號
        /// </summary>
        public string EmployeeNo { get; set; } = string.Empty;

        /// <summary>
        /// 公司
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        public string? JobTitle { get; set; }

        /// <summary>
        /// 到職日
        /// </summary>
        public DateTime? JoinDate { get; set; }

        /// <summary>
        /// 電子信箱
        /// </summary>
        public string? Email { get; set; }
    }
}
