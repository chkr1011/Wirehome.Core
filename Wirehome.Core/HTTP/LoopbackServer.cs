using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.HTTP
{
    public class LoopbackServer : IServer
    {
        Func<Task> _bla;


        public IFeatureCollection Features { get; } =  new FeatureCollection();

        public void Dispose()
        {
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());

            //_bla = async () =>
            //{
            //    var x = application.CreateContext(new FeatureCollection());
            //    Exception exception = null;
            //    try
            //    {
            //        var context = (HostingApplication.Context)(object)application.CreateContext(Features);
            //        context.HttpContext = new X();
            //        await application.ProcessRequestAsync((TContext)(object)context);
            //        context.HttpContext.Response.OnCompleted(null, null);

            //        await application.ProcessRequestAsync(x).ConfigureAwait(false);
            //    }
            //    catch (Exception ex)
            //    {
            //        exception = ex;
            //    }
            //    finally
            //    {
            //        application.DisposeContext(x, exception);
            //    }
            //};

            return Task.CompletedTask;
        }

        public class X : HttpContext
        {
            public override ConnectionInfo Connection => throw new NotImplementedException();

            public override IFeatureCollection Features => throw new NotImplementedException();

            public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override HttpRequest Request => throw new NotImplementedException();

            public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override HttpResponse Response => throw new NotImplementedException();

            public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override WebSocketManager WebSockets => throw new NotImplementedException();

            public override void Abort()
            {
                
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new global::System.NotImplementedException();
        }
    }
}
