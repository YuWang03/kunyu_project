using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 電子表單選單服務實作
    /// </summary>
    public class EFormsMenuService : IEFormsMenuService
    {
        private readonly ILogger<EFormsMenuService> _logger;

        public EFormsMenuService(ILogger<EFormsMenuService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 取得電子表單選單列表
        /// </summary>
        /// <param name="request">請求參數</param>
        /// <returns>電子表單選單列表回應</returns>
        public async Task<EFormsMenuResponse> GetEFormsMenuListAsync(EFormsMenuRequest request)
        {
            try
            {
                _logger.LogInformation($"取得電子表單選單列表 - TokenId: {request.tokenid}, CID: {request.cid}, UID: {request.uid}");

                // 建立電子表單選單列表
                var menuList = new List<EFormsMenuItem>
                {
                    new EFormsMenuItem
                    {
                        menuname = "出勤確認單",
                        menudesc = "補打卡申請"
                    },
                    new EFormsMenuItem
                    {
                        menuname = "外出外訓申請單",
                        menudesc = "外出申請"
                    },
                    new EFormsMenuItem
                    {
                        menuname = "請假單",
                        menudesc = "休假申請"
                    },
                    new EFormsMenuItem
                    {
                        menuname = "加班預申請",
                        menudesc = "預先提報"
                    },
                    new EFormsMenuItem
                    {
                        menuname = "加班確認申請",
                        menudesc = "實際加班"
                    },
                    new EFormsMenuItem
                    {
                        menuname = "銷假單",
                        menudesc = "異動請假"
                    }
                };

                var response = new EFormsMenuResponse
                {
                    code = "200",
                    msg = "請求成功",
                    data = new EFormsMenuData
                    {
                        menulist = menuList
                    }
                };

                _logger.LogInformation($"成功取得電子表單選單列表，共 {menuList.Count} 項");

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得電子表單選單列表時發生錯誤 - TokenId: {request.tokenid}");

                return new EFormsMenuResponse
                {
                    code = "500",
                    msg = "請求超時"
                };
            }
        }
    }
}
