import { useCallback, useState } from "react";
import { invoke } from "../bridge/native";
import type { BridgeMethod } from "../bridge/types";

export function useNativeInvoke(method: BridgeMethod) {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<unknown>(null);
  const [error, setError] = useState<unknown>(null);

  const call = useCallback(
    async (params?: Record<string, unknown>) => {
      setLoading(true);
      setError(null);
      try {
        const result = await invoke(method, params);
        setData(result);
        return result;
      } catch (e) {
        setError(e);
        throw e;
      } finally {
        setLoading(false);
      }
    },
    [method]
  );

  return { call, loading, data, error };
}
