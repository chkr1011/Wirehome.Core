# Status

All status relevant properties properties of a component are combined into _modules_. Such a module is identified by the prefix of a status property name (ID). Both are called the status property _UID_.

The following tables shows all predefined property UIDs in Wirehome. Other services or adapters etc. may add their own UIDs.

Based on the available status UIDs the app (Wirehome.App) will try to find the best matching view for each component. So using this predefined properties is recommended.

## Actuators

| Property UID | Value | Description |
|-|-|-|
| **Light** |
| `brightness.value` | `0 - 100` | The brightness in %. |
| `color.red` | `0 - 100` | The red color in %. |
| `color.green` | `0 - 100` | The green color in %. |
| `color.blue` | `0 - 100` | The blue color in %. |
| **Power** |
| `power.state` | `"on" | "off"` | The overall power status. |
| `power.consumption` | e.g `0.0` | The actual power consumption in W/h. |
| **Roller shutter** |
| `roller_shutter.state` | `"moving_up" | "off" | "moving_down"` | The status of a roller shutter. |
| `roller_shutter.position` | `0 - 100` | The position of a roller shutter in %. |
| **State machine** |
| `state_machine.state` | e.g. `"couch_only"` | The current state of the state machine. |
| **Valve** |
| `valve.state` | `"open" | "closed"` | The current state of a valve. |

## Sensors

| Property UID | Value | Description |
|-|-|-|
| **Air pressure** |
| `air_pressure.value` | e.g. `0.0` | The air pressure in ?. |
| **Button** |
| `button.state` | `"pressed" | "released"` | The pressed state. |
| **Gas meter** |
| `gas_meter.value` | e.g. `0.0` | The current value of a gas meter. |
| **Humidity** |
| `humidity.value` | e.g. `0.0` | The temperature in %. |
| **Motion detector** |
| `motion_detection.status` | `"idle" | "detected"` | The status of motion detection. |
| **Temperature** |
| `temperature.value` | e.g. `0.0` | The temperature in °C. |
| **Window** |
| `window.state` | `"open" | "closed" | "tilt"` | The status of a window. |