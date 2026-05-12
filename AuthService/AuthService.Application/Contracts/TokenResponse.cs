namespace AuthService.Application.Contracts;

public class TokenResponse
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public string RefreshToken { get; set; } = null!;
    public UserResponse User { get; set; } = null!;
}
