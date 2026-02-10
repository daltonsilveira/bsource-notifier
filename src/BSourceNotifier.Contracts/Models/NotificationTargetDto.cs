namespace BSourceNotifier.Contracts.Models;

public sealed class NotificationTargetDto
{
    public string UserId { get; init; } = string.Empty;
    public NotificationTargetEndpointsDto Endpoints { get; init; } = new();
    public object? Data { get; init; }
}
