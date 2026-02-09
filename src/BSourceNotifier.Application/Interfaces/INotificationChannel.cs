using BSourceNotifier.Domain.Entities;
using BSourceNotifier.Domain.Enums;

namespace BSourceNotifier.Application.Interfaces;

public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }
    Task SendAsync(Notification notification, CancellationToken cancellationToken);
}
