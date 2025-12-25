import { invoke } from "../bridge/native";
import { BridgeMethods } from "../bridge/types";
import { isWebView } from "./env";

export type Level = "info" | "warn" | "error";
const basePrefix = "[winshell]";

const consoleMap: Record<Level, (...args: unknown[]) => void> = {
  info: console.info.bind(console),
  warn: console.warn.bind(console),
  error: console.error.bind(console),
};

async function sendToNative(level: Level, message: string, meta?: unknown) {
  if (!isWebView) return;
  try {
    await invoke(BridgeMethods.AppLog, { level, message, meta });
  } catch (err) {
    // native logging failures are non-fatal; keep console output
    console.warn(basePrefix, "native log failed", err);
  }
}

function emit(level: Level, prefix: string, message: string, meta?: unknown) {
  const payload = meta === undefined ? [prefix, message] : [prefix, message, meta];
  consoleMap[level](...payload);
  void sendToNative(level, `${prefix} ${message}`, meta);
}

export function createLogger(scope?: string) {
  const prefix = scope ? `${basePrefix}[${scope}]` : basePrefix;
  return {
    info: (message: string, meta?: unknown) => emit("info", prefix, message, meta),
    warn: (message: string, meta?: unknown) => emit("warn", prefix, message, meta),
    error: (message: string, meta?: unknown) => emit("error", prefix, message, meta),
  };
}

export const logger = createLogger();
