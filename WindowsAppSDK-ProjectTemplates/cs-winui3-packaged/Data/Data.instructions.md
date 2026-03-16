# Data Layer - Data Access

## Purpose
Repositories, database contexts, API clients, and data mapping.

## Rules

**✅ DO:**
- Implement repository pattern (interface-first)
- Handle database connections/transactions
- Make HTTP/API calls
- Log all operations (entry/exit/duration/errors)

**❌ DON'T:**
- Reference ViewModels or Views
- Implement business rules (use Services layer)
- Expose database-specific types to other layers

## Dependencies
**Depends on:** Models ONLY  
**Injected:** DbContext, HttpClient, etc.

## Testing (Co-located)
- Location: `Data/{RepositoryName}.Test.cs` (SAME FOLDER)
- Example: `UserRepository.Sample.cs` → `UserRepository.Sample.Test.cs`
- Mock: Use in-memory database or fakes
- Run: `dotnet test .\Tests\BlankApp.Tests.csproj --filter "FullyQualifiedName~Data"`

## Logging (Required)
```csharp
_logger.LogTrace("Entering {Method} with {Params}", nameof(Method), params);
// database/API call
_logger.LogInformation("{Method} completed in {Duration}ms, rows: {Count}", 
    nameof(Method), sw.ElapsedMilliseconds, count);
// on error
_logger.LogError(ex, "{Method} failed after {Duration}ms", nameof(Method), sw.ElapsedMilliseconds);
```

## File Naming
- Interfaces: `I[Name]Repository.cs`
- Implementations: `[Name]Repository.cs`
- DB Context: `AppDbContext.cs`
- API Clients: `[Name]ApiClient.cs`

## Code Samples
- Interface: [`IUserRepository.Sample.cs`](IUserRepository.Sample.cs)
- Implementation: [`UserRepository.Sample.cs`](UserRepository.Sample.cs)
- Tests: [`UserRepository.Sample.Test.cs`](UserRepository.Sample.Test.cs)

## Best Practices
- **Repository Pattern:** Interface-first (I{Name}Repository)
- **DbContext:** Inject via DI, never instantiate directly
- **HttpClient:** Use IHttpClientFactory for API clients
- **Transactions:** Use for multi-step operations
- **Connections:** Let DI container manage lifetime

## Cross-References
- Models: [`../Models/Models.instructions.md`](../Models/Models.instructions.md)
- Called by: [`../Services/Services.instructions.md`](../Services/Services.instructions.md)
