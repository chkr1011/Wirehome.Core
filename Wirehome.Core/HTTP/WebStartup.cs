using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Rewrite;
using Wirehome.Core.Cloud;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Diagnostics.Log;
using Wirehome.Core.Discovery;
using Wirehome.Core.Extensions;
using Wirehome.Core.FunctionPool;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Hardware.GPIO;
using Wirehome.Core.Hardware.I2C;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.History;
using Wirehome.Core.HTTP.Controllers;
using Wirehome.Core.HTTP.Filters;
using Wirehome.Core.Macros;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Notifications;
using Wirehome.Core.Packages;
using Wirehome.Core.Python;
using Wirehome.Core.Resources;
using Wirehome.Core.Scheduler;
using Wirehome.Core.ServiceHost;
using Wirehome.Core.Storage;
using Wirehome.Core.System;
using Wirehome.Core.System.StartupScripts;

namespace Wirehome.Core.HTTP
{
    public class WebStartup
    {
        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddLogging(options =>
            {
                options.AddFilter("Wirehome", LogLevel.Trace);
                options.AddFilter("Microsoft", LogLevel.Warning);
                options.AddConsole();
            });

            IMvcBuilder mvcBuilder = services.AddMvc(config => config.Filters.Add(new DefaultExceptionFilter()));
            mvcBuilder.ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Remove(manager.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                manager.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(ComponentsController).Namespace));
            });

            services.AddSwaggerGen(c =>
            {
                c.DescribeAllEnumsAsStrings();                
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Wirehome.Core API",
                    Version = "v1",
                    Description = "The public API for the Wirehome.Core service.",
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

            foreach (var singletonService in Reflection.GetClassesImplementingInterface<IService>())
            {
                services.AddSingleton(singletonService);
            }

            foreach (var pythonProxy in Reflection.GetClassesImplementingInterface<IInjectedPythonProxy>())
            {
                services.AddSingleton(typeof(IPythonProxy), pythonProxy);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            HttpServerService httpServerService,
            LogService logService,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            GlobalVariablesService globalVariablesService,
            PackageManagerService packageManagerService)
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
            ConfigureWebApps(app, globalVariablesService, packageManagerService);
            ConfigureMvc(app);

            app.Run(httpServerService.HandleRequestAsync);

            StartServices(serviceProvider);
        }

        private void StartServices(IServiceProvider serviceProvider)
        {
            var systemService = serviceProvider.GetRequiredService<SystemService>();
            systemService.Start();

            serviceProvider.GetRequiredService<StorageService>().Start();
            serviceProvider.GetRequiredService<DiagnosticsService>().Start();
            serviceProvider.GetRequiredService<MessageBusService>().Start();

            serviceProvider.GetRequiredService<ResourceService>().Start();
            serviceProvider.GetRequiredService<GlobalVariablesService>().Start();
            serviceProvider.GetRequiredService<CloudService>().Start();

            serviceProvider.GetRequiredService<SchedulerService>().Start();

            // Start hardware related services.
            serviceProvider.GetRequiredService<GpioRegistryService>().Start();
            serviceProvider.GetRequiredService<I2CBusService>().Start();
            serviceProvider.GetRequiredService<MqttService>().Start();
            serviceProvider.GetRequiredService<HttpServerService>().Start();
            serviceProvider.GetRequiredService<DiscoveryService>().Start();

            serviceProvider.GetRequiredService<PythonEngineService>().Start();

            var startupScriptsService = serviceProvider.GetRequiredService<StartupScriptsService>();
            startupScriptsService.Start();

            serviceProvider.GetRequiredService<FunctionPoolService>().Start();
            serviceProvider.GetRequiredService<ServiceHostService>().Start();

            serviceProvider.GetRequiredService<NotificationsService>().Start();

            serviceProvider.GetRequiredService<HistoryService>().Start();

            systemService.OnServicesInitialized();

            // Start data related services.
            serviceProvider.GetRequiredService<ComponentGroupRegistryService>().Start();
            serviceProvider.GetRequiredService<ComponentRegistryService>().Start();
            serviceProvider.GetRequiredService<MacroRegistryService>().Start();

            systemService.OnConfigurationLoaded();

            systemService.OnStartupCompleted();
        }

        private static void ConfigureMvc(IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseMvc(config =>
            {
                config.MapRoute("default", "api/{controller}/{action}/{id?}", null, null, null);
            });
        }

        private static void ConfigureWebApps(IApplicationBuilder app, GlobalVariablesService globalVariablesService, PackageManagerService packageManagerService)
        {
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

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = "/app",
                FileProvider = new PackageFileProvider(GlobalVariableUids.AppPackageUid, globalVariablesService, packageManagerService)
            });

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = "/configurator",
                FileProvider = new PackageFileProvider(GlobalVariableUids.ConfiguratorPackageUid, globalVariablesService, packageManagerService)
            });

            // Open the configurator by default if no path is specified.
            var option = new RewriteOptions();
            option.AddRedirect("^$", "/configurator");
            app.UseRewriter(option);

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
                o.RoutePrefix = "api";
                o.DocumentTitle = "Wirehome.API";
                o.SwaggerEndpoint("/api/v1/swagger.json", "Wirehome.Core API v1");
                o.DisplayRequestDuration();
                o.DocExpansion(DocExpansion.None);
                o.DefaultModelRendering(ModelRendering.Model);
            });
        }
    }
}