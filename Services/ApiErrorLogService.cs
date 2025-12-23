using Microsoft.EntityFrameworkCore;
using HRSystemAPI.Models;
using System.Text.Json;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// API 錯誤日誌服務介面
    /// </summary>
    public interface IApiErrorLogService
    {
        Task LogErrorAsync(
            string endpoint,
            string httpMethod,
            string? requestBody,
            string errorCode,
            string errorMessage,
            string? errorDetails = null,
            string? stackTrace = null,
            string? userId = null,
            string? companyId = null,
            string? tokenId = null,
            string? clientIp = null,
            string? userAgent = null,
            object? additionalInfo = null);
    }

    /// <summary>
    /// API 錯誤日誌資料庫上下文
    /// </summary>
    public class ApiErrorLogDbContext : DbContext
    {
        public ApiErrorLogDbContext(DbContextOptions<ApiErrorLogDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApiErrorLog> ApiErrorLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<ApiErrorLog>(entity =>
            {
                entity.ToTable("api_error_logs");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.Endpoint)
                    .HasColumnName("endpoint")
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(e => e.HttpMethod)
                    .HasColumnName("http_method")
                    .HasMaxLength(10);
                
                entity.Property(e => e.RequestBody)
                    .HasColumnName("request_body")
                    .HasColumnType("text");
                
                entity.Property(e => e.ErrorCode)
                    .HasColumnName("error_code")
                    .HasMaxLength(10);
                
                entity.Property(e => e.ErrorMessage)
                    .HasColumnName("error_message")
                    .HasColumnType("text");
                
                entity.Property(e => e.ErrorDetails)
                    .HasColumnName("error_details")
                    .HasColumnType("text");
                
                entity.Property(e => e.StackTrace)
                    .HasColumnName("stack_trace")
                    .HasColumnType("text");
                
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasMaxLength(50);
                
                entity.Property(e => e.CompanyId)
                    .HasColumnName("company_id")
                    .HasMaxLength(50);
                
                entity.Property(e => e.TokenId)
                    .HasColumnName("token_id")
                    .HasMaxLength(100);
                
                entity.Property(e => e.ClientIp)
                    .HasColumnName("client_ip")
                    .HasMaxLength(50);
                
                entity.Property(e => e.UserAgent)
                    .HasColumnName("user_agent")
                    .HasMaxLength(500);
                
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.Property(e => e.AdditionalInfo)
                    .HasColumnName("additional_info")
                    .HasColumnType("text");
                
                entity.HasIndex(e => e.Endpoint).HasDatabaseName("idx_endpoint");
                entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_company_id");
                entity.HasIndex(e => e.ErrorCode).HasDatabaseName("idx_error_code");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_created_at");
            });
        }
    }

    /// <summary>
    /// API 錯誤日誌服務實作
    /// </summary>
    public class ApiErrorLogService : IApiErrorLogService
    {
        private readonly ApiErrorLogDbContext? _context;
        private readonly ILogger<ApiErrorLogService> _logger;

        public ApiErrorLogService(
            ILogger<ApiErrorLogService> logger,
            ApiErrorLogDbContext? context = null)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 記錄 API 錯誤到資料庫
        /// </summary>
        public async Task LogErrorAsync(
            string endpoint,
            string httpMethod,
            string? requestBody,
            string errorCode,
            string errorMessage,
            string? errorDetails = null,
            string? stackTrace = null,
            string? userId = null,
            string? companyId = null,
            string? tokenId = null,
            string? clientIp = null,
            string? userAgent = null,
            object? additionalInfo = null)
        {
            try
            {
                // 如果 DbContext 不存在，只記錄到應用程式日誌
                if (_context == null)
                {
                    _logger.LogWarning("資料庫未連接，錯誤僅記錄到應用程式日誌: Endpoint={Endpoint}, ErrorCode={ErrorCode}, UserId={UserId}, Message={Message}", 
                        endpoint, errorCode, userId, errorMessage);
                    return;
                }

                var errorLog = new ApiErrorLog
                {
                    Endpoint = endpoint,
                    HttpMethod = httpMethod,
                    RequestBody = requestBody,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage,
                    ErrorDetails = errorDetails,
                    StackTrace = stackTrace,
                    UserId = userId,
                    CompanyId = companyId,
                    TokenId = tokenId,
                    ClientIp = clientIp,
                    UserAgent = userAgent,
                    AdditionalInfo = additionalInfo != null ? JsonSerializer.Serialize(additionalInfo) : null,
                    CreatedAt = DateTime.Now
                };

                await _context.ApiErrorLogs.AddAsync(errorLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功記錄錯誤到資料庫: Endpoint={Endpoint}, ErrorCode={ErrorCode}, UserId={UserId}", 
                    endpoint, errorCode, userId);
            }
            catch (Exception ex)
            {
                // 如果記錄錯誤失敗，只記錄到應用程式日誌，不拋出例外以避免影響主要業務流程
                _logger.LogError(ex, "記錄錯誤到資料庫時發生錯誤: Endpoint={Endpoint}, ErrorCode={ErrorCode}", 
                    endpoint, errorCode);
            }
        }
    }
}
