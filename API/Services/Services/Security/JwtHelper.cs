using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Entities.Model.Users;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces.Settings;

namespace Services.Security;

public static class JwtHelper
{
    public static string GenerateToken(User user, IJwtSettings jwtSettings)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("firstname", user.FirstName ?? ""),
            new("lastname", user.LastName ?? ""),
            new("lastlogin", user.LastLogin.ToString("o")),
            new("avatar", user.AvatarUrl ?? "")
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var token = new JwtSecurityToken(
            jwtSettings.Issuer,
            jwtSettings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.TokenBaseExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ValidationResult ValidateToken(string token, bool withExp, IJwtSettings jwtSettings)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = withExp,
            ClockSkew = TimeSpan.Zero // Optional: reduce or eliminate clock skew if required
        };

        try
        {
            tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return new ValidationResult(true, (JwtSecurityToken)validatedToken);
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, null);
        }
    }


    public record ValidationResult(bool IsValid, JwtSecurityToken? Token);
}