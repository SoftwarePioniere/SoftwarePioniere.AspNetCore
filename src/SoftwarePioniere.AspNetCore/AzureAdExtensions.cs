using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using Microsoft.AspNetCore.Authentication;
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
    }

    public static class AzureAdAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder) => builder.AddAzureAd(_ => { });

        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);

            var settings = builder.Services.BuildServiceProvider().GetService<IOptions<AzureAdOptions>>().Value;

            var tokenValParam = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = settings.IssuerUrl,
                ValidateAudience = true,
                ValidAudience = settings.Resource                
            };

            if (!string.IsNullOrEmpty(settings.IssuerSigningKey) && !string.Equals(settings.IssuerSigningKey, "XXX", StringComparison.InvariantCultureIgnoreCase))
            {
                tokenValParam.ValidateIssuerSigningKey = true;
                tokenValParam.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.IssuerSigningKey));
            }

            builder.AddJwtBearer(options =>
            {
                options.Audience = settings.Resource;
                options.Authority = settings.Authority;
                options.ClaimsIssuer = settings.IssuerUrl;
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = tokenValParam;
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", policy => policy.RequireClaim("groups", settings.AdminGroupId));
            });

            return builder;

        }
    }


}


