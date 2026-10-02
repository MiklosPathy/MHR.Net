// Body part segmentation for MHR meshes
// Ported from tools/mhr_create_segmentation/example.py
// Original: https://github.com/facebookresearch/MHR
// Licensed under Apache 2.0

using System.Numerics;
using System.Text.RegularExpressions;
using static TorchSharp.torch;

namespace MHR.Net;

/// <summary>
/// Effect of a parameter change on one body part.
/// </summary>
/// <param name="Part">Part index in <see cref="MhrSegmentation.Parts"/></param>
/// <param name="Name">Part key (e.g. "l_lowarm")</param>
/// <param name="DeformationShare">
/// Fraction of the total mesh edge-length change on this part (0-1): where the surface changes shape.
/// Insensitive to rigid motion, so for a joint rotation it points at the joint, not at the limb it carries.
/// </param>
/// <param name="MotionShare">Fraction of the total vertex displacement on this part (0-1): what moves.</param>
/// <param name="MaxDisplacement">Largest single-vertex displacement in this part (input units, cm for raw model output)</param>
public readonly record struct MhrPartEffect(int Part, string Name, float DeformationShare, float MotionShare, float MaxDisplacement);

/// <summary>
/// Per-part effect of a parameter change.
/// </summary>
/// <param name="Parts">Affected parts, sorted by deformation share (motion share when rigid)</param>
/// <param name="Rigid">True when the mesh moved without changing shape (e.g. root translation/rotation)</param>
/// <param name="MaxDisplacement">Largest single-vertex displacement over the whole mesh</param>
public readonly record struct MhrParamEffect(MhrPartEffect[] Parts, bool Rigid, float MaxDisplacement);

/// <summary>
/// Per-vertex body part segmentation of the MHR (LOD1) mesh, derived from the skinning weights.
/// Joints are grouped into 20 semantic parts (e.g. l_uparm, r_hand, c_head); each vertex is
/// assigned to the part of its dominant joint.
/// </summary>
public sealed class MhrSegmentation
{
    /// <summary>
    /// The 127 skeleton joint names, in the order used by the skinning weight indices
    /// (TorchScript get_joint_names()).
    /// </summary>
    public static readonly string[] JointNames =
    [
        "body_world", "root", "l_upleg", "l_lowleg", "l_foot", "l_talocrural", "l_subtalar",
        "l_transversetarsal", "l_ball", "l_lowleg_twist1_proc", "l_lowleg_twist2_proc",
        "l_lowleg_twist3_proc", "l_lowleg_twist4_proc", "l_upleg_twist0_proc", "l_upleg_twist1_proc",
        "l_upleg_twist2_proc", "l_upleg_twist3_proc", "l_upleg_twist4_proc", "r_upleg", "r_lowleg",
        "r_foot", "r_talocrural", "r_subtalar", "r_transversetarsal", "r_ball", "r_lowleg_twist1_proc",
        "r_lowleg_twist2_proc", "r_lowleg_twist3_proc", "r_lowleg_twist4_proc", "r_upleg_twist0_proc",
        "r_upleg_twist1_proc", "r_upleg_twist2_proc", "r_upleg_twist3_proc", "r_upleg_twist4_proc",
        "c_spine0", "c_spine1", "c_spine2", "c_spine3", "r_clavicle", "r_uparm", "r_lowarm",
        "r_wrist_twist", "r_wrist", "r_pinky0", "r_pinky1", "r_pinky2", "r_pinky3", "r_pinky_null",
        "r_ring1", "r_ring2", "r_ring3", "r_ring_null", "r_middle1", "r_middle2", "r_middle3",
        "r_middle_null", "r_index1", "r_index2", "r_index3", "r_index_null", "r_thumb0", "r_thumb1",
        "r_thumb2", "r_thumb3", "r_thumb_null", "r_lowarm_twist1_proc", "r_lowarm_twist2_proc",
        "r_lowarm_twist3_proc", "r_lowarm_twist4_proc", "r_uparm_twist0_proc", "r_uparm_twist1_proc",
        "r_uparm_twist2_proc", "r_uparm_twist3_proc", "r_uparm_twist4_proc", "l_clavicle", "l_uparm",
        "l_lowarm", "l_wrist_twist", "l_wrist", "l_pinky0", "l_pinky1", "l_pinky2", "l_pinky3",
        "l_pinky_null", "l_ring1", "l_ring2", "l_ring3", "l_ring_null", "l_middle1", "l_middle2",
        "l_middle3", "l_middle_null", "l_index1", "l_index2", "l_index3", "l_index_null", "l_thumb0",
        "l_thumb1", "l_thumb2", "l_thumb3", "l_thumb_null", "l_lowarm_twist1_proc",
        "l_lowarm_twist2_proc", "l_lowarm_twist3_proc", "l_lowarm_twist4_proc", "l_uparm_twist0_proc",
        "l_uparm_twist1_proc", "l_uparm_twist2_proc", "l_uparm_twist3_proc", "l_uparm_twist4_proc",
        "c_neck", "c_neck_twist1_proc", "c_neck_twist0_proc", "c_head", "c_jaw", "c_teeth",
        "c_jaw_null", "c_tongue0", "c_tongue1", "c_tongue2", "c_tongue3", "c_tongue4", "r_eye",
        "r_eye_null", "l_eye", "l_eye_null", "c_head_null",
    ];

