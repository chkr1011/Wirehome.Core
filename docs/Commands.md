# Commands

The state of all components is updated via commands only. This means that no property is changed directly (like the power state). Furthermore the component receives a command which will perform the requested operation and the assigned logic or adapter of the component will update the properties respectively.

The following table contains all predefined commands in Wirehome. Other adapters, services etc. may also implement own commands.

| Property | Value | Description |
|-|-|-|
| **Turn on** | | Turns a component on. |
| `type` | `"turn_on"` | |
| **Turn off** | | Turns a component off. |
| `type` | `"turn_off"` | |
| **Increase level** | | Increases the level of a component (e.g. ventilation). |
| `type` | `"increase_level"` | |
| **Decrease level** | | Decreases the level of a component (e.g. ventilation). |
| `type` | `"decrease_level"` | |
| **Move up** | | Move a component up (e.g. roller shutters). |
| `type` | `"move_up"` | |
| **Move up** | | Move a component down (e.g. roller shutters). |
| `type` | `"move_down"` | |
| **Open** | | Opens a component (e.g. valves). |
| `type` | `"open"` | |
| **Close** | | Closes a component (e.g. valves). |
| `type` | `"close"` | |
| **Set brightness** | | Sets the brightness of a component (e.g. lamps). |
| `type` | `"set_brightness"` | |
| `brightness` | `0 - 100` | The brightness in %. |
| **Increase brightness** | | Increases the brightness of a component (e.g. lamps). |
| `type` | `"increase_brightness"` | |
| `value` | `0 - 100` | The value for increasing the brightness. |
| **Decrease brightness** | | Decreases the brightness of a component (e.g. lamps). |
| `type` | `"decrease_brightness"` | |
| `value` | `0 - 100` | The value for decreasing the brightness. |
| **Set state** | | Sets the state of a component (e.g. state machines). |
| `type` | `"set_state"` | |
| `state` | `"xyz"` | The ID of the state. |
| **Set next state** | | Sets the next of a component (e.g. state machines). |
| `type` | `"set_next_state"` | |
| **Set previous state** | | Sets the previous of a component (e.g. state machines). |
| `type` | `"set_previous_state"` | |
| **Set color** | | Sets the brightness of a component (e.g. lamps). |
| `type` | `"set_color"` | |
| `format` | `"rgb" | "rgbw" | "hsv"` | The format of color value. |
| `r` | `0 - 100` | The value for red in % (only when format is "rgb"). |
| `g` | `0 - 100` | The value for green in % (only when format is "rgb"). |
| `b` | `0 - 100` | The value for blue in % (only when format is "rgb"). |
| **Set level** | | Sets the level of a component (e.g. ventilation). |
| `type` | `"set_level"` | |
| `level` | `0 - n` | Defines the index of the level where 0 means off. |
| **Set position** | | Sets the position of a component (e.g. roller shutter). |
| `type` | `"set_position"` | |
| `position` | `0 - n` | Defines the absolute position. |
| **Refresh** | | Forces a sensor to refresh its value (e.g. temperature sensors). |
| `type` | `"refresh"` | |