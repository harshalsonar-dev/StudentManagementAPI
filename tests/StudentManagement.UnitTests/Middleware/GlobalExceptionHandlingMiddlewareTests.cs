using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagement.API.Middleware;
using StudentManagement.Application.Common.Models;
using StudentManagement.Domain.Exceptions;
using Xunit;

namespace StudentManagement.UnitTests.Middleware
{
    public class GlobalExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _loggerMock;
        private readonly Mock<IHostEnvironment> _envMock;

        public GlobalExceptionHandlingMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
            _envMock = new Mock<IHostEnvironment>();
            _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        }

        [Fact]
        public async Task InvokeAsync_WhenValidationAppExceptionThrown_ShouldReturn400BadRequest_WithErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                { "Email", new[] { "A valid email is required." } }
            };
            var middleware = new GlobalExceptionHandlingMiddleware(
                _ => throw new ValidationAppException(errors),
                _loggerMock.Object,
                _envMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().ContainKey("Email");
        }

        [Fact]
        public async Task InvokeAsync_WhenNotFoundExceptionThrown_ShouldReturn404NotFound()
        {
            // Arrange
            var middleware = new GlobalExceptionHandlingMiddleware(
                _ => throw new NotFoundException("Student", 99),
                _loggerMock.Object,
                _envMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Contain("was not found");
        }

        [Fact]
        public async Task InvokeAsync_WhenUnauthorizedAppExceptionThrown_ShouldReturn401Unauthorized()
        {
            // Arrange
            var middleware = new GlobalExceptionHandlingMiddleware(
                _ => throw new UnauthorizedAppException("Invalid credentials."),
                _loggerMock.Object,
                _envMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task InvokeAsync_WhenDatabaseFailureOrUnhandledException_ShouldReturn500InternalServerError_Gracefully()
        {
            // Arrange (simulate sudden database disconnection or unexpected server crash)
            var middleware = new GlobalExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Fatal database connection timed out."),
                _loggerMock.Object,
                _envMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            // In Production, internal error message should be sanitized to avoid leaking sensitive connection string or SQL traces
            result.Message.Should().Be("An unexpected internal server error occurred.");
        }
    }
}
