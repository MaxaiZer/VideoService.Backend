using Polly;
using Polly.Retry;

namespace CoreService.Api.Extensions;

public static class PolicyExtensions
{
    public static AsyncRetryPolicy GetRetryPolicy()
    {
        return Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 5, // Number of retries
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} of {context.PolicyKey} due to: {exception}.");
                });
    }
}