    /// <summary>
    /// Human-readable names for the part keys produced by <see cref="JointGroupKey"/>.
    /// Left/right are the character's left/right.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PartDisplayNames = new Dictionary<string, string>
    {
        ["zero_weight"] = "Face details (zero weight)",
        ["root"] = "Pelvis",
        ["l_upleg"] = "Left thigh",
        ["l_lowleg"] = "Left shin",
        ["l_foot"] = "Left foot",
        ["r_upleg"] = "Right thigh",
        ["r_lowleg"] = "Right shin",
        ["r_foot"] = "Right foot",
        ["c_spine"] = "Spine / torso",
        ["r_clavicle"] = "Right clavicle",
        ["r_uparm"] = "Right upper arm",
        ["r_lowarm"] = "Right forearm",
        ["r_hand"] = "Right hand",
        ["l_clavicle"] = "Left clavicle",
        ["l_uparm"] = "Left upper arm",
        ["l_lowarm"] = "Left forearm",
        ["l_hand"] = "Left hand",
        ["c_neck"] = "Neck",
        ["c_head"] = "Head",
        ["c_jaw"] = "Jaw",
    };

    /// <summary>
    /// matplotlib tab20 colormap, indexed by part (same coloring as the upstream tool).
    /// </summary>
    public static readonly Vector3[] PartColors =
    [
        new(0.122f, 0.467f, 0.706f), new(0.682f, 0.780f, 0.910f),
        new(1.000f, 0.498f, 0.055f), new(1.000f, 0.733f, 0.471f),
        new(0.173f, 0.627f, 0.173f), new(0.596f, 0.875f, 0.541f),
        new(0.839f, 0.153f, 0.157f), new(1.000f, 0.596f, 0.588f),
        new(0.580f, 0.404f, 0.741f), new(0.773f, 0.690f, 0.835f),
        new(0.549f, 0.337f, 0.294f), new(0.769f, 0.612f, 0.580f),
        new(0.890f, 0.467f, 0.761f), new(0.969f, 0.714f, 0.824f),
        new(0.498f, 0.498f, 0.498f), new(0.780f, 0.780f, 0.780f),
        new(0.737f, 0.741f, 0.133f), new(0.859f, 0.859f, 0.553f),
        new(0.090f, 0.745f, 0.812f), new(0.620f, 0.855f, 0.898f),
    ];

    // Joint name substrings for grouping
    static readonly string[] ZeroWeightParts = ["eye", "tongue", "teeth", "brow", "cheek", "lip", "ear", "nose", "body_world"];
    static readonly string[] HandParts = ["thumb", "index", "middle", "ring", "pinky", "wrist"];
    static readonly string[] FootParts = ["foot", "talocrural", "subtalar", "transversetarsal", "ball"];
    static readonly Regex SubpartSuffix = new(@"(_null|_twist\d*|\d+|_twist\d_proc)$");

