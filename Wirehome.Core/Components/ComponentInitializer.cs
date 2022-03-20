using System;
using System.Collections.Generic;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Packages;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components;

public sealed class ComponentInitializer
{
    readonly ComponentRegistryService _componentRegistryService;
    readonly PackageManagerService _packageManagerService;
    readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;
    readonly ILogger<ScriptComponentAdapter> _scriptComponentAdapterLogger;
    readonly ILogger<ScriptComponentLogic> _scriptComponentLogicLogger;

    public ComponentInitializer(ComponentRegistryService componentRegistryService,
        PythonScriptHostFactoryService pythonScriptHostFactoryService,
        PackageManagerService packageManagerService,
        ILogger<ScriptComponentLogic> scriptComponentLogicLogger,
        ILogger<ScriptComponentAdapter> scriptComponentAdapterLogger)
    {
        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
        _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
        _packageManagerService = packageManagerService ?? throw new ArgumentNullException(nameof(packageManagerService));
        _scriptComponentLogicLogger = scriptComponentLogicLogger ?? throw new ArgumentNullException(nameof(scriptComponentLogicLogger));
        _scriptComponentAdapterLogger = scriptComponentAdapterLogger ?? throw new ArgumentNullException(nameof(scriptComponentAdapterLogger));
    }

    public void InitializeComponent(Component component, ComponentConfiguration configuration)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var context = new PythonDictionary
        {
            ["component_uid"] = component.Uid
        };

        if (!string.IsNullOrEmpty(configuration.Script))
        {
            // Having a script in place always overrides the logic and adapter configuration.
            SetupComponentBasedOnScript(component, configuration, context);
            return;
        }

        SetupComponentBasedOnPackages(component, configuration, context);
    }

    void SetupComponentBasedOnPackages(Component component, ComponentConfiguration configuration, IDictionary<object, object> context)
    {
        Package adapterPackage = null;
        IComponentAdapter adapter;
        ScriptComponentAdapter scriptAdapter = null;

        if (configuration.Logic?.Adapter?.Uid != null)
        {
            adapterPackage = _packageManagerService.LoadPackage(configuration.Logic.Adapter.Uid);

            scriptAdapter = new ScriptComponentAdapter(_pythonScriptHostFactoryService, _componentRegistryService, _scriptComponentAdapterLogger);
            scriptAdapter.Compile(component.Uid, adapterPackage.Script);

            if (configuration.Logic.Adapter.Variables != null)
            {
                foreach (var parameter in configuration.Logic.Adapter.Variables)
                {
                    scriptAdapter.SetVariable(parameter.Key, parameter.Value);
                }
            }

            adapter = scriptAdapter;
        }
        else
        {
            adapter = new EmptyComponentAdapter();
        }

        context["adapter_uid"] = adapterPackage?.Uid?.ToString();
        context["adapter_id"] = adapterPackage?.Uid?.Id;
        context["adapter_version"] = adapterPackage?.Uid?.Version;

        if (string.IsNullOrEmpty(configuration.Logic?.Uid?.Id))
        {
            component.SetLogic(new EmptyComponentLogic(adapter));
        }
        else
        {
            var logicPackage = _packageManagerService.LoadPackage(configuration.Logic.Uid);

            var logic = new ScriptComponentLogic(configuration.Logic, _pythonScriptHostFactoryService, _componentRegistryService, _scriptComponentLogicLogger);

            adapter.MessagePublishedCallback = message => logic.ProcessAdapterMessage(message);
            logic.AdapterMessagePublishedCallback = message => adapter.ProcessMessage(message);

            logic.Compile(component.Uid, logicPackage.Script);

            if (configuration.Logic.Variables != null)
            {
                foreach (var parameter in configuration.Logic.Variables)
                {
                    logic.SetVariable(parameter.Key, parameter.Value);
                }
            }

            context["logic_uid"] = logicPackage.Uid.ToString();
            context["logic_id"] = logicPackage.Uid.Id;
            context["logic_version"] = logicPackage.Uid.Version;

            logic.AddToWirehomeWrapper("context", context);

            component.SetLogic(logic);
        }

        scriptAdapter?.AddToWirehomeWrapper("context", context);
    }

    void SetupComponentBasedOnScript(Component component, ComponentConfiguration configuration, IDictionary<object, object> context)
    {
        var logic = new ScriptComponentLogic(configuration.Logic, _pythonScriptHostFactoryService, _componentRegistryService, _scriptComponentLogicLogger);

        logic.Compile(component.Uid, configuration.Script);
        logic.AddToWirehomeWrapper("context", context);

        component.SetLogic(logic);
    }
}