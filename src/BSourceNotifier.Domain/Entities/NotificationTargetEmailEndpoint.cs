namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTargetEmailEndpoint
{
    public NotificationTargetEmailEndpoint(string to)
    {
        To = to;
    }

    public string To { get; }
}
