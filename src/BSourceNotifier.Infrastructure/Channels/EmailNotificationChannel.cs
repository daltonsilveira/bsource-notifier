using System.Net;
using System.Net.Mail;
using BSourceNotifier.Application.Interfaces;
using BSourceNotifier.Domain.Entities;
using BSourceNotifier.Domain.Enums;
using BSourceNotifier.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BSourceNotifier.Infrastructure.Channels;

public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IOptions<NotificationOptions> _options;

    public EmailNotificationChannel(IOptions<NotificationOptions> options)
    {
        _options = options;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var settings = _options.Value.Email;
        if (!settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(notification.Target.Email))
        {
            throw new InvalidOperationException("Target email is required for email notifications.");
        }

        using var message = new MailMessage(settings.From, notification.Target.Email)
        {
            Subject = notification.Title,
            Body = notification.Message,
            IsBodyHtml = false
        };

        using var client = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port)
        {
            Credentials = new NetworkCredential(settings.Smtp.Username, settings.Smtp.Password),
            EnableSsl = settings.Smtp.EnableSsl
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
