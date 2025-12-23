using HRSystemAPI.Models;

namespace HRSystemAPI.Services
{
    /// <summary>
    /// 加班表單服務介面（BPM 整合 - PI_OVERTIME_001）
    /// </summary>
    public interface IOvertimeFormService
    {
        /// <summary>
        /// 申請加班表單（透過 BPM API + FTP 上傳附件）- 使用 PI_OVERTIME_001
        /// </summary>
        Task<OvertimeFormOperationResult> CreateOvertimeFormAsync(CreateOvertimeFormRequest request);

        /// <summary>
        /// API 1: 加班單預申請
        /// </summary>
        Task<EFotApplyResponse> EFotApplyAsync(EFotApplyRequest request);

        /// <summary>
        /// API 2: 加班確認申請列表
        /// </summary>
        Task<EFotConfirmListResponse> EFotConfirmListAsync(EFotConfirmListRequest request);

        /// <summary>
        /// API 3: 加班單詳情預覽
        /// </summary>
        Task<EFotPreviewResponse> EFotPreviewAsync(EFotPreviewRequest request);

        /// <summary>
        /// API 4: 加班單確認申請送出
        /// </summary>
        Task<EFotConfirmSubmitResponse> EFotConfirmSubmitAsync(EFotConfirmSubmitRequest request);

        /// <summary>
        /// API: 加班確認列表查詢（不含'e'前綴版本）
        /// </summary>
        Task<EFotConfirmListResponse> FotConfirmListAsync(EFotConfirmListRequest request);

        /// <summary>
        /// API: 加班確認提交（POST /app/efotconfirmlist）
        /// 提交實際發生的加班申請表單，填具實際的加班時間及所需附件後送出
        /// </summary>
        Task<FotConfirmSubmitResponse> FotConfirmSubmitAsync(FotConfirmSubmitRequest request);

        /// <summary>
        /// API 5: 代理人資料查詢
        /// </summary>
        Task<GetAgentResponse> GetAgentAsync(GetAgentRequest request);
    }
}
