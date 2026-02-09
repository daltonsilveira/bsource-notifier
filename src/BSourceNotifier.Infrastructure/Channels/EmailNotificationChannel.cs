using System.Dynamic;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using BSourceNotifier.Application.Interfaces;
using BSourceNotifier.Domain.Entities;
using BSourceNotifier.Domain.Enums;
using BSourceNotifier.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RazorLight;

namespace BSourceNotifier.Infrastructure.Channels;

public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IOptions<NotificationOptions> _options;
    private static readonly RazorLightEngine RazorEngine = new RazorLightEngineBuilder()
        .UseMemoryCachingProvider()
        .Build();

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

        var model = NormalizeModel(notification.Target.Data);
        var body = await RazorEngine.CompileRenderStringAsync(
            notification.Id.ToString("N"),
            notification.Message,
            model);

        using var message = new MailMessage(settings.From, notification.Target.Email)
        {
            Subject = notification.Title,
            Body = body,
            IsBodyHtml = true
        };

        using var client = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port)
        {
            Credentials = new NetworkCredential(settings.Smtp.Username, settings.Smtp.Password),
            EnableSsl = settings.Smtp.EnableSsl
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static object NormalizeModel(object? data)
        => data switch
        {
            null => new object(),
            JsonElement jsonElement => ConvertJsonElement(jsonElement),
            _ => data
        };

    private static object? ConvertJsonElement(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => null
        };

    private static ExpandoObject ConvertJsonObject(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = ConvertJsonElement(property.Value);
        }

        return expando;
    }
}
