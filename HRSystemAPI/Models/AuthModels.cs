using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 登入請求模型
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登入回應模型
    /// </summary>
    public class LoginResponse
    {
        public string TokenId { get; set; } = string.Empty;         // Token ID (格式: nn+ssssss+nn)
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Uid { get; set; } = string.Empty;             // 員工工號 (對應 EMPLOYEE_NO)
        public string EmployeeNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }                          // 是否可使用 (uwork='W')
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 刷新 Token 請求
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 使用者資訊模型
    /// </summary>
    public class UserInfo
    {
        public string Uid { get; set; } = string.Empty;             // 員工工號 (對應 EMPLOYEE_NO)
        public string EmployeeNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }                          // 是否可使用 (uwork='W')
        public string Status { get; set; } = string.Empty;          // uwork 狀態: W/S/X
        public string? CompanyName { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobTitle { get; set; }
    }

    /// <summary>
    /// 帳號狀態查詢回應
    /// </summary>
    public class EmployeeStatusResponse
    {
        public string Uid { get; set; } = string.Empty;             // 員工工號
        public bool IsActive { get; set; }                          // 是否可使用
        public string Status { get; set; } = string.Empty;          // W/S/X
        public string StatusName { get; set; } = string.Empty;      // 使用/停用/永久停權
    }

    /// <summary>
    /// Token 驗證請求
    /// </summary>
    public class TokenValidationRequest
    {
        [Required]
        public string TokenId { get; set; } = string.Empty;
    }
}
