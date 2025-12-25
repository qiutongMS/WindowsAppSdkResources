import { useMemo } from "react";
import { createLogger, logger as defaultLogger } from "../utils/log";

export function useLogger(scope?: string) {
  return useMemo(() => (scope ? createLogger(scope) : defaultLogger), [scope]);
}
