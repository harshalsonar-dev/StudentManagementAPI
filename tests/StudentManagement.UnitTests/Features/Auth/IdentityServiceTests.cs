using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using StudentManagement.Domain.Enums;
using StudentManagement.Infrastructure.Data;
using StudentManagement.Infrastructure.Identity;
using StudentManagement.Infrastructure.Services;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class IdentityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly IdentityService _identityService;

        public IdentityServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object, null!, null!, null!, null!);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object, null!, null!, null!, null!);

            _identityService = new IdentityService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _roleManagerMock.Object);
        }

        [Fact]
        public async Task CreateUserAsync_ValidInput_ShouldCreateUserAndAssignRole()
        {
            // Arrange
            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "StrongPass123!"))
                            .ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(r => r.RoleExistsAsync(AppRoles.Student))
                            .ReturnsAsync(true);

            _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.Student))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var (succeeded, _, errors) = await _identityService.CreateUserAsync(
                "user@enterprise.edu", "StrongPass123!", "John Smith", AppRoles.Student);

            // Assert
            succeeded.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateUserAsync_IdentityFailure_ShouldReturnErrors()
        {
            // Arrange
            var identityError = new IdentityError { Description = "Password too weak." };
            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
                            .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var (succeeded, _, errors) = await _identityService.CreateUserAsync(
                "user@enterprise.edu", "weak", "John Smith", AppRoles.Student);

            // Assert
            succeeded.Should().BeFalse();
            errors.Should().Contain("Password too weak.");
        }

        [Fact]
        public async Task AuthenticateAsync_ValidUserAndPassword_ShouldReturnSuccess()
        {
            // Arrange
            var user = new ApplicationUser { Id = "u1", Email = "test@enterprise.edu", UserName = "test" };
            _userManagerMock.Setup(u => u.FindByEmailAsync("test@enterprise.edu"))
                            .ReturnsAsync(user);

            _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(user, "ValidPass123!", true))
                              .ReturnsAsync(SignInResult.Success);

            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                            .ReturnsAsync(new List<string> { AppRoles.Admin });

            // Act
            var (succeeded, isLockedOut, userId, userName, roles) = await _identityService.AuthenticateAsync(
                "test@enterprise.edu", "ValidPass123!");

            // Assert
            succeeded.Should().BeTrue();
            isLockedOut.Should().BeFalse();
            userId.Should().Be("u1");
            roles.Should().Contain(AppRoles.Admin);
        }

        [Fact]
        public async Task AuthenticateAsync_NonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByEmailAsync("nonexistent@enterprise.edu"))
                            .ReturnsAsync((ApplicationUser?)null);

            // Act
            var (succeeded, _, _, _, _) = await _identityService.AuthenticateAsync(
                "nonexistent@enterprise.edu", "AnyPass");

            // Assert
            succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task AssignRoleAsync_ValidUserAndValidRole_ShouldSucceed()
        {
            // Arrange
            var user = new ApplicationUser { Id = "u1", Email = "student@enterprise.edu" };
            _userManagerMock.Setup(u => u.FindByEmailAsync("student@enterprise.edu"))
                            .ReturnsAsync(user);

            _roleManagerMock.Setup(r => r.RoleExistsAsync(AppRoles.Teacher))
                            .ReturnsAsync(true);

            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                            .ReturnsAsync(new List<string> { AppRoles.Student });

            _userManagerMock.Setup(u => u.AddToRoleAsync(user, AppRoles.Teacher))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _identityService.AssignRoleAsync("student@enterprise.edu", AppRoles.Teacher);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AssignRoleAsync_InvalidRole_ShouldReturnFalse()
        {
            // Arrange
            var user = new ApplicationUser { Id = "u1", Email = "student@enterprise.edu" };
            _userManagerMock.Setup(u => u.FindByEmailAsync("student@enterprise.edu"))
                            .ReturnsAsync(user);

            // Act
            var result = await _identityService.AssignRoleAsync("student@enterprise.edu", "FakeNonExistentRole");

            // Assert
            result.Should().BeFalse();
            _roleManagerMock.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        [Fact]
        public async Task AssignRoleAsync_NonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByEmailAsync("unknown@enterprise.edu"))
                            .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _identityService.AssignRoleAsync("unknown@enterprise.edu", AppRoles.Teacher);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
