using System.Text.Json;
using Microsoft.Extensions.Options;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 附件服務 - 用於查詢和管理附件
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// 根據附件 ID 查詢實際的檔案 URL
        /// </summary>
        Task<string?> GetFileUrlByIdAsync(string fileId);

        /// <summary>
        /// 批量查詢檔案 URL
        /// </summary>
        Task<Dictionary<string, string>> GetFileUrlsByIdsAsync(List<string> fileIds);

        /// <summary>
        /// 根據附件 ID 構建本地 SFTP 路徑
        /// </summary>
        string BuildLocalSftpPath(string fileId, string? fileName = null);
    }

    /// <summary>
    /// 附件服務實作
    /// </summary>
    public class AttachmentService : IAttachmentService
    {
        private readonly AttachmentSettings _attachmentSettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AttachmentService> _logger;
        private readonly FtpService _ftpService;

        public AttachmentService(
            IOptions<AttachmentSettings> attachmentSettings,
            HttpClient httpClient,
            ILogger<AttachmentService> logger,
            FtpService ftpService)
        {
            _attachmentSettings = attachmentSettings.Value;
            _httpClient = httpClient;
            _logger = logger;
            _ftpService = ftpService;
        }

        /// <summary>
        /// 根據附件 ID 查詢實際的檔案 URL
        /// 首先嘗試從遠端 Attachment 服務查詢，如果失敗則使用本地 SFTP 路徑
        /// </summary>
        public async Task<string?> GetFileUrlByIdAsync(string fileId)
        {
            try
            {
                _logger.LogInformation("查詢檔案 URL，檔案 ID: {FileId}", fileId);

                // 方案 1：嘗試從遠端 Attachment 服務查詢
                if (!string.IsNullOrEmpty(_attachmentSettings.QueryApiUrl))
                {
                    try
                    {
                        var queryUrl = $"{_attachmentSettings.QueryApiUrl}?fileId={fileId}";
                        var response = await _httpClient.GetAsync(queryUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);

                            if (jsonResponse.TryGetProperty("data", out var dataElement) &&
                                dataElement.TryGetProperty("tfileurl", out var urlElement))
                            {
                                var fileUrl = urlElement.GetString();
                                _logger.LogInformation("從遠端服務查詢到檔案 URL: {FileUrl}", fileUrl);
                                return fileUrl;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "從遠端服務查詢檔案 URL 失敗，將使用本地 SFTP 路徑");
                    }
                }

                // 方案 2：使用本地 SFTP 路徑（假設檔案已存在於 SFTP 伺服器）
                var localPath = BuildLocalSftpPath(fileId);
                _logger.LogInformation("使用本地 SFTP 路徑: {LocalPath}", localPath);
                return localPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢檔案 URL 失敗，檔案 ID: {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// 批量查詢檔案 URL
        /// </summary>
        public async Task<Dictionary<string, string>> GetFileUrlsByIdsAsync(List<string> fileIds)
        {
            var results = new Dictionary<string, string>();

            foreach (var fileId in fileIds)
            {
                var fileUrl = await GetFileUrlByIdAsync(fileId);
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    results[fileId] = fileUrl;
                }
            }

            _logger.LogInformation("批量查詢完成，成功查詢 {Count}/{Total} 個檔案", results.Count, fileIds.Count);
            return results;
        }

        /// <summary>
        /// 根據附件 ID 構建本地 SFTP 路徑
        /// 根據 BPM 系統要求，需使用 FTP 服務器格式：FTPTest~~/FTPShare/AppAttachments/{fileId}
        /// SFTP 伺服器上的實際路徑為：D:/FTPShare/AppAttachments/
        /// </summary>
        public string BuildLocalSftpPath(string fileId, string? fileName = null)
        {
            // BPM 系統要求的 FTP 路徑格式：FTPTest~~/FTPShare/AppAttachments/{fileId}
            // 這個格式會被 BPM 中間件解析並映射到實際的 SFTP 伺服器路徑
            
            if (!string.IsNullOrEmpty(fileName))
            {
                // 有檔名時：FTPTest~~/FTPShare/AppAttachments/{fileId}/{fileName}
                return $"FTPTest~~/FTPShare/AppAttachments/{fileId}/{fileName}";
            }

            // 預設假設檔案直接存放在 fileId 目錄下，或檔案名就是 fileId
            // FTPTest~~/FTPShare/AppAttachments/{fileId}
            return $"FTPTest~~/FTPShare/AppAttachments/{fileId}";
        }
    }
}
