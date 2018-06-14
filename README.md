# SoftwarePioniere.AspNetCore

Utils and Helpers for AspNetCore

SeriloggingExtensions
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