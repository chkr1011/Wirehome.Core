using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Wirehome.Cloud.Controllers;
using Wirehome.Cloud.Filters;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.Connector;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Cloud.Services.StaticFiles;
using Wirehome.Core.HTTP.Controllers;

namespace Wirehome.Cloud
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Startup
    {
        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ConnectorService>();
            services.AddSingleton<AuthorizationService>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<StaticFilesService>();

            services.AddMvc(config =>
            {
                config.Filters.Add(new DefaultExceptionFilter());
            })
            .ConfigureApplicationPartManager(config =>
            {
                config.FeatureProviders.Remove(config.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                config.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(FunctionsController).Namespace));
            });

            ConfigureSwaggerServices(services);
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            AuthorizationService authorizationService,
            ConnectorService connectorService,
            StaticFilesService staticFilesService)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (authorizationService == null) throw new ArgumentNullException(nameof(authorizationService));
            if (connectorService == null) throw new ArgumentNullException(nameof(connectorService));
            if (staticFilesService == null) throw new ArgumentNullException(nameof(staticFilesService));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConfigureMvc(app);
            ConfigureSwagger(app);
            ConfigureConnector(app, connectorService, authorizationService);

            app.Run(async context =>
            {
                if (await staticFilesService.HandleRequestAsync(context))
                {
                    return;
                }

                await connectorService.ForwardHttpRequestAsync(context);
            });
        }

        private static void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("/api/v1/swagger.json", "Wirehome.Cloud API v1");
            });
        }

        private static void ConfigureMvc(IApplicationBuilder app)
        {
            app.UseMvc(config =>
            {
                config.MapRoute("default", "api/{controller}/{action}/{id?}", null, null, null);
            });
        }

        private static void ConfigureConnector(IApplicationBuilder app, ConnectorService connectorService, AuthorizationService authorizationService)
        {
            app.Map("/Connectors", config =>
            {
                config.UseWebSockets(new WebSocketOptions
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(30),
                    ReceiveBufferSize = 1024
                });

                config.Use(async (context, next) =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    try
                    {
                        var authorizationContext = authorizationService.AuthorizeConnector(context);

                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
                        {
                            await connectorService.RunAsync(webSocket, authorizationContext, context.RequestAborted).ConfigureAwait(false);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                });
            });
        }

        private static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Wirehome.Cloud API",
                    Version = "v1",
                    Description = "This is the public API for the Wirehome.Cloud service.",
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
        }
    }
}
