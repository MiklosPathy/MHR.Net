// MHR Parameter Identifier
// Reads sweep output images, sends them to a local vision model (llama.cpp),
// and collects parameter name suggestions.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHR.Identify;

class ModelProfile
{
    public string Name { get; init; } = "";
    public string ModelId { get; init; } = "";
    public bool UseSystemRole { get; init; } = true;
    public int MaxTokens { get; init; } = 200;
    public float Temperature { get; init; } = 0.1f;
    public string AnglePrompt { get; init; } = "";
    public string SummaryPrompt { get; init; } = "";
}

class Program
{
    static readonly ModelProfile Gemma3 = new()
    {
        Name = "gemma3",
        ModelId = "gemma3",
        UseSystemRole = true,
        MaxTokens = 200,
        Temperature = 0.1f,
        AnglePrompt = """
            You are validating parameter names in a 3D human body model. You will see images from a single viewpoint showing
            the effect of changing a single parameter, along with its current name.

            The images you receive:
            - "baseline" shows the neutral/default pose
            - "neg_half" / "neg_full" show the parameter decreased to 50% and 100% of its negative range
            - "pos_half" / "pos_full" show the parameter increased to 50% and 100% of its positive range
            - "heatmap" highlights in red/yellow where pixels changed most

            Your task: evaluate whether the current parameter name accurately describes the visible effect from this viewpoint.

            Reply with ONLY a JSON object in this exact format:
            {"accurate": true/false, "better_name": "suggested name if inaccurate", "body_part": "affected body part", "motion": "what change is visible", "confidence": "high/medium/low"}

            If the name is accurate, set "accurate": true and leave "better_name" empty.
            If the name is wrong or misleading, set "accurate": false and suggest a better name.
            """,
        SummaryPrompt = """
            You are summarizing multiple viewpoint analyses of a single parameter in a 3D human body model.
            You will receive per-viewpoint judgements (front, left side, right side, back) about whether a parameter name is accurate.

            Combine these into a single final verdict. Consider all viewpoints - if any viewpoint clearly shows the name is wrong, the final verdict should reflect that.

            Reply with ONLY a JSON object in this exact format:
            {"accurate": true/false, "better_name": "suggested name if inaccurate", "body_part": "affected body part", "motion": "what change is visible", "confidence": "high/medium/low"}
            """
    };

    static readonly ModelProfile Glm4 = new()
    {
        Name = "glm4",
        ModelId = "glm-4v",
        UseSystemRole = false,  // system prompt merged into user message
        MaxTokens = 512,
        Temperature = 0.3f,
        AnglePrompt = """
            You are an expert at analyzing 3D human body models. You will see images from a single viewpoint showing
            the effect of changing a single parameter, along with its current name.

            The images you receive:
            - "baseline" shows the neutral/default pose
            - "neg_half" / "neg_full" show the parameter decreased to 50% and 100% of its negative range
            - "pos_half" / "pos_full" show the parameter increased to 50% and 100% of its positive range
            - "heatmap" highlights in red/yellow where pixels changed most

            Your task: evaluate whether the current parameter name accurately describes the visible effect from this viewpoint.

            You MUST reply with ONLY a JSON object, nothing else. Use this exact format:
            {"accurate": true/false, "better_name": "suggested name if inaccurate", "body_part": "affected body part", "motion": "what change is visible", "confidence": "high/medium/low"}

            If the name is accurate, set "accurate": true and "better_name": "".
            If the name is wrong or misleading, set "accurate": false and suggest a better name in "better_name".
            Do not output any text before or after the JSON.
            """,
        SummaryPrompt = """
            You are summarizing multiple viewpoint analyses of a single parameter in a 3D human body model.
            You will receive per-viewpoint judgements (front, left side, right side, back) about whether a parameter name is accurate.

            Combine these into a single final verdict. Consider all viewpoints - if any viewpoint clearly shows the name is wrong, the final verdict should reflect that.

            You MUST reply with ONLY a JSON object, nothing else. Use this exact format:
            {"accurate": true/false, "better_name": "suggested name if inaccurate", "body_part": "affected body part", "motion": "what change is visible", "confidence": "high/medium/low"}
            Do not output any text before or after the JSON.
            """
    };

