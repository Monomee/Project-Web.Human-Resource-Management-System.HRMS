using System.Threading.Tasks;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    /// <summary>
    /// Trừu tượng hoá kênh push real-time (SignalR) để Application layer
    /// không phụ thuộc trực tiếp vào Microsoft.AspNetCore.SignalR.
    /// </summary>
    public interface IRequestNotifier
    {
        /// <summary>Báo cho quản lý biết có đơn mới cần duyệt.</summary>
        Task NotifyNewRequestAsync(RequestListItemDto request);

        /// <summary>Báo cho nhân viên biết đơn của họ vừa được xử lý (Approved/Rejected).</summary>
        Task NotifyRequestProcessedAsync(int employeeId, RequestListItemDto request);
    }
}