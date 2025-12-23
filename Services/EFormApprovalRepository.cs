using HRSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 签核记录仓储实现
    /// </summary>
    public class EFormApprovalRepository : IEFormApprovalRepository
    {
        private readonly EFormApprovalDbContext _context;
        private readonly ILogger<EFormApprovalRepository> _logger;

        public EFormApprovalRepository(
            EFormApprovalDbContext context,
            ILogger<EFormApprovalRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 保存签核记录
        /// </summary>
        public async Task SaveApprovalRecordAsync(EFormApprovalRecord record)
        {
            try
            {
                _logger.LogInformation("开始保存签核记录: Uid={Uid}, FormId={FormId}", 
                    record.Uid, record.EFormId);

                await _context.EFormApprovalRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation("签核记录保存成功: Id={Id}", record.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存签核记录失败: Uid={Uid}, FormId={FormId}", 
                    record.Uid, record.EFormId);
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID获取签核记录
        /// </summary>
        public async Task<List<EFormApprovalRecord>> GetApprovalRecordsByUidAsync(
            string uid, 
            int page = 1, 
            int pageSize = 20, 
            string? eformType = null)
        {
            try
            {
                _logger.LogInformation("查询签核记录: Uid={Uid}, Page={Page}, PageSize={PageSize}, EFormType={EFormType}", 
                    uid, page, pageSize, eformType);

                var query = _context.EFormApprovalRecords
                    .Where(r => r.Uid == uid);

                // 筛选表单类型
                if (!string.IsNullOrEmpty(eformType))
                {
                    var types = eformType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToUpper())
                        .ToList();

                    query = query.Where(r => types.Contains(r.EFormType.ToUpper()));
                }

                var records = await query
                    .OrderByDescending(r => r.ApprovalDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("查询到 {Count} 条签核记录", records.Count);
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询签核记录失败: Uid={Uid}", uid);
                throw;
            }
        }

        /// <summary>
        /// 获取用户签核记录总数
        /// </summary>
        public async Task<int> GetApprovalRecordsCountAsync(string uid, string? eformType = null)
        {
            try
            {
                var query = _context.EFormApprovalRecords
                    .Where(r => r.Uid == uid);

                // 筛选表单类型
                if (!string.IsNullOrEmpty(eformType))
                {
                    var types = eformType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToUpper())
                        .ToList();

                    query = query.Where(r => types.Contains(r.EFormType.ToUpper()));
                }

                var count = await query.CountAsync();
                _logger.LogInformation("用户 {Uid} 的签核记录总数: {Count}", uid, count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询签核记录总数失败: Uid={Uid}", uid);
                throw;
            }
        }

        /// <summary>
        /// 根据表单ID获取签核记录
        /// </summary>
        public async Task<List<EFormApprovalRecord>> GetApprovalRecordsByFormIdAsync(string formId)
        {
            try
            {
                _logger.LogInformation("查询表单签核记录: FormId={FormId}", formId);

                var records = await _context.EFormApprovalRecords
                    .Where(r => r.EFormId == formId)
                    .OrderBy(r => r.ApprovalDate)
                    .ToListAsync();

                _logger.LogInformation("查询到 {Count} 条表单签核记录", records.Count);
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询表单签核记录失败: FormId={FormId}", formId);
                throw;
            }
        }
    }
}
