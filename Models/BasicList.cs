using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 基本資料選單列表 - 查詢請求
    /// </summary>
    public class MenuListRequest
    {
        /// <summary>
        /// Token ID
        /// </summary>
        [JsonPropertyName("tokenid")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// 公司代碼
        /// </summary>
        [JsonPropertyName("cid")]
        public string Cid { get; set; } = string.Empty;

        /// <summary>
        /// 使用者ID (員工編號)
        /// </summary>
        [Required(ErrorMessage = "使用者ID為必填")]
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 基本資料選單列表 - 查詢結果
    /// </summary>
    public class MenuListResponse
    {
        /// <summary>
        /// 狀態碼 ("200" 成功, "500" 失敗)
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "200";

        /// <summary>
        /// 訊息
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 選單資料
        /// </summary>
        [JsonPropertyName("data")]
        public MenuData? Data { get; set; }
    }

    /// <summary>
    /// 選單資料
    /// </summary>
    public class MenuData
    {
        /// <summary>
        /// 選單列表
        /// </summary>
        [JsonPropertyName("menulist")]
        public List<string> MenuList { get; set; } = new();
    }

    #region 舊版本模型 (保留供內部使用)

    /// <summary>
    /// 員工基本資料模型 (內部使用)
    /// </summary>
    internal class BasicInformation
    {
        /// <summary>
        /// 員工ID
        /// </summary>
        [Display(Name = "員工ID")]
        public int EMPLOYEE_ID { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Display(Name = "姓名")]
        public string? EMPLOYEE_CNAME { get; set; }

        /// <summary>
        /// 工號
        /// </summary>
        [Display(Name = "工號")]
        public string? EMPLOYEE_NO { get; set; }

        /// <summary>
        /// 公司
        /// </summary>
        [Display(Name = "公司")]
        public string? COMPANY_CNAME { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        [Display(Name = "部門")]
        public string? DEPARTMENT_CNAME { get; set; }

        /// <summary>
        /// 職稱
        /// </summary>
        [Display(Name = "職稱")]
        public string? JOB_CNAME { get; set; }

        /// <summary>
        /// 到職日
        /// </summary>
        [Display(Name = "到職日")]
        public DateTime? EMPLOYEE_ORG_START_DATE { get; set; }

        /// <summary>
        /// 電子信箱
        /// </summary>
        [Display(Name = "電子信箱")]
        public string? EMPLOYEE_EMAIL_1 { get; set; }
    }

    #endregion
}