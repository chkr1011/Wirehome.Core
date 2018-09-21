using System;
using System.Collections;
using System.IO;
using IronPython.Runtime;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Python;
using Wirehome.Core.Repositories;

namespace Wirehome.Core.HTTP.Controllers
{
    public class ToolsController : Controller
    {
        private readonly PythonEngineService _pythonEngineService;
        private readonly RepositoryService _repositoryService;

        public ToolsController(PythonEngineService pythonEngineService, RepositoryService repositoryService)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        [HttpPost]
        [Route("api/v1/tools/{uid}/execute")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object ExecuteTool(string uid, [FromBody] IDictionary parameters = null)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            if (parameters == null)
            {
                parameters = new PythonDictionary();
            }

            var repositoryEntity = _repositoryService.LoadEntity(RepositoryType.Tools, RepositoryEntityUid.Parse(uid));

            var scriptHost = _pythonEngineService.CreateScriptHost();
            scriptHost.Initialize(repositoryEntity.Script);
            return scriptHost.InvokeFunction("main", parameters);
        }

        [HttpPost]
        [Route("api/v1/tools/execute")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object ExecuteScript(string function_name = "main")
        {
            string script;
            using (var streamReader = new StreamReader(HttpContext.Request.Body))
            {
                script = streamReader.ReadToEnd();
            }

            var scriptHost = _pythonEngineService.CreateScriptHost();
            scriptHost.Initialize(script);

            object result = null;
            if (!string.IsNullOrEmpty(function_name))
            {
                result = scriptHost.InvokeFunction(function_name);
            }

            return result;
        }
    }
}
