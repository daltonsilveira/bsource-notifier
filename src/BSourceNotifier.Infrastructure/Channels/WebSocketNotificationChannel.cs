using BSourceNotifier.Application.Interfaces;
using BSourceNotifier.Domain.Entities;
using BSourceNotifier.Domain.Enums;
using BSourceNotifier.Infrastructure.Options;
using BSourceNotifier.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BSourceNotifier.Infrastructure.Channels;

public sealed class WebSocketNotificationChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IOptions<NotificationOptions> _options;

    public WebSocketNotificationChannel(
        IHubContext<NotificationHub> hubContext,
        IOptions<NotificationOptions> options)
    {
        _hubContext = hubContext;
        _options = options;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.WebSocket;

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (!_options.Value.WebSocket.Enabled)
        {
            return;
        }

        var groupName = notification.Target.Endpoints.WebSocket?.Group;
        if (string.IsNullOrWhiteSpace(groupName))
        {
            groupName = $"user-{notification.Target.UserId}";
        }
        await _hubContext.Clients.Group(groupName).SendAsync(
            "notification",
            new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.CreatedAt,
                notification.Target.UserId,
                notification.Target.Data
            },
            cancellationToken);
    }
}
