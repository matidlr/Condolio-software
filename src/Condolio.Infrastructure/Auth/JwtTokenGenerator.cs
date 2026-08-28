using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Condolio.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Condolio.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTime ExpiraUtc) Generar(TokenRequest request)
    {
        var expira = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UsuarioId),
            new(ClaimTypes.NameIdentifier, request.UsuarioId),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(request.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (request.AdministradorId is { } tenantId)
            claims.Add(new Claim(CondolioClaims.TenantId, tenantId.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
