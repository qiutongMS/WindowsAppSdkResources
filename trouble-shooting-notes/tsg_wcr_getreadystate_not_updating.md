---
id: wcr_getreadystate_not_updating
title: "GetReadyState returns EnsureNeeded after successful EnsureReadyAsync"
area: Runtime
symptoms:
  - "GetReadyState still returns EnsureNeeded after EnsureReadyAsync succeeds"
  - "State check APIs not updating after model deployment"
  - "ImageDescriptionGenerator state inconsistency"
errorCodes: []
keywords:
  - "GetReadyState"
  - "EnsureReadyAsync"
  - "EnsureNeeded"
  - "state"
  - "ImageDescriptionGenerator"
  - "WCR"
  - "model availability"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.8.250410001-experimental1"
projectType: "packaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5352"
---

# Problem

When using Windows Copilot Runtime APIs like `ImageDescriptionGenerator`, calling `GetReadyState()` returns `AIFeatureReadyState.EnsureNeeded` even after a successful call to `EnsureReadyAsync()` that returns `AIFeatureReadyResultState.Success`. Despite the incorrect state value, the API actually works correctly when calling `CreateAsync()` and using the features.

# Quick Diagnosis

1. Call `GetReadyState()` on any WCR API (e.g., `ImageDescriptionGenerator.GetReadyState()`)
2. Observe it returns `EnsureNeeded`
3. Call `EnsureReadyAsync()` - it returns `Success`
4. Call `GetReadyState()` again - it still returns `EnsureNeeded` (incorrect)
5. Despite the state, calling `CreateAsync()` works successfully

# Root Cause

This is a known issue in the Windows Copilot Runtime state management layer. The `GetReadyState()` API does not properly refresh or reflect the updated state after a successful `EnsureReadyAsync()` operation. The underlying models are correctly deployed and functional, but the state query API does not accurately report the ready status.

The issue appears to be related to caching or state synchronization in the WCR infrastructure, where the availability check does not reflect the post-deployment state.

# Fix / Workaround

**Workaround: Ignore the GetReadyState() value after EnsureReadyAsync() succeeds**

Instead of relying on `GetReadyState()` to confirm readiness, rely on the return value of `EnsureReadyAsync()`:

```csharp
// Check initial state (optional)
var initialState = ImageDescriptionGenerator.GetReadyState();
Debug.WriteLine($"Initial state: {initialState}");

// Ensure model is ready
var ensureResult = await ImageDescriptionGenerator.EnsureReadyAsync();
if (ensureResult.Status != AIFeatureReadyResultState.Success)
{
    throw new Exception($"Failed to ensure ready: {ensureResult.ExtendedError?.Message}");
}

// DON'T check GetReadyState() again here - it may still return EnsureNeeded
// Instead, proceed directly to CreateAsync()
var generator = await ImageDescriptionGenerator.CreateAsync();

// Generator is ready to use
```

**Alternative Pattern:**

```csharp
// Simpler: Just call EnsureReadyAsync first, then CreateAsync
var ensureResult = await ImageDescriptionGenerator.EnsureReadyAsync();
if (ensureResult.Status == AIFeatureReadyResultState.Success)
{
    var generator = await ImageDescriptionGenerator.CreateAsync();
    // Use generator...
}
```

# Verification

1. Implement the workaround pattern shown above
2. Verify `EnsureReadyAsync()` returns `Success`
3. Verify `CreateAsync()` succeeds and returns a functional instance
4. Verify you can use the WCR API features successfully
5. **Do not rely on `GetReadyState()` value after ensure operation**

# Deep Dive

## Expected Behavior

```csharp
GetReadyState() => EnsureNeeded
await EnsureReadyAsync() => Success
GetReadyState() => Ready  // THIS IS WHAT SHOULD HAPPEN
```

## Actual Behavior

```csharp
GetReadyState() => EnsureNeeded
await EnsureReadyAsync() => Success
GetReadyState() => EnsureNeeded  // THIS IS THE BUG
```

## Impact

This issue has **low practical impact** because:
- The models ARE actually deployed and functional
- `CreateAsync()` works correctly despite the state value
- Most developers call `EnsureReadyAsync()` once at app startup and don't re-check state

However, it can cause confusion for:
- Developers implementing health checks or diagnostics
- Applications that dynamically check availability
- Testing scenarios that validate state transitions

## Affected APIs

This behavior has been observed with:
- ImageDescriptionGenerator
- Potentially other WCR APIs (TextRecognizer, LanguageModel, etc.)

## Best Practices

Given this issue, follow this pattern:

```csharp
public class WCRImageService
{
    private ImageDescriptionGenerator? _generator;
    private bool _isReady = false;

    public async Task InitializeAsync()
    {
        // Check if already initialized
        if (_isReady) return;

        // Ensure model is deployed (only check EnsureReadyAsync result)
        var result = await ImageDescriptionGenerator.EnsureReadyAsync();
        if (result.Status != AIFeatureReadyResultState.Success)
        {
            throw new Exception($"Model deployment failed: {result.ExtendedError?.Message}");
        }

        // Create generator instance
        _generator = await ImageDescriptionGenerator.CreateAsync();
        _isReady = true;
    }

    public async Task<string> DescribeImageAsync(ImageBuffer image)
    {
        if (!_isReady || _generator == null)
        {
            await InitializeAsync();
        }

        var result = await _generator!.DescribeAsync(image, ImageDescriptionKind.BriefDescription);
        return result.Description;
    }
}
```

## Related Issues

- Some users report `GetReadyState()` returning `NotReady` even when model is available (Issue #5982)
- State management appears to have broader issues across WCR APIs

# References

- [GitHub Issue #5352](https://github.com/microsoft/WindowsAppSDK/issues/5352)
- [WCR Documentation - Get Started](https://learn.microsoft.com/windows/ai/apis/get-started)
