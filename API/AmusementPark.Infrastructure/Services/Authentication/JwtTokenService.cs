using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur JWT pour la génération et la validation des tokens.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings settings;

    public JwtTokenService(JwtSettings settings)
    {
        this.settings = settings;
    }

    public string GenerateUserToken(User user)
    {
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.settings.Key));
        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("firstname", user.FirstName ?? string.Empty),
            new Claim("lastname", user.LastName ?? string.Empty),
            new Claim("lastlogin", user.LastLoginUtc.ToString("o")),
            new Claim("avatar", user.AvatarUrl ?? string.Empty),
        };

        foreach (Role role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, ToLegacyRoleName(role)));
        }

        JwtSecurityToken token = new JwtSecurityToken(
            this.settings.Issuer,
            this.settings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(this.settings.TokenBaseExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Application.Ports.TokenValidationResult ValidateToken(string token, bool validateLifetime)
    {
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.settings.Key)),
            ValidateIssuer = true,
            ValidIssuer = this.settings.Issuer,
            ValidateAudience = true,
            ValidAudience = this.settings.Audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            JwtSecurityToken jwtSecurityToken = (JwtSecurityToken)validatedToken;
            return new Application.Ports.TokenValidationResult
            {
                IsValid = true,
                Subject = jwtSecurityToken.Claims.FirstOrDefault(static claim => claim.Type == JwtRegisteredClaimNames.Sub)?.Value,
                Claims = principal.Claims.ToList(),
            };
        }
        catch
        {
            return new Application.Ports.TokenValidationResult
            {
                IsValid = false,
            };
        }
    }

    private static string ToLegacyRoleName(Role role)
    {
        return role switch
        {
            Role.Admin => "ADMIN",
            Role.Moderator => "MODERATOR",
            _ => "USER",
        };
    }
}
