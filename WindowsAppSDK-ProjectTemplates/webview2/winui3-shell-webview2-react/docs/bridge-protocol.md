# Winshell Bridge Protocol (v1)

Winshell Bridge is a lightweight JSON protocol over WebView2 `postMessage`, used for Web (JS) to call native (C# / WinAppSDK) capabilities.

## Goals

- Simple: web developers only need `winshell.invoke(method, params)`.
- Evolvable: protocol carries version `v`; can extend to batch calls, streaming responses, etc.
- Diagnosable: consistent error codes and error shape.

## Request

```json
{
  "v": 1,
  "id": "1700000000000-abc123",
  "method": "clipboard.getText",
  "params": {
    "any": "json"
  }
}
```

Fields:
- `v`: protocol version, currently fixed to `1`
- `id`: request ID (string), used to correlate the response
- `method`: method name (string), recommended format `namespace.action`
- `params`: optional parameters object (JSON object)

Constraints:
- `id` must exist and be non-empty
- `method` must exist and be non-empty

## Response

Success:
```json
{
  "v": 1,
  "id": "1700000000000-abc123",
  "ok": true,
  "result": {
    "any": "json"
  }
}
```

Failure:
```json
{
  "v": 1,
  "id": "1700000000000-abc123",
  "ok": false,
  "error": {
    "code": "method_not_found",
    "message": "Unknown method: xxx.yyy",
    "details": {
      "any": "json"
    }
  }
}
```

Fields:
- `v`: protocol version
- `id`: same as request
- `ok`: whether the call succeeded
- `result`: success payload (any JSON)
- `error`: failure payload
  - `code`: stable error code (machine readable)
  - `message`: human-readable error message
  - `details`: optional extra info (JSON)

## Standard error codes

- `invalid_request`: request JSON does not satisfy the protocol (missing/invalid fields)
- `version_not_supported`: protocol version not supported
- `method_not_found`: method does not exist
- `exception`: unhandled exception on native side

## Method list (examples)

- `app.getInfo`
- `clipboard.getText`
- `clipboard.setText`
- `ai.echo` (placeholder)
