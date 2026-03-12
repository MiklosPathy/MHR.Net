// Main MHR body model wrapper
// Loads TorchScript model for inference
// Ported from Meta's MHR library: https://github.com/facebookresearch/MHR

using System.Numerics;
using TorchSharp;
using static TorchSharp.torch;

namespace MHR.Net;

/// <summary>
/// Level of detail options for MHR model.
/// Lower LOD = higher detail, more vertices.
/// </summary>
public enum MhrLod
{
    LOD0 = 0,  // Highest detail (~200k vertices)
    LOD1 = 1,  // High detail (~50k vertices)
    LOD2 = 2,  // Medium-high detail
    LOD3 = 3,  // Medium detail
    LOD4 = 4,  // Medium-low detail
    LOD5 = 5,  // Low detail
    LOD6 = 6   // Lowest detail (~3k vertices)
}

/// <summary>
/// Output from MHR model forward pass.
/// </summary>
public readonly struct MhrOutput
{
    /// <summary>
    /// Vertex positions in world space [numVerts, 3]
    /// </summary>
    public Tensor Vertices { get; init; }

    /// <summary>
    /// Skeleton state containing joint transforms
    /// </summary>
    public Tensor SkeletonState { get; init; }
}

/// <summary>
/// MHR body model for generating 3D human meshes.
/// Uses TorchScript model for inference.
/// </summary>
public class MhrModel : IDisposable
{
    private readonly jit.ScriptModule _model;
    private readonly Device _device;
    private readonly MhrLod _lod;
    private bool _disposed;

    // Cached mesh topology (loaded from FBX)
    private uint[]? _indices;
    private float[]? _fbxNormals;  // Pre-computed normals from FBX
    private int _numVertices;
    private Dictionary<int, (int vertCount, uint[] indices)>? _allFbxData;

    /// <summary>
    /// Number of identity blendshape parameters (body shape).
    /// First 20 affect body, next 20 affect head, last 5 affect hands.
    /// </summary>
    public int NumIdentityParams => MhrAssets.NumIdentityBlendshapes;

    /// <summary>
    /// Number of model parameters (pose, rigid transform, scale).
    /// </summary>
    public int NumModelParams => 204;

    /// <summary>
    /// Number of face expression blendshape parameters.
    /// </summary>
    public int NumExpressionParams => MhrAssets.NumFaceExpressionBlendshapes;

    /// <summary>
    /// Number of vertices in the mesh (depends on LOD).
    /// </summary>
    public int NumVertices => _numVertices;

    /// <summary>
    /// Triangle indices for rendering.
    /// </summary>
    public uint[]? Indices => _indices;

    /// <summary>
    /// Current level of detail.
    /// </summary>
    public MhrLod Lod => _lod;

    private MhrModel(jit.ScriptModule model, Device device, MhrLod lod)
    {
        _model = model;
        _device = device;
        _lod = lod;
    }

