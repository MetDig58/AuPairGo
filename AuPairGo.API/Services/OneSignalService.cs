using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuPairGo.API.Services
{
    public class OneSignalService : IOneSignalService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OneSignalService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task SendPushNotificationAsync(
            string subscriptionId,
            string title,
            string message,
            int notificationId,
            int dutyTaskId)
        {
            var appId = _configuration["OneSignal:AppId"];
            var restApiKey = _configuration["OneSignal:RestApiKey"];

            if (string.IsNullOrWhiteSpace(appId) ||
                string.IsNullOrWhiteSpace(restApiKey))
            {
                throw new InvalidOperationException(
                    "OneSignal configuration is missing. Configure OneSignal:AppId and OneSignal:RestApiKey.");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Key",
                    restApiKey);

            var requestBody = new
            {
                app_id = appId,
                include_subscription_ids = new[]
                {
                    subscriptionId
                },
                target_channel = "push",
                headings = new
                {
                    en = title
                },
                contents = new
                {
                    en = message
                },
                data = new
                {
                    notificationId,
                    dutyTaskId
                }
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "notifications",
                requestBody);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"OneSignal rejected the push notification. " +
                    $"Status: {(int)response.StatusCode}. " +
                    $"Response: {responseBody}");
            }
        }
    }
}