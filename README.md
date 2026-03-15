# MHR.Net Solution

C# port of Meta's MHR (Momentum Human Rig) parametric 3D human body model with 321 parameters (45 identity, 204 pose, 72 expression). Uses TorchSharp for inference and DirectX 12 for real-time rendering.

This solution provides tools for interactive exploration, parameter range analysis, visual sweeps, and automated parameter name validation using vision models.

## Projects

| Project | Description |
|---|---|
| [MHR](MHR/) | Interactive UI app with per-parameter sliders and real-time D3D12 rendering |
| [MHR.Net](MHR.Net/) | Shared library: model loading, inference, `MhrParameters` (single source of truth for all 321 parameter definitions) |
| [MHR.Range](MHR.Range/) | Determines safe parameter ranges via triangle inversion detection |
| [MHR.Sweep](MHR.Sweep/) | Renders each parameter's effect from multiple angles, generates difference heatmaps |
| [MHR.Identify](MHR.Identify/) | Validates parameter names using local vision models (llama.cpp) |

See each project's README.md for detailed documentation.

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

---
Generated with [Claude Code](https://claude.ai/claude-code) (Anthropic)
