namespace Wirehome.Core.Cloud.Protocol;

public static class CloudMessageType
{
    public const string Ping = "ping";

    public const string HttpInvoke = "http_invoke";

    public const string Raw = "raw";

    public const string Error = "error";
}