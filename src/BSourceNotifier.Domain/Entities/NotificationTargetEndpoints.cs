namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTargetEndpoints
{
    public NotificationTargetEndpoints(
        NotificationTargetEmailEndpoint? email,
        NotificationTargetWebSocketEndpoint? webSocket)
    {
        Email = email;
        WebSocket = webSocket;
    }

    public NotificationTargetEmailEndpoint? Email { get; }
    public NotificationTargetWebSocketEndpoint? WebSocket { get; }
}
