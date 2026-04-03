using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Settings.Security
{
    public class AddJwtBearerAuthorizationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ControllerActionDescriptor? descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor != null)
        {
            bool hasAuthorize = descriptor.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()
                                || descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                                    .Any();

            if (hasAuthorize)
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
                OpenApiSecurityScheme securityScheme = new()
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