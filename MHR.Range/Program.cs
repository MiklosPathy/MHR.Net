// MHR Parameter Range Finder
// Finds parameter limits by detecting triangle inversion (normal flipping).
// Binary searches each parameter in +/- directions until mesh quality degrades.

using System.Globalization;
using MHR.Net;
using System.Text.Json;
using TorchSharp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

const int IdentityParamCount = MhrParameters.IdentityCount;
const int PoseParamCount = MhrParameters.PoseCount;
const int ExpressionParamCount = MhrParameters.ExpressionCount;
const int TotalParamCount = MhrParameters.TotalCount;

// When this fraction of triangles have flipped normals, we consider the parameter "too far"
const float InversionThreshold = 0.001f; // 0.1% of triangles
const int BinarySearchSteps = 20;

// Search bounds per category
const float IdentityMaxSearch = 5f;
const float PoseMaxSearch = 3.14f;    // ~pi
const float ExpressionMaxSearch = 2f;

Console.WriteLine("Loading MHR model...");

var assetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
using var mhrModel = MhrModel.Load(
    device: torch.cuda.is_available() ? torch.CUDA : torch.CPU,
    lod: MhrLod.LOD1,
    assetFolder: assetFolder);

Console.WriteLine($"Model loaded. Vertices: {mhrModel.NumVertices}");

// Get indices (triangle list)
uint[]? indices = mhrModel.Indices;
indices ??= mhrModel.FindMatchingIndices(mhrModel.NumVertices);
if (indices == null || indices.Length < 3)
{
    Console.WriteLine("ERROR: No triangle indices available. Cannot compute normals.");
    return;
}
int triCount = indices.Length / 3;
Console.WriteLine($"Triangles: {triCount}");

// Generate baseline vertices and compute baseline normals
var baselineVerts = GetVertices(mhrModel);
var baselineNormals = ComputeTriangleNormals(baselineVerts, indices);
Console.WriteLine($"Baseline generated. {baselineVerts.Length / 3} vertices, {triCount} triangles.");

var results = new List<ParamRangeResult>();

Console.WriteLine($"\nSearching inversion limits for {TotalParamCount} parameters...\n");
Console.WriteLine(string.Format("{0,-5} {1,-25} {2,-12} {3,-16} {4,10} {5,10} {6,10}", "Idx", "Name", "Category", "Group", "Min", "Max", "Flipped%"));
Console.WriteLine(new string('-', 100));

for (int paramIdx = 0; paramIdx < TotalParamCount; paramIdx++)
{
    var paramDef = MhrParameters.All[paramIdx];
    float maxSearch = GetMaxSearch(paramIdx);

    // Binary search positive direction
    float posLimit = BinarySearchLimit(mhrModel, baselineNormals, indices, paramIdx, maxSearch);
    // Binary search negative direction
    float negLimit = BinarySearchLimit(mhrModel, baselineNormals, indices, paramIdx, -maxSearch);

    // Measure flipped % at the found limits (for reporting)
    float posFlipped = MeasureInversion(mhrModel, baselineNormals, indices, paramIdx, posLimit);
    float negFlipped = MeasureInversion(mhrModel, baselineNormals, indices, paramIdx, negLimit);
    float reportFlipped = MathF.Max(posFlipped, negFlipped);

    var result = new ParamRangeResult
    {
        ParamIndex = paramIdx,
        Name = paramDef.Name,
        Category = paramDef.Category,
        Group = paramDef.Group,
        SuggestedMin = negLimit,
        SuggestedMax = posLimit,
    };

    results.Add(result);

    Console.WriteLine(string.Format("{0,-5} {1,-25} {2,-12} {3,-16} {4,10:F4} {5,10:F4} {6,9:F3}%",
        paramIdx, paramDef.Name, paramDef.Category, paramDef.Group, negLimit, posLimit, reportFlipped * 100f));
}

// Save results
var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "param_ranges.json");
var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(outputPath, json);
Console.WriteLine($"\nResults saved to: {outputPath}");

var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "param_ranges.csv");
using (var csv = new StreamWriter(csvPath))
{
    csv.WriteLine("ParamIndex,Name,Category,Group,SuggestedMin,SuggestedMax");
    foreach (var r in results)
        csv.WriteLine($"{r.ParamIndex},{r.Name},{r.Category},{r.Group},{r.SuggestedMin:F4},{r.SuggestedMax:F4}");
}
Console.WriteLine($"CSV saved to: {csvPath}");

// === Helper methods ===

