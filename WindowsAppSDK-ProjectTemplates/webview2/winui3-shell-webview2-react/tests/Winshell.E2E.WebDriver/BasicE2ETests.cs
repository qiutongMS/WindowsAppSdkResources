using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace Winshell.E2E.WebDriver;

[TestClass]
public class BasicE2ETests
{
    private const int DebuggingPort = 9222;
    private static Process? _app;
    private static EdgeDriver? _driver;

    [ClassInitialize]
    public static void StartAppAndDriver(TestContext _)
    {
        var repoRoot = GetRepoRoot();
        var exePath = ResolveExePath(repoRoot);
        Assert.IsTrue(File.Exists(exePath), $"Winshell.exe not found at {exePath}. Build the matching configuration first.");

        _app = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath),
            UseShellExecute = false,
            Environment =
            {
                ["WEBVIEW2_REMOTE_DEBUGGING_PORT"] = DebuggingPort.ToString(),
                ["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={DebuggingPort}"
            }
        });
        Assert.IsNotNull(_app, "Failed to start app");

        WaitForPort(DebuggingPort, TimeSpan.FromSeconds(40));

        var options = new EdgeOptions
        {
            DebuggerAddress = $"localhost:{DebuggingPort}"
        };
        options.AddAdditionalOption("webview2", true);

        var service = CreateDriverService();
        service.HideCommandPromptWindow = true;

        _driver = CreateDriverWithRetry(service, options);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _driver?.Quit();
        if (_app is { HasExited: false })
        {
            _app.Kill(true);
            _app.Dispose();
        }
    }

    [TestMethod]
    public void Basic_BackgroundTab_Navigates()
    {
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        wait.Until(d => d.FindElement(By.XPath("//*[contains(text(),'Winshell4')]")));

        var tab = wait.Until(d => d.FindElement(By.XPath("//button[normalize-space()='Background Removal']")));
        tab.Click();

        wait.Until(d => d.FindElement(By.XPath("//h2[normalize-space()='Background Removal']")));
    }

    [TestMethod]
    public void Basic_AiEcho_RoundTrip_Works()
    {
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        // Ensure page title is loaded
        wait.Until(d => d.FindElement(By.XPath("//*[contains(text(),'Winshell4')]")));

        // Switch to Basics / Home
        var homeTab = wait.Until(d => d.FindElement(By.XPath("//button[normalize-space()='Basics']")));
        var classAttr = homeTab.GetDomAttribute("class") ?? string.Empty;
        if (!classAttr.Contains("active", StringComparison.OrdinalIgnoreCase))
        {
            homeTab.Click();
        }

        // Find the input and type
        var input = wait.Until(d => d.FindElement(By.CssSelector("input.text")));
        const string text = "hello-e2e";
        input.Clear();
        input.SendKeys(text);

        // Click the echo button (assumes it contains ai.echo text)
        var button = wait.Until(d => d.FindElement(By.XPath("//button[contains(translate(., 'ECHO', 'echo'),'ai.echo')]")));
        button.Click();

        // Wait until output contains the echo text (initially null, so wait for change)
        wait.Until(d =>
        {
            var node = d.FindElement(By.CssSelector("pre.output"));
            return (node.Text ?? string.Empty).ToLowerInvariant().Contains($"echo: {text}".ToLowerInvariant());
        });

        var resultNode = _driver!.FindElement(By.CssSelector("pre.output"));
        StringAssert.Contains(resultNode.Text.ToLowerInvariant(), $"echo: {text}".ToLowerInvariant());
    }

    private static EdgeDriver CreateDriverWithRetry(EdgeDriverService service, EdgeOptions options)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return new EdgeDriver(service, options);
            }
            catch (Exception ex) when (attempt < 2)
            {
                last = ex;
                Thread.Sleep(1000);
            }
        }

        throw last ?? new InvalidOperationException("EdgeDriver creation failed without exception");
    }

    private static string GetRepoRoot()
    {
        // Walk up until we find the repo marker (winshell.slnx) to avoid off-by-one path issues.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var slnx = Path.Combine(current.FullName, "winshell.slnx");
            if (File.Exists(slnx))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (winshell.slnx not found).");
    }

    private static string ResolveExePath(string repoRoot)
    {
        var platformEnv = Environment.GetEnvironmentVariable("TEST_PLATFORM");
        var preferredPlatform = string.IsNullOrWhiteSpace(platformEnv) ? null : platformEnv.Trim().ToUpperInvariant();

        string? Candidate(string platform) => Path.Combine(repoRoot, platform, "Debug", "Winshell.exe");

        if (preferredPlatform is null)
        {
            // Prefer native arch first to avoid picking an incompatible build on ARM machines.
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            var order = arch is System.Runtime.InteropServices.Architecture.Arm64
                ? new[] { "ARM64", "x64" }
                : new[] { "x64", "ARM64" };

            foreach (var candidate in order)
            {
                var path = Candidate(candidate);
                if (File.Exists(path)) return path;
            }
        }

        if (preferredPlatform is not null)
        {
            var path = Candidate(preferredPlatform);
            if (File.Exists(path)) return path;
            throw new FileNotFoundException($"Winshell.exe not found for platform {preferredPlatform} at {path}. Build it first or adjust TEST_PLATFORM.");
        }

        throw new FileNotFoundException("Winshell.exe not found. Build Debug for x64 or ARM64, or set TEST_PLATFORM.");
    }

    private static EdgeDriverService CreateDriverService()
    {
        var driverPath = Environment.GetEnvironmentVariable("MSEDGEDRIVER_PATH")?.Trim();
        if (!string.IsNullOrWhiteSpace(driverPath))
        {
            var dir = File.Exists(driverPath) ? Path.GetDirectoryName(driverPath)! : driverPath;
            return EdgeDriverService.CreateDefaultService(dir);
        }

        var dirEnv = Environment.GetEnvironmentVariable("MSEDGEDRIVER_DIR")?.Trim();
        if (!string.IsNullOrWhiteSpace(dirEnv) && Directory.Exists(dirEnv))
        {
            return EdgeDriverService.CreateDefaultService(dirEnv);
        }

        return EdgeDriverService.CreateDefaultService();
    }

    private static void WaitForPort(int port, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync("127.0.0.1", port);
                task.Wait(TimeSpan.FromMilliseconds(200));
                if (task.IsCompletedSuccessfully)
                    return;
            }
            catch
            {
                // ignore
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException($"Port {port} did not open within {timeout.TotalSeconds} seconds");
    }
}
