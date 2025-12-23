using Microsoft.EntityFrameworkCore;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 签核记录数据库上下文
    /// </summary>
    public class EFormApprovalDbContext : DbContext
    {
        public EFormApprovalDbContext(DbContextOptions<EFormApprovalDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 签核记录表
        /// </summary>
        public DbSet<EFormApprovalRecord> EFormApprovalRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置索引
            modelBuilder.Entity<EFormApprovalRecord>()
                .HasIndex(e => e.Uid)
                .HasDatabaseName("idx_uid");

            modelBuilder.Entity<EFormApprovalRecord>()
                .HasIndex(e => e.EFormId)
                .HasDatabaseName("idx_eformid");

            modelBuilder.Entity<EFormApprovalRecord>()
                .HasIndex(e => e.EFormType)
                .HasDatabaseName("idx_eformtype");

            modelBuilder.Entity<EFormApprovalRecord>()
                .HasIndex(e => e.ApprovalDate)
                .HasDatabaseName("idx_approval_date");
        }
    }
}
