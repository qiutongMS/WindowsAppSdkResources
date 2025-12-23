using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

namespace Winshell;

public sealed partial class MainWindow : Window
{
    private readonly BridgeRouter _router = new();

    public MainWindow()
    {
        InitializeComponent();
        _ = EnsureWebViewAsync();
    }

    private bool _webViewReady;

    private async Task EnsureWebViewAsync()
    {
        if (_webViewReady)
            return;

        _webViewReady = true;

        await ShellWebView.EnsureCoreWebView2Async();

        ShellWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        ShellWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

        var webRoot = Path.Combine(AppContext.BaseDirectory, "Web");
        ShellWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            hostName: "app",
            folderPath: webRoot,
            accessKind: CoreWebView2HostResourceAccessKind.Allow);

        ShellWebView.CoreWebView2.WebMessageReceived += async (_, e) =>
        {
            var json = e.WebMessageAsJson;
            var response = await _router.HandleAsync(json);
            ShellWebView.CoreWebView2.PostWebMessageAsJson(response);
        };

        // Navigate: prefer explicit env (for tooling), else packaged content via virtual host
        var envUrl = Environment.GetEnvironmentVariable("WINSHELL_DEV_URL");

        var target = !string.IsNullOrWhiteSpace(envUrl)
            ? envUrl!
            : "https://app/index.html";

        ShellWebView.CoreWebView2.Navigate(target);
    }
}
