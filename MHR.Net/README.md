# MHR.Net

A C# port of Meta's [MHR (Momentum Human Rig)](https://github.com/facebookresearch/MHR) parametric body model for AI-based 3D human mesh generation.

## Overview

MHR.Net provides a .NET interface to the MHR TorchScript model, enabling real-time generation of detailed 3D human body meshes with:

- **45 identity parameters** - Control body shape (20 body, 20 head, 5 hands)
- **204 pose parameters** - Full-body articulation and joint rotations
- **72 expression parameters** - Detailed facial animations

## Requirements

- .NET 10.0 or later
- TorchSharp with CUDA support (for GPU acceleration)
- MHR model assets (see below)

## Asset Setup

The library requires the official MHR assets from Meta's release.

1. Download `assets.zip` from the [MHR GitHub Releases](https://github.com/facebookresearch/MHR/releases) (latest: v1.0.1; identical to v1.0.0 apart from an added `LICENSE.txt`)

2. Extract the contents to an `Assets` folder in your application's output directory:
   ```
   YourApp/
   ├── YourApp.exe
   └── Assets/
       ├── mhr_model.pt          # TorchScript model (required)
       ├── lod0.fbx              # Mesh topology LOD 0 (highest detail)
       ├── lod1.fbx              # Mesh topology LOD 1
       ├── lod2.fbx              # Mesh topology LOD 2
       ├── lod3.fbx              # Mesh topology LOD 3
       ├── lod4.fbx              # Mesh topology LOD 4
       ├── lod5.fbx              # Mesh topology LOD 5
       ├── lod6.fbx              # Mesh topology LOD 6 (lowest detail)
       └── ...                   # Other asset files
   ```

3. The `mhr_model.pt` TorchScript model is fixed at LOD1 resolution per MHR documentation.

## Usage

### Basic Example

```csharp
using MHR.Net;
using TorchSharp;

// Load the model (uses CUDA if available)
using var model = MhrModel.Load(
    device: torch.cuda.is_available() ? torch.CUDA : torch.CPU,
    lod: MhrLod.LOD1);

// Create parameter tensors
var identity = torch.zeros(model.NumIdentityParams);      // 45 params
var pose = torch.zeros(model.NumModelParams);             // 204 params
var expression = torch.zeros(model.NumExpressionParams);  // 72 params

// Generate mesh
var output = model.Forward(identity, pose, expression);

// Convert to vertex array (positions + normals)
MhrVertex[] vertices = model.ToVertexArray(output);

// Get triangle indices for rendering
uint[] indices = model.Indices ?? model.GenerateFallbackIndices(vertices.Length);

// Clean up tensors
output.Vertices.Dispose();
output.SkeletonState.Dispose();
```

### Custom Asset Folder

```csharp
var model = MhrModel.Load(
    assetFolder: @"C:\Path\To\Assets");
```

### Neutral Pose

```csharp
// Generate mesh with default (zero) parameters
var output = model.ForwardNeutral();
```

### Setting Parameters by Name

`MhrParameters.All` holds all 321 parameter definitions (global indices: identity 0-44, pose 45-248, expression 249-320).

```csharp
var pose = torch.zeros(204);
pose[MhrParameters.IndexOf("l_elbow_bend") - MhrParameters.PoseOffset] = 1.2f;

var expression = torch.zeros(72);
expression[MhrParameters.IndexOf("jawDrop") - MhrParameters.ExpressionOffset] = 1.0f;
```

### Computing Gradients

The model supports autograd (e.g. for fitting). Set `requires_grad` on the inputs to optimize and pass `trackGradients: true`:

```csharp
var pose = torch.zeros(204).requires_grad_();
var output = model.Forward(identity, pose, expression, trackGradients: true);
var loss = output.Vertices.square().mean();
loss.backward();
```

## Parameters

| Group | Count | Names | Typical range |
|---|---|---|---|
| Identity (`identity_coeffs`) | 45 | No official names: first 20 body shape, next 20 head, last 5 hands. Labels in `MhrParameters` are heuristic. | -3 .. +3 (zero-mean, unit variance) |
| Pose (`model_parameters`) | 204 | Official pymomentum names (e.g. `root_tx`, `spine_bend0`, `r_elbow_bend`, `l_index2_rz`, `scale_uplegs`), from `compact_v6_1.model` | Official per-joint limits |
| Expression (`face_expr_coeffs`) | 72 | Official FACS-based semantic names (e.g. `jawDrop`, `lipCornerPuller_L`, `eyesClosed_R`), from upstream `FACE_EXPRESSION_NAMES` | -1 .. +1 |

Notes:

- **Pose**: rotations are in radians, `root_t*` is in units of 10 cm. `*_flexible` parameters and some others (e.g. `r_clavicle_rx`, `r_foot_lean1`) have a `[0, 0]` official limit, meaning they are locked in the default rig. Official limits are in `MhrParamDef.LimitMin/LimitMax`, and `MhrModel.GetParameterLimits()` reads them from the TorchScript model.
- **Expression**: artist-sculpted, sparse semantic blendshapes following FACS. They are not PCA components and not a one-to-one list of FACS Action Units. Expression index `i` corresponds to FBX shape `shape_{45+i}`. `_L`/`_R` refer to the character's left/right; lip-quadrant suffixes add `T` (top) / `B` (bottom). See upstream [docs/face-expressions.md](https://github.com/facebookresearch/MHR/blob/main/docs/face-expressions.md).
- **Coordinate system**: right-handed, +X = character left, +Y = up, +Z = character forward. The model outputs centimeters; `ToVertexArray` scales to meters by default.

## API Reference

### MhrModel

| Property | Type | Description |
|----------|------|-------------|
| `NumIdentityParams` | int | Number of identity parameters (45) |
| `NumModelParams` | int | Number of pose parameters (204) |
| `NumExpressionParams` | int | Number of expression parameters (72) |
| `NumVertices` | int | Vertex count for current LOD |
| `Indices` | uint[]? | Triangle indices from FBX |
| `Lod` | MhrLod | Current level of detail |

| Method | Description |
|--------|-------------|
| `Load(device, lod, assetFolder)` | Load model from assets |
| `Forward(identity, pose, expression, applyCorrectivees, trackGradients)` | Generate mesh from parameters |
| `ForwardNeutral()` | Generate mesh with zero parameters |
| `GetParameterLimits()` | Official limits from the model: [249, 2], pose rows 0-203, identity rows 204-248 |
| `ToVertexArray(output, scale)` | Convert output to vertex array |
| `FindMatchingIndices(vertexCount)` | Find FBX indices matching vertex count |
| `GenerateFallbackIndices(vertexCount)` | Generate simple triangle indices |

### MhrLod

| Value | Description |
|-------|-------------|
| `LOD0` | Highest detail (~200k vertices) |
| `LOD1` | High detail (~50k vertices) - TorchScript model default |
| `LOD2` | Medium-high detail |
| `LOD3` | Medium detail |
| `LOD4` | Medium-low detail |
| `LOD5` | Low detail |
| `LOD6` | Lowest detail (~3k vertices) |

### MhrSegmentation

20-part body segmentation of the (LOD1) mesh from the skinning weights, ported from upstream `tools/mhr_create_segmentation`.

```csharp
var seg = MhrSegmentation.Create(model);
int part = seg.VertexPart[vertexIndex];          // e.g. "l_lowarm" = seg.Parts[part]
uint[][] perPart = seg.SplitIndicesByPart(model.Indices!);
var effect = seg.MeasureEffect(baselineXyz, [deformedXyz]);  // per-part deformation/motion share
```

| Member | Description |
|--------|-------------|
| `JointNames` | The 127 skeleton joint names (skinning index order) |
| `JointGroupKey(name)` | Joint name → part key |
| `Parts`, `PartDisplayNames`, `PartColors` | Part keys, readable names, tab20 colors |
| `VertexPart`, `PartWeights` | Hard per-vertex part index, soft per-part weights |
| `SplitIndicesByPart(indices)` | Triangle index list per part |
| `MeasureEffect(baseline, variants)` | Where the shape changes (edge-length change) and what moves, per part |

`MhrModel.GetSkinningWeights()` returns the raw skinning weights (`Index`, `Weight`: [vertices, 8]).

### MhrVertex

```csharp
public struct MhrVertex
{
    public Vector3 Position;  // World space position
    public Vector3 Normal;    // Unit normal vector
}
```

## License

This port follows the same license as the original MHR library. See the [original repository](https://github.com/facebookresearch/MHR) for license details.

## Acknowledgments

- [Meta Research](https://github.com/facebookresearch) for the original MHR model
- [TorchSharp](https://github.com/dotnet/TorchSharp) for .NET PyTorch bindings
- C# port by [Claude](https://claude.ai) (Anthropic)
