using System.Collections.Generic;
using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    public interface IRequestService
    {
       
        Task<int> SubmitRequestAsync(RequestDto model);

        /// <summary>Gửi 1 đơn đang ở trạng thái Draft (đã lưu nháp trước đó) sang Pending - hệ thống xác định người duyệt ngay lúc này.</summary>
        Task SubmitDraftRequestAsync(int requestId, int accountId);

        
        // sua don dang gui
        Task UpdateDraftRequestAsync(int requestId, RequestDto model);

        
        Task ApproveRequestAsync(int requestId, int approverAccountId);

        
        Task RejectRequestAsync(int requestId, int approverAccountId);

        Task CancelRequestAsync(int requestId, int accountId);

        Task<List<RequestListItemDto>> GetMyRequestsAsync(int accountId);

        Task<List<RequestListItemDto>> GetPendingApprovalsAsync(int approverAccountId);

        Task<List<RequestListItemDto>> GetProcessedApprovalsAsync(int approverAccountId);

        Task<List<RequestTypeDto>> GetRequestTypesAsync();
    }
}