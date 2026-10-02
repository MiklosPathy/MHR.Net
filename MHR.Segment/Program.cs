// MHR Body Part Segmentation & Parameter Effect Analysis
// Builds the 20-part body segmentation from the skinning weights (port of upstream
// tools/mhr_create_segmentation), then measures which body parts each parameter moves.

using System.Globalization;
using System.Text.Json;
using MHR.Net;
using TorchSharp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("Loading MHR model...");

var assetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
using var mhrModel = MhrModel.Load(
    device: torch.cuda.is_available() ? torch.CUDA : torch.CPU,
    lod: MhrLod.LOD1,
    assetFolder: assetFolder);

var segmentation = MhrSegmentation.Create(mhrModel);
Console.WriteLine($"Model loaded. Vertices: {segmentation.NumVertices}, parts: {segmentation.Parts.Length}");

var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "segment_output");
Directory.CreateDirectory(outputDir);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// === Part segmentation ===

Console.WriteLine();
Console.WriteLine(string.Format("{0,-4} {1,-14} {2,-28} {3,8}", "Idx", "Part", "Name", "Vertices"));
Console.WriteLine(new string('-', 58));
var partEntries = new List<PartEntry>();
for (int p = 0; p < segmentation.Parts.Length; p++)
{
    var vertices = segmentation.VerticesOf(p);
    var color = MhrSegmentation.PartColors[p % MhrSegmentation.PartColors.Length];
    partEntries.Add(new PartEntry
    {
        Index = p,
        Key = segmentation.Parts[p],
        DisplayName = segmentation.DisplayName(p),
        Color = [color.X, color.Y, color.Z],
        Joints = MhrSegmentation.JointNames.Where((_, j) => segmentation.JointPart[j] == p).ToArray(),
        Vertices = vertices
    });
    Console.WriteLine(string.Format("{0,-4} {1,-14} {2,-28} {3,8}", p, segmentation.Parts[p], segmentation.DisplayName(p), vertices.Length));
}

var partsPath = Path.Combine(outputDir, "part_segments.json");
File.WriteAllText(partsPath, JsonSerializer.Serialize(new
{
    NumVertices = segmentation.NumVertices,
    Parts = partEntries,
    segmentation.VertexPart
}, jsonOptions));

// === Parameter effects ===

Console.WriteLine($"\nMeasuring body part effects of {MhrParameters.TotalCount} parameters...\n");
Console.WriteLine(string.Format("{0,-5} {1,-26} {2,-11} {3,8}  {4}", "Idx", "Name", "Category", "Max(cm)", "Deformed parts (share of shape change) | rigid: moved parts"));
Console.WriteLine(new string('-', 110));

var baseline = GetVertices(mhrModel);
var results = new List<ParamEffectResult>();

for (int paramIdx = 0; paramIdx < MhrParameters.TotalCount; paramIdx++)
{
    var def = MhrParameters.All[paramIdx];

    // Same variants as MHR.Sweep: half and full range in both directions
    float[] values = [def.RangeMin, def.RangeMin * 0.5f, def.RangeMax * 0.5f, def.RangeMax];
    var variants = values.Where(v => v != 0f).Select(v => GetVertices(mhrModel, paramIdx, v)).ToList();

    var effect = segmentation.MeasureEffect(baseline, variants);
    var result = new ParamEffectResult
    {
        ParamIndex = paramIdx,
        Name = def.Name,
        Category = def.Category,
        Group = def.Group,
        Rigid = effect.Rigid,
        MaxDisplacementCm = effect.MaxDisplacement,
        Parts = effect.Parts.Select(e => new PartShare
        {
            Part = e.Name,
            DisplayName = segmentation.DisplayName(e.Part),
            DeformationShare = e.DeformationShare,
            MotionShare = e.MotionShare,
            MaxDisplacementCm = e.MaxDisplacement
        }).ToList()
    };
    results.Add(result);

    Console.WriteLine(string.Format("{0,-5} {1,-26} {2,-11} {3,8:F2}  {4}",
        paramIdx, def.Name, def.Category, result.MaxDisplacementCm, FormatParts(result)));
}

var resultsPath = Path.Combine(outputDir, "param_segments.json");
File.WriteAllText(resultsPath, JsonSerializer.Serialize(results, jsonOptions));

var csvPath = Path.Combine(outputDir, "param_segments.csv");
using (var csv = new StreamWriter(csvPath))
{
    csv.WriteLine("ParamIndex,Name,Category,Group,Rigid,MaxDisplacementCm,TopPart,TopDeformationShare,DeformedParts,MovedParts");
    foreach (var r in results)
    {
        var top = r.Parts.FirstOrDefault();
        csv.WriteLine($"{r.ParamIndex},{r.Name},{r.Category},\"{r.Group}\",{r.Rigid},{r.MaxDisplacementCm:F3}," +
                      $"{top?.Part ?? ""},{top?.DeformationShare ?? 0:F3}," +
                      $"\"{FormatShares(r.Parts, p => p.DeformationShare)}\",\"{FormatShares(r.Parts, p => p.MotionShare)}\"");
    }
}

Console.WriteLine($"\nSegmentation saved to: {partsPath}");
Console.WriteLine($"Results saved to: {resultsPath}");
Console.WriteLine($"CSV saved to: {csvPath}");

// === Helper methods ===

static string FormatParts(ParamEffectResult r) =>
    r.Parts.Count == 0 ? "(no effect)"
    : r.Rigid ? "rigid: " + FormatShares(r.Parts, p => p.MotionShare)
    : FormatShares(r.Parts, p => p.DeformationShare);

static string FormatShares(List<PartShare> parts, Func<PartShare, float> share) =>
    string.Join("; ", parts.Where(p => share(p) >= 0.05f).OrderByDescending(share).Select(p => $"{p.Part} {share(p) * 100:F0}%"));

static float[] GetVertices(MhrModel model, int paramIdx = -1, float value = 0f)
{
    var identity = new float[MhrParameters.IdentityCount];
    var pose = new float[MhrParameters.PoseCount];
    var expression = new float[MhrParameters.ExpressionCount];

    if (paramIdx >= 0)
    {
        if (paramIdx < MhrParameters.PoseOffset)
            identity[paramIdx] = value;
        else if (paramIdx < MhrParameters.ExpressionOffset)
            pose[paramIdx - MhrParameters.PoseOffset] = value;
        else
            expression[paramIdx - MhrParameters.ExpressionOffset] = value;
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

// Result types
record PartEntry
{
    public int Index { get; set; }
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float[] Color { get; set; } = [];
    public string[] Joints { get; set; } = [];
    public int[] Vertices { get; set; } = [];
}

record PartShare
{
    public string Part { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float DeformationShare { get; set; }
    public float MotionShare { get; set; }
    public float MaxDisplacementCm { get; set; }
}

record ParamEffectResult
{
    public int ParamIndex { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Group { get; set; } = "";
    public bool Rigid { get; set; }
    public float MaxDisplacementCm { get; set; }
    public List<PartShare> Parts { get; set; } = [];
}
