using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.App
{
    public class AppService : IService
    {
        private readonly ConcurrentDictionary<string, AppPanelDefinition> _panelDefinitions = new ConcurrentDictionary<string, AppPanelDefinition>();

        public void Start()
        {
        }

        public List<AppPanelDefinition> GetRegisteredPanels()
        {
            return _panelDefinitions.Values.ToList();
        }

        public void RegisterPanel(AppPanelDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            _panelDefinitions[definition.Uid] = definition;
        }

        public bool UnregisterPanel(string uid)
        {
            return _panelDefinitions.TryRemove(uid, out _);
        }

        public bool PanelRegistered(string uid)
        {
            return _panelDefinitions.ContainsKey(uid);
        }
    }
}
