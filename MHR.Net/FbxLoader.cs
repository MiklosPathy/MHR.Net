// Simple FBX loader for extracting mesh topology
// Only extracts polygon indices for rendering

using System.Text;

namespace MHR.Net;

/// <summary>
/// Minimal FBX loader that extracts mesh polygon indices.
/// Supports FBX binary format (7.x).
/// </summary>
public static class FbxLoader
{
    private static readonly byte[] FBX_MAGIC = Encoding.ASCII.GetBytes("Kaydara FBX Binary  ");

    /// <summary>
    /// Load mesh indices and normals from an FBX file, combining all geometries.
    /// </summary>
    /// <param name="path">Path to FBX file</param>
    /// <returns>Tuple of (vertex count, triangle indices, normals array or null)</returns>
    public static (int vertexCount, uint[] indices, float[]? normals) LoadMeshWithNormals(string path)
    {
        var (vertexCount, indices, normals, _) = LoadMeshData(path);
        return (vertexCount, indices, normals);
    }

    /// <summary>
    /// Load mesh indices from an FBX file, combining all geometries.
    /// </summary>
    /// <param name="path">Path to FBX file</param>
    /// <returns>Tuple of (vertex count, triangle indices)</returns>
    public static (int vertexCount, uint[] indices) LoadMeshIndices(string path)
    {
        var (vertexCount, indices, _, _) = LoadMeshData(path);
        return (vertexCount, indices);
    }

    /// <summary>
    /// Internal method to load all mesh data from FBX.
    /// </summary>
    private static (int vertexCount, uint[] indices, float[]? normals, string? mappingType) LoadMeshData(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        // Verify FBX magic
        var magic = reader.ReadBytes(20);
        if (!magic.SequenceEqual(FBX_MAGIC))
        {
            throw new InvalidDataException("Not a valid FBX binary file");
        }

        // Skip rest of header
        reader.ReadBytes(3); // Additional header bytes
        var version = reader.ReadUInt32();

        // Collect all geometries with normals
        var geometries = new List<(string name, int vertexCount, List<int> polygonIndices, double[]? normals, string? normalMapping)>();

        // Read until end of file
        while (stream.Position < stream.Length - 160) // 160 = footer size
        {
            var node = ReadNode(reader, version);
            if (node == null) break;

            if (node.Name == "Objects")
            {
                // Find ALL Geometry nodes inside Objects
                foreach (var child in node.Children)
                {
                    if (child.Name == "Geometry")
                    {
                        string geomName = "";
                        int geomVertexCount = 0;
                        var geomPolygonIndices = new List<int>();
                        double[]? geomNormals = null;
                        string? normalMappingType = null;

                        // Get geometry name from properties (usually second property)
                        if (child.Properties.Count >= 2 && child.Properties[1] is string name)
                        {
                            geomName = name;
                        }

                        foreach (var geomChild in child.Children)
                        {
                            if (geomChild.Name == "Vertices" && geomChild.Properties.Count > 0)
                            {
                                var vertArray = geomChild.Properties[0] as double[];
                                if (vertArray != null)
                                {
                                    geomVertexCount = vertArray.Length / 3;
                                }
                            }
                            else if (geomChild.Name == "PolygonVertexIndex" && geomChild.Properties.Count > 0)
                            {
                                var indexArray = geomChild.Properties[0] as int[];
                                if (indexArray != null)
                                {
                                    geomPolygonIndices.AddRange(indexArray);
                                }
                            }
                            else if (geomChild.Name == "LayerElementNormal")
                            {
                                // Extract normals
                                foreach (var normalChild in geomChild.Children)
                                {
                                    if (normalChild.Name == "MappingInformationType" && normalChild.Properties.Count > 0)
                                    {
                                        normalMappingType = normalChild.Properties[0] as string;
                                    }
                                    else if (normalChild.Name == "Normals" && normalChild.Properties.Count > 0)
                                    {
                                        geomNormals = normalChild.Properties[0] as double[];
                                    }
                                }
                            }
                        }

                        if (geomVertexCount > 0 && geomPolygonIndices.Count > 0)
                        {
                            geometries.Add((geomName, geomVertexCount, geomPolygonIndices, geomNormals, normalMappingType));
                        }
                    }
                }
            }
        }

        if (geometries.Count == 0)
        {
            return (0, Array.Empty<uint>(), null, null);
        }

        // Combine all geometries
        // Each geometry's indices need to be offset by the cumulative vertex count of previous geometries
        int totalVertexCount = 0;
        var allTriangleIndices = new List<uint>();
        var allNormals = new List<float>();
        string? normalMapping = null;

        foreach (var (name, vertCount, polyIndices, normals, mapping) in geometries)
        {
            // Convert this geometry's polygon indices to triangles with offset
            var triangles = ConvertPolygonsToTriangles(polyIndices, (uint)totalVertexCount);
            allTriangleIndices.AddRange(triangles);

            // Handle normals based on mapping type
            if (normals != null)
            {
                normalMapping = mapping;

                if (mapping == "ByVertice" || mapping == "ByVertex")
                {
                    // Normals are per-vertex, convert to float and add
                    foreach (var n in normals)
                    {
                        allNormals.Add((float)n);
                    }
                }
                else if (mapping == "ByPolygonVertex")
                {
                    // Normals are per polygon-vertex - need to average per vertex
                    var vertexNormals = new double[vertCount * 3];
                    var vertexCounts = new int[vertCount];

                    int normalIdx = 0;
                    foreach (var idx in polyIndices)
                    {
                        int vertIdx = idx < 0 ? ~idx : idx;
                        if (vertIdx < vertCount && normalIdx * 3 + 2 < normals.Length)
                        {
                            vertexNormals[vertIdx * 3 + 0] += normals[normalIdx * 3 + 0];
                            vertexNormals[vertIdx * 3 + 1] += normals[normalIdx * 3 + 1];
                            vertexNormals[vertIdx * 3 + 2] += normals[normalIdx * 3 + 2];
                            vertexCounts[vertIdx]++;
                        }
                        normalIdx++;
                    }

                    // Average and normalize
                    for (int i = 0; i < vertCount; i++)
                    {
                        if (vertexCounts[i] > 0)
                        {
                            double nx = vertexNormals[i * 3 + 0] / vertexCounts[i];
                            double ny = vertexNormals[i * 3 + 1] / vertexCounts[i];
                            double nz = vertexNormals[i * 3 + 2] / vertexCounts[i];
                            double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                            if (len > 0.0001)
                            {
                                allNormals.Add((float)(nx / len));
                                allNormals.Add((float)(ny / len));
                                allNormals.Add((float)(nz / len));
                            }
                            else
                            {
                                allNormals.Add(0);
                                allNormals.Add(0);
                                allNormals.Add(1);
                            }
                        }
                        else
                        {
                            allNormals.Add(0);
                            allNormals.Add(0);
                            allNormals.Add(1);
                        }
                    }
                }
            }

            totalVertexCount += vertCount;
        }

        float[]? resultNormals = allNormals.Count > 0 ? allNormals.ToArray() : null;
        return (totalVertexCount, allTriangleIndices.ToArray(), resultNormals, normalMapping);
    }

