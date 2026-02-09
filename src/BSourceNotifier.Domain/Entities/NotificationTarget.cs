namespace BSourceNotifier.Domain.Entities;

public sealed class NotificationTarget
{
    public NotificationTarget(string userId, string? email, string? phoneNumber, object? data)
    {
        UserId = userId;
        Email = email;
        PhoneNumber = phoneNumber;
        Data = data;
    }

    public string UserId { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }
    public object? Data { get; }
}
