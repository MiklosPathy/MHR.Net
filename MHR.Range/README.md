# MHR.Range

Determines safe parameter ranges for each of the 321 MHR model parameters by detecting triangle inversion (normal flipping).

## How it works

1. Generates the baseline mesh with all parameters at zero
2. Computes per-triangle normals for the baseline
3. For each parameter, binary searches in both positive and negative directions
4. At each step, generates the mesh with the test value and checks how many triangle normals have flipped (dot product with baseline normal < 0)
5. Stops when the inversion fraction exceeds the threshold (0.1% of triangles)
6. The result is an asymmetric min/max range per parameter

## Configuration

| Constant | Value | Description |
|---|---|---|
| `InversionThreshold` | 0.001 (0.1%) | Fraction of flipped triangles that marks "too far" |
| `BinarySearchSteps` | 20 | Precision of the binary search |
| `IdentityMaxSearch` | 5.0 | Maximum search bound for identity params |
| `PoseMaxSearch` | 3.14 | Maximum search bound for pose params (~pi) |
| `ExpressionMaxSearch` | 2.0 | Maximum search bound for expression params |

## Output

- `param_ranges.json` - Full results with per-parameter min/max
- `param_ranges.csv` - Same data in CSV format

These results are used to update the `MhrParameters.cs` range values in `MHR.Net`.
