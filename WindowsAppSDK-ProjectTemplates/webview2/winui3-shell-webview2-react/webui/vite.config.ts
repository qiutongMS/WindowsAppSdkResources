import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
  },
  build: {
    outDir: resolve(__dirname, "../native/Winshell/Web"),
    emptyOutDir: true,
    sourcemap: true,
  },
});
