using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Graphics.Imaging;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Imaging;
using Microsoft.Extensions.Logging;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Winshell.Bridge;

namespace Winshell.Handlers;

public sealed class AiRemoveBackgroundHandler : Winshell.Bridge.IBridgeHandler
{
    private readonly ILogger<AiRemoveBackgroundHandler>? _log;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public AiRemoveBackgroundHandler(ILogger<AiRemoveBackgroundHandler>? log = null)
    {
        _log = log;
    }

    public async Task<JsonNode?> HandleAsync(JsonObject? p)
    {
        var imageBase64 = p?["imageBase64"]?.GetValue<string>() ?? p?["image"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(imageBase64))
            throw new ArgumentException("imageBase64 is required");

        using var bitmap = await DecodeSoftwareBitmapAsync(imageBase64);
        
        var includeCount = (p?["includePoints"] as JsonArray)?.Count ?? 0;
        var excludeCount = (p?["excludePoints"] as JsonArray)?.Count ?? 0;
        _log?.LogInformation("AiRemoveBackground called, includePoints={IncludeCount}, excludePoints={ExcludeCount}", includeCount, excludeCount);

        var readyBefore = "Unknown";
        var readyAfter = "Unknown";
        string? ensureStatus = null;
        string? ensureError = null;
        string? ensureExtendedError = null;

        // Create extractor and run EnsureReadyAsync when available
        Exception? createError = null;
        ImageObjectExtractor? extractor = null;

        try
        {
                extractor = await ImageObjectExtractor.CreateWithSoftwareBitmapAsync(bitmap);
                readyBefore = GetReadyState(extractor);
            _log?.LogInformation("ImageObjectExtractor created successfully. readyBefore={ReadyBefore}", readyBefore);

            var ensureResult = await EnsureReadySafeAsync(extractor);
            readyAfter = ensureResult.ReadyAfter;
            ensureStatus = ensureResult.Status;
            ensureError = ensureResult.Error;
            ensureExtendedError = ensureResult.ExtendedError;
        }
        catch (Exception ex)
        {
            createError = ex;
            _log?.LogError(ex, "CreateWithSoftwareBitmapAsync failed");
        }

        if (extractor is null)
        {
            _log?.LogError("ImageObjectExtractor create failed: {CreateError}", createError?.Message);
            var result = new RemoveBackgroundResult(MaskBase64: null);
            return JsonSerializer.SerializeToNode(result, WebJson);
        }

        var includePoints = ParsePoints(p?["includePoints"] as JsonArray);
        var excludePoints = ParsePoints(p?["excludePoints"] as JsonArray);

        using (extractor)
        {
            var hint = new ImageObjectExtractorHint(includeRects: null, includePoints: includePoints, excludePoints: excludePoints);

            using var mask = extractor.GetSoftwareBitmapObjectMask(hint);
            var maskBase64 = await EncodeSoftwareBitmapToBase64PngAsync(mask);

            var result = new RemoveBackgroundResult(MaskBase64: maskBase64);

            _log?.LogInformation("Background removed. size={Width}x{Height} readyBefore={ReadyBefore} readyAfter={ReadyAfter} status={Status}", mask.PixelWidth, mask.PixelHeight, readyBefore, readyAfter, ensureStatus ?? string.Empty);

            return JsonSerializer.SerializeToNode(result, WebJson);
        }
    }

    public string Method => BridgeMethods.AiRemoveBackground;

    private string GetReadyState(ImageObjectExtractor extractor)
    {
        try
        {
            var readyStateProperty = extractor.GetType().GetProperty("ReadyState");
            var value = readyStateProperty?.GetValue(extractor);
            return value?.ToString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            _log?.LogDebug(ex, "Failed to read ReadyState");
            return "Unknown";
        }
    }

