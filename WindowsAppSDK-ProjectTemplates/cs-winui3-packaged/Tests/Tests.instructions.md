# Tests Folder - Test Project Configuration

## Purpose

This folder contains the **test project configuration** (`BlankApp.Tests.csproj`) that references all `*.Test.cs` files from parent layer folders.

## Why Separate Folder?

Having the test project in a separate `Tests/` folder avoids bin/obj conflicts:
- ✅ `BlankApp.csproj` → outputs to `bin/`, `obj/`
- ✅ `Tests/BlankApp.Tests.csproj` → outputs to `Tests/bin/`, `Tests/obj/`
- ✅ No conflicts or mixed outputs

## Test File Locations (Co-located)

Test files remain **beside their source files** in layer folders:

```
Views/
├── MainPage.xaml.cs        ← Source
└── MainPage.Test.cs        ← Test (co-located)

Services/
├── UserService.Sample.cs          ← Source (example, excluded from build)
└── UserService.Sample.Test.cs     ← Test (co-located example)

Data/
├── UserRepository.Sample.cs       ← Source (example, excluded from build)
└── UserRepository.Sample.Test.cs  ← Test (co-located example)
```

## Code Samples
- No dedicated test code in this folder; it only contains `BlankApp.Tests.csproj`.
- See layer folders for source + test pairs (e.g., `Services/UserService.Sample.cs` and `Services/UserService.Sample.Test.cs`).

## Project Configuration
- `BlankApp.Tests.csproj` includes all co-located `*.Test.cs` under the parent tree via a single glob.
- Excludes `*.Sample.cs` and `*.Sample.Test.cs` so reference-only examples are not compiled.

## Running Tests

### From Solution Root

**Important:** WinUI3 projects require platform specification.

```bash
# Run all tests
dotnet test .\Tests\BlankApp.Tests.csproj -p:Platform=x64

# Run tests for specific class (incremental)
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~UserServiceSample"

# Run specific test method
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~UserServiceSampleTests.GetUserAsync_WhenExists_ShouldReturnUser"
```

### From Tests Folder

```bash
cd Tests

# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Copilot Integration

Copilot automatically knows:
1. Test files are beside source files (co-located)
2. Test project is at `.\Tests\BlankApp.Tests.csproj`
3. To run incremental tests: `dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~{ClassName}"`

## Build Outputs

```
BlankApp/
├── bin/                    ← Main project output
├── obj/                    ← Main project intermediate
├── Tests/
│   ├── bin/                ← Test project output
│   ├── obj/                ← Test project intermediate
│   └── BlankApp.Tests.csproj
```

**No conflicts!** Each project has its own build output directory.

## Adding New Tests

1. **Create source**: `Services/UserService.Sample.cs`
2. **Create test (same folder)**: `Services/UserService.Sample.Test.cs`
3. **Run**: `dotnet test .\Tests\BlankApp.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~UserServiceSample"`

## Dependencies

- MSTest (Microsoft's test framework)
- Moq (mocking library)

## Important Notes

⚠️ **Never put source code in this folder** - only test project configuration  
✅ **Test files stay beside source files** in layer folders  
✅ **This project only contains BlankApp.Tests.csproj**
