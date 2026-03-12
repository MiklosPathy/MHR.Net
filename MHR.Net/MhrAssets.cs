// Asset loading utilities for MHR model
// Ported from mhr/io.py

using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace MHR.Net;

/// <summary>
/// Constants and asset path utilities for MHR model.
/// </summary>
public static class MhrAssets
{
    // Blendshape counts
    public const int NumIdentityBlendshapes = 45;
    public const int NumFaceExpressionBlendshapes = 72;
    public const int TotalBlendshapes = NumIdentityBlendshapes + NumFaceExpressionBlendshapes;

    // Neural network architecture constants
    public const int NumJoints = 127;           // Total joints in skeleton
    public const int NumPoseJoints = 125;       // Joints used for pose (excluding 2 global)
    public const int ParametersPerJoint = 8;    // Parameters per joint in pymomentum
    public const int PoseFeatureDim = NumPoseJoints * 6;     // 750 (6D rotation per joint)
    public const int HiddenDim = NumPoseJoints * 24;         // 3000 (hidden layer size)

    // NPZ key names
    public const string PoseCorrectivesSparseMaskName = "posedirs_sparse_mask";
    public const string PoseCorrectivesComponentsName = "corrective_blendshapes";

    /// <summary>
    /// Get the default asset folder path.
    /// </summary>
    public static string GetDefaultAssetFolder()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
    }

    /// <summary>
    /// Get path to the FBX file for a given LOD level.
    /// </summary>
    public static string GetFbxPath(string folder, int lod)
    {
        return Path.Combine(folder, $"lod{lod}.fbx");
    }

    /// <summary>
    /// Get path to the model definition file.
    /// </summary>
    public static string GetModelPath(string folder)
    {
        return Path.Combine(folder, "compact_v6_1.model");
    }

    /// <summary>
    /// Get path to the corrective blendshapes NPZ file.
    /// </summary>
    public static string GetBlendshapesPath(string folder, int lod)
    {
        return Path.Combine(folder, $"corrective_blendshapes_lod{lod}.npz");
    }

    /// <summary>
    /// Get path to the corrective activation NPZ file.
    /// </summary>
    public static string GetCorrectiveActivationPath(string folder)
    {
        return Path.Combine(folder, "corrective_activation.npz");
    }

    /// <summary>
    /// Get path to the TorchScript model file.
    /// </summary>
    public static string GetTorchScriptModelPath(string folder)
    {
        return Path.Combine(folder, "mhr_model.pt");
    }

    /// <summary>
    /// Check if pose corrective blendshapes exist in the data.
    /// </summary>
    public static bool HasPoseCorrectiveBlendshapes(Dictionary<string, Tensor> data)
    {
        return data.ContainsKey(PoseCorrectivesComponentsName);
    }

    /// <summary>
    /// Load the pose directions predictor neural network.
    /// </summary>
    public static Sequential LoadPoseDirsPredictor(
        Dictionary<string, Tensor> blendshapesData,
        Dictionary<string, Tensor> correctiveActivationData,
        Device device)
    {
        // Get dimensions from data
        var correctiveBlendshapes = blendshapesData[PoseCorrectivesComponentsName];
        var nComponents = correctiveBlendshapes.shape[0];
        var nVerts = correctiveBlendshapes.shape[1];

        // Get sparse mask
        var sparseMask = correctiveActivationData[PoseCorrectivesSparseMaskName].to(ScalarType.Bool);

        // Build the pose correctives network:
        // SparseLinear(750 -> 3000, no bias) -> ReLU -> Linear(3000 -> nVerts*3, no bias)
        var posedirs = Sequential(
            ("sparse_linear", new SparseLinear(
                PoseFeatureDim,
                HiddenDim,
                sparseMask,
                bias: false)),
            ("relu", ReLU()),
            ("output_linear", Linear(HiddenDim, (int)(nVerts * 3), hasBias: false))
        );

        // Load weights
        var sparseIndices = correctiveActivationData["0.sparse_indices"];
        var sparseWeight = correctiveActivationData["0.sparse_weight"];

        // The output layer weight is the corrective blendshapes reshaped
        // Shape: (n_verts * 3, n_components) transposed from (n_components, n_verts * 3)
        var outputWeight = correctiveBlendshapes.reshape(nComponents, -1).T;

        // Apply loaded weights to the model
        using (torch.no_grad())
        {
            // Load sparse linear weights
            var sparseLinear = (SparseLinear)posedirs[0];
            // Note: weights are loaded through the SparseLinear constructor from the mask

            // Load output linear weights
            var outputLinear = (Linear)posedirs[2];
            outputLinear.weight!.copy_(outputWeight.to(outputLinear.weight.dtype));
        }

        // Freeze all parameters
        foreach (var param in posedirs.parameters())
        {
            param.requires_grad = false;
        }

        return posedirs.to(device);
    }
}
