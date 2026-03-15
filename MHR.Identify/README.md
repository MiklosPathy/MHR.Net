# MHR.Identify

Validates MHR parameter names by sending sweep images to local vision models (via llama.cpp OpenAI-compatible API) and collecting structured judgements.

## How it works

1. Loads sweep output from **MHR.Sweep** (`sweep_metadata.json` + images)
2. For each parameter, sends images to a vision model in 5 sequential API calls:
   - 4 per-angle requests (front, left, right, back) - each with 6 images: baseline + 4 variants + heatmap
   - 1 summary request (text only) that combines the 4 angle results into a final verdict
3. The model evaluates whether the current parameter name matches the visible effect
4. Results are saved incrementally to `identify_results.json`

## Features

- **Multi-server parallel processing**: Distributes work across multiple llama.cpp instances
- **Model profiles**: Configurable per-model settings (system role handling, temperature, max tokens, prompts)
- **Retry with failover**: On server error/timeout, retries on a different server; stops if all servers fail
- **Resume support**: Loads previous results on startup, skips already-completed parameters
- **Interactive status display**: Per-server progress lines with ANSI escape codes, showing current angle and elapsed time

## Configuration

### Active model

```csharp
static readonly ModelProfile ActiveModel = Gemma3;  // or Glm4
```

### Servers

Server URLs are configured in `servers.json` (copied to output directory on build):

```json
[
  "http://192.168.1.2:8080",
  "http://192.168.1.3:8080",
  "http://192.168.1.4:8080"
]
```

If `servers.json` is missing at runtime, a default one is created with `http://127.0.0.1:8080`.

### Model profiles

| Setting | Gemma3 | GLM4 |
|---|---|---|
| `UseSystemRole` | true | false (merged into user message) |
| `MaxTokens` | 200 | 512 |
| `Temperature` | 0.1 | 0.3 |

## Output

- `identify_results.json` - Array of results per parameter:
  - `Accurate` (bool) - Whether the current name is correct
  - `BetterName` - Suggested alternative if inaccurate
  - `BodyPart` - Affected body region
  - `Motion` - Description of visible change
  - `Confidence` - high / medium / low
  - `RawResponse` - Full per-angle + summary responses

## Usage

```
MHR.Identify [sweep_output_dir]
```

If no directory is given, it searches for `sweep_output/` in common build output locations.

## Pipeline

```
MHR.Range  -->  MhrParameters.cs (ranges)
                      |
                      v
               MHR.Sweep (images)
                      |
                      v
              MHR.Identify (name validation)
```
