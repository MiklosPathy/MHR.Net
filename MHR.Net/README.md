# MHR.Net

A C# port of Meta's [MHR (Meta Human Rig)](https://github.com/facebookresearch/MHR) parametric body model for AI-based 3D human mesh generation.

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

1. Download `assets.zip` from the [MHR GitHub Releases](https://github.com/facebookresearch/MHR/releases)

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
| `Forward(identity, pose, expression)` | Generate mesh from parameters |
| `ForwardNeutral()` | Generate mesh with zero parameters |
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
