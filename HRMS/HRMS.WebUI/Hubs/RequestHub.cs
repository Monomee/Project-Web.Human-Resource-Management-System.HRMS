using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

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
        private readonly HRMS.WebUI.Services.TempTokenStore _tokenStore;

        public RequestHub(HRMS.WebUI.Services.TempTokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var token = httpContext?.Request.Query["token"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                var principal = _tokenStore.Get(token);
                if (principal != null)
                {
                    Context.Items["UserPrincipal"] = principal;
                }
            }
            await base.OnConnectedAsync();
        }

        public async Task JoinManagerGroup()
        {
            var principal = (Context.Items["UserPrincipal"] as ClaimsPrincipal) ?? Context.User;
            var accountId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(accountId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"manager-{accountId}");
            }
        }

        public async Task JoinEmployeeGroup()
        {
            var principal = (Context.Items["UserPrincipal"] as ClaimsPrincipal) ?? Context.User;
            var accountId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(accountId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"employee-{accountId}");
            }
        }
    }
}