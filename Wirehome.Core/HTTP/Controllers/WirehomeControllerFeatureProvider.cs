using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Reflection;

namespace Wirehome.Core.HTTP.Controllers
{
    public class WirehomeControllerFeatureProvider : ControllerFeatureProvider
    {
        private readonly string _controllerNamespace;

        public WirehomeControllerFeatureProvider(string controllerNamespace)
        {
            _controllerNamespace = controllerNamespace ?? throw new ArgumentNullException(nameof(controllerNamespace));
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            var isController = base.IsController(typeInfo);
            if (isController)
            {
                if (!typeInfo.Namespace.StartsWith(_controllerNamespace))
                {
                    return false;
                }
            }

            return isController;
        }
    }
}
