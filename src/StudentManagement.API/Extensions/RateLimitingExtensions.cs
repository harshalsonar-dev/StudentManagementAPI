using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace StudentManagement.API.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string GeneralPolicy = "general-limiter";
        public const string AuthPolicy = "auth-limiter";

        public static IServiceCollection AddEnterpriseRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

                // 1. General Policy: 100 requests per minute per IP / client
                options.AddPolicy(GeneralPolicy, httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                // 2. Strict Auth Policy (Brute-force protection): 10 requests per minute per IP
                options.AddPolicy(AuthPolicy, httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"success\":false,\"message\":\"Too many requests. Please slow down.\",\"errors\":{\"RateLimit\":[\"Rate limit quota exceeded.\"]}}",
                        token);
                };
            });

            return services;
        }
    }
}
