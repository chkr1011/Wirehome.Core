# Control types

All messages in Wirehome like commands, adapter messages etc. have a _type_ property. This property identifies the type of the message, event or an exception. Additional properties are available depending on the type of the message. The following table shows the predefined types in Wirehome. Other services, adapters etc. may add their own types.

If the _type_ property is missing the message is interpreted as _null_. All other available properties will be discarded.

## General

| Type | Description |
|-|-|
| `success` | An operation has succeeded. Can contain more properties. |

## Exceptions

The following table shows all types of exceptions which are defined by Wirehome. More maybe added by other services etc.

| Type | Description |
|-|-|
| `exception.not_supported` | The operation is in general not supported. The original type of the message by be added to the dictionary (`origin_type`). |
| `exception.not_implemented` | An operation is available in general but not implemented at the moment. |
| `exception.parameter_missing` | An operation could not be performed because a required parameter is missing. The name of the parameter should be added to the dictionary (`parameter_name`). |
| `exception.parameter_invalid` | An operation could not be performed because a parameter was present but it's value was invalid. The name of the parameter should be added to the dictionary (`parameter_name`). The value may be added to the properties (`parameter_value`). A reason describing the issue may be added (`message`). |
| `exception.invalid_operation` | The requested operation is not performed because it is not allowed by design. |
| `exception.timeout` | The requested operation timed out. |

All exception can contain several more parameters with exception details. The following tables shows these parameters.

| Parameter | Description |
|-|-|
| `exception_type` | The type of the exception. |
| `message` | The original message of the exception. |
| `stack_trace` | The stack trace of the exception. |