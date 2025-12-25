import { useEffect, useMemo, useRef, useState } from "react";
import { useNativeInvoke } from "../hooks/useNative";
import { BridgeMethods } from "../bridge/types";
import { useLogger } from "../hooks/useLogger";

function pretty(v: unknown): string {
  if (v instanceof Error) return v.message;
  if (typeof v === "string") return v;
  return JSON.stringify(v, null, 2);
}

function fileToDataUrl(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = (e) => reject(e);
    img.src = src;
  });
}

async function composeWithMask(originalUrl: string, maskUrl: string): Promise<string> {
  const [origImg, maskImg] = await Promise.all([loadImage(originalUrl), loadImage(maskUrl)]);
  const w = Math.min(origImg.width, maskImg.width);
  const h = Math.min(origImg.height, maskImg.height);

  const canvas = document.createElement("canvas");
  const maskCanvas = document.createElement("canvas");
  canvas.width = maskCanvas.width = w;
  canvas.height = maskCanvas.height = h;

  const ctx = canvas.getContext("2d");
  const maskCtx = maskCanvas.getContext("2d");
  if (!ctx || !maskCtx) throw new Error("Canvas not supported");

  ctx.drawImage(origImg, 0, 0, w, h);
  maskCtx.drawImage(maskImg, 0, 0, w, h);

  const imageData = ctx.getImageData(0, 0, w, h);
  const maskData = maskCtx.getImageData(0, 0, w, h);

  const pixels = imageData.data;
  const maskPixels = maskData.data;
  for (let i = 0; i < pixels.length; i += 4) {
    // mask image is grayscale; any channel works as alpha
    pixels[i + 3] = maskPixels[i];
  }

  ctx.putImageData(imageData, 0, 0);
  return canvas.toDataURL("image/png");
}

export function Background() {
  const [bgImageDataUrl, setBgImageDataUrl] = useState<string>("");
  const [bgImageName, setBgImageName] = useState<string>("");
  const [bgIncludePoints, setBgIncludePoints] = useState<{ x: number; y: number }[]>([]);
  const [bgMask, setBgMask] = useState<string>("");
  const [bgMasked, setBgMasked] = useState<string>("");
  const [hasResult, setHasResult] = useState(false);
  const log = useLogger("background");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const bgImgRef = useRef<HTMLImageElement | null>(null);

  const aiBg = useNativeInvoke(BridgeMethods.AiRemoveBackground);

  const lastError = useMemo(() => pretty(aiBg.error), [aiBg.error]);

  useEffect(() => {
    const mask = (aiBg.data as any)?.maskBase64;
    if (!mask || !bgImageDataUrl) {
      setBgMask("");
      setBgMasked("");
      return;
    }
    setBgMask(mask);
    composeWithMask(bgImageDataUrl, mask)
      .then((res) => setBgMasked(res))
      .catch(() => setBgMasked(""));
  }, [aiBg.data, bgImageDataUrl]);

  async function onBgImageSelected(file?: File) {
    if (!file) return;
    const dataUrl = await fileToDataUrl(file);
    setBgImageDataUrl(dataUrl);
    setBgImageName(file.name);
    setBgIncludePoints([]);
    setBgMask("");
    setBgMasked("");
    setHasResult(false);
    // allow re-selecting the same file next time
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  function onBgImageClick(ev: React.MouseEvent<HTMLImageElement, MouseEvent>) {
    const img = bgImgRef.current;
    if (!img) return;
    const rect = img.getBoundingClientRect();
    const scaleX = img.naturalWidth / img.clientWidth;
    const scaleY = img.naturalHeight / img.clientHeight;
    const x = Math.round((ev.clientX - rect.left) * scaleX);
    const y = Math.round((ev.clientY - rect.top) * scaleY);
    setBgIncludePoints((pts) => [...pts, { x, y }]);
  }

  async function runBgRemove() {
    if (!bgImageDataUrl) return;

    if (bgIncludePoints.length === 0) {
      alert("Please add at least one include point before running background removal.");
      log.warn("Remove background blocked: no include points");
      return;
    }

    log.info("Remove background start", { points: bgIncludePoints.length, file: bgImageName || undefined });
    try {
      await aiBg.call({ imageBase64: bgImageDataUrl, includePoints: bgIncludePoints });
      setHasResult(true);
      log.info("Remove background success", { points: bgIncludePoints.length, file: bgImageName || undefined });
    } catch (err) {
      log.error("Remove background failed", err);
      throw err;
    }
  }

  return (
    <div className="card bg-layout">
      <div className="bg-header">
        <div>
          <h2>Background Removal</h2>
          <p className="muted">Pick an image, tap the subject to guide extraction, then view mask and composited result.</p>
        </div>
        <div className="bg-steps">
          <div className="pill">1. Choose Image</div>
          <div className="pill">2. Pick Foreground Points</div>
          <div className="pill">3. Remove</div>
        </div>
      </div>

      <div className="bg-panels">
        <div className="panel">
          <div className="panel-head">
            <div>
              <div className="label">Input</div>
              <div className="muted small">Click on subject to add include points</div>
            </div>
            <div className="pill neutral">{bgIncludePoints.length} points</div>
          </div>

          <div className="buttons">
            <input
              type="file"
              accept="image/*"
              ref={fileInputRef}
              onChange={(e) => onBgImageSelected(e.target.files?.[0])}
            />
            <button className="primary" disabled={aiBg.loading || !bgImageDataUrl} onClick={runBgRemove}>
              {aiBg.loading ? "Processing..." : "Remove background"}
            </button>
            {bgImageDataUrl && (
              <button
                className="secondary"
                onClick={() => {
                  setBgImageDataUrl("");
                  setBgImageName("");
                  setBgIncludePoints([]);
                  setBgMask("");
                  setBgMasked("");
                  setHasResult(false);
                  if (fileInputRef.current) fileInputRef.current.value = "";
                }}
              >
                Clear
              </button>
            )}
          </div>

          {bgImageDataUrl ? (
            <div className="preview-box" onClick={onBgImageClick} title="Click to add include point">
              <img ref={bgImgRef} src={bgImageDataUrl} alt="Background removal input" />
              {bgImageName && <div className="tag top-left">{bgImageName}</div>}
              {bgIncludePoints.length > 0 && (
                <div className="tag bottom-left">{bgIncludePoints.map((p) => `${p.x},${p.y}`).join(" · ")}</div>
              )}
            </div>
          ) : (
            <div className="placeholder">Upload an image to start</div>
          )}
        </div>

        <div className="panel">
          <div className="panel-head">
            <div className="label">Results</div>
            {lastError && <div className="error">{lastError}</div>}
          </div>

          {aiBg.data && bgImageDataUrl && hasResult ? (
            (() => {
              const payload = aiBg.data as any;
              const mask = payload?.maskBase64 ?? bgMask;
              const computedReady = mask || bgMasked ? "Ready" : undefined;
              return (
                <div className="result-grid two-up">
                  {mask && (
                    <div className="preview-box">
                      <div className="tag top-left">Mask</div>
                      <img src={mask} alt="Mask" />
                    </div>
                  )}
                  {bgMasked && (
                    <div className="preview-box">
                      <div className="tag top-left">Foreground</div>
                      <img src={bgMasked} alt="Foreground" />
                    </div>
                  )}
                  {!mask && !bgMasked && <pre className="output">{pretty(payload)}</pre>}
                  {computedReady && <div className="badge floating">{computedReady}</div>}
                </div>
              );
            })()
          ) : (
            <div className="placeholder">Run removal to see mask and composite.</div>
          )}
        </div>
      </div>
    </div>
  );
}