    /// <summary>
    /// Load MHR model from the default asset folder.
    /// </summary>
    /// <param name="device">Device to run model on (cuda or cpu)</param>
    /// <param name="lod">Level of detail (0-6, lower is higher quality)</param>
    /// <param name="assetFolder">Optional custom asset folder path</param>
    public static MhrModel Load(Device? device = null, MhrLod lod = MhrLod.LOD3, string? assetFolder = null)
    {
        device ??= cuda.is_available() ? CUDA : CPU;
        assetFolder ??= MhrAssets.GetDefaultAssetFolder();

        var modelPath = MhrAssets.GetTorchScriptModelPath(assetFolder);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                $"MHR TorchScript model not found at: {modelPath}\n" +
                "Please download assets.zip from https://github.com/facebookresearch/MHR/releases " +
                "and extract it to the Assets folder.");
        }

        // Load TorchScript model
        var model = jit.load(modelPath);
        model.to(device);
        model.eval();

        var mhr = new MhrModel(model, device, lod);

        // Try to load mesh topology from FBX
        mhr.LoadMeshTopology(assetFolder);

        return mhr;
    }

    /// <summary>
    /// Generate mesh vertices from parameters.
    /// </summary>
    /// <param name="identityCoeffs">Identity parameters [batch, 45] or [45]</param>
    /// <param name="modelParams">Model parameters [batch, 204] or [204]</param>
    /// <param name="expressionCoeffs">Expression parameters [batch, 72] or [72], optional</param>
    /// <param name="applyCorrectivees">Whether to apply pose correctives</param>
    public MhrOutput Forward(
        Tensor identityCoeffs,
        Tensor modelParams,
        Tensor? expressionCoeffs = null,
        bool applyCorrectivees = true)
    {
        using var _ = torch.no_grad();

        // Ensure batch dimension
        if (identityCoeffs.dim() == 1)
            identityCoeffs = identityCoeffs.unsqueeze(0);
        if (modelParams.dim() == 1)
            modelParams = modelParams.unsqueeze(0);

        // Move to device
        identityCoeffs = identityCoeffs.to(_device);
        modelParams = modelParams.to(_device);

        // Handle expression coefficients
        if (expressionCoeffs is null)
        {
            expressionCoeffs = torch.zeros(modelParams.shape[0], NumExpressionParams, device: _device);
        }
        else
        {
            if (expressionCoeffs.dim() == 1)
                expressionCoeffs = expressionCoeffs.unsqueeze(0);
            expressionCoeffs = expressionCoeffs.to(_device);
        }

        // Try different ways to call the model
        try
        {
            // Method 1: Try invoke with tuple return
            var result = _model.invoke<(Tensor, Tensor)>(
                "forward",
                identityCoeffs,
                modelParams,
                expressionCoeffs,
                applyCorrectivees);

            return new MhrOutput
            {
                Vertices = result.Item1,
                SkeletonState = result.Item2
            };
        }
        catch
        {
            try
            {
                // Method 2: Try invoke without apply_correctives
                var result = _model.invoke<(Tensor, Tensor)>(
                    "forward",
                    identityCoeffs,
                    modelParams,
                    expressionCoeffs);

                return new MhrOutput
                {
                    Vertices = result.Item1,
                    SkeletonState = result.Item2
                };
            }
            catch
            {
                try
                {
                    // Method 3: Try call method
                    var callResult = _model.call(identityCoeffs, modelParams, expressionCoeffs);

                    if (callResult is Tensor t)
                    {
                        return new MhrOutput
                        {
                            Vertices = t,
                            SkeletonState = torch.zeros(1)
                        };
                    }
                    else if (callResult is (Tensor v, Tensor s))
                    {
                        return new MhrOutput { Vertices = v, SkeletonState = s };
                    }

                    throw new Exception($"Unexpected return type: {callResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"All model invocation methods failed. Last error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Generate mesh with default (neutral) pose and identity.
    /// </summary>
    public MhrOutput ForwardNeutral()
    {
        var identity = torch.zeros(1, NumIdentityParams);
        var modelParams = torch.zeros(1, NumModelParams);
        return Forward(identity, modelParams);
    }

    /// <summary>
    /// Convert model output to vertex array.
    /// </summary>
    /// <param name="output">MHR model output</param>
    /// <param name="scale">Scale factor (default 0.01 to convert cm to meters)</param>
    /// <returns>Array of vertices with positions and computed normals</returns>
    public MhrVertex[] ToVertexArray(MhrOutput output, float scale = 0.01f)
    {
        // Get vertices as CPU float array
        var verts = output.Vertices.squeeze(0).cpu().to(ScalarType.Float32);
        var numVerts = (int)verts.shape[0];
        var vertData = verts.data<float>().ToArray();

        // Check for vertex count mismatch
        if (_indices != null && numVerts != _numVertices)
        {
            // Clear invalid indices, will regenerate below
            _indices = null;
        }

        var vertices = new MhrVertex[numVerts];

        // Extract positions with scale conversion (model outputs in cm, we want meters)
        for (int i = 0; i < numVerts; i++)
        {
            vertices[i].Position = new Vector3(
                vertData[i * 3 + 0] * scale,
                vertData[i * 3 + 1] * scale,
                vertData[i * 3 + 2] * scale);
        }

        // Use FBX normals if available (these are authored correctly)
        if (_fbxNormals != null && _fbxNormals.Length >= numVerts * 3)
        {
            for (int i = 0; i < numVerts; i++)
            {
                vertices[i].Normal = new Vector3(
                    _fbxNormals[i * 3 + 0],
                    _fbxNormals[i * 3 + 1],
                    _fbxNormals[i * 3 + 2]);
            }
        }
        // Compute normals from face indices if FBX normals not available
        else if (_indices != null && _indices.Length > 0)
        {
            ComputeNormals(vertices, _indices);
        }
        else
        {
            // No valid indices - compute normals from position (pointing outward from center)
            var center = Vector3.Zero;
            for (int i = 0; i < numVerts; i++)
                center += vertices[i].Position;
            center /= numVerts;

            for (int i = 0; i < numVerts; i++)
            {
                var dir = vertices[i].Position - center;
                vertices[i].Normal = dir.Length() > 0.0001f ? Vector3.Normalize(dir) : Vector3.UnitY;
            }
        }

        // Update internal vertex count to match model output
        _numVertices = numVerts;

        return vertices;
    }

    /// <summary>
    /// Generate simple triangle indices as a fallback when FBX doesn't match.
    /// </summary>
    public uint[] GenerateFallbackIndices(int numVertices)
    {
        var indices = new List<uint>();
        for (int i = 0; i < numVertices - 2; i += 3)
        {
            indices.Add((uint)i);
            indices.Add((uint)(i + 1));
            indices.Add((uint)(i + 2));
        }
        return indices.ToArray();
    }

    /// <summary>
    /// Try to find FBX indices that match the given vertex count.
    /// </summary>
    public uint[]? FindMatchingIndices(int targetVertexCount)
    {
        if (_allFbxData == null) return null;

        foreach (var kvp in _allFbxData)
        {
            if (kvp.Value.vertCount == targetVertexCount)
            {
                return kvp.Value.indices;
            }
        }

        return null;
    }

    /// <summary>
    /// Compute vertex normals from triangle indices.
    /// </summary>
    private static void ComputeNormals(MhrVertex[] vertices, uint[] indices)
    {
        // Initialize normals to zero
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = Vector3.Zero;
        }

        var referencedVertices = new HashSet<int>();

        // Accumulate face normals
        for (int i = 0; i < indices.Length; i += 3)
        {
            int i0 = (int)indices[i];
            int i1 = (int)indices[i + 1];
            int i2 = (int)indices[i + 2];

            if (i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                continue;

            referencedVertices.Add(i0);
            referencedVertices.Add(i1);
            referencedVertices.Add(i2);

            var v0 = vertices[i0].Position;
            var v1 = vertices[i1].Position;
            var v2 = vertices[i2].Position;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var faceNormal = Vector3.Cross(edge1, edge2);

            if (faceNormal.LengthSquared() < 1e-10f)
                continue;

            vertices[i0].Normal += faceNormal;
            vertices[i1].Normal += faceNormal;
            vertices[i2].Normal += faceNormal;
        }

        // Build adjacency map for neighbor propagation
        var vertexNeighbors = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            int i0 = (int)indices[i];
            int i1 = (int)indices[i + 1];
            int i2 = (int)indices[i + 2];

            if (i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                continue;

            if (!vertexNeighbors.ContainsKey(i0)) vertexNeighbors[i0] = new HashSet<int>();
            if (!vertexNeighbors.ContainsKey(i1)) vertexNeighbors[i1] = new HashSet<int>();
            if (!vertexNeighbors.ContainsKey(i2)) vertexNeighbors[i2] = new HashSet<int>();

            vertexNeighbors[i0].Add(i1);
            vertexNeighbors[i0].Add(i2);
            vertexNeighbors[i1].Add(i0);
            vertexNeighbors[i1].Add(i2);
            vertexNeighbors[i2].Add(i0);
            vertexNeighbors[i2].Add(i1);
        }

        // First pass: normalize valid normals
        for (int i = 0; i < vertices.Length; i++)
        {
            var len = vertices[i].Normal.Length();
            if (len > 0.0001f)
            {
                vertices[i].Normal = Vector3.Normalize(vertices[i].Normal);
            }
        }

        // Second pass: propagate normals to vertices with zero normals
        for (int i = 0; i < vertices.Length; i++)
        {
            var len = vertices[i].Normal.Length();
            if (len < 0.0001f)
            {
                // Try to get normal from neighbors
                Vector3 avgNormal = Vector3.Zero;
                int validNeighbors = 0;

                if (vertexNeighbors.TryGetValue(i, out var neighbors))
                {
                    foreach (var neighborIdx in neighbors)
                    {
                        var neighborNormal = vertices[neighborIdx].Normal;
                        if (neighborNormal.LengthSquared() > 0.0001f)
                        {
                            avgNormal += neighborNormal;
                            validNeighbors++;
                        }
                    }
                }

                if (validNeighbors > 0 && avgNormal.LengthSquared() > 0.0001f)
                {
                    vertices[i].Normal = Vector3.Normalize(avgNormal);
                }
                else
                {
                    // Fallback: compute based on region
                    var pos = vertices[i].Position;

                    if (pos.Y > 0.014f) // Head region
                    {
                        var headCenter = new Vector3(0, 0.016f, 0.0005f);
                        var toVertex = pos - headCenter;

                        if (pos.Z > 0 && toVertex.LengthSquared() > 0.0001f)
                        {
                            var outward = Vector3.Normalize(toVertex);
                            vertices[i].Normal = Vector3.Normalize(outward + Vector3.UnitZ * 0.5f);
                        }
                        else if (toVertex.LengthSquared() > 0.0001f)
                        {
                            vertices[i].Normal = Vector3.Normalize(toVertex);
                        }
                        else
                        {
                            vertices[i].Normal = Vector3.UnitZ;
                        }
                    }
                    else
                    {
                        // Body: point outward from body center
                        var bodyCenter = new Vector3(0, 0.0085f, 0);
                        var toVertex = pos - bodyCenter;
                        if (toVertex.LengthSquared() > 0.0001f)
                        {
                            vertices[i].Normal = Vector3.Normalize(toVertex);
                        }
                        else
                        {
                            vertices[i].Normal = Vector3.UnitZ;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Load mesh topology (triangle indices) and normals from FBX file.
    /// </summary>
    private void LoadMeshTopology(string assetFolder)
    {
        var fbxData = new Dictionary<int, (int vertCount, uint[] indices, float[]? normals)>();

        for (int lod = 0; lod <= 6; lod++)
        {
            var fbxPath = MhrAssets.GetFbxPath(assetFolder, lod);
            if (File.Exists(fbxPath))
            {
                try
                {
                    var (vertCount, indices, normals) = FbxLoader.LoadMeshWithNormals(fbxPath);
                    fbxData[lod] = (vertCount, indices, normals);
                }
                catch
                {
                    // Skip files that fail to load
                }
            }
        }

        // Try to load the requested LOD first
        if (fbxData.TryGetValue((int)_lod, out var requestedData))
        {
            _numVertices = requestedData.vertCount;
            _indices = requestedData.indices;
            _fbxNormals = requestedData.normals;
        }
        else
        {
            _numVertices = 0;
        }

        // Store all FBX data for later matching
        _allFbxData = fbxData.ToDictionary(kv => kv.Key, kv => (kv.Value.vertCount, kv.Value.indices));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _model.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
