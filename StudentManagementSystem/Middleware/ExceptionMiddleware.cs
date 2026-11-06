using System.Text.Json;
using StudentManagementSystem.DTOs.Common;

namespace StudentManagementSystem.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, cannot write error response.");
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var errorMessage = _env.IsDevelopment()
                    ? ex.Message
                    : "An unexpected internal server error occurred. Please try again later.";

                var response = ApiResponse<string>.FailureResult(
                    errorMessage,
                    _env.IsDevelopment() && ex.StackTrace != null
                        ? new[] { ex.StackTrace }
                        : null);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
            }
        }
    }
}
