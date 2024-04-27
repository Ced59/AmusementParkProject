using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Settings.Security
{
    public class AddJwtBearerAuthorizationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor != null)
            {
                var hasAuthorize = descriptor.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()
                                   || descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();

                if (hasAuthorize)
                {
                    operation.Security = new List<OpenApiSecurityRequirement>();
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    };

                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [securityScheme] = new List<string>()
                    });
                }
            }
        }
    }
}