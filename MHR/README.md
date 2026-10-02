# MHR

Interactive WinForms application for real-time exploration of the MHR parametric body model with DirectX 12 rendering.

## Features

- **Tabbed parameter panels** with collapsible groups for Identity (45), Pose (204), and Expression (72) parameters
- **Per-parameter sliders** displaying actual mapped values (not raw 0-1), using asymmetric ranges from `MhrParameters`
- **Real-time D3D12 rendering** with mouse orbit (drag to rotate), scroll wheel zoom, and arrow keys for camera height
- **GPU device selector** for switching between available adapters
- **Body part segments**: "Show body part segments" colors the mesh by the 20 skinning-based body parts (see MHR.Segment)
- **FPS and vertex count** display

## Controls

| Input | Action |
|---|---|
| Left mouse drag | Orbit camera |
| Scroll wheel | Zoom in/out |
| Up/Down arrows | Adjust camera height |
| R | Reset camera to default view |

## UI Layout

- Left side: parameter panel with tabs (Identity / Pose / Expression), collapsible groups, sliders, and reset button
- Right side: D3D12 viewport with FPS counter, vertex count, and GPU selector

## Parameter mapping

Sliders internally operate on 0-1 range. The value is mapped to each parameter's actual range at display and inference time:

```
actualValue = RangeMin + sliderValue * (RangeMax - RangeMin)
```

Ranges are defined per-parameter in `MhrParameters.All` and are asymmetric (e.g. `-0.5 .. 2.0` for `r_elbow_bend`). Pose sliders use the official joint limits where the model defines one; other ranges come from MHR.Range.

Slider names are the official MHR names: pymomentum parameter names for pose (e.g. `spine_bend0`, `l_knee_bend`) and FACS-based semantic names for expressions (e.g. `jawDrop`, `lipCornerPuller_L`). `_L`/`_R` and `l_`/`r_` refer to the character's left/right.

---
Generated with [Claude Code](https://claude.ai/claude-code) (Anthropic)
