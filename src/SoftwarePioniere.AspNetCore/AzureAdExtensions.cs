using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CheckNamespace
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SoftwarePioniere.AspNetCore
{

    public class AzureAdOptions
    {
        public string TenantId { get; set; }
        public string Authority => $"https://login.microsoftonline.com/{TenantId}/";
        public string IssuerUrl => $"https://sts.windows.net/{TenantId}/";
        public string IssuerSigningKey { get; set; }
        public string Resource { get; set; }
        public string AdminGroupId { get; set; }
        public string UserGroupId { get; set; }
        public string SwaggerClientId { get; set; }
        public string ContextTokenAddPaths { get; set; }
    }

    public static class AzureAdAuthenticationBuilderExtensions
    {

        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions, Action<AuthorizationOptions> configureAuthorization = null)
        {
            Console.WriteLine("AddAzureAd");

            Console.WriteLine("AddAzureAd: Adding Configuration");
            builder.Services.Configure(configureOptions);

            var azureAdOptions = new AzureAdOptions();
            configureOptions(azureAdOptions);

            var tokenValParam = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = azureAdOptions.IssuerUrl,
                ValidateAudience = true,
                ValidAudience = azureAdOptions.Resource
            };

            if (!string.IsNullOrEmpty(azureAdOptions.IssuerSigningKey) && !string.Equals(azureAdOptions.IssuerSigningKey, "XXX", StringComparison.InvariantCultureIgnoreCase))
            {
                tokenValParam.ValidateIssuerSigningKey = true;
                tokenValParam.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(azureAdOptions.IssuerSigningKey));
            }

            string[] contextTokenAddPaths = null;
            if (!string.IsNullOrEmpty(azureAdOptions.ContextTokenAddPaths))
            {
                contextTokenAddPaths = azureAdOptions.ContextTokenAddPaths.Split(';');
            }

            Console.WriteLine("AddAzureAd: Adding JwtBeaerer");
            builder.AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.Audience = azureAdOptions.Resource;
                jwtBearerOptions.Authority = azureAdOptions.Authority;
                jwtBearerOptions.ClaimsIssuer = azureAdOptions.IssuerUrl;
                jwtBearerOptions.RequireHttpsMetadata = false;
                jwtBearerOptions.SaveToken = true;
                jwtBearerOptions.TokenValidationParameters = tokenValParam;

                jwtBearerOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is JwtSecurityToken token)
                        {
                            if (context.Principal.Identity is ClaimsIdentity identity)
                            {
                                identity.AddClaim(new Claim("access_token", token.RawData));
                                identity.AddClaim(new Claim("tenant", azureAdOptions.TenantId));
                                identity.AddClaim(new Claim("provider", "aad"));
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

            Console.WriteLine("AddAzureAd: Adding AddAuthorization Admin Policy");
            builder.Services.AddAuthorization(authorizationOptions =>
            {
                authorizationOptions.AddPolicy(Constants.IsAdminPolicy, policy => policy.RequireClaim("groups", azureAdOptions.AdminGroupId));

                if (!string.IsNullOrEmpty(azureAdOptions.UserGroupId))
                {
                    authorizationOptions.AddPolicy(Constants.IsAdminPolicy, policy => policy.RequireClaim("groups", azureAdOptions.UserGroupId));
                }

                configureAuthorization?.Invoke(authorizationOptions);
            });

            return builder;

        }
    }
}


