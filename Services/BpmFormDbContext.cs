using Microsoft.EntityFrameworkCore;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// BPM 表單資料庫上下文
    /// 用於連接 MySQL 資料庫 (54.46.24.34)
    /// </summary>
    public class BpmFormDbContext : DbContext
    {
        public BpmFormDbContext(DbContextOptions<BpmFormDbContext> options)
            : base(options)
        {
        }

        /// <summary>BPM 表單主表</summary>
        public DbSet<BpmForm> BpmForms { get; set; }

        /// <summary>請假單詳細資料</summary>
        public DbSet<BpmLeaveForm> BpmLeaveForms { get; set; }

        /// <summary>加班單詳細資料</summary>
        public DbSet<BpmOvertimeForm> BpmOvertimeForms { get; set; }

        /// <summary>出差單詳細資料</summary>
        public DbSet<BpmBusinessTripForm> BpmBusinessTripForms { get; set; }

        /// <summary>銷假單詳細資料</summary>
        public DbSet<BpmCancelLeaveForm> BpmCancelLeaveForms { get; set; }

        /// <summary>表單簽核歷程</summary>
        public DbSet<BpmFormApprovalHistory> BpmFormApprovalHistory { get; set; }

        /// <summary>表單同步日誌</summary>
        public DbSet<BpmFormSyncLog> BpmFormSyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // BpmForm 配置
            modelBuilder.Entity<BpmForm>(entity =>
            {
                entity.ToTable("bpm_forms");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId).IsUnique();
                entity.HasIndex(e => e.FormCode);
                entity.HasIndex(e => e.FormType);
                entity.HasIndex(e => e.ApplicantId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ApplyDate);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.IsCancelled);

                // 關聯配置
                entity.HasOne(e => e.LeaveForm)
                    .WithOne(l => l.BpmForm)
                    .HasForeignKey<BpmLeaveForm>(l => l.FormId)
                    .HasPrincipalKey<BpmForm>(f => f.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.OvertimeForm)
                    .WithOne(o => o.BpmForm)
                    .HasForeignKey<BpmOvertimeForm>(o => o.FormId)
                    .HasPrincipalKey<BpmForm>(f => f.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BusinessTripForm)
                    .WithOne(b => b.BpmForm)
                    .HasForeignKey<BpmBusinessTripForm>(b => b.FormId)
                    .HasPrincipalKey<BpmForm>(f => f.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CancelLeaveForm)
                    .WithOne(c => c.BpmForm)
                    .HasForeignKey<BpmCancelLeaveForm>(c => c.FormId)
                    .HasPrincipalKey<BpmForm>(f => f.FormId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ApprovalHistory)
                    .WithOne(h => h.BpmForm)
                    .HasForeignKey(h => h.FormId)
                    .HasPrincipalKey(f => f.FormId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BpmLeaveForm 配置
            modelBuilder.Entity<BpmLeaveForm>(entity =>
            {
                entity.ToTable("bpm_leave_forms");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId).IsUnique();
                entity.HasIndex(e => e.LeaveTypeCode);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
                entity.HasIndex(e => e.AgentId);
            });

            // BpmOvertimeForm 配置
            modelBuilder.Entity<BpmOvertimeForm>(entity =>
            {
                entity.ToTable("bpm_overtime_forms");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId).IsUnique();
                entity.HasIndex(e => e.OvertimeDate);
                entity.HasIndex(e => e.ProcessType);
            });

            // BpmBusinessTripForm 配置
            modelBuilder.Entity<BpmBusinessTripForm>(entity =>
            {
                entity.ToTable("bpm_business_trip_forms");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId).IsUnique();
                entity.HasIndex(e => e.TripDate);
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
                entity.HasIndex(e => e.Location);
            });

            // BpmCancelLeaveForm 配置
            modelBuilder.Entity<BpmCancelLeaveForm>(entity =>
            {
                entity.ToTable("bpm_cancel_leave_forms");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId).IsUnique();
                entity.HasIndex(e => e.OriginalLeaveFormId);
            });

            // BpmFormApprovalHistory 配置
            modelBuilder.Entity<BpmFormApprovalHistory>(entity =>
            {
                entity.ToTable("bpm_form_approval_history");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId);
                entity.HasIndex(e => e.ApproverId);
                entity.HasIndex(e => e.ActionTime);
            });

            // BpmFormSyncLog 配置
            modelBuilder.Entity<BpmFormSyncLog>(entity =>
            {
                entity.ToTable("bpm_form_sync_logs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FormId);
                entity.HasIndex(e => e.SyncType);
                entity.HasIndex(e => e.SyncStatus);
                entity.HasIndex(e => e.SyncTime);
            });
        }
    }
}
