namespace BSourceNotifier.Contracts.Models;

public sealed class NotificationTargetDto
{
    public string UserId { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}
