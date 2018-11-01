using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.HTTP.Controllers
{
    public class PythonScratchpadController : Controller
    {
        private readonly PythonEngineService _pythonEngineService;

        public PythonScratchpadController(PythonEngineService pythonEngineService)
        {
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
        }

        [HttpPost]
        [Route("api/v1/python_scratchpad/execute")]
        [ApiExplorerSettings(GroupName = "v1")]
        public object ExecuteScript(string function_name = "main")
        {
            try
            {
                string script;
                using (var streamReader = new StreamReader(HttpContext.Request.Body))
                {
                    script = streamReader.ReadToEnd();
                }

                var scriptHost = _pythonEngineService.CreateScriptHost();
                scriptHost.Initialize(script);

                if (string.IsNullOrEmpty(function_name))
                {
                    return null;
                }
                
                return scriptHost.InvokeFunction(function_name);
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ConvertToPythonDictionary();
            }
        }
    }
}
