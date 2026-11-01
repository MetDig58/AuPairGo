namespace AuPairGo.API.Services
{
    public interface IOneSignalService
    {
        Task SendPushNotificationAsync(
            string subscriptionId,
            string title,
            string message,
            int notificationId,
            int dutyTaskId);
    }
}