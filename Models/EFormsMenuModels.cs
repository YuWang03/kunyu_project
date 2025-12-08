namespace HRSystemAPI.Models
{
    /// <summary>
    /// 電子表單選單列表請求模型
    /// </summary>
    public class EFormsMenuRequest
    {
        public string tokenid { get; set; } = string.Empty;
        public string cid { get; set; } = string.Empty;
        public string uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 電子表單選單項目
    /// </summary>
    public class EFormsMenuItem
    {
        public string menuname { get; set; } = string.Empty;
        public string menudesc { get; set; } = string.Empty;
    }

    /// <summary>
    /// 電子表單選單列表資料
    /// </summary>
    public class EFormsMenuData
    {
        public List<EFormsMenuItem> menulist { get; set; } = new List<EFormsMenuItem>();
    }

    /// <summary>
    /// 電子表單選單列表回應模型
    /// </summary>
    public class EFormsMenuResponse
    {
        public string code { get; set; } = string.Empty;
        public string msg { get; set; } = string.Empty;
        public EFormsMenuData? data { get; set; }
    }
}
