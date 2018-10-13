# Settings
Settings are values which are changing more frequently than the configuration. They also sometimes represent a state which can be set by the user itself (i.e. `is_enabled`).

The following table shows all predefined settings in Wirehome. Other services, logics, adapters etc. may add their own settings.

| Setting UID | Value | Description |
|-|-|-|
| **General** |
| `is_enabled` | `true | false` | Defines whether the component is currently active or disabled. |
| **App** |
| `app.caption` | e.g. `"My Lamp 1"` | Defines the text which is shown in the App. |
| `app.image_color` | e.g. `"#334455"` | Defines the color of the image. |
| `app.image_id` | e.g. `"fas fa-bath"` | Defines the ID of the icon (from Font Awesome). |
| `app.is_visible` | `true | false` | Indicates whether the component is shown in the App. |
| `app.position_index` | e.g. `0` | Defines the index of the position within a component group in the App. |
