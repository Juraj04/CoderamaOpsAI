using System.ComponentModel.DataAnnotations;

namespace CoderamaOpsAI.Api.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password is required")]
    public required string Password { get; set; }
}
