using AuPairGo.API.Data;
using AuPairGo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Services
{
    public sealed class NotificationRetryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationRetryWorker> _logger;

        private static readonly TimeSpan RetryInterval =
            TimeSpan.FromMinutes(1);

        private const int MaximumAttempts = 5;
        private const int BatchSize = 50;

        public NotificationRetryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationRetryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RetryFailedNotificationsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (DbUpdateException exception)
                {
                    _logger.LogError(
                        exception,
                        "The notification retry worker could not update notification records.");
                }

                await Task.Delay(RetryInterval, stoppingToken);
            }
        }

        private async Task RetryFailedNotificationsAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var oneSignalService = scope.ServiceProvider
                .GetRequiredService<IOneSignalService>();

            var retryBefore = DateTime.UtcNow.Subtract(RetryInterval);

            var notifications = await context.NotificationLogs
                .Where(notification =>
                    notification.PushDeliveryStatus ==
                        PushDeliveryStatus.Failed &&
                    notification.PushAttemptCount <
                        MaximumAttempts &&
                    (
                        notification.PushLastAttemptAt == null ||
                        notification.PushLastAttemptAt <= retryBefore
                    ))
                .OrderBy(notification => notification.PushLastAttemptAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                await RetryNotificationAsync(
                    context,
                    oneSignalService,
                    notification,
                    cancellationToken);
            }
        }

        private async Task RetryNotificationAsync(
            ApplicationDbContext context,
            IOneSignalService oneSignalService,
            NotificationLog notification,
            CancellationToken cancellationToken)
        {
            var recipient = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.Id == notification.RecipientUserId,
                    cancellationToken);

            if (recipient == null)
            {
                notification.PushLastAttemptAt = DateTime.UtcNow;
                notification.PushError =
                    "Recipient user could not be found.";

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    "Notification {NotificationId} cannot be retried because recipient user {UserId} was not found.",
                    notification.Id,
                    notification.RecipientUserId);

                return;
            }

            if (string.IsNullOrWhiteSpace(recipient.OneSignalPlayerId))
            {
                notification.PushLastAttemptAt = DateTime.UtcNow;
                notification.PushError =
                    "Recipient has no registered OneSignal subscription.";

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Notification {NotificationId} is waiting for a OneSignal subscription.",
                    notification.Id);

                return;
            }

            notification.PushDeliveryStatus =
                PushDeliveryStatus.Pending;
            notification.PushAttemptCount++;
            notification.PushLastAttemptAt = DateTime.UtcNow;
            notification.PushError = null;

            await context.SaveChangesAsync(cancellationToken);

            try
            {
                await oneSignalService.SendPushNotificationAsync(
                    recipient.OneSignalPlayerId,
                    notification.Title,
                    notification.Message,
                    notification.Id,
                    notification.DutyTaskId ?? 0);

                notification.PushDeliveryStatus =
                    PushDeliveryStatus.Sent;
                notification.PushSentAt = DateTime.UtcNow;
                notification.PushError = null;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Notification {NotificationId} was delivered on retry attempt {Attempt}.",
                    notification.Id,
                    notification.PushAttemptCount);
            }
            catch (HttpRequestException exception)
            {
                notification.PushDeliveryStatus =
                    PushDeliveryStatus.Failed;
                notification.PushError = exception.Message;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    exception,
                    "Notification {NotificationId} retry attempt {Attempt} failed.",
                    notification.Id,
                    notification.PushAttemptCount);
            }
            catch (InvalidOperationException exception)
            {
                notification.PushDeliveryStatus =
                    PushDeliveryStatus.Failed;
                notification.PushError = exception.Message;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    exception,
                    "Notification {NotificationId} retry failed because OneSignal configuration is missing.",
                    notification.Id);
            }
        }
    }
}