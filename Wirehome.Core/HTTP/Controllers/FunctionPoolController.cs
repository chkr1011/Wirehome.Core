using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Wirehome.Core.FunctionPool;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class FunctionPoolController : Controller
    {
        private readonly FunctionPoolService _functionPoolService;

        public FunctionPoolController(FunctionPoolService functionPoolService)
        {
            _functionPoolService = functionPoolService ?? throw new ArgumentNullException(nameof(functionPoolService));
        }

        [HttpGet]
        [Route("/api/v1/function_pool/functions")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetFunctions()
        {
            return _functionPoolService.GetRegisteredFunctions();
        }

        [HttpPost]
        [Route("/api/v1/function_pool/functions/{uid}/invoke")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<object, object> PostInvokeFunction(string uid, [FromBody] IDictionary<object, object> parameters)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            return _functionPoolService.InvokeFunction(uid, parameters);
        }
    }
}
