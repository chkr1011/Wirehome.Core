namespace Wirehome.Core.Constants;

public static class WirehomeMessageType
{
    public const string ParameterMissingException = "exception.parameter_missing";

    public const string ParameterInvalidException = "exception.parameter_invalid";

    public const string NotSupportedException = "exception.not_supported";

    public const string NotImplementedException = "exception.not_implemented";

    public const string ReturnValueTypeMismatchException = "exception.return_value_type_mismatch";

    public const string ParameterValueTypeMismatchException = "exception.parameter_value_type_mismatch";

    public const string Exception = "exception";

    public const string Success = "success";

    public const string Initialize = "initialize";

    public const string Destroy = "destroy";

    public const string SettingChanged = "setting_changed";

    public const string Enable = "enable";

    public const string Disable = "disable";
}