import type { BridgeRequest, BridgeResponse, NativeInvoker, WebViewBridge } from "./types";
import { BridgeErrorClass } from "./types";

const PROTOCOL_VERSION = 1;
const DEFAULT_TIMEOUT_MS = 30_000;

// Module-level cached webview and optional debug hook
let cachedWebView: WebViewBridge | null = null;
let debugHandler: ((...args: unknown[]) => void) | null = null;
export const setOnBridgeDebug = (fn: ((...args: unknown[]) => void) | null) => {
  debugHandler = fn;
};
export const clearOnBridgeDebug = () => setOnBridgeDebug(null);

function makeId(): string {
  try {
    // prefer crypto.randomUUID() when available
    if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
      return crypto.randomUUID();
    }
  } catch { /* empty */ }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function ensureWebView(): WebViewBridge {
  if (cachedWebView) return cachedWebView;
  const webview = window.chrome?.webview;
  if (!webview?.postMessage) {
    throw new BridgeErrorClass("This page must be hosted inside WebView2 (chrome.webview missing)", "no-webview");
  }
  cachedWebView = webview;
  return webview;
}

type PendingEntry = {
  resolve: (v: unknown) => void;
  reject: (e: unknown) => void;
  timer: ReturnType<typeof setTimeout> | null;
};

const pending = new Map<string, PendingEntry>();
let listenerInitialized = false;

function initListener(webview: WebViewBridge) {
  if (listenerInitialized) return;

  const handler = (ev: MessageEvent<unknown>) => {
    try {
      const msg = ev?.data as BridgeResponse | undefined;
      if (!msg?.id) return;
      if (msg.v !== PROTOCOL_VERSION) return;

      const entry = pending.get(msg.id);
      if (!entry) return;

      pending.delete(msg.id);
      if (entry.timer) clearTimeout(entry.timer);

      if (msg.ok) entry.resolve(msg.result);
      else {
        const errObj = msg.error ?? { code: "unknown", message: "Unknown error" };
        const e = new BridgeErrorClass(errObj.message ?? String(errObj), errObj.code, errObj);
        entry.reject(e);
      }
    } catch (err) {
      // swallow to avoid crashing listener; optionally debug
      debugHandler?.("bridge-listener-error", err);
    }
  };

  webview.addEventListener("message", handler);

  // Cleanup on page unload: reject outstanding requests
  const cleanup = () => {
    for (const [id, entry] of pending.entries()) {
      if (entry.timer) clearTimeout(entry.timer);
      entry.reject(new BridgeErrorClass("Page unload: request cancelled", "page-unload"));
      pending.delete(id);
    }
  };

  window.addEventListener("beforeunload", cleanup);
  listenerInitialized = true;
}

export const invoke: NativeInvoker = (method, params, timeoutMs = DEFAULT_TIMEOUT_MS) => {
  const webview = ensureWebView();
  initListener(webview);

  const id = makeId();
  const req: BridgeRequest = { v: PROTOCOL_VERSION, id, method, params };

  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      if (!pending.has(id)) return;
      pending.delete(id);
      const e = new BridgeErrorClass("Native invoke timeout", "timeout");
      debugHandler?.("bridge-timeout", id, method);
      reject(e);
    }, timeoutMs);

    pending.set(id, { resolve, reject, timer });

    try {
      debugHandler?.("bridge-post", id, method, params);
      webview.postMessage(req);
    } catch (err) {
      const entry = pending.get(id);
      if (entry) {
        if (entry.timer) clearTimeout(entry.timer);
        pending.delete(id);
      }
      const e = err instanceof Error ? err : new BridgeErrorClass(String(err), "postMessage-failed", err);
      debugHandler?.("bridge-post-error", id, e);
      reject(e);
    }
  });
};
