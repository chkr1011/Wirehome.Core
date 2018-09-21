using System;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Components.Adapters;
using Wirehome.Core.Components.Configuration;
using Wirehome.Core.Components.Logic;
using Wirehome.Core.Components.Logic.Implementations;
using Wirehome.Core.Exceptions;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Repositories;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Components
{
    public class ComponentInitializer
    {
        private readonly ComponentRegistryService _componentRegistryService;
        private readonly PythonEngineService _pythonEngineService;
        private readonly StorageService _storageService;
        private readonly RepositoryService _repositoryService;
        private readonly ILoggerFactory _loggerFactory;
        
        public ComponentInitializer(
            ComponentRegistryService componentRegistryService, 
            PythonEngineService pythonEngineService,
            StorageService storageService, 
            RepositoryService repositoryService,
            ILoggerFactory loggerFactory)
        {
            _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
            _pythonEngineService = pythonEngineService ?? throw new ArgumentNullException(nameof(pythonEngineService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void InitializeComponent(Component component, ComponentConfiguration configuration)
        {
            InitializeSettings(component);
            component.Logic = InitializeLogic(component, configuration);
        }

        private void InitializeSettings(Component component)
        {
            if (_storageService.TryRead(out WirehomeDictionary settings, "Components", component.Uid, "Settings.json"))
            {
                foreach (var setting in settings)
                {
                    component.Settings[setting.Key] = setting.Value;
                }
            }
        }

        private IComponentLogic InitializeLogic(Component component, ComponentConfiguration configuration)
        {
            var adapter = InitializeAdapter(component, configuration.Logic.Adapter);

            if (configuration.Logic.Uid == null)
            {
                return new EmptyLogic(adapter);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.Lamp")
            {
                return new LampLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.Button")
            {
                return new ButtonLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.MotionDetector")
            {
                return new MotionDetectorLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.Ventilation")
            {
                return new VentilationLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.RollerShutter")
            {
                return new RollerShutterLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.TemperatureSensor")
            {
                return new SensorLogic(component.Uid, "temperature.value", adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.HumiditySensor")
            {
                return new SensorLogic(component.Uid, "humidity.value", adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.Window")
            {
                return new WindowLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.StateMachine")
            {
                return new StateMachineLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            if (configuration.Logic.Uid?.Id == "Wirehome.Socket")
            {
                return new SocketLogic(component.Uid, adapter, _componentRegistryService, _loggerFactory);
            }

            throw new WirehomeConfigurationException($"Logic '{configuration.Logic.Uid}' not found.");
        }

        private IComponentAdapter InitializeAdapter(Component component, ComponentAdapterConfiguration configuration)
        {
            var repositoryEntity = _repositoryService.LoadEntity(RepositoryType.ComponentAdapters, configuration.Uid);

            var pythonAdapter = new ScriptComponentAdapter(_pythonEngineService, _componentRegistryService, _loggerFactory);
            pythonAdapter.Initialize(component, repositoryEntity.Script);

            if (configuration.Variables != null)
            {
                foreach (var parameter in configuration.Variables)
                {
                    pythonAdapter.SetVariable(parameter.Key, parameter.Value);
                }
            }

            return pythonAdapter;
        }
    }
}
