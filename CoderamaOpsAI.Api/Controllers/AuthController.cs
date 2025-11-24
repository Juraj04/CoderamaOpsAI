using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Api.Services;
using CoderamaOpsAI.Dal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Use generic error message to prevent email enumeration
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Verify password using BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

        if (!isPasswordValid)
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Calculate expiration once and use it for both token and response
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user, expiresAt);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email,
            Name = user.Name
        });
    }
}
