using System.Net;
using System.Net.Http.Headers;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CoderamaOpsAI.IntegrationTests.Api;

public class AuthorizationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_NoAuthToken_When_AccessProtectedEndpoint_Then_ReturnsUnauthorized()
    {
        // Arrange - Ensure no authorization header
        ApiClient.DefaultRequestHeaders.Authorization = null;

        var protectedEndpoints = new[]
        {
            "/api/products",
            "/api/orders",
            "/api/users"
        };

        // Act & Assert - Test each endpoint
        foreach (var endpoint in protectedEndpoints)
        {
            var response = await ApiClient.GetAsync(endpoint);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"Endpoint {endpoint} should require authentication");
        }
    }

    [Fact]
    public async Task Given_ExpiredToken_When_AccessProtectedEndpoint_Then_ReturnsUnauthorized()
    {
        // Arrange - Create and set expired token
        var expiredToken = CreateExpiredJwtToken();
        ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act - Try to access protected endpoint
        var response = await ApiClient.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Expired token should not grant access to protected endpoints");
    }
}
