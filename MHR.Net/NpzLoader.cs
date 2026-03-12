// NPZ file loader for loading numpy array archives
// NPZ files are ZIP archives containing .npy files

using System.IO.Compression;
using TorchSharp;
using static TorchSharp.torch;

namespace MHR.Net;

/// <summary>
/// Loader for NumPy NPZ (zipped archive of .npy files) format.
/// </summary>
public static class NpzLoader
{
    /// <summary>
    /// Load all arrays from an NPZ file.
    /// </summary>
    public static Dictionary<string, Tensor> Load(string path)
    {
        var result = new Dictionary<string, Tensor>();

        using var archive = ZipFile.OpenRead(path);
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".npy"))
            {
                var key = Path.GetFileNameWithoutExtension(entry.Name);
                using var stream = entry.Open();
                var tensor = LoadNpy(stream);
                result[key] = tensor;
            }
        }

        return result;
    }

    /// <summary>
    /// Load a single .npy file from a stream.
    /// NPY format: https://numpy.org/doc/stable/reference/generated/numpy.lib.format.html
    /// </summary>
    private static Tensor LoadNpy(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        // Magic number: \x93NUMPY
        var magic = reader.ReadBytes(6);
        if (magic[0] != 0x93 || magic[1] != 'N' || magic[2] != 'U' ||
            magic[3] != 'M' || magic[4] != 'P' || magic[5] != 'Y')
        {
            throw new InvalidDataException("Invalid NPY magic number");
        }

        // Version
        var majorVersion = reader.ReadByte();
        var minorVersion = reader.ReadByte();

        // Header length
        int headerLen;
        if (majorVersion == 1)
        {
            headerLen = reader.ReadUInt16();
        }
        else if (majorVersion == 2 || majorVersion == 3)
        {
            headerLen = (int)reader.ReadUInt32();
        }
        else
        {
            throw new InvalidDataException($"Unsupported NPY version: {majorVersion}.{minorVersion}");
        }

        // Parse header (Python dict literal)
        var headerBytes = reader.ReadBytes(headerLen);
        var header = System.Text.Encoding.ASCII.GetString(headerBytes).Trim();

        // Extract dtype, fortran_order, and shape from header
        var (dtype, fortranOrder, shape) = ParseHeader(header);

        if (fortranOrder)
        {
            throw new NotSupportedException("Fortran-ordered arrays are not supported");
        }

        // Calculate total elements
        long totalElements = 1;
        foreach (var dim in shape)
        {
            totalElements *= dim;
        }

        // Read data based on dtype
        return dtype switch
        {
            "<f4" or "float32" => LoadFloat32(reader, shape, totalElements),
            "<f8" or "float64" => LoadFloat64(reader, shape, totalElements),
            "<i4" or "int32" => LoadInt32(reader, shape, totalElements),
            "<i8" or "int64" => LoadInt64(reader, shape, totalElements),
            "|b1" or "bool" => LoadBool(reader, shape, totalElements),
            _ => throw new NotSupportedException($"Unsupported dtype: {dtype}")
        };
    }

    private static (string dtype, bool fortranOrder, long[] shape) ParseHeader(string header)
    {
        // Header format: {'descr': '<f4', 'fortran_order': False, 'shape': (10, 20), }
        string dtype = "";
        bool fortranOrder = false;
        var shape = new List<long>();

        // Extract descr
        var descrMatch = System.Text.RegularExpressions.Regex.Match(header, @"'descr':\s*'([^']+)'");
        if (descrMatch.Success)
        {
            dtype = descrMatch.Groups[1].Value;
        }

        // Extract fortran_order
        var fortranMatch = System.Text.RegularExpressions.Regex.Match(header, @"'fortran_order':\s*(True|False)");
        if (fortranMatch.Success)
        {
            fortranOrder = fortranMatch.Groups[1].Value == "True";
        }

        // Extract shape
        var shapeMatch = System.Text.RegularExpressions.Regex.Match(header, @"'shape':\s*\(([^)]*)\)");
        if (shapeMatch.Success)
        {
            var shapeStr = shapeMatch.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(shapeStr))
            {
                foreach (var dim in shapeStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (long.TryParse(dim.Trim(), out var dimValue))
                    {
                        shape.Add(dimValue);
                    }
                }
            }
        }

        return (dtype, fortranOrder, shape.ToArray());
    }

    private static Tensor LoadFloat32(BinaryReader reader, long[] shape, long totalElements)
    {
        var data = new float[totalElements];
        for (int i = 0; i < totalElements; i++)
        {
            data[i] = reader.ReadSingle();
        }
        return torch.tensor(data, dtype: ScalarType.Float32).reshape(shape);
    }

    private static Tensor LoadFloat64(BinaryReader reader, long[] shape, long totalElements)
    {
        var data = new double[totalElements];
        for (int i = 0; i < totalElements; i++)
        {
            data[i] = reader.ReadDouble();
        }
        return torch.tensor(data, dtype: ScalarType.Float64).reshape(shape);
    }

    private static Tensor LoadInt32(BinaryReader reader, long[] shape, long totalElements)
    {
        var data = new int[totalElements];
        for (int i = 0; i < totalElements; i++)
        {
            data[i] = reader.ReadInt32();
        }
        return torch.tensor(data, dtype: ScalarType.Int32).reshape(shape);
    }

    private static Tensor LoadInt64(BinaryReader reader, long[] shape, long totalElements)
    {
        var data = new long[totalElements];
        for (int i = 0; i < totalElements; i++)
        {
            data[i] = reader.ReadInt64();
        }
        return torch.tensor(data, dtype: ScalarType.Int64).reshape(shape);
    }

    private static Tensor LoadBool(BinaryReader reader, long[] shape, long totalElements)
    {
        var data = new bool[totalElements];
        for (int i = 0; i < totalElements; i++)
        {
            data[i] = reader.ReadByte() != 0;
        }
        return torch.tensor(data, dtype: ScalarType.Bool).reshape(shape);
    }
}