    static readonly ModelProfile ActiveModel = Glm4;

    static string[] Servers = [];

    static async Task Main(string[] args)
    {
        // Load server list from config
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servers.json");
        if (!File.Exists(configPath))
        {
            // Create default config
            var defaultServers = new[] { "http://127.0.0.1:8080" };
            File.WriteAllText(configPath, JsonSerializer.Serialize(defaultServers, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Created default config: {configPath}");
        }
        Servers = JsonSerializer.Deserialize<string[]>(File.ReadAllText(configPath)) ?? [];
        if (Servers.Length == 0)
        {
            Console.Error.WriteLine($"No servers configured in {configPath}");
            return;
        }
        Console.WriteLine($"Loaded {Servers.Length} server(s) from {configPath}");

        var sweepDir = args.Length > 0 ? args[0] : FindSweepDir();
        if (sweepDir == null)
        {
            Console.Error.WriteLine("Usage: MHR.Identify <sweep_output_dir>");
            Console.Error.WriteLine("Or run from the MHR.Sweep output directory.");
            return;
        }

        Console.WriteLine($"Sweep directory: {sweepDir}");

        // Load metadata
        var metadataPath = Path.Combine(sweepDir, "sweep_metadata.json");
        if (!File.Exists(metadataPath))
        {
            Console.Error.WriteLine($"sweep_metadata.json not found in {sweepDir}");
            return;
        }

        var metadata = JsonSerializer.Deserialize<List<ParamSweepEntry>>(
            File.ReadAllText(metadataPath))!;

        Console.WriteLine($"Found {metadata.Count} parameters to identify.");

        // Check API availability - skip unavailable servers
        var httpClients = new HttpClient[Servers.Length];
        var availableServers = new List<int>();
        for (int s = 0; s < Servers.Length; s++)
        {
            httpClients[s] = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
            try
            {
                var health = await httpClients[s].GetAsync($"{Servers[s]}/health");
                Console.WriteLine($"Server {Servers[s]}: {health.StatusCode}");
                availableServers.Add(s);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Server {Servers[s]}: OFFLINE ({ex.Message})");
            }
        }
        if (availableServers.Count == 0)
        {
            Console.Error.WriteLine("No servers available. Exiting.");
            return;
        }
        Console.WriteLine($"{availableServers.Count}/{Servers.Length} servers online. Model: {ActiveModel.Name}");

        // Load baseline images (shared across all params)
        var baselineImages = new Dictionary<string, string>(); // angle -> base64
        foreach (var angle in new[] { "front", "side_l", "side_r", "back" })
        {
            var path = Path.Combine(sweepDir, $"baseline_{angle}.png");
            if (File.Exists(path))
                baselineImages[angle] = Convert.ToBase64String(File.ReadAllBytes(path));
        }
        Console.WriteLine($"Loaded {baselineImages.Count} baseline images.");

        // Load previous results for resuming
        var outputPath = Path.Combine(sweepDir, "identify_results.json");
        var previousResults = new Dictionary<int, IdentifyResult>();
        if (File.Exists(outputPath))
        {
            try
            {
                var prev = JsonSerializer.Deserialize<List<IdentifyResult>>(
                    File.ReadAllText(outputPath));
                if (prev != null)
                {
                    foreach (var r in prev.Where(r => r.Error == null))
                        previousResults[r.ParamIndex] = r;
                }
                Console.WriteLine($"Loaded {previousResults.Count} previous results (skipping already completed).");
            }
            catch
            {
                Console.WriteLine("Could not load previous results, starting fresh.");
            }
        }

        // Filter to only unprocessed parameters
        var toProcess = metadata.Where(m => !previousResults.ContainsKey(m.ParamIndex)).ToList();
        Console.WriteLine($"{toProcess.Count} parameters remaining to process.");

        if (toProcess.Count == 0)
        {
            Console.WriteLine("All parameters already processed.");
            return;
        }

        // Process parameters in parallel across servers
        var results = new IdentifyResult[metadata.Count];
        // Pre-fill with previous results
        for (int i = 0; i < metadata.Count; i++)
        {
            if (previousResults.TryGetValue(metadata[i].ParamIndex, out var prev))
                results[i] = prev;
        }

        var serverSemaphores = Servers.Select(_ => new SemaphoreSlim(1, 1)).ToArray();
        var saveLock = new object();
        int completed = 0;
        var totalSw = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        var display = new StatusDisplay(Servers.Length);

        try
        {
            await Parallel.ForEachAsync(
                toProcess.Select(p => (Param: p, Idx: metadata.IndexOf(p))),
                new ParallelOptions { MaxDegreeOfParallelism = Servers.Length, CancellationToken = cts.Token },
                async (item, ct) =>
                {
                    var (param, i) = item;
                    var failedServers = new HashSet<int>();
                    var sw = Stopwatch.StartNew();

                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Acquire a free server (skip failed ones)
                        int serverIdx = -1;
                        while (serverIdx < 0)
                        {
                            for (int s = 0; s < serverSemaphores.Length; s++)
                            {
                                if (!failedServers.Contains(s) && serverSemaphores[s].Wait(0))
                                {
                                    serverIdx = s;
                                    break;
                                }
                            }
                            if (serverIdx < 0)
                            {
                                if (failedServers.Count >= Servers.Length)
                                    break; // All servers failed
                                await Task.Delay(50, ct);
                            }
                        }

                        // All servers exhausted - abort everything
                        if (serverIdx < 0)
                        {
                            display.PrintResult($"All servers failed on P{param.ParamIndex} ({param.CurrentName}). Stopping...");
                            await cts.CancelAsync();
                            ct.ThrowIfCancellationRequested();
                            return;
                        }

                        display.UpdateSlot(serverIdx, $"P{param.ParamIndex:D3} {param.CurrentName,-20} connecting...  {sw.Elapsed.TotalSeconds:F0}s");

                        try
                        {
                            var result = await IdentifyParameter(
                                httpClients[serverIdx], Servers[serverIdx], sweepDir, param, baselineImages,
                                (step) => display.UpdateSlot(serverIdx, $"P{param.ParamIndex:D3} {param.CurrentName,-20} {step,-12} {sw.Elapsed.TotalSeconds:F0}s"));
                            result.ParamIndex = param.ParamIndex;
                            result.CurrentName = param.CurrentName;
                            result.Category = param.Category;
                            results[i] = result;
                            sw.Stop();

                            var done = Interlocked.Increment(ref completed);
                            var total = toProcess.Count;
                            var elapsed = totalSw.Elapsed;
                            var avg = elapsed / done;
                            var remaining = avg * (total - done);

                            var resultLine = result.Accurate
                                ? $"[{done}/{total}] P{param.ParamIndex:D3} {param.CurrentName,-20} -> OK ({result.Confidence}) [{sw.Elapsed.TotalSeconds:F1}s, srv {serverIdx}] ETA: {remaining:hh\\:mm\\:ss}"
                                : $"[{done}/{total}] P{param.ParamIndex:D3} {param.CurrentName,-20} -> \"{result.BetterName}\" ({result.Confidence}) [{sw.Elapsed.TotalSeconds:F1}s, srv {serverIdx}] ETA: {remaining:hh\\:mm\\:ss}";

                            display.ClearSlot(serverIdx);
                            display.PrintResult(resultLine);

                            serverSemaphores[serverIdx].Release();
                            break;
                        }
                        catch (HttpRequestException ex)
                        {
                            serverSemaphores[serverIdx].Release();
                            failedServers.Add(serverIdx);
                            display.ClearSlot(serverIdx);
                            display.PrintResult($"  P{param.ParamIndex:D3}: srv {serverIdx} ({Servers[serverIdx]}) failed: {ex.Message} - retrying...");
                        }
                        catch (TaskCanceledException) when (ct.IsCancellationRequested)
                        {
                            serverSemaphores[serverIdx].Release();
                            display.ClearSlot(serverIdx);
                            throw;
                        }
                        catch (TaskCanceledException ex)
                        {
                            serverSemaphores[serverIdx].Release();
                            failedServers.Add(serverIdx);
                            display.ClearSlot(serverIdx);
                            display.PrintResult($"  P{param.ParamIndex:D3}: srv {serverIdx} ({Servers[serverIdx]}) timeout: {ex.Message} - retrying...");
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            serverSemaphores[serverIdx].Release();
                            var done = Interlocked.Increment(ref completed);
                            display.ClearSlot(serverIdx);
                            display.PrintResult($"[{done}/{toProcess.Count}] P{param.ParamIndex:D3} {param.CurrentName,-20} ERROR: {ex.Message} [{sw.Elapsed.TotalSeconds:F1}s, srv {serverIdx}]");
                            results[i] = new IdentifyResult
                            {
                                ParamIndex = param.ParamIndex,
                                CurrentName = param.CurrentName,
                                Category = param.Category,
                                Error = ex.Message
                            };
                            break;
                        }
                    }

                    // Save incrementally (thread-safe)
                    lock (saveLock)
                    {
                        var partial = results.Where(r => r != null).OrderBy(r => r!.ParamIndex).ToList();
                        File.WriteAllText(outputPath, JsonSerializer.Serialize(partial,
                            new JsonSerializerOptions { WriteIndented = true }));
                    }
                });
        }
        catch (OperationCanceledException)
        {
            display.Finish();
            Console.WriteLine("Execution stopped due to server failures.");
            // Save what we have
            var partial = results.Where(r => r != null).OrderBy(r => r!.ParamIndex).ToList();
            File.WriteAllText(outputPath, JsonSerializer.Serialize(partial,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        totalSw.Stop();
        display.Finish();

        // Print summary
        var finalResults = results.Where(r => r != null).OrderBy(r => r!.ParamIndex).ToList();
        Console.WriteLine("\n=== RESULTS ===");
        Console.WriteLine($"{"Idx",-5} {"Category",-12} {"Current Name",-20} {"Status",-8} {"Better Name",-25} {"Confidence",-10}");
        Console.WriteLine(new string('-', 85));
        foreach (var r in finalResults)
        {
            var status = r!.Accurate ? "OK" : "RENAME";
            var better = r.Accurate ? "" : r.BetterName;
            Console.WriteLine($"{r.ParamIndex,-5} {r.Category,-12} {r.CurrentName,-20} {status,-8} {better,-25} {r.Confidence,-10}");
        }

        var processedCount = finalResults.Count(r => r!.Error == null);
        var totalCount = metadata.Count;
        Console.WriteLine($"\nCompleted: {processedCount}/{totalCount} ({totalCount - processedCount} remaining)");
        if (completed > 0)
            Console.WriteLine($"Total time: {totalSw.Elapsed:hh\\:mm\\:ss} ({totalSw.Elapsed.TotalSeconds / completed:F1}s avg per param, {availableServers.Count} servers)");
        Console.WriteLine($"Results saved to: {outputPath}");
        if (totalCount - processedCount > 0)
            Console.WriteLine("Run again to resume processing remaining parameters.");

        foreach (var c in httpClients) c.Dispose();
    }

    static readonly string[] Angles = ["front", "side_l", "side_r", "back"];
    static readonly string[] VariantOrder = ["neg_full", "neg_half", "pos_half", "pos_full"];

    static async Task<IdentifyResult> IdentifyParameter(
        HttpClient http,
        string serverUrl,
        string sweepDir,
        ParamSweepEntry param,
        Dictionary<string, string> baselineImages,
        Action<string>? onProgress = null)
    {
        var model = ActiveModel;

        // Step 1: Send one request per angle, collect per-angle results
        var angleResults = new Dictionary<string, string>();
        int step = 0;

        foreach (var angle in Angles)
        {
            step++;
            onProgress?.Invoke($"[{step}/5] {angle}");
            var content = new List<object>();

            // Baseline
            if (baselineImages.TryGetValue(angle, out var baselineB64))
            {
                content.Add(new { type = "text", text = $"[baseline]:" });
                content.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:image/png;base64,{baselineB64}" }
                });
            }

            // Variants
            foreach (var variant in VariantOrder)
            {
                var img = param.Images.FirstOrDefault(img =>
                    img.Variant == variant && img.Angle == angle);
                if (img != null)
                {
                    var imgPath = Path.Combine(sweepDir, img.FileName);
                    if (File.Exists(imgPath))
                    {
                        content.Add(new { type = "text", text = $"[{variant} (value: {img.Value:F2})]:" });
                        content.Add(new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(imgPath))}" }
                        });
                    }
                }
            }

            // Heatmap
            var heatImg = param.Images.FirstOrDefault(img =>
                img.Variant == "heatmap" && img.Angle == angle);
            if (heatImg != null)
            {
                var heatPath = Path.Combine(sweepDir, heatImg.FileName);
                if (File.Exists(heatPath))
                {
                    content.Add(new { type = "text", text = $"[heatmap (changed regions)]:" });
                    content.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(heatPath))}" }
                    });
                }
            }

            content.Add(new
            {
                type = "text",
                text = $"Viewpoint: {angle}. Parameter #{param.ParamIndex} in '{param.Category}' category. Current name: \"{param.CurrentName}\". Does this name accurately describe the visible effect?"
            });

            var angleResponse = await CallApi(http, serverUrl, model.AnglePrompt, content);
            angleResults[angle] = angleResponse;

            // Show per-angle result in status
            var angleResult = ParseModelResponse(angleResponse);
            var shortResult = angleResult.Accurate ? "OK" : angleResult.BetterName;
            onProgress?.Invoke($"[{step}/5] {angle} -> {shortResult}");
        }

        // Step 2: Send summary request (text only, no images)
        onProgress?.Invoke("[5/5] summary...");
        var summaryContent = new List<object>();
        foreach (var (angle, response) in angleResults)
        {
            summaryContent.Add(new
            {
                type = "text",
                text = $"[{angle} viewpoint result]: {response}"
            });
        }
        summaryContent.Add(new
        {
            type = "text",
            text = $"Parameter #{param.ParamIndex}, category: '{param.Category}', current name: \"{param.CurrentName}\". Combine the above viewpoint analyses into a single final verdict."
        });

        var summaryResponse = await CallApi(http, serverUrl, model.SummaryPrompt, summaryContent);

        var result = ParseModelResponse(summaryResponse);
        result.RawResponse = string.Join("\n---\n",
            angleResults.Select(kv => $"[{kv.Key}]: {kv.Value}")
                .Append($"[summary]: {summaryResponse}"));
        return result;
    }

    static async Task<string> CallApi(HttpClient http, string serverUrl, string systemPrompt, List<object> content)
    {
        var model = ActiveModel;
        object[] messages;

        if (model.UseSystemRole)
        {
            messages =
            [
                new { role = "system", content = (object)systemPrompt },
                new { role = "user", content = (object)content }
            ];
        }
        else
        {
            // Merge system prompt into user message for models that don't support system role
            var merged = new List<object>();
            merged.Add(new { type = "text", text = systemPrompt });
            merged.AddRange(content);
            messages =
            [
                new { role = "user", content = (object)merged }
            ];
        }

        var request = new
        {
            model = model.ModelId,
            messages,
            max_tokens = model.MaxTokens,
            temperature = model.Temperature
        };

        var response = await http.PostAsJsonAsync($"{serverUrl}/v1/chat/completions", request);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"API returned {response.StatusCode}: {responseText}");

        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText);
        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
    }

    static IdentifyResult ParseModelResponse(string response)
    {
        var result = new IdentifyResult { RawResponse = response };

        // Try to extract JSON from the response
        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonStr = response[jsonStart..(jsonEnd + 1)];
            try
            {
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (root.TryGetProperty("accurate", out var accurateProp))
                    result.Accurate = accurateProp.ValueKind == JsonValueKind.True;

                if (root.TryGetProperty("better_name", out var betterProp))
                    result.BetterName = betterProp.GetString() ?? "";

                if (root.TryGetProperty("body_part", out var bodyProp))
                    result.BodyPart = bodyProp.GetString() ?? "";

                if (root.TryGetProperty("motion", out var motionProp))
                    result.Motion = motionProp.GetString() ?? "";

                if (root.TryGetProperty("confidence", out var confProp))
                    result.Confidence = confProp.GetString() ?? "";
            }
            catch
            {
                result.BetterName = response.Trim();
            }
        }
        else
        {
            result.BetterName = response.Trim();
        }

        return result;
    }

    static string? FindSweepDir()
    {
        // Look for sweep_output relative to current dir or in MHR.Sweep build output
        var candidates = new[]
        {
            "sweep_output",
            Path.Combine("..", "MHR.Sweep", "bin", "Debug", "net10.0-windows", "sweep_output"),
            Path.Combine("..", "MHR.Sweep", "bin", "Release", "net10.0-windows", "sweep_output"),
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full) && File.Exists(Path.Combine(full, "sweep_metadata.json")))
                return full;
        }

        return null;
    }
}

