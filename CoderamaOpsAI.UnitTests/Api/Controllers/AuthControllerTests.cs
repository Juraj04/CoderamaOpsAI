using CoderamaOpsAI.Api.Controllers;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Api.Services;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.UnitTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoderamaOpsAI.UnitTests.Api.Controllers;

public class AuthControllerTests : DatabaseTestBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _logger = Substitute.For<ILogger<AuthController>>();
        _controller = new AuthController(DbContext, _jwtTokenService, _logger);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var expectedExpiration = DateTime.UtcNow.AddMinutes(10);
        var expectedToken = "mock-jwt-token";

        _jwtTokenService.GenerateToken(
            Arg.Is<User>(u => u.Email == user.Email),
            Arg.Any<DateTime>())
            .Returns(expectedToken);

        // Act
        var result = await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as LoginResponse;

        response.Should().NotBeNull();
        response!.Token.Should().Be(expectedToken);
        response.Email.Should().Be(user.Email);
        response.Name.Should().Be(user.Name);
        response.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));

        _jwtTokenService.Received(1).GenerateToken(
            Arg.Is<User>(u => u.Email == user.Email),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().NotBeNull();

        _jwtTokenService.DidNotReceive().GenerateToken(Arg.Any<User>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongPassword"
        };

        // Act
        var result = await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().NotBeNull();

        _jwtTokenService.DidNotReceive().GenerateToken(Arg.Any<User>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Login_WithValidCredentials_LogsSuccessfulLogin()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _jwtTokenService.GenerateToken(Arg.Any<User>(), Arg.Any<DateTime>())
            .Returns("mock-token");

        // Act
        await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("logged in successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_LogsWarning()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("non-existent email")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_LogsWarning()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPassword");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongPassword"
        };

        // Act
        await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed login attempt")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Login_UsesNoTrackingQuery()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        DbContext.ChangeTracker.Clear();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _jwtTokenService.GenerateToken(Arg.Any<User>(), Arg.Any<DateTime>())
            .Returns("mock-token");

        // Act
        await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Entries().Should().BeEmpty();
    }
}
