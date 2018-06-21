using Google.Cloud.Diagnostics.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace _safe_project_name_
{
    public class Program
    {
        public static IHostingEnvironment HostingEnvironment { get; private set; }
        public static IConfiguration Configuration { get; private set; }

        public static string GcpProjectId { get; private set; }
        public static bool HasGcpProjectId => !string.IsNullOrEmpty(GcpProjectId);

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    HostingEnvironment = context.HostingEnvironment;

                    configBuilder.SetBasePath(HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{HostingEnvironment.EnvironmentName}.json", optional: true)
                        .AddEnvironmentVariables();

                    Configuration = configBuilder.Build();
                    GcpProjectId = GetProjectId(Configuration);
                })
                .ConfigureServices(services =>
                {
                    // Add framework services.Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService
                    services.AddMvc();

                    if (HasGcpProjectId)
                    {
                        // Enables Stackdriver Trace.
                        services.AddGoogleTrace(options => options.ProjectId = GcpProjectId);
                        // Sends Exceptions to Stackdriver Error Reporting.
                        services.AddGoogleExceptionLogging(
                            options =>
                            {
                                options.ProjectId = GcpProjectId;
                                options.ServiceName = GetServiceName(Configuration);
                                options.Version = GetVersion(Configuration);
                            });
                        services.AddSingleton<ILoggerProvider>(sp => GoogleLoggerProvider.Create(GcpProjectId));
                    }
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                    if (HostingEnvironment.IsDevelopment())
                    {
                        // Only use Console and Debug logging during development.
                        loggingBuilder.AddConsole(options =>
                            options.IncludeScopes = Configuration.GetValue<bool>("Logging:IncludeScopes"));
                        loggingBuilder.AddDebug();
                    }
                })
                .Configure((app) =>
                {
                    var logger = app.ApplicationServices.GetService<ILoggerFactory>().CreateLogger("Startup");
                    if (HasGcpProjectId)
                    {
                        // Sends logs to Stackdriver Trace.
                        app.UseGoogleTrace();
                        // Sends logs to Stackdriver Error Reporting.
                        app.UseGoogleExceptionLogging();

                        logger.LogInformation(
                            "Stackdriver Logging enabled: https://console.cloud.google.com/logs/");
                        logger.LogInformation(
                            "Stackdriver Error Reporting enabled: https://console.cloud.google.com/errors/");
                        logger.LogInformation(
                            "Stackdriver Trace enabled: https://console.cloud.google.com/traces/");
                    }
                    else
                    {
                        logger.LogWarning(
                            "Stackdriver Logging not enabled. Missing Google:ProjectId in configuration.");
                        logger.LogWarning(
                            "Stackdriver Error Reporting not enabled. Missing Google:ProjectId in configuration.");
                        logger.LogWarning(
                            "Stackdriver Trace not enabled. Missing Google:ProjectId in configuration.");
                    }

                    if (HostingEnvironment.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    else
                    {
                        app.UseExceptionHandler("/Home/Error");
                    }

                    app.UseStaticFiles();

                    app.UseMvc(routes =>
                    {
                        routes.MapRoute(
                            name: "default",
                            template: "{controller=Home}/{action=Index}/{id?}");
                    });
                })
                .Build();

            host.Run();
        }

        private static string GetProjectId(IConfiguration config)
        {
            var instance = Google.Api.Gax.Platform.Instance();
            var projectId = instance?.ProjectId ?? config["Google:ProjectId"];
            if (string.IsNullOrEmpty(projectId))
            {
                // Set Google:ProjectId in appsettings.json to enable stackdriver logging outside of GCP.
                return null;
            }
            return projectId;
        }

        private static string GetServiceName(IConfiguration config)
        {
            var instance = Google.Api.Gax.Platform.Instance();
            // An identifier of the service.
            // See https://cloud.google.com/error-reporting/docs/formatting-error-messages#FIELDS.service.
            var serviceName =
                instance?.GaeDetails?.ServiceId ??
                config["Google:ErrorReporting:ServiceName"];
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new Exception(
                    "The error reporting library needs a service name. " +
                    "Update appsettings.json by setting the Google:ErrorReporting:ServiceName property with your " +
                    "Service Id, then recompile.");
            }
            return serviceName;
        }

        private static string GetVersion(IConfiguration config)
        {
            var instance = Google.Api.Gax.Platform.Instance();
            // The source version of the service.
            // See https://cloud.google.com/error-reporting/docs/formatting-error-messages#FIELDS.version.
            var versionId =
                instance?.GaeDetails?.VersionId ??
                config["Google:ErrorReporting:Version"];
            if (string.IsNullOrEmpty(versionId))
            {
                throw new Exception(
                    "The error reporting library needs a version id. " +
                    "Update appsettings.json by setting the Google:ErrorReporting:Version property with your " +
                    "service version id, then recompile.");
            }
            return versionId;
        }
    }
}
