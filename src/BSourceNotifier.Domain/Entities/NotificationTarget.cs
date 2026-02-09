namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTarget
{
    public NotificationTarget(string userId, string? email, string? phoneNumber)
    {
        UserId = userId;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public string UserId { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }
}
