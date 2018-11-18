using System;
using System.Collections;
using System.Net;
using IronPython.Runtime;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Repository;
using Wirehome.Core.Repository.Exceptions;

namespace Wirehome.Core.HTTP.Controllers
{
    public class ToolsController : Controller
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly RepositoryService _repositoryService;

        public ToolsController(PythonScriptHostFactoryService pythonScriptHostFactoryService, RepositoryService repositoryService)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
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

            RepositoryEntity repositoryEntity;
            try
            {
                repositoryEntity = _repositoryService.LoadEntity(RepositoryEntityUid.Parse(uid));
            }
            catch (WirehomeRepositoryEntityNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            try
            {
                var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(null);
                scriptHost.Initialize(repositoryEntity.Script);
                return scriptHost.InvokeFunction("main", parameters);
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ConvertToPythonDictionary();
            }
        }
    }
}
