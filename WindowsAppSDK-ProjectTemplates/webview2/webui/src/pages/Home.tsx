import { useMemo, useState } from "react";
import { useNativeInvoke } from "../hooks/useNative";
import { BridgeMethods } from "../bridge/types";

function pretty(v: unknown) {
  if (v instanceof Error) return v.message;
  if (typeof v === "string") return v;
  return JSON.stringify(v, null, 2);
}

export function Home() {
  const [text, setText] = useState("");

  const info = useNativeInvoke(BridgeMethods.AppGetInfo);
  const clipGet = useNativeInvoke(BridgeMethods.ClipboardGetText);
  const clipSet = useNativeInvoke(BridgeMethods.ClipboardSetText);
  const aiEcho = useNativeInvoke(BridgeMethods.AiEcho);

  const lastResult = useMemo(
    () => info.data ?? clipGet.data ?? clipSet.data ?? aiEcho.data,
    [info.data, clipGet.data, clipSet.data, aiEcho.data]
  );
  const lastError = useMemo(
    () => info.error ?? clipGet.error ?? clipSet.error ?? aiEcho.error,
    [info.error, clipGet.error, clipSet.error, aiEcho.error]
  );


  return (
    <div className="card">
      <h2>Native Bridge Demo</h2>
      <p>Call WinUI host methods and get JSON back.</p>

      <div className="buttons">
        <button className="primary" disabled={info.loading} onClick={() => info.call()}>
          app.getInfo
        </button>
        <button className="primary" disabled={clipGet.loading} onClick={() => clipGet.call()}>
          clipboard.getText
        </button>
        <button className="primary" disabled={clipSet.loading} onClick={() => clipSet.call({ text })}>
          clipboard.setText
        </button>
        <button className="primary" disabled={aiEcho.loading} onClick={() => aiEcho.call({ text })}>
          ai.echo
        </button>
      </div>

      <input
        className="text"
        placeholder="Type something..."
        value={text}
        onChange={(e) => setText(e.target.value)}
      />

      <h3>Result</h3>
      <pre className="output">{lastError ? pretty(lastError) : pretty(lastResult)}</pre>

    </div>
  );
}
