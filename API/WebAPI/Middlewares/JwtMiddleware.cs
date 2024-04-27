//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using System;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Services.Interfaces;

//namespace WebAPI.Middlewares
//{
//    public class JwtMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public JwtMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task InvokeAsync(HttpContext context, IJwtSettings jwtSettings)
//        {
//            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

//            if (token != null)
//                AttachUserToContext(context, jwtSettings, token);

//            await _next(context);
//        }

//        private void AttachUserToContext(HttpContext context, IJwtSettings jwtSettings, string token)
//        {
//            try
//            {
//                var tokenHandler = new JwtSecurityTokenHandler();
//                var key = Encoding.UTF8.GetBytes(jwtSettings.Key);
//                tokenHandler.ValidateToken(token, new TokenValidationParameters
//                {
//                    ValidateIssuerSigningKey = true,
//                    IssuerSigningKey = new SymmetricSecurityKey(key),
//                    ValidateIssuer = true,
//                    ValidIssuer = jwtSettings.Issuer,
//                    ValidateAudience = true,
//                    ValidAudience = jwtSettings.Audience,
//                    ValidateLifetime = true,
//                    ClockSkew = TimeSpan.Zero
//                }, out SecurityToken validatedToken);

//                var jwtToken = (JwtSecurityToken)validatedToken;
//                var userId = jwtToken.Claims.First(x => x.Type == "nameid").Value;

//                // Attach user to context on successful jwt validation
//                context.Items["User"] = userId;
//            }
//            catch
//            {
//                // do nothing if jwt validation fails
//                // user is not attached to context so request won't have access to secure routes
//            }
//        }
//    }
//}
