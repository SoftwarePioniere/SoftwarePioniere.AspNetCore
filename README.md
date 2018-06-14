# SoftwarePioniere.AspNetCore

Utils and Helpers for AspNetCore

## User Secrets

https://docs.microsoft.com/de-de/aspnet/core/security/app-secrets?view=aspnetcore-2.1&tabs=windows#secret-manager

## SeriloggingExtensions

```csharp

        var builder = WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureAppServices)
                .Configure(ConfigureApplication)
                .UseSerilog((context, configuration) => context.ConfigureSerilog(configuration))
                ;

        var host = builder.Build();
        await host.RunAsync();


        private static void ConfigureApplication(IApplicationBuilder app)
        {
            app.UseSeriLogging();
        }

```