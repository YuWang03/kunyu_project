using System.ComponentModel.DataAnnotations;

namespace HRSystemAPI.Models
{
    /// <summary>
    /// 考勤查詢 - 個人出勤記錄
    /// </summary>
    public class AttendanceRecord
    {
        /// <summary>
        /// 日期 (格式: 2025/10/28)
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 上班刷卡時間 (格式: 2025/10/28 08:00:00 或 "應刷未刷")
        /// </summary>
        public string ClockInTime { get; set; } = string.Empty;

        /// <summary>
        /// 上班刷卡狀態 (正常/應刷未刷/遲到/曠職/超時出勤)
        /// </summary>
        public string ClockInStatus { get; set; } = string.Empty;

        /// <summary>
        /// 下班刷卡時間 (格式: 2025/10/28 17:30:00 或 "應刷未刷")
        /// </summary>
        public string ClockOutTime { get; set; } = string.Empty;

        /// <summary>
        /// 下班刷卡狀態 (正常/應刷未刷/早退/曠職/超時出勤)
        /// </summary>
        public string ClockOutStatus { get; set; } = string.Empty;

        /// <summary>
        /// 上班異常代碼 (0:應刷未刷 1:遲到 2:早退 3:超時出勤 4:曠職 空白:正常)
        /// </summary>
        public string? ClockInCode { get; set; }

        /// <summary>
        /// 下班異常代碼 (0:應刷未刷 1:遲到 2:早退 3:超時出勤 4:曠職 空白:正常)
        /// </summary>
        public string? ClockOutCode { get; set; }
    }

    /// <summary>
    /// 打卡原始資料 (對應 vwZZ_CARD_DATA_MATCH)
    /// </summary>
    internal class CardDataMatch
    {
        /// <summary>
        /// 員工ID
        /// </summary>
        public int? EMPLOYEE_ID { get; set; }

        /// <summary>
        /// 員工編號
        /// </summary>
        public string? EMPLOYEE_NO { get; set; }

        /// <summary>
        /// 員工姓名
        /// </summary>
        public string? EMPLOYEE_CNAME { get; set; }

        /// <summary>
        /// 應出勤日期
        /// </summary>
        public DateTime? WORK_DATE { get; set; }

        /// <summary>
        /// 應刷卡別 (0:上班 1:下班)
        /// </summary>
        public int? WORK_CARD_TYPE { get; set; }

        /// <summary>
        /// 應刷卡時間
        /// </summary>
        public DateTime? WORK_CARD_DATE { get; set; }

        /// <summary>
        /// 實際刷卡時間 (1900-01-01 表示未打卡)
        /// </summary>
        public DateTime? CARD_DATA_DATE { get; set; }

        /// <summary>
        /// 刷卡異常代碼
        /// 空白:正常 0:應刷未刷 1:遲到 2:早退 3:超時出勤 4:曠職
        /// </summary>
        public string? CARD_DATA_CODE { get; set; }
    }

    /// <summary>
    /// 異常代碼定義
    /// </summary>
    public static class AttendanceStatusCode
    {
        public const string Normal = "";           // 正常
        public const string NotClocked = "0";      // 應刷未刷
        public const string Late = "1";            // 遲到
        public const string LeaveEarly = "2";      // 早退
        public const string Overtime = "3";        // 超時出勤
        public const string Absent = "4";          // 曠職

        /// <summary>
        /// 將異常代碼轉換為中文說明
        /// </summary>
        public static string GetStatusDescription(string? code)
        {
            return code switch
            {
                Normal or null => "正常",
                NotClocked => "應刷未刷",
                Late => "遲到",
                LeaveEarly => "早退",
                Overtime => "超時出勤",
                Absent => "曠職",
                _ => "未知狀態"
            };
        }

        /// <summary>
        /// 判斷是否為應刷未刷或曠職 (需顯示 "應刷未刷")
        /// </summary>
        public static bool IsNotClockedOrAbsent(string? code)
        {
            return code == NotClocked || code == Absent;
        }
    }
}