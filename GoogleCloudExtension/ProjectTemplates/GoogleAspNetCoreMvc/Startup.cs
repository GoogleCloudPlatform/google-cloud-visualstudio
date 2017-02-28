using Google.Cloud.Diagnostics.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace $safeprojectname$
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddGoogleTrace(GetProjectId());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var projectId = GetProjectId();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddGoogle(projectId);
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseGoogleExceptionLogging(projectId,
                    Configuration["GoogleErrorReporting:ServiceName"],
                    Configuration["GoogleErrorReporting:Version"]);
            }

            app.UseStaticFiles();
            app.UseGoogleTrace();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private string GetProjectId()
        {
            string projectId = Configuration["ProjectId"];
            if (projectId == ("YOUR-PROJECT-ID"))
            {
                throw new Exception("Update appsettings.json and replace YOUR-PROJECT-ID"
                    + " with your Google Cloud Project ID, and recompile.");
            }
            if (projectId == null)
            {
                projectId = Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId;
                if (projectId == null)
                {
                    throw new Exception("The logging, tracing and error reporting libraries need a project ID. "
                        + "Update appsettings.json and replace YOUR-PROJECT-ID with your "
                        + "Google Cloud Project ID, and recompile.");
                }
            }
            return projectId;
        }
    }
}
