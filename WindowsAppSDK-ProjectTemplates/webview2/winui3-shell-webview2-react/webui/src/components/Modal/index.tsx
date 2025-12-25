import type { ReactNode } from "react";

interface ModalProps {
  title: string;
  open: boolean;
  onClose?: () => void;
  children?: ReactNode;
}

export function Modal({ title, open, onClose, children }: ModalProps) {
  if (!open) return null;
  return (
    <div style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.35)", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <div style={{ background: "#fff", borderRadius: 12, minWidth: 320, maxWidth: 520, padding: 16 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
          <strong>{title}</strong>
          {onClose && (
            <button className="secondary" onClick={onClose}>
              Close
            </button>
          )}
        </div>
        <div>{children}</div>
      </div>
    </div>
  );
}
