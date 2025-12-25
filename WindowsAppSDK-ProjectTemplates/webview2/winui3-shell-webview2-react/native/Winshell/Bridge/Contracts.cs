namespace Winshell.Bridge;

public record AppInfoResult(string Name, string Version, bool Packaged);

public record ClipboardTextResult(string Text);

public record OperationOkResult(bool Ok);

public record AiEchoResult(string Text);

public record RemoveBackgroundResult(string? MaskBase64);
