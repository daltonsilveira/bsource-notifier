namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTargetWebSocketEndpoint
{
    public NotificationTargetWebSocketEndpoint(string? group)
    {
        Group = group;
    }

    public string? Group { get; }
}
