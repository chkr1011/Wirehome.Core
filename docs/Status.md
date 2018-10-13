# Status
All status relevant values of a component are combined into _modules_. Such a module is identified by the prefix of a status name (ID). Both are called the status _UID_.

The following tables shows all predefined status UIDs in Wirehome. Other services, logics, adapters etc. may add additional UIDs.

## General
| Status UID | Value | Description |
|-|-|-|
| `is_outdated` | `true | false` | Defines whether the component is outdated and may be wrong. |

## Actuators
| Status UID | Value | Description |
|-|-|-|
| `brightness.value` | `0 - 100` | The brightness in %. |
| `color.red` | `0 - 100` | The red color in %. |
| `color.green` | `0 - 100` | The green color in %. |
| `color.blue` | `0 - 100` | The blue color in %. |
| `display.text` | e.g. `"Hello World"` | The text which is shown at the display. |
| `power.state` | `"on" | "off"` | The overall power status. |
| `power.consumption` | e.g `0.0` | The actual power consumption in Wh. |
| `roller_shutter.state` | `"moving_up" | "off" | "moving_down"` | The status of a roller shutter. |
| `roller_shutter.position` | `0 - 100` | The position of a roller shutter in %. |
| `roller_shutter.is_closed` | `true | false` | Indicates whether the roller shutter is closed completely. |
| `state_machine.state` | e.g. `"couch_only"` | The current state of the state machine. |
| `valve.state` | `"open" | "closed"` | The current state of a valve. |

## Sensors
| Status UID | Value | Description |
|-|-|-|
| `air_pressure.value` | e.g. `0.0` | The air pressure in ?. |
| `button.state` | `"pressed" | "released"` | The pressed state. |
| `gas_meter.value` | e.g. `0.0` | The current value of a gas meter. |
| `humidity.value` | e.g. `0.0` | The temperature in %. |
| `motion_detection.status` | `"idle" | "detected"` | The status of motion detection. |
| `switch.state` | `true | false` | The boolean state of the switch. |
| `temperature.value` | e.g. `0.0` | The temperature in °C. |
| `window.state` | `"open" | "closed" | "tilt"` | The status of a window. |

Based on the available status UIDs the app (Wirehome.App) will try to find the best matching view for each component. So using this predefined UIDs is recommended.