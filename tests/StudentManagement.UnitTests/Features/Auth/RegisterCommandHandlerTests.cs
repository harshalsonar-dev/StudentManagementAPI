using FluentAssertions;
using Moq;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Register;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Exceptions;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock;
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _identityServiceMock = new Mock<IIdentityService>();
            _handler = new RegisterCommandHandler(_identityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRegistration_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "newstudent@enterprise.edu",
                Password = "Password123!",
                FullName = "New Student",
                Role = AppRoles.Student
            };
            var command = new RegisterCommand(registerDto);

            _identityServiceMock
                .Setup(i => i.CreateUserAsync(registerDto.Email, registerDto.Password, registerDto.FullName, AppRoles.Student, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "user-created-id", Enumerable.Empty<string>()));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().Contain("User registered successfully");
        }

        [Fact]
        public async Task Handle_ValidRegistration_WithTeacherRole_ShouldAssignTeacherRole()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "teacher@enterprise.edu",
                Password = "TeacherPassword123!",
                FullName = "Prof. Smith",
                Role = AppRoles.Teacher
            };
            var command = new RegisterCommand(registerDto);

            _identityServiceMock
                .Setup(i => i.CreateUserAsync(registerDto.Email, registerDto.Password, registerDto.FullName, AppRoles.Teacher, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "teacher-id", Enumerable.Empty<string>()));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _identityServiceMock.Verify(
                i => i.CreateUserAsync(registerDto.Email, registerDto.Password, registerDto.FullName, AppRoles.Teacher, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ExistingEmail_ShouldThrowConflictException()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "alreadyexists@enterprise.edu",
                Password = "Password123!",
                FullName = "Existing User"
            };
            var command = new RegisterCommand(registerDto);

            _identityServiceMock
                .Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, string.Empty, new[] { "User already exists with this email." }));

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                     .WithMessage("*already exists*");
        }

        [Fact]
        public async Task Handle_IdentityFailure_ShouldThrowValidationAppException()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "invalid@enterprise.edu",
                Password = "Password123!",
                FullName = "Invalid User"
            };
            var command = new RegisterCommand(registerDto);

            _identityServiceMock
                .Setup(i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, string.Empty, new[] { "Username contains invalid characters." }));

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationAppException>();
        }
    }
}
