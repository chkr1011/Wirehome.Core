# Control types

All messages in Wirehome like commands, adapter messages etc. have a _type_ property. This property identifies the type of the message or an exception. Additional properties are available depending on the type of the message. The following table shows the predefined types in Wirehome. Other services, adapters etc. may add their own types.

If the _type_ property is missing the message is interpreted as _null_ and thus not existent. All other available properties will be discarded.

| Type | Description |
|-|-|
| `success` | An operation has succeeded. Can contain more properties. |
| `exception.not_supported` | The operation is in general not supported |
| `exception.not_implemented` | An operation is available in general but not implemented at the moment. |
| `exception.parameter_missing` | An operation could not be performed because a required parameter is missing. The name of the parameter should be added to the dictionary. |
| `exception.parameter_invalid` | An operation could not be performed because a parameter was present but it's value was invalid. |
| `exception.invalid_operation` | The requested operation is not performed because it is now allowed by design. |