    /// <summary>
    /// Convert FBX polygon indices to triangle indices.
    /// FBX marks the last vertex of each polygon with a negative index (bitwise NOT of actual index).
    /// </summary>
    /// <param name="polygonIndices">FBX polygon vertex indices</param>
    /// <param name="vertexOffset">Offset to add to each index (for combining multiple geometries)</param>
    private static uint[] ConvertPolygonsToTriangles(List<int> polygonIndices, uint vertexOffset = 0)
    {
        var triangles = new List<uint>();
        var polygon = new List<int>();

        foreach (var idx in polygonIndices)
        {
            if (idx < 0)
            {
                // End of polygon - decode the actual index
                polygon.Add(~idx);

                // Triangulate polygon (fan triangulation) with vertex offset
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    triangles.Add((uint)polygon[0] + vertexOffset);
                    triangles.Add((uint)polygon[i] + vertexOffset);
                    triangles.Add((uint)polygon[i + 1] + vertexOffset);
                }

                polygon.Clear();
            }
            else
            {
                polygon.Add(idx);
            }
        }

        return triangles.ToArray();
    }

    private class FbxNode
    {
        public string Name { get; set; } = "";
        public List<object> Properties { get; } = new();
        public List<FbxNode> Children { get; } = new();
    }

    private static FbxNode? ReadNode(BinaryReader reader, uint version)
    {
        // Read node header
        long endOffset, numProperties, propertyListLen;

        if (version >= 7500)
        {
            endOffset = reader.ReadInt64();
            numProperties = reader.ReadInt64();
            propertyListLen = reader.ReadInt64();
        }
        else
        {
            endOffset = reader.ReadUInt32();
            numProperties = reader.ReadUInt32();
            propertyListLen = reader.ReadUInt32();
        }

        var nameLen = reader.ReadByte();

        if (endOffset == 0 && numProperties == 0 && propertyListLen == 0 && nameLen == 0)
        {
            return null; // Null node
        }

        var name = Encoding.ASCII.GetString(reader.ReadBytes(nameLen));
        var node = new FbxNode { Name = name };

        // Read properties
        for (int i = 0; i < numProperties; i++)
        {
            var prop = ReadProperty(reader);
            node.Properties.Add(prop);
        }

        // Read nested nodes until we reach endOffset
        while (reader.BaseStream.Position < endOffset)
        {
            var child = ReadNode(reader, version);
            if (child == null) break;
            node.Children.Add(child);
        }

        // Ensure we're at the end offset
        if (reader.BaseStream.Position < endOffset)
        {
            reader.BaseStream.Position = endOffset;
        }

        return node;
    }

    private static object ReadProperty(BinaryReader reader)
    {
        var typeCode = (char)reader.ReadByte();

        return typeCode switch
        {
            'Y' => reader.ReadInt16(),
            'C' => reader.ReadBoolean(),
            'I' => reader.ReadInt32(),
            'F' => reader.ReadSingle(),
            'D' => reader.ReadDouble(),
            'L' => reader.ReadInt64(),
            'f' => ReadFloatArray(reader),
            'd' => ReadDoubleArray(reader),
            'l' => ReadLongArray(reader),
            'i' => ReadIntArray(reader),
            'b' => ReadBoolArray(reader),
            'S' => ReadString(reader),
            'R' => ReadRawData(reader),
            _ => throw new InvalidDataException($"Unknown property type: {typeCode}")
        };
    }

    private static float[] ReadFloatArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt32();
        var encoding = reader.ReadUInt32();
        var compressedLength = reader.ReadUInt32();

        if (encoding == 1)
        {
            // Compressed with zlib
            var compressed = reader.ReadBytes((int)compressedLength);
            var decompressed = DecompressZlib(compressed);
            var result = new float[arrayLength];
            Buffer.BlockCopy(decompressed, 0, result, 0, (int)(arrayLength * 4));
            return result;
        }
        else
        {
            var result = new float[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadSingle();
            }
            return result;
        }
    }

    private static double[] ReadDoubleArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt32();
        var encoding = reader.ReadUInt32();
        var compressedLength = reader.ReadUInt32();

        if (encoding == 1)
        {
            var compressed = reader.ReadBytes((int)compressedLength);
            var decompressed = DecompressZlib(compressed);
            var result = new double[arrayLength];
            Buffer.BlockCopy(decompressed, 0, result, 0, (int)(arrayLength * 8));
            return result;
        }
        else
        {
            var result = new double[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadDouble();
            }
            return result;
        }
    }

    private static int[] ReadIntArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt32();
        var encoding = reader.ReadUInt32();
        var compressedLength = reader.ReadUInt32();

        if (encoding == 1)
        {
            var compressed = reader.ReadBytes((int)compressedLength);
            var decompressed = DecompressZlib(compressed);
            var result = new int[arrayLength];
            Buffer.BlockCopy(decompressed, 0, result, 0, (int)(arrayLength * 4));
            return result;
        }
        else
        {
            var result = new int[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadInt32();
            }
            return result;
        }
    }

    private static long[] ReadLongArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt32();
        var encoding = reader.ReadUInt32();
        var compressedLength = reader.ReadUInt32();

        if (encoding == 1)
        {
            var compressed = reader.ReadBytes((int)compressedLength);
            var decompressed = DecompressZlib(compressed);
            var result = new long[arrayLength];
            Buffer.BlockCopy(decompressed, 0, result, 0, (int)(arrayLength * 8));
            return result;
        }
        else
        {
            var result = new long[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadInt64();
            }
            return result;
        }
    }

    private static bool[] ReadBoolArray(BinaryReader reader)
    {
        var arrayLength = reader.ReadUInt32();
        var encoding = reader.ReadUInt32();
        var compressedLength = reader.ReadUInt32();

        if (encoding == 1)
        {
            var compressed = reader.ReadBytes((int)compressedLength);
            var decompressed = DecompressZlib(compressed);
            var result = new bool[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = decompressed[i] != 0;
            }
            return result;
        }
        else
        {
            var result = new bool[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                result[i] = reader.ReadBoolean();
            }
            return result;
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        return Encoding.UTF8.GetString(reader.ReadBytes((int)length));
    }

    private static byte[] ReadRawData(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        return reader.ReadBytes((int)length);
    }

    private static byte[] DecompressZlib(byte[] compressed)
    {
        // Skip zlib header (2 bytes)
        using var input = new MemoryStream(compressed, 2, compressed.Length - 2);
        using var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }
}
