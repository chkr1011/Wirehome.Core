# Messages

Wirehome contains a simple message queue which supports publishing and subscribing of messages. A filter can be specified when subscribing. The following table contains all predefined messages of Wirehome. Other services, adapters etc. may add more messages.

| Property | Value | Description |
|-|-|-|
| **Component State Changed** |
| Created for every changed component setting. |
| `type` | `"component_registry.event.component_property_changed"` ||
| `component_uid` | i.e. `"testLamp1"` | The UID of the affected component. |
| `property_uid` | i.e. `"temperature.value"` | The UID of the affected property. |
| `old_value` | i.e. `22.5` | The old value. |
| `new_value` | i.e. `3` | The new value. |
| **Component Setting Changed** |
| Created for every changed component setting. |
| `type` | `"component_registry.event.component_setting_changed"` ||
| `component_uid` | i.e. `"testLamp1"` | The UID of the affected setting. |
| `property_uid` | i.e. `"app.caption"` | The UID of the affected property. |
| `old_value` | i.e. `"Lamp1"` | The old value. |
| `new_value` | i.e. `"Lamp2"` | The new value. |
| **GPIO Interrupt Detected** |
| Created for every detected interrupt for GPIOs. |
| `type` | `"gpio_service.event.gpio_state_changed"` ||
| `gpio_host_id` | i.e. `""` | The ID of the host (empty for built-in host). |
| `gpio_id` | i.e. `21` | The ID of the GPIO. |
| `old_state` | `"high" | "low"` | The old state. |
| `new_state` | `"high" | "low"` | The new state. |
