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
                _logger.LogInformation($"取得電子表單選單列表 - TokenId: {request.tokenid}, CID: {request.cid}, UID: {request.uid}, Language: {request.language}");

                // 根據語系取得電子表單選單列表
                var menuList = GetMenuListByLanguage(request.language);

                var response = new EFormsMenuResponse
                {
                    code = "200",
                    msg = GetSuccessMessage(request.language),
                    data = new EFormsMenuData
                    {
                        menulist = menuList
                    }
                };

                _logger.LogInformation($"成功取得電子表單選單列表，共 {menuList.Count} 項 - Language: {request.language}");

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取得電子表單選單列表時發生錯誤 - TokenId: {request.tokenid}");

                return new EFormsMenuResponse
                {
                    code = "500",
                    msg = GetErrorMessage(request.language)
                };
            }
        }

        /// <summary>
        /// 根據語系取得選單列表
        /// </summary>
        /// <param name="language">語系代碼 (T: 繁體中文, C: 簡體中文)</param>
        /// <returns>選單項目列表</returns>
        private List<EFormsMenuItem> GetMenuListByLanguage(string language)
        {
            // 預設為繁體中文
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "T";
            }

            language = language.ToUpper();

            return language switch
            {
                "T" => GetChineseTraditionalMenu(),
                "C" => GetChineseSimplifiedMenu(),
                _ => GetChineseTraditionalMenu() // 預設繁體中文
            };
        }

        /// <summary>
        /// 繁體中文選單
        /// </summary>
        private List<EFormsMenuItem> GetChineseTraditionalMenu()
        {
            return new List<EFormsMenuItem>
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
        }

        /// <summary>
        /// 簡體中文選單
        /// </summary>
        private List<EFormsMenuItem> GetChineseSimplifiedMenu()
        {
            return new List<EFormsMenuItem>
            {
                new EFormsMenuItem
                {
                    menuname = "出勤确认单",
                    menudesc = "补打卡申请"
                },
                new EFormsMenuItem
                {
                    menuname = "外出外训申请单",
                    menudesc = "外出申请"
                },
                new EFormsMenuItem
                {
                    menuname = "请假单",
                    menudesc = "休假申请"
                },
                new EFormsMenuItem
                {
                    menuname = "加班预申请",
                    menudesc = "预先提报"
                },
                new EFormsMenuItem
                {
                    menuname = "加班确认申请",
                    menudesc = "实际加班"
                },
                new EFormsMenuItem
                {
                    menuname = "销假单",
                    menudesc = "异动请假"
                }
            };
        }

        /// <summary>
        /// 取得成功訊息（根據語系）
        /// </summary>
        private string GetSuccessMessage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "T";
            }

            language = language.ToUpper();

            return language switch
            {
                "T" => "請求成功",
                "C" => "请求成功",
                _ => "請求成功"
            };
        }

        /// <summary>
        /// 取得失敗訊息（根據語系）
        /// </summary>
        private string GetErrorMessage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "T";
            }

            language = language.ToUpper();

            return language switch
            {
                "T" => "請求超時",
                "C" => "请求超时",
                _ => "請求超時"
            };
        }
    }
}
