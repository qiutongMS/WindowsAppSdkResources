import type { ButtonHTMLAttributes } from "react";

export function Button(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button {...props} className={`primary ${props.className ?? ""}`.trim()}>
      {props.children}
    </button>
  );
}
