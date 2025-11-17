using FluentFTP;
using Microsoft.Extensions.Options;
using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    public class FtpService
    {
        private readonly FtpSettings _ftpSettings;
        private readonly ILogger<FtpService> _logger;

        public FtpService(
            IOptions<FtpSettings> ftpSettings,
            ILogger<FtpService> logger)
        {
            _ftpSettings = ftpSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// 建立 FTP 客戶端連線
        /// </summary>
        private AsyncFtpClient CreateClient()
        {
            var client = new AsyncFtpClient(
                _ftpSettings.Host,
                _ftpSettings.Username,
                _ftpSettings.Password,
                _ftpSettings.Port
            );

            return client;
        }

        /// <summary>
        /// 測試 FTP 連線
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("測試 FTP 連線: {Host}:{Port}", _ftpSettings.Host, _ftpSettings.Port);

                using var client = CreateClient();
                await client.Connect();

                var isConnected = client.IsConnected;
                _logger.LogInformation("FTP 連線測試結果: {Result}", isConnected ? "成功" : "失敗");

                await client.Disconnect();
                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP 連線測試失敗");
                return false;
            }
        }

        /// <summary>
        /// 上傳單一檔案（使用 Stream）
        /// </summary>
        public async Task<bool> UploadFileAsync(Stream fileStream, string remotePath)
        {
            try
            {
                _logger.LogInformation("上傳檔案到 FTP: {RemotePath}", remotePath);

                using var client = CreateClient();
                await client.Connect();

                // 確保遠端目錄存在
                var remoteDir = Path.GetDirectoryName(remotePath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(remoteDir))
                {
                    await client.CreateDirectory(remoteDir);
                }

                // 上傳檔案
                var status = await client.UploadStream(
                    fileStream,
                    remotePath,
                    FtpRemoteExists.Overwrite
                );

                await client.Disconnect();

                var success = (status == FtpStatus.Success);
                _logger.LogInformation("檔案上傳{Result}: {RemotePath}", success ? "成功" : "失敗", remotePath);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上傳檔案時發生錯誤: {RemotePath}", remotePath);
                throw;
            }
        }

        /// <summary>
        /// 批次上傳多個檔案（用於表單附件）
        /// </summary>
        public async Task<List<string>> UploadFilesAsync(List<IFormFile> files)
        {
            var uploadedPaths = new List<string>();

            try
            {
                _logger.LogInformation("開始批次上傳 {Count} 個檔案", files.Count);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // 產生唯一檔名
                        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
                        var remotePath = $"{_ftpSettings.UploadPath}{fileName}".Replace("\\", "/");

                        // 上傳檔案
                        using var stream = file.OpenReadStream();
                        var success = await UploadFileAsync(stream, remotePath);

                        if (success)
                        {
                            uploadedPaths.Add(remotePath);
                        }
                        else
                        {
                            _logger.LogWarning("檔案上傳失敗，跳過: {FileName}", file.FileName);
                        }
                    }
                }

                _logger.LogInformation("批次上傳完成，成功上傳 {Count} 個檔案", uploadedPaths.Count);
                return uploadedPaths;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次上傳檔案時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 下載檔案（回傳 Stream）
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string remotePath)
        {
            try
            {
                _logger.LogInformation("從 FTP 下載檔案: {RemotePath}", remotePath);

                using var client = CreateClient();
                await client.Connect();

                var memoryStream = new MemoryStream();
                var status = await client.DownloadStream(memoryStream, remotePath);

                await client.Disconnect();

                if (!status)
                {
                    throw new Exception($"下載檔案失敗: {remotePath}");
                }

                memoryStream.Position = 0;
                _logger.LogInformation("檔案下載成功: {RemotePath}", remotePath);

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載檔案時發生錯誤: {RemotePath}", remotePath);
                throw;
            }
        }

        /// <summary>
        /// 刪除檔案
        /// </summary>
        public async Task<bool> DeleteFileAsync(string remotePath)
        {
            try
            {
                _logger.LogInformation("刪除 FTP 檔案: {RemotePath}", remotePath);

                using var client = CreateClient();
                await client.Connect();

                await client.DeleteFile(remotePath);

                await client.Disconnect();

                _logger.LogInformation("檔案刪除成功: {RemotePath}", remotePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除檔案時發生錯誤: {RemotePath}", remotePath);
                return false;
            }
        }

        /// <summary>
        /// 列出目錄中的檔案
        /// </summary>
        public async Task<List<string>> ListFilesAsync(string remoteDirectory)
        {
            try
            {
                _logger.LogInformation("列出 FTP 目錄: {RemoteDirectory}", remoteDirectory);

                using var client = CreateClient();
                await client.Connect();

                var items = await client.GetListing(remoteDirectory);
                var files = items
                    .Where(item => item.Type == FtpObjectType.File)
                    .Select(item => item.FullName)
                    .ToList();

                await client.Disconnect();

                _logger.LogInformation("找到 {Count} 個檔案", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "列出目錄時發生錯誤: {RemoteDirectory}", remoteDirectory);
                throw;
            }
        }

        /// <summary>
        /// 檢查檔案是否存在
        /// </summary>
        public async Task<bool> FileExistsAsync(string remotePath)
        {
            try
            {
                using var client = CreateClient();
                await client.Connect();

                var exists = await client.FileExists(remotePath);

                await client.Disconnect();
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查檔案是否存在時發生錯誤: {RemotePath}", remotePath);
                return false;
            }
        }
    }
}