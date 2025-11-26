using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Common.Configuration;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace CoderamaOpsAI.IntegrationTests.Infrastructure;

public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program>? _apiFactory;
    private IHost? _workerHost;

    protected HttpClient ApiClient { get; private set; } = null!;
    protected string PostgresConnectionString => _postgresContainer.GetConnectionString();
    protected string RabbitMqConnectionString => _rabbitMqContainer.GetConnectionString();

    public IntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("coderamaopsai_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start containers
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        // Create API factory
        var testConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = PostgresConnectionString,
                ["RabbitMq:Host"] = _rabbitMqContainer.Hostname,
                ["RabbitMq:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["Jwt:Key"] = "ThisIsATestSecretKeyForIntegrationTestsOnly123456789",
                ["Jwt:Issuer"] = "CoderamaOpsAI-Test",
                ["Jwt:Audience"] = "CoderamaOpsAI-Test"
            })
            .Build();

        _apiFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseConfiguration(testConfig);

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext with test database
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(PostgresConnectionString));
                });

                builder.UseEnvironment("Test");
            });

        ApiClient = _apiFactory.CreateClient();

        // Apply migrations and seed data
        using var scope = _apiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        // Start Worker service
        _workerHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = PostgresConnectionString,
                    ["RabbitMq:Host"] = _rabbitMqContainer.Hostname,
                    ["RabbitMq:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                    ["RabbitMq:VirtualHost"] = "/",
                    ["RabbitMq:Username"] = "guest",
                    ["RabbitMq:Password"] = "guest",
                    ["OrderExpiration:IntervalSeconds"] = "5" // Faster for tests
                });
            })
            .ConfigureServices((context, services) =>
            {
                // Configure Worker services (same as in Worker Program.cs)
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(PostgresConnectionString));

                // Add MassTransit with consumers
                services.AddEventBus(context.Configuration, x =>
                {
                    x.AddConsumer<CoderamaOpsAI.Worker.Consumers.OrderCreatedConsumer>();
                    x.AddConsumer<CoderamaOpsAI.Worker.Consumers.OrderCompletedConsumer>();
                    x.AddConsumer<CoderamaOpsAI.Worker.Consumers.OrderExpiredConsumer>();
                });

                // Add background job
                services.AddHostedService<CoderamaOpsAI.Worker.BackgroundJobs.OrderExpirationJob>();
            })
            .Build();

        // Start worker in background
        _ = _workerHost.StartAsync();

        // Give worker time to initialize
        await Task.Delay(2000);
    }

    public async Task DisposeAsync()
    {
        if (_workerHost != null)
        {
            await _workerHost.StopAsync();
            _workerHost.Dispose();
        }

        _apiFactory?.Dispose();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    protected AppDbContext GetDbContext()
    {
        var scope = _apiFactory!.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    protected async Task<string> GetAuthTokenAsync(string email, string password)
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }

    /// <summary>
    /// Sets the authorization header on ApiClient with the provided token
    /// </summary>
    protected void SetAuthorizationHeader(string token)
    {
        ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Gets IEventBus for manually publishing events in tests
    /// </summary>
    protected IEventBus GetEventBus()
    {
        var scope = _apiFactory!.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IEventBus>();
    }

    /// <summary>
    /// Creates an expired JWT token for testing authorization
    /// Returns a token that expired 1 hour ago
    /// </summary>
    protected string CreateExpiredJwtToken()
    {
        var key = "ThisIsATestSecretKeyForIntegrationTestsOnly123456789";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "999"),
            new Claim(JwtRegisteredClaimNames.Email, "expired@example.com"),
            new Claim(JwtRegisteredClaimNames.Name, "Expired User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiredTime = DateTime.UtcNow.AddHours(-1); // Expired 1 hour ago

        var token = new JwtSecurityToken(
            issuer: "CoderamaOpsAI-Test",
            audience: "CoderamaOpsAI-Test",
            claims: claims,
            expires: expiredTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
