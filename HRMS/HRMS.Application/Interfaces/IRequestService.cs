using System.Collections.Generic;
using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    public interface IRequestService
    {
        /// <summary>Nhân viên tạo và gửi đơn (Draft -> Pending nếu SubmitImmediately = true).</summary>
        Task<int> SubmitRequestAsync(RequestDto model);

        /// <summary>Quản lý duyệt đơn: Pending -> Approved.</summary>
        Task ApproveRequestAsync(int requestId, int approverAccountId, string? note = null);

        /// <summary>Quản lý từ chối đơn: Pending -> Rejected.</summary>
        Task RejectRequestAsync(int requestId, int approverAccountId, string? note = null);

        /// <summary>Nhân viên huỷ đơn của chính mình khi còn Draft/Pending.</summary>
        Task CancelRequestAsync(int requestId, int accountId);

        /// <summary>Danh sách đơn của một tài khoản (trang MyRequests).</summary>
        Task<List<RequestListItemDto>> GetMyRequestsAsync(int accountId);

        /// <summary>Danh sách đơn đang chờ tài khoản này duyệt (trang ApprovalList).</summary>
        Task<List<RequestListItemDto>> GetPendingApprovalsAsync(int approverAccountId);

        /// <summary>Danh sách đơn tài khoản này đã phê duyệt hoặc từ chối (lịch sử duyệt đơn).</summary>
        Task<List<RequestListItemDto>> GetProcessedApprovalsAsync(int approverAccountId);

        /// <summary>Danh sách loại đơn (đọc từ bảng RequestTypes có sẵn trong DB) để hiển thị dropdown trên UI.</summary>
        Task<List<RequestTypeDto>> GetRequestTypesAsync();
    }
}