// === Data types ===

record ParamSweepEntry
{
    [JsonPropertyName("ParamIndex")] public int ParamIndex { get; set; }
    [JsonPropertyName("CurrentName")] public string CurrentName { get; set; } = "";
    [JsonPropertyName("Category")] public string Category { get; set; } = "";
    [JsonPropertyName("Images")] public List<ImageEntry> Images { get; set; } = [];
}

record ImageEntry
{
    [JsonPropertyName("FileName")] public string FileName { get; set; } = "";
    [JsonPropertyName("Variant")] public string Variant { get; set; } = "";
    [JsonPropertyName("Value")] public float Value { get; set; }
    [JsonPropertyName("Angle")] public string Angle { get; set; } = "";
}

record IdentifyResult
{
    public int ParamIndex { get; set; }
    public string CurrentName { get; set; } = "";
    public string Category { get; set; } = "";
    public bool Accurate { get; set; }
    public string BetterName { get; set; } = "";
    public string BodyPart { get; set; } = "";
    public string Motion { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string RawResponse { get; set; } = "";
    public string? Error { get; set; }
}

// OpenAI-compatible response types
record ChatCompletionResponse
{
    [JsonPropertyName("choices")] public List<ChatChoice>? Choices { get; set; }
}

record ChatChoice
{
    [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
}

record ChatMessage
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}

/// <summary>
/// Interactive console display with fixed status lines at the bottom.
/// Result lines are printed above, status slots update in-place.
/// Uses \r and ANSI escape to avoid buffer size issues.
/// </summary>
class StatusDisplay
{
    private readonly object _lock = new();
    private readonly int _slotCount;
    private readonly string[] _slots;
    private bool _statusDrawn;
    private bool _finished;

