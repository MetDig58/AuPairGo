using AuPairGo.API.Services;

namespace AuPairGo.API.Tests;

public sealed class FakeOneSignalService : IOneSignalService
{
    public List<PushAttempt> Attempts { get; } = new();

    public Task SendPushNotificationAsync(
        string subscriptionId,
        string title,
        string message,
        int notificationId,
        int dutyTaskId)
    {
        Attempts.Add(new PushAttempt
        {
            SubscriptionId = subscriptionId,
            Title = title,
            Message = message,
            NotificationId = notificationId,
            DutyTaskId = dutyTaskId
        });

        return Task.CompletedTask;
    }
}

public sealed class PushAttempt
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int NotificationId { get; set; }
    public int DutyTaskId { get; set; }
}