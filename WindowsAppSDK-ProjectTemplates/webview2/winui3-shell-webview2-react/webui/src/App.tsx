import { useState } from "react";
import { Home } from "./pages/Home";
import { Background } from "./pages/Background";
import { Settings } from "./pages/Settings";

const tabs = [
  { key: "home", label: "Basics", element: <Home /> },
  { key: "bg", label: "Background Removal", element: <Background /> },
  { key: "settings", label: "Settings", element: <Settings /> },
];

export default function App() {
  const [active, setActive] = useState("home");

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="title">Winshell4 (WebView2 + Native Bridge)</div>
        <nav className="tabs">
          {tabs.map((t) => (
            <button
              key={t.key}
              className={active === t.key ? "tab active" : "tab"}
              onClick={() => setActive(t.key)}
            >
              {t.label}
            </button>
          ))}
        </nav>
      </header>
      <main className="app-main">
        {tabs.find((t) => t.key === active)?.element}
      </main>
    </div>
  );
}
