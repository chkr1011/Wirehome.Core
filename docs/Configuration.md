# Configuration
The whole configuration of the Wirehome system is stored in JSON files. 

## Component configuration
Sometimes it is required that a adapter or logic exposes some additional parameters to the app, automations or other components. 

Example:
The roller shutter is a state machine with some predefined states (`off`, `moving_up`, `moving_down`). Due to this fact the app and other components can rely on these states. But other state machines which are defined by the user or a custom adapter may have several states which are not known by other components. 

To allow the app to show all available states a component can contain several configuration values. In comparison to _settings_ and _properties_ of a component the _configuration_ cannot be changed at runtime and will not be stored.

The following table shows all predefined configuration entries in Wirehome. Other adapters may add more configuration entries.

| Property UID | Value | Description |
|-|-|-|
| **State machine** |
| `state_machine.states` | e.g. `["couch_only", "off", "desk_only", "all_on"]` | All supported states for the state machine. |
| `state_machine.states.alias.on` | e.g. `"all_on"` | Defines a state which is close to the generic `on` state. The defined state is applied if the component should be turned on and no dedicated `on` state is available. |
| `state_machine.states.alias.off` | e.g. `"desk_only"` | Defines a state which is close to the generic `off` state. The defined state is applied if the component should be turned off and no dedicated `off` state is available. |
