# Troubleshooting Windows App Launch Failures

This guide provides a concise, actionable method for debugging Windows app launch failures caused by 
XAML initialization errors.

## Problem

The app crashes on launch, often without error or with a generic error in Event Viewer such as
`Faulting module name: Microsoft.UI.Xaml.dll`. The root cause is usually a misconfigured XAML file
causing parsing error that occurs inside the `InitializeComponent()` method.

## Trouble-shooting guide: Display the Exception in the UI

Catch the exception and display it in the app's window to see the root cause.

### Step 1: Add a `try-catch` block

In your main window's code-behind (e.g., `MainWindow.xaml.cs`), wrap the `InitializeComponent()` 
call in the constructor with a `try-catch` block.

```C#
// In MainWindow.xaml.cs
public MainWindow()
{
    try
    {
        InitializeComponent();
    }
    catch (Exception ex)
    {
        // Error handling logic here
    }
}
```

### Step 2: Display the Exception

In the `catch` block, set the window's `Content` to a `TextBlock` that displays the exception 
details. This makes the error visible instead of crashing the app.

```csharp
// In MainWindow.xaml.cs, inside the catch block
catch (Exception ex)
{
    // Log for debugging
    System.Diagnostics.Debug.WriteLine($"XAML Loading Failed: {ex}");

    // Display the error in the window
    this.Content = new TextBlock
    {
        Text = $"Failed to load UI:\n\n{ex}",
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(20)
    };
    return;
}
```

### Step 3: Re-build and Re-launch to see the error.

1.  Call the `#official-build-tool`.
2.  Launch the app to see the error 
    Or, for AI Agents, ask the developer to launch the rebuilt app, and **Request a screenshot** of 
    the app window, which will now display the specific XAML error message. 
3.  fix the underlying issue in the XAML file based on the captured error message.

#### Step 4: Remove Test Code After the Issue's Resolved.

Remove the `try-catch` block once the issue is resolved to restore normal app behavior.