using System.Security.Claims;
using FieldOps.COMMON.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FieldOps.API.Hubs;

[Authorize]
public class JobStatusHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var companyId = Context.User?.FindFirstValue(AppClaimTypes.CompanyId);
        if (!string.IsNullOrWhiteSpace(companyId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company-{companyId}");

        await base.OnConnectedAsync();
    }
}
