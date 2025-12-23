namespace HRSystemAPI.Models
{
    /// <summary>
    /// 電子表單類型請求模型
    /// </summary>
    public class EFormTypeRequest
    {
        public string tokenid { get; set; } = string.Empty;
        public string uid { get; set; } = string.Empty;
        public string cid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 電子表單類型項目
    /// </summary>
    public class EFormTypeItem
    {
        public string eformtype { get; set; } = string.Empty;
        public string eformname { get; set; } = string.Empty;
    }

    /// <summary>
    /// 電子表單類型資料
    /// </summary>
    public class EFormTypeData
    {
        public List<EFormTypeItem> eformflow { get; set; } = new List<EFormTypeItem>();
    }

    /// <summary>
    /// 電子表單類型回應模型
    /// </summary>
    public class EFormTypeResponse
    {
        public string code { get; set; } = string.Empty;
        public string msg { get; set; } = string.Empty;
        public EFormTypeData? data { get; set; }
    }
}
