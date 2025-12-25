import { useState } from "react";
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
  return (
    <div className="cards">
      <h2>Native Bridge Demo</h2>
      <p>Call WinUI host methods and get JSON back.</p>

      <div className="card">
        <h3>App Info</h3>
        <button className="primary" disabled={info.loading} onClick={() => info.call()}>
          app.getInfo
        </button>
        <h4>Result</h4>
        <pre className="output">{info.error ? pretty(info.error) : pretty(info.data)}</pre>
      </div>

      <div className="card">
        <h3>Clipboard - Get</h3>
        <button className="primary" disabled={clipGet.loading} onClick={() => clipGet.call()}>
          clipboard.getText
        </button>
        <h4>Result</h4>
        <pre className="output">{clipGet.error ? pretty(clipGet.error) : pretty(clipGet.data)}</pre>
      </div>

      <div className="card">
        <h3>Clipboard - Set</h3>
        <input
          className="text"
          placeholder="Text to set to clipboard"
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button className="primary" disabled={clipSet.loading} onClick={() => clipSet.call({ text })}>
          clipboard.setText
        </button>
        <h4>Result</h4>
        <pre className="output">{clipSet.error ? pretty(clipSet.error) : pretty(clipSet.data)}</pre>
      </div>

      <div className="card">
        <h3>AI - Echo</h3>
        <input
          className="text"
          placeholder="Text to echo"
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
        <button className="primary" disabled={aiEcho.loading} onClick={() => aiEcho.call({ text })}>
          ai.echo
        </button>
        <h4>Result</h4>
        <pre className="output">{aiEcho.error ? pretty(aiEcho.error) : pretty(aiEcho.data)}</pre>
      </div>
    </div>
  );
}
