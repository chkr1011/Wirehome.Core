using System;
using System.Collections.Generic;
using System.Linq;
using Wirehome.Core.Contracts;
using Wirehome.Core.Resources;

namespace Wirehome.Core.App
{
    public class AppService : IService
    {
        readonly Dictionary<string, AppPanelDefinition> _panelDefinitions = new Dictionary<string, AppPanelDefinition>();
        readonly Dictionary<string, Func<object>> _statusProviders = new Dictionary<string, Func<object>>();

        readonly ResourceService _resourceService;

        public AppService(ResourceService resourceService)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        }

        public void Start()
        {
        }

        public List<AppPanelDefinition> GetRegisteredPanels()
        {
            lock (_panelDefinitions)
            {
                return _panelDefinitions.Values.ToList();
            }
        }

        public void RegisterPanel(AppPanelDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            lock (_panelDefinitions)
            {
                _panelDefinitions[definition.Uid] = definition;
            }
        }

        public bool UnregisterPanel(string uid)
        {
            if (uid is null) throw new ArgumentNullException(nameof(uid));

            lock (_panelDefinitions)
            {
                return _panelDefinitions.Remove(uid);
            }
        }

        public bool PanelRegistered(string uid)
        {
            if (uid is null) throw new ArgumentNullException(nameof(uid));

            lock (_panelDefinitions)
            {
                return _panelDefinitions.ContainsKey(uid);
            }
        }

        public void RegisterStatusProvider(string uid, Func<object> provider)
        {
            if (uid is null) throw new ArgumentNullException(nameof(uid));
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            lock (_statusProviders)
            {
                _statusProviders[uid] = provider;
            }
        }

        public void UnregisterStatusProvider(string uid)
        {
            if (uid is null) throw new ArgumentNullException(nameof(uid));

            lock (_statusProviders)
            {
                _statusProviders.Remove(uid);
            }
        }

        public Dictionary<string, object> GenerateStatusContainer()
        {
            var statusContainer = new Dictionary<string, object>
            {
                ["panels"] = GetRegisteredPanels(),
                ["resources"] = _resourceService.GetResources("")
            };

            lock (_statusProviders)
            {
                foreach (var statusProvider in _statusProviders)
                {
                    statusContainer[statusProvider.Key] = statusProvider.Value();
                }
            }

            return statusContainer;
        }
    }
}