    private async Task<EnsureState> EnsureReadySafeAsync(ImageObjectExtractor extractor)
    {
        var readyAfter = GetReadyState(extractor);
        string? status = null;
        string? error = null;
        string? extendedError = null;

        var ensureMethod = extractor.GetType().GetMethod("EnsureReadyAsync", Type.EmptyTypes);
        if (ensureMethod is null)
        {
            _log?.LogInformation("EnsureReadyAsync not available; skipping readiness check");
            return new EnsureState(readyAfter, status, error, extendedError);
        }

        try
        {
            var ensureTaskObj = ensureMethod.Invoke(extractor, Array.Empty<object?>());

            var ensureResult = await AwaitMaybeAsync(ensureTaskObj);

            if (ensureResult is not null)
            {
                status = ensureResult.GetType().GetProperty("Status")?.GetValue(ensureResult)?.ToString();
                error = FormatError(ensureResult.GetType().GetProperty("Error")?.GetValue(ensureResult));
                extendedError = FormatError(ensureResult.GetType().GetProperty("ExtendedError")?.GetValue(ensureResult));
            }

            readyAfter = GetReadyState(extractor);
        }
            catch (Exception ex)
            {
                status ??= "Failure";
                error ??= ex.Message;
                readyAfter = GetReadyState(extractor);
                _log?.LogError(ex, "EnsureReadyAsync failed");
            }

        return new EnsureState(readyAfter, status, error, extendedError);
    }

    private async Task<object?> AwaitMaybeAsync(object? candidate)
    {
        switch (candidate)
        {
            case null:
                _log?.LogWarning("EnsureReadyAsync returned null; skipping readiness check");
                return null;
            case Task task:
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                    return taskType.GetProperty("Result")?.GetValue(task);
                return null;
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return null;
        }

        // Handle WinRT IAsyncAction / IAsyncOperation`1 via WindowsRuntimeSystemExtensions.AsTask
        var type = candidate.GetType();

        var isAsyncAction = type.GetInterfaces().Any(i => i.FullName == "Windows.Foundation.IAsyncAction");
        if (isAsyncAction)
        {
            var task = WindowsRuntimeSystemExtensions.AsTask((dynamic)candidate);
            await task.ConfigureAwait(false);
            return null;
        }

        var asyncOpInterface = type.GetInterfaces().FirstOrDefault(i => i.FullName?.StartsWith("Windows.Foundation.IAsyncOperation`1") == true);
        if (asyncOpInterface is not null)
        {
            var task = WindowsRuntimeSystemExtensions.AsTask((dynamic)candidate);
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }

        _log?.LogWarning("EnsureReadyAsync returned unsupported type {Type}; skipping readiness check", type.FullName);
        return null;
    }

    private string? FormatError(object? value)
    {
        return value switch
        {
            null => null,
            Exception ex => ex.Message,
            _ => value.ToString(),
        };
    }

    private sealed record EnsureState(string ReadyAfter, string? Status, string? Error, string? ExtendedError);

    private static async Task<SoftwareBitmap> DecodeSoftwareBitmapAsync(string dataUrlOrBase64)
    {
        var base64 = SanitizeBase64(dataUrlOrBase64);
        var bytes = Convert.FromBase64String(base64);

        using InMemoryRandomAccessStream stream = new();
        await stream.WriteAsync(bytes.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync();
    }

    private static async Task<string> EncodeSoftwareBitmapToBase64PngAsync(SoftwareBitmap bitmap)
    {
        using var converted = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        using InMemoryRandomAccessStream stream = new();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(converted);
        await encoder.FlushAsync();

        stream.Seek(0);
        var bytes = new byte[stream.Size];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private static string SanitizeBase64(string value)
    {
        var trimmed = value.Trim();
        var commaIndex = trimmed.IndexOf(',');
        return commaIndex >= 0 ? trimmed[(commaIndex + 1)..] : trimmed;
    }

    private static IList<PointInt32>? ParsePoints(JsonArray? arr)
    {
        if (arr is null)
            return null;

        var list = new List<PointInt32>();

        foreach (var node in arr)
        {
            var p = ParsePoint(node);
            if (p is not null)
                list.Add(p.Value);
        }

        return list.Count > 0 ? list : null;
    }

    private static PointInt32? ParsePoint(JsonNode? node)
    {
        if (node is not JsonObject obj)
            return null;

        if (obj["x"] is null || obj["y"] is null)
            return null;

        return new PointInt32(obj["x"]!.GetValue<int>(), obj["y"]!.GetValue<int>());
    }
}
