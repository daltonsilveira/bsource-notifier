using BSourceNotifier.Contracts.Enums;
using BSourceNotifier.Contracts.Models;

namespace BSourceNotifier.Contracts.Commands;

public sealed class SendNotificationCommand
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationChannelType[] Channels { get; init; } = Array.Empty<NotificationChannelType>();
    public NotificationTargetDto Target { get; init; } = new();
}
