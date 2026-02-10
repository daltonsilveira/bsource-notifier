namespace BSourceNotifier.Contracts.Models;

public sealed class NotificationTargetEndpointsDto
{
    public NotificationTargetEmailEndpointDto? Email { get; init; }
    public NotificationTargetWebSocketEndpointDto? WebSocket { get; init; }
}
