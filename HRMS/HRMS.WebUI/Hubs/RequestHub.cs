using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.WebUI.Hubs
{
    /// <summary>
    /// Hub SignalR dùng để đẩy real-time các sự kiện liên quan tới đơn:
    /// - "RequestCreated": báo quản lý có đơn mới cần duyệt.
    /// - "RequestProcessed": báo nhân viên đơn của họ vừa được Approve/Reject.
    ///
    /// Client join theo 2 nhóm:
    /// - "manager-{approverId}"  : quản lý lắng nghe đơn cần duyệt của mình
    /// - "employee-{employeeId}" : nhân viên lắng nghe cập nhật đơn của chính họ
    /// </summary>
    public class RequestHub : Hub
    {
        public Task JoinManagerGroup(int approverId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"manager-{approverId}");

        public Task JoinEmployeeGroup(int employeeId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"employee-{employeeId}");
    }
}