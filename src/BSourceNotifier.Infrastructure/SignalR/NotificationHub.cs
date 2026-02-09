using Microsoft.AspNetCore.SignalR;

namespace BSourceNotifier.Infrastructure.SignalR;

public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = Context.User?.Identity?.Name
            ?? httpContext?.Request.Query["userId"].ToString();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }
}
