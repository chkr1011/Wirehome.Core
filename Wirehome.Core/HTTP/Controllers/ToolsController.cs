﻿using IronPython.Runtime;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Net;
using Wirehome.Core.Packages;
using Wirehome.Core.Packages.Exceptions;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class ToolsController : Controller
    {
        private readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
        private readonly PackageManagerService _packageManagerService;

        public ToolsController(PythonScriptHostFactoryService pythonScriptHostFactoryService, PackageManagerService packageManagerService)
        {
            _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
            _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
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

            Package package;
            try
            {
                package = _packageManagerService.LoadPackage(PackageUid.Parse(uid));
            }
            catch (WirehomePackageNotFoundException)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            try
            {
                var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost(null);
                scriptHost.Compile(package.Script);
                return scriptHost.InvokeFunction("main", parameters);
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ToDictionary();
            }
        }
    }
}
