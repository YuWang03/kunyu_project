using System.Text.Json;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public class OvertimeService : IOvertimeService
    {
        private readonly BpmService _bpmService;
        private readonly FtpService _ftpService;
        private readonly ILogger<OvertimeService> _logger;

        public OvertimeService(BpmService bpmService, FtpService ftpService, ILogger<OvertimeService> logger)
        {
            _bpmService = bpmService;
            _ftpService = ftpService;
            _logger = logger;
        }

        public async Task<List<OvertimeRecord>> GetOvertimeRecordsAsync(OvertimeQueryRequest request)
        {
            return new List<OvertimeRecord>();
        }

        public async Task<OvertimeRecord?> GetOvertimeByIdAsync(string formId)
        {
            return null;
        }

        public async Task<OvertimeOperationResult> CreateOvertimeAsync(CreateOvertimeRequest request)
        {
            return new OvertimeOperationResult { Success = false, Message = "尚未實作" };
        }

        public async Task<OvertimeOperationResult> CancelOvertimeAsync(string formId, string employeeNo)
        {
            return new OvertimeOperationResult { Success = false, Message = "尚未實作" };
        }

        public async Task<OvertimeOperationResult> UpdateActualOvertimeAsync(string formId, UpdateActualOvertimeRequest request)
        {
            return new OvertimeOperationResult { Success = false, Message = "尚未實作" };
        }

        public async Task<List<OvertimeRecord>> GetRecentOvertimeRecordsAsync(string employeeNo, int months = 2)
        {
            return new List<OvertimeRecord>();
        }
    }
}
