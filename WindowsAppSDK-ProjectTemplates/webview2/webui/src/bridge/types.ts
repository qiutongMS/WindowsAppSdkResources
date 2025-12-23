export const BridgeMethods = {
  AppGetInfo: "app.getInfo",
  ClipboardGetText: "clipboard.getText",
  ClipboardSetText: "clipboard.setText",
  AiEcho: "ai.echo",
  AiRemoveBackground: "ai.removeBackground",
  AppLog: "app.log",
} as const;

export type BridgeMethod = (typeof BridgeMethods)[keyof typeof BridgeMethods];

export const BridgeErrorCodes = {
  InvalidRequest: "invalid_request",
  VersionNotSupported: "version_not_supported",
  MethodNotFound: "method_not_found",
  Exception: "exception",
} as const;

export type BridgeRequest = {
  v: 1;
  id: string;
  method: BridgeMethod;
  params?: Record<string, unknown>;
};

export type BridgeError = {
  code: string;
  message: string;
  details?: unknown;
};

export type BridgeResponse =
  | { v: 1; id: string; ok: true; result?: unknown }
  | { v: 1; id: string; ok: false; error: BridgeError };

export type NativeInvoker = (method: BridgeMethod, params?: Record<string, unknown>) => Promise<unknown>;

export type WebViewBridge = {
  postMessage: (data: unknown) => void;
  addEventListener: (type: "message", listener: (ev: MessageEvent<any>) => void) => void;
  removeEventListener: (type: "message", listener: (ev: MessageEvent<any>) => void) => void;
};

declare global {
  interface Window {
    chrome?: {
      webview?: WebViewBridge;
    };
  }
}

export {};
