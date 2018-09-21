# Commands

The state of all components is updated via commands only. This means that no property is changed directly (like the power state). Furthermore the component receives a command which will perform the requested operation and the assigned logic or adapter of the component will update the properties respectively.

The following table contains all predefined commands in Wirehome. Other adapters, services etc. may also implement own commands.

| Property | Value | Description |
|-|-|-|-|
| **Turn on** || Turns a component on. |
| `type` | `"turn_on"` ||
| **Turn off** || Turns a component off. |
| `type` | `"turn_off"` ||
| **Increase level** || Increases the level of a component (e.g. ventilation). |
| `type` | `"increase_level"` ||
| **Decrease level** || Decreases the level of a component (e.g. ventilation). |
| `type` | `"decrease_level"` ||
| **Move up** || Move a component up (e.g. roller shutters). |
| `type` | `"move_up"`||
| **Move up** || Move a component down (e.g. roller shutters). |
| `type` | `"move_down"`||
| **Open** || Opens a component (e.g. valves). |
| `type` | `"open"`||
| **Close** || Closes a component (e.g. valves). |
| `type` | `"close"`||
| **Set brightness** || Sets the brightness of a component (e.g. lamps). |
| `type` | `"set_brightness"` ||
| `brightness` | `0 - 100` | The brightness in %. |
| **Set state** || Sets the state of a component (e.g. state machines). |
| `type` | `"set_state"` ||
| `state` | `"xyz"` | The ID of the state. |
| **Set next state** || Sets the next of a component (e.g. state machines). |
| `type` | `"set_next_state"` ||
| **Set previous state** || Sets the previous of a component (e.g. state machines). |
| `type` | `"set_previous_state"` ||
| **Set color** || Sets the brightness of a component (e.g. lamps). |
| `type` | `"set_color"`||
| `color` | `"0xFFFFFF"`| The hex representation of the value. |
| `color_format` | `"rgb" | "rgbw" | "hsv"` | The format of the value. |
| **Set level** || Sets the level of a component (e.g. ventilation). |
| `type` | `"set_level"` ||
| `level` | `0 - n` | Defines the index of the level where 0 means off. |
| **Refresh** || Forces a sensor to refresh its value (e.g. temperature sensors). |
| `type` | `"refresh"` ||