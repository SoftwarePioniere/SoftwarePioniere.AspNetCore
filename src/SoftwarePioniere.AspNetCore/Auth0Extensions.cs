using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CheckNamespace
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace SoftwarePioniere.AspNetCore
{
    public class Auth0Options
    {
        public string TenantId { get; set; }
        public string Domain => $"https://{TenantId}/";
        public string Audience { get; set; }
        public string AdminGroupId { get; set; }
        public string GroupClaimType { get; set; } = "http://softwarepioniere.de/groups";
        public string SwaggerClientId { get; set; }
        public string SwaggerClientSecret { get; set; }
    }

    internal static class Auth0AuthenticationBuilderExtensions
    {       
        public static AuthenticationBuilder AddAuth0(this AuthenticationBuilder builder, Action<Auth0Options> configureOptions)
        {
            builder.Services.Configure(configureOptions);

            var settings = builder.Services.BuildServiceProvider().GetService<IOptions<Auth0Options>>().Value;

            builder.AddJwtBearer(options =>
            {
                options.Audience = settings.Audience;
                options.Authority = settings.Domain;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is JwtSecurityToken token)
                        {
                            if (context.Principal.Identity is ClaimsIdentity identity)
                            {
                                identity.AddClaim(new Claim("access_token", token.RawData));
                                identity.AddClaim(new Claim("tenant", settings.TenantId));
                                identity.AddClaim(new Claim("provider", "auth0"));
                            }
                        }

                        return Task.FromResult(0);
                    }
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", policy => policy.RequireClaim(settings.GroupClaimType, settings.AdminGroupId));
            });

            return builder;

        }
    }
}
