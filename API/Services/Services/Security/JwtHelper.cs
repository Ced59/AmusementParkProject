using Entities.Model.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Services.Security
{
    public static class JwtHelper
    {
        public static string GenerateToken(User user, IJwtSettings jwtSettings)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new("FirstName", user.FirstName ?? ""), 
                new("LastName", user.LastName ?? ""), 
                new("lastLogin", user.LastLogin.ToString("o")), 
            };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public record ValidationResult(bool IsValid, JwtSecurityToken? Token);

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

    }
}