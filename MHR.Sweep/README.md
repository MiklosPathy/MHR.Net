# MHR.Sweep

Renders the MHR model with each parameter varied individually, capturing images from multiple viewpoints for visual analysis.

## How it works

1. Renders baseline images (all params at zero) from 4 camera angles: front, left side, right side, back
2. For each of the 321 parameters:
   - Sets the parameter to 4 variant values: negative full, negative half, positive half, positive full (using ranges from `MhrParameters`)
   - Renders from all 4 angles for each variant (16 images per parameter)
   - Generates a per-angle heatmap showing pixel differences between baseline and all variants combined
3. Saves all images as PNG and a `sweep_metadata.json` describing the output

## Output

All files are saved to `sweep_output/` in the build directory:

- `baseline_{angle}.png` - Baseline renders (4 images)
- `p{NNN}_{variant}_{angle}.png` - Parameter variant renders (16 per parameter)
- `p{NNN}_heatmap_{angle}.png` - Difference heatmaps (4 per parameter)
- `sweep_metadata.json` - Index of all images with parameter info, variant values, and angles

The heatmap uses a black -> red -> yellow -> white color scale, computed using parallel `LockBits` pixel processing.

## Camera setup

| Angle | Description |
|---|---|
| `front` | Front view (Y rotation = pi) |
| `side_l` | Left side (Y rotation = pi + pi/2) |
| `side_r` | Right side (Y rotation = pi - pi/2) |
| `back` | Back view (Y rotation = 0) |

## Usage

The output is consumed by **MHR.Identify** for automated parameter name validation.
