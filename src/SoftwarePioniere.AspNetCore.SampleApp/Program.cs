using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureAppServices)
                .Configure(ConfigureApplication)
                .UseSerilog((context, configuration) => context.ConfigureSerilog(configuration))
                ;

            var host = builder.Build();

            await host.RunAsync();
        }


        private static void ConfigureApplication(IApplicationBuilder app)
        {
            app
                .UseAuthentication()
                .UseMvc()
                .UseMySeriLogging()
                .UseMySwagger(c =>
                {
                    c.Docs = new[] { "api", "test" };

                    var auth0Options = app.ApplicationServices.GetService<IOptions<Auth0Options>>().Value;
                    c.OAuthAdditionalQueryStringParams = new Dictionary<string, string>
                    {
                        {"API", auth0Options.Audience}
                    };
                    c.OAuthClientId = auth0Options.SwaggerClientId;
                    c.OAuthClientSecret = auth0Options.SwaggerClientSecret;
                });
        }

        private static void ConfigureAppServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddAuth0(options => context.Configuration.Bind("Auth0", options))
                //.AddAzureAd(options => context.Configuration.Bind("AzureAd", options))
                ;


            services.AddMySwagger(c =>
            {
                c.ApiTitle = "Test Api";
                c.Docs = new[] { "api", "test" };
                c.XmlFiles = new string[0];
                c.OAuth2SchemeName = "my_app_auth0";

                var scopes = new Dictionary<string, string>();

                var auth0Options = services.BuildServiceProvider().GetService<IOptions<Auth0Options>>().Value;
                c.OAuth2Scheme = new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = $"{auth0Options.Domain}authorize",
                    Scopes = scopes
                };
            });
        }
    }
}