static float[] GetVertices(MhrModel model, int paramIdx = -1, float value = 0f)
{
    var identity = new float[IdentityParamCount];
    var pose = new float[PoseParamCount];
    var expression = new float[ExpressionParamCount];

    if (paramIdx >= 0)
    {
        if (paramIdx < IdentityParamCount)
            identity[paramIdx] = value;
        else if (paramIdx < IdentityParamCount + PoseParamCount)
            pose[paramIdx - IdentityParamCount] = value;
        else
            expression[paramIdx - IdentityParamCount - PoseParamCount] = value;
    }

    using var identityT = torch.tensor(identity, dtype: torch.ScalarType.Float32);
    using var poseT = torch.tensor(pose, dtype: torch.ScalarType.Float32);
    using var exprT = torch.tensor(expression, dtype: torch.ScalarType.Float32);

    var output = model.Forward(identityT, poseT, exprT);

    using var cpu = output.Vertices.cpu().to(torch.ScalarType.Float32);
    var data = new float[cpu.NumberOfElements];
    cpu.data<float>().CopyTo(data.AsSpan());

    output.Vertices.Dispose();
    output.SkeletonState.Dispose();

    return data;
}

/// <summary>
/// Compute per-triangle normals (not normalized - we only care about direction).
/// Returns float[triCount * 3].
/// </summary>
static float[] ComputeTriangleNormals(float[] verts, uint[] indices)
{
    int triCount = indices.Length / 3;
    var normals = new float[triCount * 3];

    for (int t = 0; t < triCount; t++)
    {
        int i0 = (int)indices[t * 3] * 3;
        int i1 = (int)indices[t * 3 + 1] * 3;
        int i2 = (int)indices[t * 3 + 2] * 3;

        // Edge vectors
        float e1x = verts[i1] - verts[i0];
        float e1y = verts[i1 + 1] - verts[i0 + 1];
        float e1z = verts[i1 + 2] - verts[i0 + 2];

        float e2x = verts[i2] - verts[i0];
        float e2y = verts[i2 + 1] - verts[i0 + 1];
        float e2z = verts[i2 + 2] - verts[i0 + 2];

        // Cross product
        normals[t * 3]     = e1y * e2z - e1z * e2y;
        normals[t * 3 + 1] = e1z * e2x - e1x * e2z;
        normals[t * 3 + 2] = e1x * e2y - e1y * e2x;
    }

    return normals;
}

/// <summary>
/// Count fraction of triangles whose normal has flipped compared to baseline.
/// A triangle is "flipped" when dot(baseline_normal, new_normal) < 0.
/// </summary>
static float CountFlippedFraction(float[] baselineNormals, float[] testNormals)
{
    int triCount = baselineNormals.Length / 3;
    int flipped = 0;

    for (int t = 0; t < triCount; t++)
    {
        int i = t * 3;
        float dot = baselineNormals[i] * testNormals[i]
                   + baselineNormals[i + 1] * testNormals[i + 1]
                   + baselineNormals[i + 2] * testNormals[i + 2];
        if (dot < 0) flipped++;
    }

    return (float)flipped / triCount;
}

/// <summary>
/// Measure inversion fraction for a single parameter value.
/// </summary>
static float MeasureInversion(MhrModel model, float[] baselineNormals, uint[] indices, int paramIdx, float value)
{
    var verts = GetVertices(model, paramIdx, value);
    var normals = ComputeTriangleNormals(verts, indices);
    return CountFlippedFraction(baselineNormals, normals);
}

/// <summary>
/// Binary search for the largest absolute value (in the direction of searchMax)
/// that doesn't cause more than InversionThreshold fraction of triangles to flip.
/// Returns the found limit (with the same sign as searchMax).
/// </summary>
static float BinarySearchLimit(MhrModel model, float[] baselineNormals, uint[] indices, int paramIdx, float searchMax)
{
    float lo = 0f;
    float hi = MathF.Abs(searchMax);
    float sign = MathF.Sign(searchMax);

    // First check if max value is fine (no inversion at all)
    float maxFlipped = MeasureInversion(model, baselineNormals, indices, paramIdx, sign * hi);
    if (maxFlipped < InversionThreshold)
        return sign * hi; // Full range is safe

    // Binary search
    for (int step = 0; step < BinarySearchSteps; step++)
    {
        float mid = (lo + hi) * 0.5f;
        float flipped = MeasureInversion(model, baselineNormals, indices, paramIdx, sign * mid);

        if (flipped < InversionThreshold)
            lo = mid;  // Safe, try higher
        else
            hi = mid;  // Too many flipped, try lower
    }

    return sign * lo;
}

static float GetMaxSearch(int paramIdx)
{
    if (paramIdx < IdentityParamCount) return IdentityMaxSearch;
    if (paramIdx < IdentityParamCount + PoseParamCount) return PoseMaxSearch;
    return ExpressionMaxSearch;
}

// === Data types ===

record ParamRangeResult
{
    public int ParamIndex { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Group { get; set; } = "";
    public float SuggestedMin { get; set; }
    public float SuggestedMax { get; set; }
}