    /// <summary>
    /// Map a joint name to its semantic group key:
    /// zero-weight facial/body joints, hands (fingers + wrist), feet, otherwise the joint name
    /// with trailing subpart indices/suffixes stripped (e.g. "l_upleg_twist2_proc" -> "l_upleg").
    /// </summary>
    public static string JointGroupKey(string name)
    {
        var lower = name.ToLowerInvariant();

        if (ZeroWeightParts.Any(lower.Contains))
            return "zero_weight";

        // Check after the 2-char side prefix (e.g. "l_" or "r_")
        var suffix = lower.Length > 2 ? lower[2..] : "";
        if (HandParts.Any(suffix.Contains))
            return $"{lower[..2]}hand";
        if (FootParts.Any(suffix.Contains))
            return $"{lower[..2]}foot";

        return SubpartSuffix.Replace(name, "").TrimEnd('_');
    }

    /// <summary>Part keys, in order of first appearance in <see cref="JointNames"/>.</summary>
    public string[] Parts { get; }

    /// <summary>Part index of every joint.</summary>
    public int[] JointPart { get; }

    /// <summary>Part index of every vertex (dominant skinning influence).</summary>
    public int[] VertexPart { get; }

    /// <summary>Soft per-vertex part weights [part][vertex]: summed skinning weights of the part's joints.</summary>
    public float[][] PartWeights { get; }

    public int NumVertices => VertexPart.Length;

    // Unique mesh edges (vertex index pairs), used to measure deformation
    readonly int[] _edgeA;
    readonly int[] _edgeB;

