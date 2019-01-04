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
        public string UserGroupId { get; set; }
        public string GroupClaimType { get; set; } = "http://softwarepioniere.de/groups";
        public string SwaggerClientId { get; set; }
        public string SwaggerClientSecret { get; set; }
    }

    public static class Auth0AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAuth0(this AuthenticationBuilder builder, Action<Auth0Options> configureOptions, string[] contextTokenAddPaths = null)
        {
            Console.WriteLine("AddAuth0");

            Console.WriteLine("AddAuth0: Adding Configuration");
            builder.Services.Configure(configureOptions);
            var settings = builder.Services.BuildServiceProvider().GetService<IOptions<Auth0Options>>().Value;

            Console.WriteLine("AddAuth0: Adding JwtBeaerer");
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

                        return Task.CompletedTask;
                    },

                    OnMessageReceived = context =>
                    {
                        //https://docs.microsoft.com/de-de/aspnet/core/signalr/authn-and-authz?view=aspnetcore-2.1
                        if (contextTokenAddPaths != null)
                        {
                            var accessToken = context.Request.Query["access_token"];

                            bool matchesAny = false;

                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                // If the request is for our hub...
                                var path = context.HttpContext.Request.Path;

                                foreach (var s in contextTokenAddPaths)
                                {
                                    if (path.StartsWithSegments(s))
                                    {
                                        matchesAny = true;
                                        break;
                                    }
                                }
                            }

                            if (matchesAny)
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            Console.WriteLine("AddAuth0: Adding AddAuthorization Admin Policy");
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.IsAdminPolicy, policy => policy.RequireClaim(settings.GroupClaimType, settings.AdminGroupId));
                if (!string.IsNullOrEmpty(settings.UserGroupId))
                {
                    options.AddPolicy(Constants.IsUserPolicy, policy => policy.RequireClaim(settings.GroupClaimType, settings.UserGroupId));
                }
            });

            return builder;

        }
    }
}
