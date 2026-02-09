using BSourceNotifier.Domain.Enums;

namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationDelivery
{
    public NotificationDelivery(
        Guid id,
        Guid notificationId,
        NotificationChannelType channel,
        DeliveryStatus status,
        string? error,
        DateTime? sentAt)
    {
        Id = id;
        NotificationId = notificationId;
        Channel = channel;
        Status = status;
        Error = error;
        SentAt = sentAt;
    }

    public Guid Id { get; }
    public Guid NotificationId { get; }
    public NotificationChannelType Channel { get; }
    public DeliveryStatus Status { get; }
    public string? Error { get; }
    public DateTime? SentAt { get; }
}