    MhrSegmentation(string[] parts, int[] jointPart, int[] vertexPart, float[][] partWeights, uint[]? indices)
    {
        Parts = parts;
        JointPart = jointPart;
        VertexPart = vertexPart;
        PartWeights = partWeights;

        var edges = new HashSet<long>();
        if (indices != null)
        {
            for (int t = 0; t + 2 < indices.Length; t += 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    long a = indices[t + k], b = indices[t + (k + 1) % 3];
                    edges.Add(a < b ? (a << 32) | b : (b << 32) | a);
                }
            }
        }
        _edgeA = edges.Select(e => (int)(e >> 32)).ToArray();
        _edgeB = edges.Select(e => (int)(e & 0xFFFFFFFF)).ToArray();
    }

    /// <summary>
    /// Build the segmentation from the model's skinning weights.
    /// </summary>
    public static MhrSegmentation Create(MhrModel model)
    {
        var (indexTensor, weightTensor) = model.GetSkinningWeights();
        int numVerts = (int)indexTensor.shape[0];
        int numInfluences = (int)indexTensor.shape[1];
        var index = indexTensor.to(ScalarType.Int64).data<long>().ToArray();
        var weight = weightTensor.to(ScalarType.Float32).data<float>().ToArray();

        // Group joints into parts
        var parts = new List<string>();
        var jointPart = new int[JointNames.Length];
        for (int j = 0; j < JointNames.Length; j++)
        {
            var key = JointGroupKey(JointNames[j]);
            int p = parts.IndexOf(key);
            if (p < 0)
            {
                parts.Add(key);
                p = parts.Count - 1;
            }
            jointPart[j] = p;
        }

        // Soft weights: sum the skinning weights of each part's joints.
        // Hard assignment: the part of the primary (first, highest-weight) influence.
        var partWeights = new float[parts.Count][];
        for (int p = 0; p < parts.Count; p++)
            partWeights[p] = new float[numVerts];
        var vertexPart = new int[numVerts];

        for (int v = 0; v < numVerts; v++)
        {
            for (int k = 0; k < numInfluences; k++)
            {
                float w = weight[v * numInfluences + k];
                if (w != 0f)
                    partWeights[jointPart[index[v * numInfluences + k]]][v] += w;
            }
            vertexPart[v] = jointPart[index[v * numInfluences]];
        }

        // Mesh topology for deformation measurement (only if it matches the skinned mesh)
        var indices = model.Indices;
        if (indices != null && (indices.Length == 0 || indices.Max() >= numVerts))
            indices = null;

        return new MhrSegmentation(parts.ToArray(), jointPart, vertexPart, partWeights, indices);
    }

    /// <summary>Human-readable name of a part.</summary>
    public string DisplayName(int part) =>
        PartDisplayNames.TryGetValue(Parts[part], out var name) ? name : Parts[part];

    /// <summary>Indices of the vertices assigned to a part.</summary>
    public int[] VerticesOf(int part) =>
        Enumerable.Range(0, NumVertices).Where(v => VertexPart[v] == part).ToArray();

    /// <summary>
    /// Split a triangle index list into one index list per part. Each triangle goes to the part
    /// that owns the majority of its vertices (the first vertex's part on a three-way tie).
    /// </summary>
    public uint[][] SplitIndicesByPart(uint[] indices)
    {
        var lists = Enumerable.Range(0, Parts.Length).Select(_ => new List<uint>()).ToArray();
        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            int a = VertexPart[indices[t]], b = VertexPart[indices[t + 1]], c = VertexPart[indices[t + 2]];
            int part = (b == c) ? b : a;
            lists[part].Add(indices[t]);
            lists[part].Add(indices[t + 1]);
            lists[part].Add(indices[t + 2]);
        }
        return lists.Select(l => l.ToArray()).ToArray();
    }

    /// <summary>
    /// Measure how a set of deformed meshes differ from a baseline, per body part.
    /// Positions are flat xyz arrays (length 3 * NumVertices), summed over all variants.
    /// Deformation is the change of mesh edge lengths (each edge counts half for each endpoint's part),
    /// motion is the vertex displacement. Parts below <paramref name="minShare"/> in both measures are omitted.
    /// </summary>
    public MhrParamEffect MeasureEffect(float[] baseline, IEnumerable<float[]> variants, float minShare = 0.05f)
    {
        var motion = new double[Parts.Length];
        var deformation = new double[Parts.Length];
        var maxDisp = new float[Parts.Length];
        float maxEdgeChange = 0f;

        foreach (var deformed in variants)
        {
            for (int v = 0; v < NumVertices; v++)
            {
                float d = Distance(deformed, v, baseline, v);
                int p = VertexPart[v];
                motion[p] += d;
                if (d > maxDisp[p]) maxDisp[p] = d;
            }

            for (int e = 0; e < _edgeA.Length; e++)
            {
                int a = _edgeA[e], b = _edgeB[e];
                float change = MathF.Abs(Distance(deformed, a, deformed, b) - Distance(baseline, a, baseline, b));
                deformation[VertexPart[a]] += change * 0.5;
                deformation[VertexPart[b]] += change * 0.5;
                if (change > maxEdgeChange) maxEdgeChange = change;
            }
        }

        float maxDisplacement = maxDisp.Max();
        double totalMotion = motion.Sum();
        if (totalMotion <= 0) return new MhrParamEffect([], false, 0f);

        // Shape unchanged relative to how far the mesh moved: rigid motion
        bool rigid = maxEdgeChange < 1e-3f * maxDisplacement;
        double totalDeformation = rigid ? 0 : deformation.Sum();

        var parts = Enumerable.Range(0, Parts.Length)
            .Select(p => new MhrPartEffect(p, Parts[p],
                totalDeformation > 0 ? (float)(deformation[p] / totalDeformation) : 0f,
                (float)(motion[p] / totalMotion),
                maxDisp[p]))
            .Where(e => e.DeformationShare >= minShare || e.MotionShare >= minShare)
            .OrderByDescending(e => e.DeformationShare)
            .ThenByDescending(e => e.MotionShare)
            .ToArray();

        return new MhrParamEffect(parts, rigid, maxDisplacement);
    }

    static float Distance(float[] p, int i, float[] q, int j)
    {
        float dx = p[i * 3] - q[j * 3];
        float dy = p[i * 3 + 1] - q[j * 3 + 1];
        float dz = p[i * 3 + 2] - q[j * 3 + 2];
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
