using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CoderamaOpsAI.IntegrationTests.Api;

public class AuthenticationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_ValidCredentials_When_Login_Then_ReturnsTokenWithCorrectStructure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - HTTP Status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();

        // Assert - Response structure
        loginResult!.Token.Should().NotBeNullOrEmpty();
        loginResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        loginResult.UserId.Should().BeGreaterThan(0);
        loginResult.Email.Should().Be("test@example.com");
        loginResult.Name.Should().NotBeNullOrEmpty();

        // Assert - Token structure
        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(loginResult.Token).Should().BeTrue("Token should be valid JWT format");

        var jwtToken = tokenHandler.ReadJwtToken(loginResult.Token);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);

        // Assert - Token expiration (approximately 10 minutes)
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Given_InvalidPassword_When_Login_Then_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Given_NonExistentEmail_When_Login_Then_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid email or password");
    }
}
