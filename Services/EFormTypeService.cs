using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子表單類型服務實作
    /// </summary>
    public class EFormTypeService : IEFormTypeService
    {
        private readonly ILogger<EFormTypeService> _logger;

        public EFormTypeService(ILogger<EFormTypeService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 取得電子表單類型列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>電子表單類型列表回應</returns>
        public async Task<EFormTypeResponse> GetEFormTypesAsync(EFormTypeRequest request)
        {
            try
            {
                _logger.LogInformation($"取得電子表單類型列表 - TokenId: {request.tokenid}, CID: {request.cid}, UID: {request.uid}");

                // 取得固定的表單類型列表
                var eformTypes = GetEFormTypeList();

                var response = new EFormTypeResponse
                {
                    code = "200",
                    msg = "請求成功",
                    data = new EFormTypeData
                    {
                        eformflow = eformTypes
                    }
                };

                _logger.LogInformation($"成功取得電子表單類型列表，共 {eformTypes.Count} 項");

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得電子表單類型列表時發生錯誤 - TokenId: {request.tokenid}");

                return new EFormTypeResponse
                {
                    code = "500",
                    msg = "請求超時"
                };
            }
        }

        /// <summary>
        /// 取得固定的電子表單類型列表
        /// </summary>
        /// <returns>電子表單類型列表</returns>
        private List<EFormTypeItem> GetEFormTypeList()
        {
            return new List<EFormTypeItem>
            {
                new EFormTypeItem
                {
                    eformtype = "L",
                    eformname = "請假單"
                },
                new EFormTypeItem
                {
                    eformtype = "D",
                    eformname = "銷假單"
                },
                new EFormTypeItem
                {
                    eformtype = "O",
                    eformname = "外出外訓單"
                },
                new EFormTypeItem
                {
                    eformtype = "A",
                    eformname = "加班單"
                },
                new EFormTypeItem
                {
                    eformtype = "R",
                    eformname = "出勤單"
                },
                new EFormTypeItem
                {
                    eformtype = "T",
                    eformname = "出差單"
                }
            };
        }
    }
}
