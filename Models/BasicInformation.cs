using System;
using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 員工基本資料模型
    /// </summary>
    public class BasicInformation
    {
        /// <summary>
        /// 員工ID
        /// </summary>
        [Display(Name = "員工ID")]
        public int EMPLOYEE_ID { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = "姓名")]
        public string? EMPLOYEE_CNAME { get; set; }

        /// <summary>
        /// 工號
        /// </summary>
        [Display(Name = "工號")]
        public string? EMPLOYEE_NO { get; set; }

        /// <summary>
        /// 公司
        /// </summary>
        [Display(Name = "公司")]
        public string? COMPANY_CNAME { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        [Display(Name = "部門")]
        public string? DEPARTMENT_CNAME { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        [Display(Name = "職稱")]
        public string? JOB_CNAME { get; set; }

        /// <summary>
        /// 到職日
        /// </summary>
        [Display(Name = "到職日")]
        public DateTime? EMPLOYEE_ORG_START_DATE { get; set; }

        /// <summary>
        /// 電子信箱
        /// </summary>
        [Display(Name = "電子信箱")]
        public string? EMPLOYEE_EMAIL_1 { get; set; }
    }
}