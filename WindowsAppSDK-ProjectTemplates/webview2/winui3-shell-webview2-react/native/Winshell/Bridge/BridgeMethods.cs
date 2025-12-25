namespace Winshell;

/// <summary>
/// Stable method names for the bridge to avoid scattered string literals.
/// </summary>
public static class BridgeMethods
{
    public const string AppGetInfo = "app.getInfo";
    public const string ClipboardGetText = "clipboard.getText";
    public const string ClipboardSetText = "clipboard.setText";
    public const string AiEcho = "ai.echo";
    public const string AiRemoveBackground = "ai.removeBackground";
    public const string AppLog = "app.log";
}