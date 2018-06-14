using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

namespace SoftwarePioniere.AspNetCore
{
    // ReSharper disable once UnusedMember.Global
    public static class SeriLoggingExtensions
    {
        private static TelemetryClient _telemetryClient;

        private static LoggerConfiguration MinimumLevelDebugOnDev(this LoggerConfiguration loggerConfiguration, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
            }

            return loggerConfiguration;
        }

        public static void ConfigureSerilog(this WebHostBuilderContext webHostBuilderContext,
            LoggerConfiguration loggerConfiguration)
        {
            var assembly = Assembly.GetEntryAssembly();
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? assembly.GetName().Name;

            var appInsightsKey = webHostBuilderContext.Configuration.GetSection("ApplicationInsights").GetValue<string>("InstrumentationKey");

            if (!string.IsNullOrEmpty(appInsightsKey)) {
                _telemetryClient = new TelemetryClient()
                {
                    InstrumentationKey = appInsightsKey
                };
            }

           // var loggerConfiguration = new LoggerConfiguration()
                loggerConfiguration.MinimumLevelDebugOnDev(webHostBuilderContext.HostingEnvironment)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("SoftwarePioniere", LogEventLevel.Information)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProperty("Application", title)
                    .WriteTo.LiterateConsole(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)

                //  .WriteTo.ApplicationInsightsEvents(appInsightsKey)
                ;

            if (!string.IsNullOrEmpty(appInsightsKey)) {
                loggerConfiguration.WriteTo.ApplicationInsightsTraces(_telemetryClient);
            }

            var debugSources = webHostBuilderContext.Configuration.GetValue<string>("DebugSources");
            if (!string.IsNullOrEmpty(debugSources))
            {
                foreach (var source in debugSources.Split(';'))
                    loggerConfiguration.MinimumLevel.Override(source, LogEventLevel.Verbose);
            }

          //  var serilogger = loggerConfiguration.CreateLogger();
        //    Log.Logger = serilogger;
          //  loggingBuilder.AddSerilog(serilogger);
        }


        public static IApplicationBuilder UseSeriLogging(this IApplicationBuilder applicationBuilder)
        {
            var logger = Log.Logger.ForContext(new PropertyEnricher("Method", "Configure"));

            var appLifetime = applicationBuilder.ApplicationServices.GetService<IApplicationLifetime>();

            appLifetime.ApplicationStarted.Register(() => { logger.Information("Application Started!!"); });

            if (_telemetryClient != null) {
                appLifetime.ApplicationStopped.Register(() => { _telemetryClient.Flush(); });
            }

            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);
            applicationBuilder.UseMiddleware<SerilogMiddleware>();

            return applicationBuilder;
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class SerilogMiddleware
    {
        const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        static readonly ILogger Log = Serilog.Log.ForContext<SerilogMiddleware>();

        readonly RequestDelegate _next;

        public SerilogMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var start = Stopwatch.GetTimestamp();
            try
            {
                await _next(httpContext);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                var statusCode = httpContext.Response?.StatusCode;
                var level = statusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;

                var log = level == LogEventLevel.Error ? LogForErrorContext(httpContext) : Log;
                log.Write(level, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, statusCode, elapsedMs);
            }
            // Never caught, because `LogException()` returns false.
            catch (Exception ex) when (LogException(httpContext, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex)) { }
        }

        static bool LogException(HttpContext httpContext, double elapsedMs, Exception ex)
        {
            LogForErrorContext(httpContext)
                .Error(ex, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, 500, elapsedMs);

            return false;
        }

        private static ILogger LogForErrorContext(HttpContext httpContext)
        {
            var request = httpContext.Request;

            var result = Log
                .ForContext("RequestHeaders", request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    destructureObjects: true)
                .ForContext("RequestHost", request.Host)
                .ForContext("RequestProtocol", request.Protocol);

            var ctx = request.HttpContext;
            if (ctx != null && ctx.User != null && ctx.User.Claims != null)
            {
                var id = GetValueFromClaims(ctx.User.Claims.ToArray(), "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");

                if (string.IsNullOrEmpty(id))
                {
                    result.ForContext("UserId", id);
                }

                result.ForContext("RequestId", request.HttpContext.TraceIdentifier);
            }

            if (request.HasFormContentType)
                result = result.ForContext("RequestForm", request.Form.ToDictionary(v => v.Key, v => v.Value.ToString()));

            return result;
        }

        private static string GetValueFromClaims(Claim[] claims, params string[] types)
        {
            foreach (var t in types)
            {
                var val = claims.FirstOrDefault(x => x.Type == t)?.Value;

                if (!string.IsNullOrEmpty(val))
                    return val;
            }

            return string.Empty;
        }

        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }
    }
}
