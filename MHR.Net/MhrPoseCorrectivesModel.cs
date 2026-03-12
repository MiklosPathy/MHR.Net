// Pose correctives model for MHR
// Ported from mhr/mhr.py MHRPoseCorrectivesModel

using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace MHR.Net;

/// <summary>
/// Non-linear pose correctives model.
/// Predicts vertex offsets based on joint rotations to improve mesh deformation.
/// </summary>
public class MhrPoseCorrectivesModel : Module<Tensor, Tensor>
{
    private readonly Sequential _poseDirsPredictor;

    public MhrPoseCorrectivesModel(Sequential poseDirsPredictor) : base("MhrPoseCorrectivesModel")
    {
        _poseDirsPredictor = poseDirsPredictor;
        RegisterComponents();
    }

    /// <summary>
    /// Compute pose features from joint parameters.
    /// Extracts Euler angles and converts to 6D rotation representation.
    /// </summary>
    /// <param name="jointParameters">Joint parameters tensor [batch, numJoints * paramsPerJoint]</param>
    /// <returns>Pose features tensor [batch, numPoseJoints * 6]</returns>
    private Tensor PoseFeaturesFromJointParams(Tensor jointParameters)
    {
        var batchSize = jointParameters.shape[0];

        // Reshape to [batch, numJoints, paramsPerJoint]
        var jointParams = jointParameters.reshape(batchSize, -1, MhrAssets.ParametersPerJoint);

        // Extract Euler rotations (indices 3:6) from joints 2 onwards (skip first 2 global joints)
        // jointParams[:, 2:, 3:6]
        var jointEulerAngles = jointParams[
            TensorIndex.Colon,
            TensorIndex.Slice(2, null),
            TensorIndex.Slice(3, 6)
        ];

        // Convert to 6D rotation representation
        var joint6dFeat = MhrUtils.Batch6DFromXYZ(jointEulerAngles);

        // Subtract identity rotation (set diagonal elements to 0 when no rotation)
        // In the 6D representation, elements 0 and 4 correspond to the diagonal
        joint6dFeat[TensorIndex.Colon, TensorIndex.Colon, 0] -= 1;
        joint6dFeat[TensorIndex.Colon, TensorIndex.Colon, 4] -= 1;

        // Flatten to [batch, numPoseJoints * 6]
        return joint6dFeat.flatten(1, 2);
    }

    /// <summary>
    /// Compute pose corrective offsets given joint parameters.
    /// </summary>
    /// <param name="jointParameters">Joint parameters [batch, numJoints * paramsPerJoint]</param>
    /// <returns>Vertex offsets [batch, numVerts, 3]</returns>
    public override Tensor forward(Tensor jointParameters)
    {
        var pose6dFeats = PoseFeaturesFromJointParams(jointParameters);
        var poseCorrectiveOffsets = _poseDirsPredictor.forward(pose6dFeats);

        // Reshape to [batch, numVerts, 3]
        return poseCorrectiveOffsets.reshape(pose6dFeats.shape[0], -1, 3);
    }
}
