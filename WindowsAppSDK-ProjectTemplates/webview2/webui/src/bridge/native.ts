import type { BridgeRequest, BridgeResponse, NativeInvoker, WebViewBridge } from "./types";

const PROTOCOL_VERSION = 1;

function guid() {
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function ensureWebView(): WebViewBridge {
  const webview = window.chrome?.webview;
  if (!webview?.postMessage) {
    throw new Error("This page must be hosted inside WebView2 (chrome.webview missing)");
  }
  return webview;
}

export const invoke: NativeInvoker = (method, params) => {
  const webview = ensureWebView();
  const id = guid();
  const req: BridgeRequest = { v: PROTOCOL_VERSION, id, method, params };

  return new Promise((resolve, reject) => {
    const handler = (ev: MessageEvent<BridgeResponse>) => {
      const msg = ev.data;
      if (!msg || msg.id !== id) return;
      webview.removeEventListener("message", handler as any);
      if (msg.ok) resolve(msg.result);
      else reject(msg.error ?? { code: "unknown", message: "Unknown error" });
    };

    webview.addEventListener("message", handler as any);
    webview.postMessage(req);
  });
};
