using System.ComponentModel.DataAnnotations;

namespace ReturnFlow.Api.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = "";

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = "";
}

public class LoginRequest
{
    [Required]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}

public class AuthResponse
{
    public string Token { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
}
