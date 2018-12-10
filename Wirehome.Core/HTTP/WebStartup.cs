using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using Wirehome.Core.Diagnostics.Log;
using Wirehome.Core.HTTP.Controllers;
using Wirehome.Core.HTTP.Filters;
using Wirehome.Core.Packages;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WebStartup
    {
        // ReSharper disable once UnusedParameter.Local
        public WebStartup(IConfiguration configuration)
        {
        }

        public static Action<IServiceCollection> OnServiceRegistration { get; set; }
        public static IServiceProvider ServiceProvider { get; set; }

        // ReSharper disable once UnusedMember.Global
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var mvcBuilder = services.AddMvc(config => config.Filters.Add(new NotFoundExceptionFilter()));
            mvcBuilder.ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Remove(manager.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                manager.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(ComponentsController).Namespace));
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Wirehome.Core API",
                    Version = "v1",
                    Description = "This is the public API for the Wirehome.Core service.",
                    License = new License
                    {
                        Name = "Apache-2.0",
                        Url = "https://github.com/chkr1011/Wirehome.Core/blob/master/LICENSE"
                    },
                    Contact = new Contact
                    {
                        Name = "Wirehome.Core",
                        Email = string.Empty,
                        Url = "https://github.com/chkr1011/Wirehome.Core"
                    },
                });
            });

            OnServiceRegistration(services);

            ServiceProvider = services.BuildServiceProvider();
            return ServiceProvider;
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            HttpServerService httpServerService,
            LogService logService,
            ILoggerFactory loggerFactory)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (httpServerService == null) throw new ArgumentNullException(nameof(httpServerService));
            if (logService == null) throw new ArgumentNullException(nameof(logService));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            loggerFactory.AddProvider(new LogServiceLoggerProvider(logService));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConfigureSwagger(app);
            ConfigureWebApps(app);
            ConfigureMvc(app);

            app.Run(httpServerService.HandleRequestAsync);
        }

        private static void ConfigureMvc(IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseMvc(config =>
            {
                config.MapRoute("default", "api/{controller}/{action}/{id?}", null, null, null);
            });
        }

        private static void ConfigureWebApps(IApplicationBuilder app)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var webAppRootPath = Path.Combine(baseDirectory, "WebApp");
            var webConfiguratorRootPath = Path.Combine(baseDirectory, "WebConfigurator");

            var storagePaths = new StoragePaths();
            var customContentRootPath = Path.Combine(storagePaths.DataPath, "CustomContent");

            var packagesRootPath = Path.Combine(storagePaths.DataPath, "Packages");
            var storageService = new StorageService(new JsonSerializerService(), new LoggerFactory().CreateLogger<StorageService>());
            storageService.Start();
            if (storageService.TryRead(out PackageManagerServiceOptions repositoryServiceOptions, PackageManagerServiceOptions.Filename))
            {
                if (!string.IsNullOrEmpty(repositoryServiceOptions.RootPath))
                {
                    packagesRootPath = repositoryServiceOptions.RootPath;
                }
            }

            if (Debugger.IsAttached)
            {
                webAppRootPath = Path.Combine(
                    baseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Wirehome.App");

                webConfiguratorRootPath = Path.Combine(
                    baseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Wirehome.Configurator");
            }

            ExposeDirectory(app, "/app", webAppRootPath);
            ExposeDirectory(app, "/configurator", webConfiguratorRootPath);
            ExposeDirectory(app, "/customContent", customContentRootPath);
            ExposeDirectory(app, "/packages", packagesRootPath);
        }

        private static void ExposeDirectory(IApplicationBuilder app, string uri, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = uri,
                FileProvider = new PhysicalFileProvider(path)
            });
        }

        private static void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

            app.UseSwaggerUI(o =>
            {
                o.DocumentTitle = "Wirehome.Swagger";
                o.SwaggerEndpoint("/api/v1/swagger.json", "Wirehome.Core API v1");
                o.DisplayRequestDuration();
                o.DocExpansion(DocExpansion.None);
                o.DefaultModelRendering(ModelRendering.Model);
            });
        }
    }
}