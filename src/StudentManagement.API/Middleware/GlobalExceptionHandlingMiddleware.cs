using System.Net;
using System.Text.Json;
using StudentManagement.Application.Common.Models;
using StudentManagement.Domain.Exceptions;

namespace StudentManagement.API.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot write standardized error.");
                return;
            }

            context.Response.ContentType = "application/json";

            var (statusCode, response) = exception switch
            {
                ValidationAppException valEx => (
                    HttpStatusCode.BadRequest,
                    ApiResponse<object>.FailureResult(valEx.Message, valEx.Errors)),

                NotFoundException nfEx => (
                    HttpStatusCode.NotFound,
                    ApiResponse<object>.FailureResult(nfEx.Message, nfEx.Message)),

                ConflictException confEx => (
                    HttpStatusCode.Conflict,
                    ApiResponse<object>.FailureResult(confEx.Message, confEx.Message)),

                UnauthorizedAppException unauthEx => (
                    HttpStatusCode.Unauthorized,
                    ApiResponse<object>.FailureResult(unauthEx.Message, unauthEx.Message)),

                BadHttpRequestException or JsonException => (
                    HttpStatusCode.BadRequest,
                    ApiResponse<object>.FailureResult("Malformed or invalid request payload.", exception.Message)),

                _ => (
                    HttpStatusCode.InternalServerError,
                    ApiResponse<object>.FailureResult(
                        "An unexpected internal server error occurred.",
                        _env.IsDevelopment()
                            ? exception.ToString()
                            : "Please contact support with the trace identifier."))
            };

            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
