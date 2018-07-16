# Modules

All properties of a component are combined into _modules_. Such a module is identified by the pefix of a property. Both are called the property _UID_.

The following tables shows all predefined property UIDs in Wirehome. Other services or adapters etc. may add their own property UIDs.

Based on the available property UIDs the app (Wirehome.App) will try to find the best matching view for each component. So using this predefined properties is recommended.

| Property UID | Value | Description |
|-|-|-|
| **Power** |
| `power.state` | `"on" | "off"` | Defines the overall power status. |
| `power.consumption` | e.g `0.0` | Defines the actual power consumption in W/h. |
| **Light** |
| `light.brightness` | `0 - 100` | Defines the brightness in %. |
| `light.color` | `#RRGGBB | #RRGGBBWW` | Defines the color in HEX RGB format with optional white. |
| **Button** |
| `button.state` | `"pressed" | "released"` | Defines the pressed state. |
| **Temperature** |
| `temperature.value` | e.g. `0.0` | Defines the temperature in °C. |
| **Humidity** |
| `humidity.value` | e.g. `0.0` | Defines the temperature in %. |
| **Air pressure** |
| `air_pressure.value` | e.g. `0.0` | Defines the air pressure in ?. |
| **Motion** |
| `motion_detection.status` | `"idle" | "detected"` | Defines the status of motion detection. |
| **Window** |
| `window.state` | `"open" | "closed" | "tilt"` | Defines the status of a window. |
| **Roller shutter** |
| `roller_shutter.state` | `"moving_up" | "off" | "moving_down"` | Defines the status of a roller shutter. |
| `roller_shutter.position` | `0 - 100` | Defines the percentual position of a roller shutter. |
| **Gas meter** |
| `gas_meter.value` | e.g. `0.0` | Defines the current value of a gas meter. |
| **Valve**
| `valve.state` | `"open" | "closed"` | Defines the current state of a valve. |