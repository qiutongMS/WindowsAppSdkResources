namespace Winshell;

/// <summary>
/// Stable error codes used by the bridge protocol.
/// </summary>
public static class BridgeErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string VersionNotSupported = "version_not_supported";
    public const string MethodNotFound = "method_not_found";
    public const string Exception = "exception";
}