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

        public string ContextTokenAddPaths { get; set; }
    }

    public static class Auth0AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAuth0(this AuthenticationBuilder builder, Action<Auth0Options> configureOptions)
        {
            Console.WriteLine("AddAuth0");

            Console.WriteLine("AddAuth0: Adding Configuration");

            var auth0Options = new Auth0Options();
            configureOptions(auth0Options);

            string[] contextTokenAddPaths = null;
            if (!string.IsNullOrEmpty(auth0Options.ContextTokenAddPaths))
            {
                contextTokenAddPaths = auth0Options.ContextTokenAddPaths.Split(';');
            }

            Console.WriteLine("AddAuth0: Adding JwtBeaerer");
            builder.AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.Audience = auth0Options.Audience;
                jwtBearerOptions.Authority = auth0Options.Domain;

                jwtBearerOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is JwtSecurityToken token)
                        {
                            if (context.Principal.Identity is ClaimsIdentity identity)
                            {
                                identity.AddClaim(new Claim("access_token", token.RawData));
                                identity.AddClaim(new Claim("tenant", auth0Options.TenantId));
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
            builder.Services.AddAuthorization(authorizationOptions =>
            {
                authorizationOptions.AddPolicy(Constants.IsAdminPolicy, policy => policy.RequireClaim(auth0Options.GroupClaimType, auth0Options.AdminGroupId));
                if (!string.IsNullOrEmpty(auth0Options.UserGroupId))
                {
                    authorizationOptions.AddPolicy(Constants.IsUserPolicy, policy => policy.RequireClaim(auth0Options.GroupClaimType, auth0Options.UserGroupId));
                }
            });

            return builder;

        }
    }
}
