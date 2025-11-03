namespace HRSystemAPI.Models
{
    /// <summary>
    /// 考勤狀態代碼定義
    /// </summary>
    public static class AttendanceStatusCode
    {
        /// <summary>
        /// 正常（空白）
        /// </summary>
        public const string Normal = "";

        /// <summary>
        /// 應刷未刷（0）
        /// </summary>
        public const string NotClocked = "0";

        /// <summary>
        /// 遲到（1）
        /// </summary>
        public const string Late = "1";

        /// <summary>
        /// 早退（2）
        /// </summary>
        public const string EarlyLeave = "2";

        /// <summary>
        /// 超時出勤（3）
        /// </summary>
        public const string Overtime = "3";

        /// <summary>
        /// 曠職（4）
        /// </summary>
        public const string Absent = "4";

        /// <summary>
        /// 取得狀態描述
        /// </summary>
        public static string GetStatusDescription(string code)
        {
            return code switch
            {
                Normal => "正常",
                NotClocked => "應刷未刷",
                Late => "遲到",
                EarlyLeave => "早退",
                Overtime => "超時出勤",
                Absent => "曠職",
                _ => "未知"
            };
        }

        /// <summary>
        /// 判斷是否為應刷未刷或曠職
        /// </summary>
        public static bool IsNotClockedOrAbsent(string code)
        {
            return code == NotClocked || code == Absent;
        }
    }
}
