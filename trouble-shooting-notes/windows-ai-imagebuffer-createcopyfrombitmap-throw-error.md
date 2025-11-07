## Troubleshooting Note: `ImageBuffer.CreateCopyFromBitmap` Compilation Error

### 🧭 Scope
This note captures a普适性的 (universally applicable) fix for the build error:

```
error CS0117: 'ImageBuffer' does not contain a definition for 'CreateCopyFromBitmap'
```

Developers hit this when porting Windows AI Foundry sample code or older Windows App SDK snippets into a project that targets **Windows App SDK 1.8 (or newer)**.

### 🕵️ Root Cause
- `ImageBuffer.CreateCopyFromBitmap(..)` existed in earlier preview documentation/samples.
- Starting with Windows App SDK 1.8, the public API surface only exposes:
	- `ImageBuffer.CreateForSoftwareBitmap(SoftwareBitmap bitmap)`
	- `ImageBuffer.CreateBufferAttachedToBitmap(SoftwareBitmap bitmap)`
- Re-using the older method name triggers the CS0117 compiler error because the method was never shipped (or was removed before GA).

### ✅ Resolution Steps
1. **Replace the deprecated call**
	 ```csharp
	 // ❌ Old
	 var imageBuffer = ImageBuffer.CreateCopyFromBitmap(softwareBitmap);

	 // ✅ New
	 var imageBuffer = ImageBuffer.CreateForSoftwareBitmap(softwareBitmap);
	 ```
	 - `CreateForSoftwareBitmap` returns a copy that is safe to hand to the AI APIs.
	 - If you must keep the original pixel buffer alive, use `CreateBufferAttachedToBitmap` instead.

2. **Confirm namespaces**
	 ```csharp
	 using Microsoft.Graphics.Imaging;
	 using Microsoft.Windows.AI.Imaging;
	 ```

3. **Rebuild the project** to verify the compiler error disappears:

(Last updated): 2025-11-07
