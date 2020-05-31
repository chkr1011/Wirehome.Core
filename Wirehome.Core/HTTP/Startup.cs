using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.IO;
using System.Linq;
using System.Net;
using MQTTnet.AspNetCore;
using Wirehome.Core.Backup;
using Wirehome.Core.Cloud;
using Wirehome.Core.Components;
using Wirehome.Core.Components.Groups;
using Wirehome.Core.Components.History;
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
    public class Startup
    {
        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddLogging(options =>
            {
                options.SetMinimumLevel(LogLevel.Debug);
                options.AddFilter("Microsoft", LogLevel.Warning);
                options.AddConsole();
            });

            var mvcBuilder = services.AddMvc(o =>
            {
                o.Filters.Add(new DefaultExceptionFilterAttribute());
            });

            mvcBuilder.AddNewtonsoftJson(o =>
            {
                o.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

            //}).AddJsonOptions(o =>
            //{
            //    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            //});

            mvcBuilder.ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Remove(manager.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                manager.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(ComponentsController).Namespace));
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Wirehome.Core API",
                    Version = "v1",
                    Description = "The public API for the Wirehome.Core service.",
                    License = new OpenApiLicense
                    {
                        Name = "Apache-2.0",
                        Url = new Uri("https://github.com/chkr1011/Wirehome.Core/blob/master/LICENSE")
                    },
                    Contact = new OpenApiContact
                    {
                        Name = "Wirehome.Core",
                        Email = string.Empty,
                        Url = new Uri("https://github.com/chkr1011/Wirehome.Core")
                    },
                });
            });

            var serviceImplementations = Reflection.GetClassesAssignableFrom<WirehomeCoreService>();
            foreach (var singletonService in serviceImplementations)
            {
                if (singletonService == typeof(WirehomeCoreService))
                {
                    continue;
                }

                Console.WriteLine($"Registering service {singletonService}");
                services.AddSingleton(singletonService);
            }

            var pythonProxyImplementations = Reflection.GetClassesAssignableFrom<IInjectedPythonProxy>();
            foreach (var pythonProxy in pythonProxyImplementations)
            {
                Console.WriteLine($"Registering Python proxy {pythonProxy}");
                services.AddSingleton(typeof(IPythonProxy), pythonProxy);
            }

            services.AddSingleton<PythonProxyFactory>();

            services.AddCors();
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(
            IApplicationBuilder app,
            HttpServerService httpServerService,
            LogService logService,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            GlobalVariablesService globalVariablesService,
            PackageManagerService packageManagerService,
            MqttService mqttService)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (httpServerService == null) throw new ArgumentNullException(nameof(httpServerService));
            if (logService == null) throw new ArgumentNullException(nameof(logService));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (mqttService == null) throw new ArgumentNullException(nameof(mqttService));

            loggerFactory.AddProvider(new LogServiceLoggerProvider(logService));

            app.UseResponseCompression();
            app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            ConfigureSwagger(app);
            ConfigureWebApps(app, globalVariablesService, packageManagerService, mqttService);
            ConfigureMvc(app);

            app.Run(httpServerService.HandleRequestAsync);

            StartServices(serviceProvider);
        }

        void StartServices(IServiceProvider serviceProvider)
        {
            // Start low level system services.
            var systemService = serviceProvider.GetRequiredService<SystemService>();
            systemService.Start();

            serviceProvider.GetRequiredService<StorageService>().Start();
            serviceProvider.GetRequiredService<BackupService>().Start();
            serviceProvider.GetRequiredService<PackageManagerService>().Start();

            // Start hardware related services.
            serviceProvider.GetRequiredService<GpioRegistryService>().Start();
            serviceProvider.GetRequiredService<I2CBusService>().Start();
            serviceProvider.GetRequiredService<MqttService>().Start();
            serviceProvider.GetRequiredService<HttpServerService>().Start();
            serviceProvider.GetRequiredService<DiscoveryService>().Start();

            serviceProvider.GetRequiredService<DiagnosticsService>().Start();
            serviceProvider.GetRequiredService<MessageBusService>().Start();

            serviceProvider.GetRequiredService<ResourceService>().Start();
            serviceProvider.GetRequiredService<GlobalVariablesService>().Start();
            serviceProvider.GetRequiredService<CloudService>().Start();

            serviceProvider.GetRequiredService<SchedulerService>().Start();

            serviceProvider.GetRequiredService<PythonEngineService>().Start();
            serviceProvider.GetRequiredService<FunctionPoolService>().Start();
            serviceProvider.GetRequiredService<PythonProxyFactory>().PreparePythonProxies();


            var startupScriptsService = serviceProvider.GetRequiredService<StartupScriptsService>();
            startupScriptsService.Start();

            serviceProvider.GetRequiredService<NotificationsService>().Start();
            serviceProvider.GetRequiredService<ServiceHostService>().Start();
            serviceProvider.GetRequiredService<HistoryService>().Start();

            systemService.OnServicesInitialized();

            // Start component related services.
            serviceProvider.GetRequiredService<ComponentGroupRegistryService>().Start();
            serviceProvider.GetRequiredService<ComponentHistoryService>().Start();
            serviceProvider.GetRequiredService<ComponentRegistryService>().Start();
            serviceProvider.GetRequiredService<MacroRegistryService>().Start();

            systemService.OnConfigurationLoaded();

            systemService.OnStartupCompleted();
        }

        static void ConfigureMvc(IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        static void ConfigureWebApps(
            IApplicationBuilder app,
            GlobalVariablesService globalVariablesService,
            PackageManagerService packageManagerService,
            MqttService mqttService)
        {
            var storagePaths = new StoragePaths();
            var customContentRootPath = Path.Combine(storagePaths.DataPath, "CustomContent");

            var packagesRootPath = Path.Combine(storagePaths.DataPath, "Packages");
            var storageService = new StorageService(new JsonSerializerService(), new LoggerFactory().CreateLogger<StorageService>());
            storageService.Start();
            if (storageService.TryReadSerializedValue(out PackageManagerServiceOptions repositoryServiceOptions, PackageManagerServiceOptions.Filename))
            {
                if (!string.IsNullOrEmpty(repositoryServiceOptions.RootPath))
                {
                    packagesRootPath = repositoryServiceOptions.RootPath;
                }
            }

            if (!Directory.Exists(customContentRootPath))
            {
                Directory.CreateDirectory(customContentRootPath);
            }

            if (!Directory.Exists(packagesRootPath))
            {
                Directory.CreateDirectory(packagesRootPath);
            }

            app.Map("/upnp.xml", options =>
            {
                options.Run(async h =>
                {
                    var upnpFilePath = Path.Combine(storagePaths.BinPath, "Discovery", "upnp.xml");
                    var upnpDefinition = await File.ReadAllBytesAsync(upnpFilePath).ConfigureAwait(false);
                    await h.Response.Body.WriteAsync(upnpDefinition).ConfigureAwait(false);
                });
            });

            // Open the configurator by default if no path is specified.
            var option = new RewriteOptions();
            option.AddRedirect("^$", "/configurator");
            app.UseRewriter(option);

            ExposeDirectory(app, "/app", new PackageFileProvider(GlobalVariableUids.AppPackageUid, globalVariablesService, packageManagerService));
            ExposeDirectory(app, "/configurator", new PackageFileProvider(GlobalVariableUids.AppPackageUid, globalVariablesService, packageManagerService));
            ExposeDirectory(app, "/customContent", new PhysicalFileProvider(customContentRootPath));
            ExposeDirectory(app, "/packages", new PhysicalFileProvider(packagesRootPath));

            var webSocketOptions = new WebSocketOptions();
            app.UseWebSockets(webSocketOptions);

            ConfigureMqttWebSocketEndpoint(app, mqttService);
        }

        static void ConfigureMqttWebSocketEndpoint(IApplicationBuilder app, MqttService mqttService)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/mqtt")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        string subProtocol = null;
                        if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                        {
                            subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                        }

                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false))
                        {
                            await mqttService.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    await next().ConfigureAwait(false);
                }
            });
        }

        static void ExposeDirectory(IApplicationBuilder app, string requestPath, IFileProvider fileProvider)
        {
            var fileServerOptions = new FileServerOptions
            {
                RequestPath = requestPath,
                FileProvider = fileProvider,
                EnableDirectoryBrowsing = true,
                EnableDefaultFiles = true
            };

            fileServerOptions.StaticFileOptions.HttpsCompression = HttpsCompressionMode.Compress;
            fileServerOptions.StaticFileOptions.OnPrepareResponse = context =>
            {
                context.Context.Response.Headers["Cache-Control"] = "public, max-age=60";
            };

            app.UseFileServer(fileServerOptions);
        }

        static void ConfigureSwagger(IApplicationBuilder app)
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