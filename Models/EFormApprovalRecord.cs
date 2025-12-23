using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 电子表单签核记录实体
    /// </summary>
    [Table("EFormApprovalRecords")]
    public class EFormApprovalRecord
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Token ID
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 公司ID
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 签核者工号
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Uid { get; set; } = string.Empty;

        /// <summary>
        /// 签核者姓名
        /// </summary>
        [MaxLength(100)]
        public string? UName { get; set; }

        /// <summary>
        /// 签核者部门
        /// </summary>
        [MaxLength(200)]
        public string? UDepartment { get; set; }

        /// <summary>
        /// 表单类型 (L=请假, A=加班, R=出勤, T=出差, O=外出, D=销假)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string EFormType { get; set; } = string.Empty;

        /// <summary>
        /// 表单编号
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string EFormId { get; set; } = string.Empty;

        /// <summary>
        /// 签核状态 (Y=同意, N=不同意, R=退回)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string ApprovalStatus { get; set; } = string.Empty;

        /// <summary>
        /// 签核流程 (A=核准, R=退回, T=终止, J=驳回)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string ApprovalFlow { get; set; } = string.Empty;

        /// <summary>
        /// 签核意见
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? Comments { get; set; }

        /// <summary>
        /// 签核日期时间
        /// </summary>
        [Required]
        public DateTime ApprovalDate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
