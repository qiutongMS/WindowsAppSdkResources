# Services Layer - Business Logic

## Purpose
This folder contains core business logic, domain rules, and orchestrates operations between Data layer and ViewModels.

## Rules

### ✅ DO:
- Define service interfaces (I[Name]Service)
- Implement business rules and validation
- Orchestrate multiple repository calls
- Transform data models between layers
- Handle business exceptions
- Log all method entry/exit with parameters and duration

### ❌ DON'T:
- Reference ViewModels or Views
- Directly manipulate UI elements
- Contain UI-specific logic

## Dependencies
- **Depends on**: Data layer (repositories), Models
- **Dependency Injection**: Constructor inject repository interfaces

## Required Logging (CRITICAL)
Every method MUST log:
```csharp
_logger.LogTrace("Entering {Method} with params: {Params}", nameof(Method), params);
// ... operation ...
_logger.LogInformation("{Method} completed in {Duration}ms with result: {Result}", 
    nameof(Method), sw.ElapsedMilliseconds, result);
// ... on error ...
_logger.LogError(ex, "{Method} failed after {Duration}ms", nameof(Method), sw.ElapsedMilliseconds);
```

## Testing

**Tests are CO-LOCATED with source files:**
- Test file location: `Services/{ServiceName}.Test.cs` (SAME FOLDER as source)
- Example: `UserService.Sample.cs` → `UserService.Sample.Test.cs`
- Test class name: `{ServiceName}Tests` (with 's' suffix)
- Mock all Data layer repository interfaces
- Test business logic, validation, error handling

**Running Tests:**
```bash
# Run all Services tests
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~Services"

# Run specific service tests
dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~UserServiceSample"
```

## File Naming Conventions
- Interfaces: `I[Name]Service.cs`
- Implementations: `[Name]Service.cs`
## Code Samples
- See [`UserService.Sample.cs`](UserService.Sample.cs) and [`UserService.Sample.Test.cs`](UserService.Sample.Test.cs) for full implementations and tests.

**Cross-layer references:**
- Data layer: [`../Data/Data.instructions.md`](../Data/Data.instructions.md)
- Models layer: [`../Models/Models.instructions.md`](../Models/Models.instructions.md)
- Called by: [`../ViewModels/ViewModels.instructions.md`](../ViewModels/ViewModels.instructions.md)

## Breaking Change Detection

**When modifying service interfaces, ALWAYS review:**
- **Interface signature changes** → Affects all ViewModels using this service
- **New required parameters** → Breaking change for all consumers
- **Return type changes** → May require ViewModel updates
- **Method removal** → Check all usages first with list_code_usages tool
