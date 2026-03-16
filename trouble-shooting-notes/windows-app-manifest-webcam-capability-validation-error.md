# Windows App Manifest: webcam Capability Validation Error

## Error Message

```
error 0xC00CE169: App manifest validation error: The app manifest must be valid as per schema: Line XX, Column XX, Reason: 'webcam' violates enumeration constraint of 'documentsLibrary picturesLibrary videosLibrary musicLibrary enterpriseAuthentication sharedUserCertificates userAccountInformation removableStorage appointments contacts phoneCall blockedChatMessages objects3D voipCall chat'.
The attribute 'Name' with value 'webcam' failed to parse.
```

## Root Cause

The `webcam` capability is being declared using the wrong XML element type. In Windows App manifests:

- **`<uap:Capability>`** is used for **general capabilities** like `documentsLibrary`, `picturesLibrary`, `internetClient`, etc.
- **`<DeviceCapability>`** is used for **device access capabilities** like `webcam`, `microphone`, `location`, etc.

The error occurs when `webcam` is incorrectly declared as:
```xml
<uap:Capability Name="webcam" />
```

## Solution

Use `<DeviceCapability>` instead of `<uap:Capability>` for device-related capabilities:

### ❌ Incorrect
```xml
<Capabilities>
    <uap:Capability Name="webcam" />
</Capabilities>
```

### ✅ Correct
```xml
<Capabilities>
    <DeviceCapability Name="webcam" />
</Capabilities>
```

## Common Device Capabilities

The following capabilities must use `<DeviceCapability>`:

| Capability | Description |
|------------|-------------|
| `webcam` | Camera access |
| `microphone` | Microphone access |
| `location` | GPS/Location services |
| `proximity` | Near-field communication |
| `bluetooth` | Bluetooth access |
| `serialcommunication` | Serial port access |
| `usb` | USB device access |

## Capability Element Order in Manifest

When combining different capability types, they must appear in a specific order within the `<Capabilities>` section:

1. `<Capability>` (general capabilities)
2. `<uap:Capability>` (UAP capabilities)
3. `<rescap:Capability>` (restricted capabilities)
4. Custom namespace capabilities (e.g., `<systemai:Capability>`)
5. `<DeviceCapability>` (device capabilities) — **must come last**

### Example with Multiple Capability Types
```xml
<Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
    <systemai:Capability Name="systemAIModels" />
    <DeviceCapability Name="webcam" />
    <DeviceCapability Name="microphone" />
</Capabilities>
```

## Related Errors

If `<DeviceCapability>` elements are placed before other capability types, you may see:
```
Element 'xxx:Capability' is unexpected according to content model... Expecting: DeviceCapability
```

This indicates the capability ordering issue — ensure `<DeviceCapability>` elements are placed at the end of the `<Capabilities>` section.

## References

- [App capability declarations - Windows UWP](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations)
- [Device capabilities - Windows UWP](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations#device-capabilities)
