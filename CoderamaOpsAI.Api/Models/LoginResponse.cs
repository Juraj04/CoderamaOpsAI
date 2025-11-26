namespace CoderamaOpsAI.Api.Models;

public class LoginResponse
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required int UserId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
}
