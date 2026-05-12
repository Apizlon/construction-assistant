namespace AuthService.Application.Contracts;

public class TokenRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

