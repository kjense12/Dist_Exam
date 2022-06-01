namespace App.Public.DTO.v1.Identity;

public class JwtResponse
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    
    public DateTime RefreshTokenExpiration { get; set; }
    
    public string? PreviousRefreshToken { get; set; }
    
    public DateTime? PreviousRefreshTokenExpiration { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
}