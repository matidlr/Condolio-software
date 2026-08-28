namespace Condolio.Infrastructure.Auth;

public class JwtSettings
{
    public string Issuer { get; set; } = "Condolio.Api";
    public string Audience { get; set; } = "Condolio.Web";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}
