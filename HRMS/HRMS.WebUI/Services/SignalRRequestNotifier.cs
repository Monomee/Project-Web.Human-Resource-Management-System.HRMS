using System.Threading.Tasks;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.WebUI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.WebUI.Services
{
    public class SignalRRequestNotifier : IRequestNotifier
    {
        private readonly IHubContext<RequestHub> _hubContext;

        public SignalRRequestNotifier(IHubContext<RequestHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewRequestAsync(RequestListItemDto request)
        {
            if (request.ApproverAccountId is null) return;

            await _hubContext.Clients
                .Group($"manager-{request.ApproverAccountId}")
                .SendAsync("RequestCreated", request);
        }

        public async Task NotifyRequestProcessedAsync(int accountId, RequestListItemDto request)
        {
            await _hubContext.Clients
                .Group($"employee-{accountId}")
                .SendAsync("RequestProcessed", request);
        }
    }
}