# Views Layer - UI Components

## Purpose
This folder contains all UI-related code: XAML files, code-behind, value converters, and custom controls.

## Rules

### ✅ DO:
- Keep code-behind minimal (UI-specific logic only)
- Bind to ViewModels via DataContext
- Use value converters for data transformation in UI
- Create reusable user controls
- Handle UI-specific events (loaded, unloaded, size changed)

### ❌ DON'T:
- Put business logic in code-behind
- Access Services or Data layers directly
- Perform data access operations
- Implement complex validation logic

## Dependencies
- **Depends on**: ViewModels layer ONLY
- **Dependency Injection**: Use constructor injection to receive ViewModels

## File Naming Conventions
- Pages: `[Name]Page.xaml` / `[Name]Page.xaml.cs`
- Controls: `[Name]Control.xaml` / `[Name]Control.xaml.cs`
- Converters: `[Name]Converter.cs`

## Testing

**Tests are CO-LOCATED with source files:**
- Test file location: `Views/{PageName}.Test.cs` (SAME FOLDER as source)
- Example: `MainPage.xaml.cs` → `MainPage.Test.cs`
- Test class name: `{PageName}Tests` (with 's' suffix)
- Mock ViewModels in tests
- Test UI bindings, converters, and control behavior

**Running Tests:**
```bash
# Run all Views tests
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~Views"

# Run specific page tests
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~MainPage"
```

## Example Structure
```
Views/
├── MainPage.xaml
├── MainPage.xaml.cs
├── SettingsPage.xaml
├── SettingsPage.xaml.cs
├── Controls/
│   ├── CustomButton.xaml
│   └── CustomButton.xaml.cs
└── Converters/
    ├── BoolToVisibilityConverter.cs
    └── DateTimeFormatConverter.cs
```

## Code Samples
- See [`MainPage.Sample.cs`](MainPage.Sample.cs) and [`MainPage.Sample.Test.cs`](MainPage.Sample.Test.cs) for full patterns.

**Cross-layer references:**
- ViewModels layer: [`../ViewModels/ViewModels.instructions.md`](../ViewModels/ViewModels.instructions.md)
- Complete samples: Check ViewModels folder for `*.Sample.cs` examples
