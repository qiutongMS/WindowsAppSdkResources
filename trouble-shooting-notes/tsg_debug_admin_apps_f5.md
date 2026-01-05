# Debugging Administrator-Elevated WinAppSDK Apps with F5

**Error Codes:** N/A (Debugger Won't Attach)  
**Affected Area:** Visual Studio Debugging, Elevated Applications  
**Common Platforms:** Windows 10/11 with Visual Studio 2022+

---

## Symptom Overview

When developing Windows App SDK applications that require administrator privileges (elevated/UAC), pressing F5 in Visual Studio to start debugging either fails to launch the app, launches it without attaching the debugger, or prompts for elevation repeatedly. Standard debugging workflows don't work for apps with `requestedExecutionLevel` set to `requireAdministrator`.

**You might see:**
- App launches but debugger doesn't attach
- Visual Studio shows "Debugging Not Started"
- UAC prompt appears on every F5 press
- Breakpoints show hollow circles (not hit)
- Output window shows "Cannot debug process with elevated privileges"

---

## Related Issues

- [#2880](https://github.com/microsoft/WindowsAppSDK/issues/2880) - Cannot debug WinAppSDK apps requiring administrator rights with F5

---

## Quick Diagnosis

1. **Check if app requires administrator**
   ```xml
   <!-- In app.manifest -->
   <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
   ```

2. **Check if Visual Studio is running as administrator**
   - Right-click VS in taskbar
   - If "Run as administrator" option is visible → VS is NOT elevated
   - If option is grayed out → VS IS elevated

3. **Test debugging behavior**
   - Press F5
   - If UAC prompt appears → VS is not elevated
   - If app runs but breakpoints don't hit → debugger not attached

4. **Check debugger attach status**
   - Debug → Attach to Process
   - Find your app process
   - If "Elevated" column shows "Yes" and debugger can't attach → elevation mismatch

---

## Common Scenarios & Solutions

### Scenario 1: F5 Debugging Fails for Administrator Apps

**Root Cause:** Visual Studio cannot attach a debugger to a process with higher privileges than itself. When Visual Studio runs as a normal user and tries to debug an app marked with `requireAdministrator`, Windows security prevents the debugger from attaching to the elevated process.

**Related Issue(s):** [#2880](https://github.com/microsoft/WindowsAppSDK/issues/2880)

**Environment:**
- Visual Studio 2022+ running as normal user
- WinUI 3 or Windows App SDK app
- app.manifest with `requestedExecutionLevel="requireAdministrator"`
- Windows 10 or Windows 11

**Symptoms:**
- F5 launches app with UAC prompt
- App runs but no debugger attached
- Breakpoints never hit
- Debug → Windows → Modules shows app assembly not loaded

**Fix Option 1: Run Visual Studio as Administrator (Recommended)**

Always run Visual Studio elevated when developing admin apps:

1. **Close Visual Studio**

2. **Run VS as Administrator**
   ```
   Method 1: Right-click Visual Studio shortcut → Run as administrator
   
   Method 2: Pin VS to taskbar
            → Right-click taskbar icon
            → Right-click "Visual Studio 2022"
            → Properties
            → Advanced
            → ✅ Run as administrator
            → OK
   ```

3. **Configure VS to always run elevated**
   ```powershell
   # Create a scheduled task to launch VS elevated (advanced)
   # Or use Compatibility settings:
   # - Right-click devenv.exe
   # - Properties → Compatibility
   # - ✅ Run this program as an administrator
   # - OK
   ```

4. **Open solution and press F5**
   - No UAC prompt should appear (already elevated)
   - Debugger attaches successfully
   - Breakpoints work normally

**Verification:**
```
Window title should show: "Microsoft Visual Studio (Administrator)"
```

**Fix Option 2: Remove Admin Requirement During Development**

Temporarily remove admin requirement, add it back only for release builds:

1. **Create Debug and Release manifests**
   ```
   Properties\
     app.manifest (Debug - no admin)
     app.release.manifest (Release - requires admin)
   ```

2. **Configure Debug manifest (no admin)**
   ```xml
   <!-- app.manifest for Debug -->
   <requestedExecutionLevel level="asInvoker" uiAccess="false" />
   ```

3. **Configure Release manifest (requires admin)**
   ```xml
   <!-- app.release.manifest for Release -->
   <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
   ```

4. **Update .csproj to use different manifests**
   ```xml
   <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
     <ApplicationManifest>Properties\app.manifest</ApplicationManifest>
   </PropertyGroup>
   
   <PropertyGroup Condition="'$(Configuration)' == 'Release'">
     <ApplicationManifest>Properties\app.release.manifest</ApplicationManifest>
   </PropertyGroup>
   ```

5. **Debug normally (no elevation needed)**
   ```
   Press F5 → App runs as normal user → Debugger attaches
   ```

6. **Test admin features separately**
   ```csharp
   // Check if running elevated
   var identity = WindowsIdentity.GetCurrent();
   var principal = new WindowsPrincipal(identity);
   bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
   
   if (!isElevated)
   {
       Debug.WriteLine("Not running as admin - some features disabled");
       // Show message to user or self-elevate
   }
   ```

**Fix Option 3: Use Manual Debugger Attach Workflow**

If you can't run VS as admin, use attach workflow:

1. **Launch app manually**
   ```powershell
   # Build app first
   dotnet build
   
   # Run app as admin
   Start-Process -FilePath "bin\Debug\net8.0-windows\YourApp.exe" -Verb RunAs
   ```

2. **Attach debugger from Visual Studio**
   - Debug → Attach to Process (Ctrl+Alt+P)
   - ✅ Show processes from all users
   - Find YourApp.exe
   - Click "Attach"
   
   **Note:** This will fail if VS is not elevated. You'll see error:
   ```
   Unable to attach to the process. Access is denied.
   ```

3. **If attach fails, run VS as admin first (back to Option 1)**

---

### Scenario 2: UAC Prompt Appears Every Time You Debug

**Root Cause:** When Visual Studio is NOT elevated but the app requires administrator, Windows shows a UAC prompt every time you press F5. This is by design but becomes tedious during development.

**Environment:**
- Visual Studio running as normal user
- App with `requireAdministrator` in manifest
- Frequent debugging iterations

**Symptoms:**
- UAC prompt on every F5 press
- Must click "Yes" to continue
- Interrupts debugging workflow

**Fix: Run Visual Studio as Administrator**

See Scenario 1, Fix Option 1.

**Alternative: Disable UAC (NOT RECOMMENDED)**

**Warning:** Disabling UAC reduces system security and is not recommended.

```powershell
# NOT RECOMMENDED - FOR REFERENCE ONLY
# Disable UAC via registry (requires restart)
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "EnableLUA" -Value 0
# Restart required
```

Instead, use Visual Studio elevated or remove admin requirement during debug.

---

### Scenario 3: Some Features Need Admin, Others Don't

**Root Cause:** Your app has some features that require admin privileges (e.g., writing to HKLM, accessing system files) but most features work as normal user. Requiring admin for the entire app reduces usability.

**Environment:**
- WinUI 3 app with mixed privilege requirements
- Want to debug without elevation when possible
- Need admin only for specific operations

**Solution: Implement Self-Elevation for Specific Features**

1. **Run app as normal user by default**
   ```xml
   <!-- app.manifest -->
   <requestedExecutionLevel level="asInvoker" uiAccess="false" />
   ```

2. **Create elevated helper for admin tasks**
   ```csharp
   public class AdminHelper
   {
       public static bool IsAdministrator()
       {
           var identity = WindowsIdentity.GetCurrent();
           var principal = new WindowsPrincipal(identity);
           return principal.IsInRole(WindowsBuiltInRole.Administrator);
       }
       
       public static void ExecuteAsAdmin(string task)
       {
           if (IsAdministrator())
           {
               // Already admin, execute directly
               PerformAdminTask(task);
               return;
           }
           
           // Launch elevated copy of this app with task parameter
           var startInfo = new ProcessStartInfo
           {
               FileName = Process.GetCurrentProcess().MainModule.FileName,
               Arguments = $"--admin-task {task}",
               Verb = "runas",  // This triggers UAC
               UseShellExecute = true
           };
           
           try
           {
               var process = Process.Start(startInfo);
               process.WaitForExit();
               
               // Elevated process completed, read result if needed
           }
           catch (Win32Exception)
           {
               // User cancelled UAC prompt
               MessageBox.Show("Administrator rights are required for this operation.");
           }
       }
       
       private static void PerformAdminTask(string task)
       {
           switch (task)
           {
               case "modify-registry":
                   // Modify HKLM registry
                   break;
               case "install-driver":
                   // Install system driver
                   break;
               // ... other admin tasks
           }
       }
   }
   ```

3. **Handle admin tasks in App.OnLaunched**
   ```csharp
   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
       // Check if launched for admin task
       var cmdArgs = Environment.GetCommandLineArgs();
       if (cmdArgs.Length > 1 && cmdArgs[0] == "--admin-task")
       {
           // We're an elevated copy, perform task and exit
           string task = cmdArgs[1];
           AdminHelper.PerformAdminTask(task);
           Application.Current.Exit();
           return;
       }
       
       // Normal app launch
       m_window = new MainWindow();
       m_window.Activate();
   }
   ```

4. **Call from UI when admin needed**
   ```csharp
   private void OnAdminFeatureClick(object sender, RoutedEventArgs e)
   {
       if (!AdminHelper.IsAdministrator())
       {
           // Show warning and self-elevate
           var dialog = new ContentDialog
           {
               Title = "Administrator Required",
               Content = "This feature requires administrator privileges. " +
                        "You will be prompted by UAC.",
               PrimaryButtonText = "Continue",
               CloseButtonText = "Cancel"
           };
           
           var result = await dialog.ShowAsync();
           if (result == ContentDialogResult.Primary)
           {
               AdminHelper.ExecuteAsAdmin("modify-registry");
           }
           return;
       }
       
       // Already admin, execute directly
       AdminHelper.PerformAdminTask("modify-registry");
   }
   ```

5. **Debug normally as non-admin**
   ```
   Press F5 → App runs as normal user → Debugger attaches
   When admin feature is triggered → Separate elevated process launches
   Debug elevated process separately if needed (attach to process)
   ```

**Benefits:**
- App runs as normal user most of the time
- Standard debugging workflow (F5) works
- Users only see UAC when absolutely necessary
- Better security posture (principle of least privilege)

---

## Additional Context

### Why This Happens

Windows security model prevents:
1. Lower privilege process (non-admin VS) debugging higher privilege process (admin app)
2. This protects against privilege escalation attacks
3. Debugger attachment grants significant control over target process

### Visual Studio Elevation Considerations

**Running VS as Admin:**
- ✅ Can debug both admin and non-admin apps
- ✅ F5 workflow works normally
- ⚠️ Extensions and tools run with admin rights
- ⚠️ May hide permission issues in your app

**Running VS as Normal User:**
- ✅ More secure (extensions don't have admin)
- ✅ Exposes permission issues during development
- ❌ Can't debug admin apps with F5
- ❌ Manual attach workflow required

### Best Practices

1. **Develop as normal user:** Catch permission issues early
2. **Self-elevate when needed:** Only request admin for specific operations
3. **Test both modes:** Verify app works as normal user and admin
4. **Use separate manifests:** Debug without admin, release with admin if needed

### Impact

- **Development Friction:** Extra steps required for debugging
- **Security Awareness:** Forces developers to think about privilege requirements
- **User Experience:** Apps that always require admin have poor UX

---

## Related Documentation

- [User Account Control](https://learn.microsoft.com/windows/security/application-security/application-control/user-account-control/)
- [App Manifests](https://learn.microsoft.com/windows/win32/sbscs/application-manifests)
- [Debugging Elevated Processes](https://learn.microsoft.com/visualstudio/debugger/debug-windows-api)
- [Least Privilege Principle](https://learn.microsoft.com/windows/security/threat-protection/security-policy-settings/user-rights-assignment)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 1.00  
**Status:** Workarounds available; Run VS as admin or redesign app to avoid always-admin requirement
