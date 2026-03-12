// C# Port of MHR utilities from Meta's MHR library
// Original: https://github.com/facebookresearch/MHR
// Licensed under Apache 2.0

using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MHR.Net;

/// <summary>
/// Utility functions for MHR model, including rotation representations
/// and custom neural network layers.
/// </summary>
public static class MhrUtils
{
    /// <summary>
    /// Generate a rotation matrix from XYZ-Euler rotation angles.
    /// Converts to 6D representation (first two columns of rotation matrix).
    /// </summary>
    /// <param name="r">Tensor of shape [..., 3] containing XYZ Euler angles in radians</param>
    /// <param name="return9D">If true, returns full 9D (3x3) matrix; otherwise returns 6D</param>
    /// <returns>Tensor of shape [..., 6] or [..., 3, 3]</returns>
    public static Tensor Batch6DFromXYZ(Tensor r, bool return9D = false)
    {
        // Compute cos and sin of all rotation angles
        var rc = torch.cos(r);
        var rs = torch.sin(r);

        // Extract individual components
        var cx = rc[TensorIndex.Ellipsis, 0];
        var cy = rc[TensorIndex.Ellipsis, 1];
        var cz = rc[TensorIndex.Ellipsis, 2];
        var sx = rs[TensorIndex.Ellipsis, 0];
        var sy = rs[TensorIndex.Ellipsis, 1];
        var sz = rs[TensorIndex.Ellipsis, 2];

        // Build the rotation matrix elements using standard XYZ Euler rotation formula
        // R = Rz * Ry * Rx
        var result = torch.stack(new Tensor[]
        {
            cy * cz,                        // R[0,0]
            -cx * sz + sx * sy * cz,        // R[1,0]
            sx * sz + cx * sy * cz,         // R[2,0]
            cy * sz,                        // R[0,1]
            cx * cz + sx * sy * sz,         // R[1,1]
            -sx * cz + cx * sy * sz,        // R[2,1]
            -sy,                            // R[0,2]
            sx * cy,                        // R[1,2]
            cx * cy                         // R[2,2]
        }, dim: -1);

        // Reshape to [..., 3, 3]
        var shape = r.shape.Take(r.shape.Length - 1).ToList();
        shape.Add(3);
        shape.Add(3);
        result = result.reshape(shape.ToArray());

        if (!return9D)
        {
            // Return first two columns concatenated: [..., 6]
            var col0 = result[TensorIndex.Ellipsis, TensorIndex.Colon, 0];
            var col1 = result[TensorIndex.Ellipsis, TensorIndex.Colon, 1];
            return torch.cat(new[] { col0, col1 }, dim: -1);
        }
        else
        {
            return result;
        }
    }
}

/// <summary>
/// Sparse linear layer implementation matching the Python SparseLinear.
/// Uses dense weight representation internally for TorchScript compatibility.
/// </summary>
public class SparseLinear : nn.Module<Tensor, Tensor>
{
    private readonly int _inChannels;
    private readonly int _outChannels;
    private readonly Parameter _sparseIndices;
    private readonly Parameter _sparseWeight;
    private readonly Parameter? _bias;
    private readonly Tensor _denseWeight;
    private readonly long[] _sparseShape;

    public SparseLinear(
        int inChannels,
        int outChannels,
        Tensor sparseMask,
        bool bias = true,
        string name = "SparseLinear") : base(name)
    {
        _inChannels = inChannels;
        _outChannels = outChannels;
        _sparseShape = sparseMask.shape;

        // Get indices of non-zero elements (sparse mask)
        var nonzeroIndices = sparseMask.nonzero().T;
        _sparseIndices = nn.Parameter(nonzeroIndices, requires_grad: false);

        // Initialize weight tensor
        var weight = torch.zeros(outChannels, inChannels);

        // Kaiming initialization for each output
        for (int outIdx = 0; outIdx < outChannels; outIdx++)
        {
            var fanIn = sparseMask[outIdx].sum().ToDouble();
            if (fanIn > 0)
            {
                var gain = CalculateGain("leaky_relu", Math.Sqrt(5));
                var std = gain / Math.Sqrt(fanIn);
                var bound = Math.Sqrt(3.0) * std;
                weight[outIdx].uniform_(-bound, bound);
            }
        }

        // Extract sparse weights at the non-zero indices
        var sparseWeightValues = weight[_sparseIndices[0], _sparseIndices[1]];
        _sparseWeight = nn.Parameter(sparseWeightValues);

        // Initialize bias if needed
        if (bias)
        {
            var biasValues = torch.zeros(outChannels);
            for (int outIdx = 0; outIdx < outChannels; outIdx++)
            {
                var fanIn = sparseMask[outIdx].sum().ToDouble();
                if (fanIn > 0)
                {
                    var bound = 1.0 / Math.Sqrt(fanIn);
                    biasValues[outIdx].uniform_(-bound, bound);
                }
            }
            _bias = nn.Parameter(biasValues);
        }

        // Dense weight buffer for forward pass
        _denseWeight = torch.zeros(_sparseShape[0], _sparseShape[1]);
        register_buffer("dense_weight", _denseWeight);

        RegisterComponents();
    }

    private static double CalculateGain(string nonlinearity, double param = 0.01)
    {
        return nonlinearity switch
        {
            "leaky_relu" => Math.Sqrt(2.0 / (1 + param * param)),
            "relu" => Math.Sqrt(2.0),
            _ => 1.0
        };
    }

    public override Tensor forward(Tensor x)
    {
        // Rebuild dense weight from sparse representation
        _denseWeight.zero_();
        _denseWeight[_sparseIndices[0], _sparseIndices[1]] = _sparseWeight;

        // Compute linear transformation: (W @ x.T).T = x @ W.T
        var result = torch.mm(x, _denseWeight.T);

        if (_bias is not null)
        {
            result = result + _bias;
        }

        return result;
    }

    public override string ToString()
    {
        return $"SparseLinear(in_channels={_inChannels}, out_channels={_outChannels}, bias={_bias is not null})";
    }
}
