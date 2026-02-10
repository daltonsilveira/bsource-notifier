namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTarget
{
    public NotificationTarget(string userId, NotificationTargetEndpoints endpoints, object? data)
    {
        UserId = userId;
        Endpoints = endpoints;
        Data = data;
    }

    public string UserId { get; }
    public NotificationTargetEndpoints Endpoints { get; }
    public object? Data { get; }
}