    public StatusDisplay(int slotCount)
    {
        _slotCount = slotCount;
        _slots = new string[slotCount];
        DrawStatus();
    }

    public void UpdateSlot(int slot, string text)
    {
        lock (_lock)
        {
            if (_finished) return;
            _slots[slot] = text;
            RedrawStatus();
        }
    }

    public void ClearSlot(int slot)
    {
        lock (_lock)
        {
            if (_finished) return;
            _slots[slot] = "";
        }
    }

    public void PrintResult(string line)
    {
        lock (_lock)
        {
            if (_finished)
            {
                Console.WriteLine(line);
                return;
            }

            ClearStatusLines();
            Console.WriteLine(line);
            DrawStatus();
        }
    }

    public void Finish()
    {
        lock (_lock)
        {
            if (_finished) return;
            _finished = true;
            ClearStatusLines();
        }
    }

    private void DrawStatus()
    {
        for (int i = 0; i < _slotCount; i++)
        {
            var slotText = string.IsNullOrEmpty(_slots[i]) ? "idle" : _slots[i];
            Console.Write($"\r  [srv {i}] {slotText}");
            Console.Write("\x1b[K"); // clear to end of line
            if (i < _slotCount - 1)
                Console.WriteLine();
        }
        _statusDrawn = true;
    }

    private void RedrawStatus()
    {
        if (!_statusDrawn) return;
        // Move cursor up to first status line
        if (_slotCount > 1)
            Console.Write($"\x1b[{_slotCount - 1}A");
        Console.Write("\r");
        DrawStatus();
    }

    private void ClearStatusLines()
    {
        if (!_statusDrawn) return;
        // Move cursor up to first status line
        if (_slotCount > 1)
            Console.Write($"\x1b[{_slotCount - 1}A");
        // Clear each line
        for (int i = 0; i < _slotCount; i++)
        {
            Console.Write("\r\x1b[K");
            if (i < _slotCount - 1)
                Console.WriteLine();
        }
        // Move back up
        if (_slotCount > 1)
            Console.Write($"\x1b[{_slotCount - 1}A");
        Console.Write("\r");
        _statusDrawn = false;
    }
}
