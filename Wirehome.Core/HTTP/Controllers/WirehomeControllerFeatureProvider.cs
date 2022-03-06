using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Wirehome.Core.HTTP.Controllers;

public sealed class WirehomeControllerFeatureProvider : ControllerFeatureProvider
{
    readonly string _controllerNamespace;

    public WirehomeControllerFeatureProvider(string controllerNamespace)
    {
        _controllerNamespace = controllerNamespace ?? throw new ArgumentNullException(nameof(controllerNamespace));
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        var isController = base.IsController(typeInfo);
        if (isController)
        {
            if (!typeInfo.Namespace.StartsWith(_controllerNamespace, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return isController;
    }
}