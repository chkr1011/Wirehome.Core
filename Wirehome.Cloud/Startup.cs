using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Wirehome.Cloud.Controllers;
using Wirehome.Cloud.Services;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.HTTP.Controllers;

namespace Wirehome.Cloud
{
    public class Startup
    {
        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ConnectorService>();
            services.AddSingleton<AuthorizationService>();
            services.AddSingleton<RepositoryService>();

            services.AddMvc().ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Remove(manager.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                manager.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(FunctionsController).Namespace));
            });

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

        // ReSharper disable once UnusedMember.Global
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConfigureMvc(app);
            ConfigureSwagger(app);
            ConfigureConnector(app);

            app.Run(async (context) =>
            {
                // TODO: Map requets to Channel for external WebApp access.
                await context.Response.WriteAsync("Hello World!");
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

        private static void ConfigureConnector(IApplicationBuilder app)
        {
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            });

            var connectorService = app.ApplicationServices.GetRequiredService<ConnectorService>();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path != "/Connector")
                {
                    await next();
                    return;
                }

                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                try
                {
                    await connectorService.ConnectAsync(webSocket, context.RequestAborted);
                }
                catch (UnauthorizedAccessException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
            });
        }
    }
}
