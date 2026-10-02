namespace MHR.Net;

/// <summary>
/// Defines a single MHR parameter with its name, category, group, and value range.
/// </summary>
public struct MhrParamDef
{
    public MhrParamDef(string Name, string Category, string Group, float RangeMin, float RangeMax,
        float? LimitMin = null, float? LimitMax = null)
    {
        this.Name = Name;
        this.Category = Category;
        this.Group = Group;
        this.RangeMin = RangeMin;
        this.RangeMax = RangeMax;
        this.LimitMin = LimitMin;
        this.LimitMax = LimitMax;
    }

    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Group { get; init; } = "";

    /// <summary>Range used by sliders and sweeps.</summary>
    public float RangeMin { get; init; }
    public float RangeMax { get; init; }

    /// <summary>
    /// Official joint limit from the MHR model ([Limits] in compact_v6_1.model, also returned by
    /// the TorchScript model's get_parameter_limits()). Null when the model defines no limit.
    /// A [0, 0] limit means the parameter is locked in the default rig (e.g. *_flexible DOFs).
    /// </summary>
    public float? LimitMin { get; init; }
    public float? LimitMax { get; init; }
}

/// <summary>
/// Central registry of all MHR parameter names, categories, and value ranges.
/// Single source of truth used by MHR, MHR.Sweep, MHR.Identify, and MHR.Range.
/// </summary>
/// <remarks>
/// Name sources:
/// - Identity: no official names exist (upstream treats them as unnamed components); the names
///   below are heuristic labels. Ranges come from MHR.Range.
/// - Pose: official pymomentum parameter names, in model_parameters order, from the
///   [ParameterTransform] section of compact_v6_1.model. Ranges are the official limits where the
///   model defines a non-zero one, otherwise MHR.Range results. Rotations are in radians;
///   root_t* is in units of 10 cm. Character left is +X, up is +Y, forward is +Z.
/// - Expression: official semantic names from upstream mhr/face_expression.py
///   (FACE_EXPRESSION_NAMES, see docs/face-expressions.md). Index i maps to FBX shape_{45+i}.
///   _L/_R are the character's left/right; lip-quadrant suffixes add T (top) / B (bottom).
///   Ranges come from MHR.Range (upstream's typical range is -1..+1).
/// </remarks>
public static class MhrParameters
{
    public const int IdentityCount = 45;
    public const int PoseCount = 204;
    public const int ExpressionCount = 72;
    public const int TotalCount = IdentityCount + PoseCount + ExpressionCount;

    public const int PoseOffset = IdentityCount;
    public const int ExpressionOffset = IdentityCount + PoseCount;

    // Category constants
    const string Id = "Identity";
    const string Po = "Pose";
    const string Ex = "Expression";

    // Identity group constants
    const string BodyShape   = "Body Shape (20)";
    const string HeadShape   = "Head Shape (20)";
    const string Hands       = "Hands (5)";

    // Pose group constants
    const string Root             = "Root (6)";
    const string Spine            = "Spine (18)";
    const string NeckHead         = "Neck & Head (6)";
    const string RightArm         = "Right Arm (10)";
    const string LeftArm          = "Left Arm (10)";
    const string RightLeg         = "Right Leg (9)";
    const string LeftLeg          = "Left Leg (9)";
    const string RightHand        = "Right Hand (27)";
    const string LeftHand         = "Left Hand (27)";
    const string FlexibleFeet     = "Flexible Feet (8)";
    const string FlexibleBody     = "Flexible Body (6)";
    const string BodyScale        = "Body Scale (18)";
    const string RightFingerScale = "Right Finger Scale (25)";
    const string LeftFingerScale  = "Left Finger Scale (25)";

    // Expression group constants (expression indices are alphabetical, so groups are not contiguous)
    const string Brows      = "Brows (6)";
    const string Eyes       = "Eyes (14)";
    const string CheeksNose = "Cheeks & Nose (14)";
    const string JawChin    = "Jaw & Chin (6)";
    const string MouthLips  = "Mouth & Lips (32)";

    /// <summary>
    /// All 321 parameter definitions.
    /// </summary>
    public static readonly MhrParamDef[] All =
    [
        // === Identity: Body Shape (0-19) ===
        new("Mass",       Id, BodyShape, -3.869f, 3.3864f),
        new("Gender",         Id, BodyShape, -5f, 5f),
        new("Shoulders",    Id, BodyShape, -5f, 5f),
        new("Hips",         Id, BodyShape, -5f, 5f),
        new("Chest",        Id, BodyShape, -5f, 5f),
        new("Waist",        Id, BodyShape, -5f, 5f),
        new("Arms Length",  Id, BodyShape, -5f, 5f),
        new("Legs Length",  Id, BodyShape, -5f, 5f),
        new("Torso",        Id, BodyShape, -5f, 5f),
        new("Muscle",       Id, BodyShape, -5f, 5f),
        new("Body Fat",     Id, BodyShape, -5f, 5f),
        new("Limb Thick",   Id, BodyShape, -5f, 5f),
        new("Neck",         Id, BodyShape, -5f, 5f),
        new("Back Width",   Id, BodyShape, -5f, 5f),
        new("Posture",      Id, BodyShape, -5f, 5f),
        new("Ribcage",      Id, BodyShape, -5f, 5f),
        new("Pelvis",       Id, BodyShape, -5f, 5f),
        new("Proportion",   Id, BodyShape, -5f, 5f),
        new("Build",        Id, BodyShape, -5f, 5f),
        new("Frame",        Id, BodyShape, -5f, 5f),

        // === Identity: Head Shape (20-39) ===
        new("Head Size",    Id, HeadShape, -5f, 5f),
        new("Face Length",  Id, HeadShape, -5f, 4.3804f),
        new("Face Width",   Id, HeadShape, -5f, 5f),
        new("Jaw Width",    Id, HeadShape, -5f, 5f),
        new("Forehead",     Id, HeadShape, -5f, 5f),
        new("Cheekbones",   Id, HeadShape, -5f, 5f),
        new("Chin",         Id, HeadShape, -5f, 5f),
        new("Nose Size",    Id, HeadShape, -5f, 5f),
        new("Nose Bridge",  Id, HeadShape, -5f, 5f),
        new("Nose Width",   Id, HeadShape, -5f, 5f),
        new("Eye Distance", Id, HeadShape, -5f, 5f),
        new("Eye Size",     Id, HeadShape, -5f, 5f),
        new("Brow Ridge",   Id, HeadShape, -5f, 5f),
        new("Ears",         Id, HeadShape, -5f, 5f),
        new("Skull Shape",  Id, HeadShape, -5f, 5f),
        new("Temple",       Id, HeadShape, -5f, 5f),
        new("Face Depth",   Id, HeadShape, -5f, 5f),
        new("Mouth Width",  Id, HeadShape, -5f, 5f),
        new("Lip Size",     Id, HeadShape, -5f, 5f),
        new("Neck Thick",   Id, HeadShape, -5f, 5f),

        // === Identity: Hands (40-44) ===
        new("Hand Size",     Id, Hands, -5f, 5f),
        new("Palm Width",    Id, Hands, -5f, 5f),
        new("Finger Length", Id, Hands, -5f, 5f),
        new("Finger Thick",  Id, Hands, -5f, 5f),
        new("Knuckles",      Id, Hands, -5f, 5f),

        // === Pose: Root (45-50, pose index 0-5) ===
        new("root_tx",                   Po, Root,             -3.14f, 3.14f),
        new("root_ty",                   Po, Root,             -3.14f, 3.14f),
        new("root_tz",                   Po, Root,             -3.14f, 3.14f),
        new("root_rx",                   Po, Root,             -1.5708f, 1.5708f),
        new("root_ry",                   Po, Root,             -1.5708f, 1.5708f),
        new("root_rz",                   Po, Root,             -1.5708f, 1.5708f),

        // === Pose: Spine (51-68, pose index 6-23) ===
        new("spine0_rx_flexible",        Po, Spine,            -1.2934f, 1.2336f, 0f, 0f),
        new("spine_twist0",              Po, Spine,            -0.9f, 0.9f, -0.9f, 0.9f),
        new("spine0_ry_flexible",        Po, Spine,            -0.8059f, 0.7839f, 0f, 0f),
        new("spine_lean0",               Po, Spine,            -0.7f, 0.7f, -0.7f, 0.7f),
        new("spine0_rz_flexible",        Po, Spine,            -0.777f, 0.7754f, 0f, 0f),
        new("spine_bend0",               Po, Spine,            -0.5f, 1.5f, -0.5f, 1.5f),
        new("spine1_rx_flexible",        Po, Spine,            -1.4564f, 1.5041f, 0f, 0f),
        new("spine_twist1",              Po, Spine,            -0.9f, 0.9f, -0.9f, 0.9f),
        new("spine1_ry_flexible",        Po, Spine,            -0.931f, 1.0268f, 0f, 0f),
        new("spine_lean1",               Po, Spine,            -0.7f, 0.7f, -0.7f, 0.7f),
        new("spine1_rz_flexible",        Po, Spine,            -1.2454f, 0.9815f, 0f, 0f),
        new("spine_bend1",               Po, Spine,            -0.5f, 1.5f, -0.5f, 1.5f),
        new("spine2_rx_flexible",        Po, Spine,            -1.395f, 1.4978f, 0f, 0f),
        new("spine2_ry_flexible",        Po, Spine,            -1.1988f, 1.1791f, 0f, 0f),
        new("spine2_rz_flexible",        Po, Spine,            -1.3395f, 0.8442f, 0f, 0f),
        new("spine3_rx_flexible",        Po, Spine,            -0.9239f, 1.0222f, 0f, 0f),
        new("spine3_ry_flexible",        Po, Spine,            -0.7543f, 0.6728f, 0f, 0f),
        new("spine3_rz_flexible",        Po, Spine,            -1.2145f, 0.9363f, 0f, 0f),

        // === Pose: NeckHead (69-74, pose index 24-29) ===
        new("neck_twist",                Po, NeckHead,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("neck_lean",                 Po, NeckHead,         -0.5f, 0.5f, -0.5f, 0.5f),
        new("neck_bend",                 Po, NeckHead,         -0.6f, 0.5f, -0.6f, 0.5f),
        new("head_twist",                Po, NeckHead,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("head_lean",                 Po, NeckHead,         -0.3f, 0.3f, -0.3f, 0.3f),
        new("head_bend",                 Po, NeckHead,         -0.4f, 0.4f, -0.4f, 0.4f),

        // === Pose: RightArm (75-84, pose index 30-39) ===
        new("r_clavicle_rx",             Po, RightArm,         -1.5323f, 1.497f, 0f, 0f),
        new("r_clavicle_ry",             Po, RightArm,         0f, 0.5f, 0f, 0.5f),
        new("r_clavicle_rz",             Po, RightArm,         -0.3f, 0.3f, -0.3f, 0.3f),
        new("r_uparm_twist",             Po, RightArm,         -1.5f, 1.8f, -1.5f, 1.8f),
        new("r_uparm_ry",                Po, RightArm,         -1f, 1.2f, -1f, 1.2f),
        new("r_uparm_rz",                Po, RightArm,         -1.2f, 2f, -1.2f, 2f),
        new("r_elbow_bend",              Po, RightArm,         -0.5f, 2f, -0.5f, 2f),
        new("r_lowarm_twist",            Po, RightArm,         -2.2f, 1f, -2.2f, 1f),
        new("r_wrist_ry",                Po, RightArm,         -1.2f, 1.5f, -1.2f, 1.5f),
        new("r_wrist_rz",                Po, RightArm,         -2.2f, 1.5f, -2.2f, 1.5f),

        // === Pose: LeftArm (85-94, pose index 40-49) ===
        new("l_clavicle_rx",             Po, LeftArm,          -1.4752f, 1.3457f, 0f, 0f),
        new("l_clavicle_ry",             Po, LeftArm,          0f, 0.5f, 0f, 0.5f),
        new("l_clavicle_rz",             Po, LeftArm,          -0.3f, 0.3f, -0.3f, 0.3f),
        new("l_uparm_twist",             Po, LeftArm,          -1.5f, 1.8f, -1.5f, 1.8f),
        new("l_uparm_ry",                Po, LeftArm,          -1f, 1.2f, -1f, 1.2f),
        new("l_uparm_rz",                Po, LeftArm,          -1.2f, 2f, -1.2f, 2f),
        new("l_elbow_bend",              Po, LeftArm,          -0.5f, 2f, -0.5f, 2f),
        new("l_lowarm_twist",            Po, LeftArm,          -2.2f, 1f, -2.2f, 1f),
        new("l_wrist_ry",                Po, LeftArm,          -1.2f, 1.5f, -1.2f, 1.5f),
        new("l_wrist_rz",                Po, LeftArm,          -2.2f, 1.5f, -2.2f, 1.5f),

        // === Pose: RightLeg (95-103, pose index 50-58) ===
        new("r_upleg_twist",             Po, RightLeg,         -1.5f, 0.8f, -1.5f, 0.8f),
        new("r_upleg_ry",                Po, RightLeg,         -1.6f, 0.6f, -1.6f, 0.6f),
        new("r_upleg_rz",                Po, RightLeg,         -2.5f, 1f, -2.5f, 1f),
        new("r_knee_bend",               Po, RightLeg,         0f, 2.8f, 0f, 2.8f),
        new("r_lowleg_twist",            Po, RightLeg,         -1f, 1.2f, -1f, 1.2f),
        new("r_foot_bend",               Po, RightLeg,         -1f, 1f, -1f, 1f),
        new("r_foot_lean0",              Po, RightLeg,         -1f, 0.6f, -1f, 0.6f),
        new("r_foot_lean1",              Po, RightLeg,         -1.3785f, 0.7977f, 0f, 0f),
        new("r_ball_bend",               Po, RightLeg,         -0.7f, 0.3f, -0.7f, 0.3f),

        // === Pose: LeftLeg (104-112, pose index 59-67) ===
        new("l_upleg_twist",             Po, LeftLeg,          -1.5f, 0.8f, -1.5f, 0.8f),
        new("l_upleg_ry",                Po, LeftLeg,          -1.6f, 0.6f, -1.6f, 0.6f),
        new("l_upleg_rz",                Po, LeftLeg,          -2.5f, 1f, -2.5f, 1f),
        new("l_knee_bend",               Po, LeftLeg,          0f, 2.8f, 0f, 2.8f),
        new("l_lowleg_twist",            Po, LeftLeg,          -1f, 1.2f, -1f, 1.2f),
        new("l_foot_bend",               Po, LeftLeg,          -1f, 1f, -1f, 1f),
        new("l_foot_lean0",              Po, LeftLeg,          -1f, 0.6f, -1f, 0.6f),
        new("l_foot_lean1",              Po, LeftLeg,          -1.267f, 1.016f, 0f, 0f),
        new("l_ball_bend",               Po, LeftLeg,          -0.7f, 0.3f, -0.7f, 0.3f),

        // === Pose: RightHand (113-139, pose index 68-94) ===
        new("r_thumb0_ry",               Po, RightHand,        -1f, 1f, -1f, 1f),
        new("r_thumb0_rz",               Po, RightHand,        -1f, 1f, -1f, 1f),
        new("r_thumb1_rx",               Po, RightHand,        -0.5f, 0.5f, -0.5f, 0.5f),
        new("r_thumb1_ry",               Po, RightHand,        -0.5f, 0.5f, -0.5f, 0.5f),
        new("r_thumb1_rz",               Po, RightHand,        -0.2f, 1f, -0.2f, 1f),
        new("r_thumb2_rz",               Po, RightHand,        -0.3f, 1.57f, -0.3f, 1.57f),
        new("r_thumb3_rz",               Po, RightHand,        -0.3f, 1.57f, -0.3f, 1.57f),
        new("r_index1_ry",               Po, RightHand,        -0.8f, 0.8f, -0.8f, 0.8f),
        new("r_ring1_ry",                Po, RightHand,        -0.8f, 0.8f, -0.8f, 0.8f),
        new("r_pinky1_ry",               Po, RightHand,        -0.8f, 0.8f, -0.8f, 0.8f),
        new("r_middle1_ry",              Po, RightHand,        -0.8f, 0.8f, -0.8f, 0.8f),
        new("r_index1_rz",               Po, RightHand,        -0.78f, 1.57f, -0.78f, 1.57f),
        new("r_index2_rz",               Po, RightHand,        -0.2f, 1.57f, -0.2f, 1.57f),
        new("r_index3_rz",               Po, RightHand,        -0.1f, 1.57f, -0.1f, 1.57f),
        new("r_middle1_rz",              Po, RightHand,        -0.78f, 1.57f, -0.78f, 1.57f),
        new("r_middle2_rz",              Po, RightHand,        -0.2f, 1.57f, -0.2f, 1.57f),
        new("r_middle3_rz",              Po, RightHand,        -0.1f, 1.57f, -0.1f, 1.57f),
        new("r_ring1_rz",                Po, RightHand,        -0.78f, 1.57f, -0.78f, 1.57f),
        new("r_ring2_rz",                Po, RightHand,        -0.2f, 1.57f, -0.2f, 1.57f),
        new("r_ring3_rz",                Po, RightHand,        -0.1f, 1.57f, -0.1f, 1.57f),
        new("r_pinky1_rz",               Po, RightHand,        -1f, 1.57f, -1f, 1.57f),
        new("r_pinky2_rz",               Po, RightHand,        -0.2f, 1.57f, -0.2f, 1.57f),
        new("r_pinky3_rz",               Po, RightHand,        -0.1f, 1.57f, -0.1f, 1.57f),
        new("r_index1_rx",               Po, RightHand,        -0.5f, 0.5f, -0.5f, 0.5f),
        new("r_ring1_rx",                Po, RightHand,        -0.3f, 0.3f, -0.3f, 0.3f),
        new("r_pinky1_rx",               Po, RightHand,        -0.3f, 0.3f, -0.3f, 0.3f),
        new("r_middle1_rx",              Po, RightHand,        -0.3f, 0.3f, -0.3f, 0.3f),

        // === Pose: LeftHand (140-166, pose index 95-121) ===
        new("l_thumb0_ry",               Po, LeftHand,         -1f, 1f, -1f, 1f),
        new("l_thumb0_rz",               Po, LeftHand,         -1f, 1f, -1f, 1f),
        new("l_thumb1_rx",               Po, LeftHand,         -0.5f, 0.5f, -0.5f, 0.5f),
        new("l_thumb1_ry",               Po, LeftHand,         -0.5f, 0.5f, -0.5f, 0.5f),
        new("l_thumb1_rz",               Po, LeftHand,         -0.2f, 1f, -0.2f, 1f),
        new("l_thumb2_rz",               Po, LeftHand,         -0.3f, 1.57f, -0.3f, 1.57f),
        new("l_thumb3_rz",               Po, LeftHand,         -0.3f, 1.57f, -0.3f, 1.57f),
        new("l_index1_ry",               Po, LeftHand,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("l_ring1_ry",                Po, LeftHand,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("l_pinky1_ry",               Po, LeftHand,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("l_middle1_ry",              Po, LeftHand,         -0.8f, 0.8f, -0.8f, 0.8f),
        new("l_index1_rz",               Po, LeftHand,         -0.78f, 1.57f, -0.78f, 1.57f),
        new("l_index2_rz",               Po, LeftHand,         -0.2f, 1.57f, -0.2f, 1.57f),
        new("l_index3_rz",               Po, LeftHand,         -0.1f, 1.57f, -0.1f, 1.57f),
        new("l_middle1_rz",              Po, LeftHand,         -0.78f, 1.57f, -0.78f, 1.57f),
        new("l_middle2_rz",              Po, LeftHand,         -0.2f, 1.57f, -0.2f, 1.57f),
        new("l_middle3_rz",              Po, LeftHand,         -0.1f, 1.57f, -0.1f, 1.57f),
        new("l_ring1_rz",                Po, LeftHand,         -0.78f, 1.57f, -0.78f, 1.57f),
        new("l_ring2_rz",                Po, LeftHand,         -0.2f, 1.57f, -0.2f, 1.57f),
        new("l_ring3_rz",                Po, LeftHand,         -0.1f, 1.57f, -0.1f, 1.57f),
        new("l_pinky1_rz",               Po, LeftHand,         -1f, 1.57f, -1f, 1.57f),
        new("l_pinky2_rz",               Po, LeftHand,         -0.2f, 1.57f, -0.2f, 1.57f),
        new("l_pinky3_rz",               Po, LeftHand,         -0.1f, 1.57f, -0.1f, 1.57f),
        new("l_index1_rx",               Po, LeftHand,         -0.5f, 0.5f, -0.5f, 0.5f),
        new("l_ring1_rx",                Po, LeftHand,         -0.3f, 0.3f, -0.3f, 0.3f),
        new("l_pinky1_rx",               Po, LeftHand,         -0.3f, 0.3f, -0.3f, 0.3f),
        new("l_middle1_rx",              Po, LeftHand,         -0.3f, 0.3f, -0.3f, 0.3f),

        // === Pose: FlexibleFeet (167-174, pose index 122-129) ===
        new("l_foot_ry_flexible",        Po, FlexibleFeet,     -1.3264f, 1.395f, 0f, 0f),
        new("l_subtalar_rz_flexible",    Po, FlexibleFeet,     -0.9606f, 1.2092f, 0f, 0f),
        new("l_talocrural_rx_flexible",  Po, FlexibleFeet,     -1.4625f, 1.4294f, 0f, 0f),
        new("l_ball_rx_flexible",        Po, FlexibleFeet,     -1.0144f, 1.1297f, 0f, 0f),
        new("r_foot_ry_flexible",        Po, FlexibleFeet,     -1.2644f, 1.2188f, 0f, 0f),
        new("r_subtalar_rz_flexible",    Po, FlexibleFeet,     -1.0142f, 1.2631f, 0f, 0f),
        new("r_talocrural_rx_flexible",  Po, FlexibleFeet,     -1.4288f, 1.4719f, 0f, 0f),
        new("r_ball_rx_flexible",        Po, FlexibleFeet,     -1.1732f, 0.9112f, 0f, 0f),

        // === Pose: FlexibleBody (175-180, pose index 130-135) ===
        new("spine_length_flexible",     Po, FlexibleBody,     -2.6613f, 3.14f, 0f, 0f),
        new("neck_length_flexible",      Po, FlexibleBody,     -0.7242f, 3.14f, 0f, 0f),
        new("shoulder_width_flexible",   Po, FlexibleBody,     -0.5866f, 3.14f, 0f, 0f),
        new("arm_length_flexible",       Po, FlexibleBody,     -2.6571f, 3.14f, 0f, 0f),
        new("hip_width_flexible",        Po, FlexibleBody,     -0.6014f, 3.14f, 0f, 0f),
        new("leg_length_flexible",       Po, FlexibleBody,     -3.14f, 3.14f, 0f, 0f),

        // === Pose: BodyScale (181-198, pose index 136-153) ===
        new("scale_eye_width",           Po, BodyScale,        -3.14f, 3.14f, 0f, 0f),
        new("scale_eye_height",          Po, BodyScale,        -3.14f, 3.14f, 0f, 0f),
        new("scale_eye_depth",           Po, BodyScale,        -3.14f, 3.14f, 0f, 0f),
        new("scale_spine_length",        Po, BodyScale,        -1.1f, 1.1f, -1.1f, 1.1f),
        new("scale_neck_length",         Po, BodyScale,        -0.4f, 0.4f, -0.4f, 0.4f),
        new("scale_shoulder_width",      Po, BodyScale,        -0.2f, 0.2f, -0.2f, 0.2f),
        new("scale_uparms",              Po, BodyScale,        -1f, 1f, -1f, 1f),
        new("scale_lowarms",             Po, BodyScale,        -1f, 1f, -1f, 1f),
        new("scale_r_hands",             Po, BodyScale,        -0.2f, 0.2f, -0.2f, 0.2f),
        new("scale_l_hands",             Po, BodyScale,        -0.2f, 0.2f, -0.2f, 0.2f),
        new("scale_hip_width",           Po, BodyScale,        -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_hip_height",          Po, BodyScale,        -3.14f, 1.3952f, 0f, 0f),
        new("scale_hip_depth",           Po, BodyScale,        -3.14f, 3.14f, 0f, 0f),
        new("scale_uplegs",              Po, BodyScale,        -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_lowlegs",             Po, BodyScale,        -1f, 1f, -1f, 1f),
        new("scale_knee_knock",          Po, BodyScale,        -1.4264f, 1.4641f, 0f, 0f),
        new("scale_ankle_height",        Po, BodyScale,        -3.14f, 0.6754f, 0f, 0f),
        new("scale_foot_length",         Po, BodyScale,        -0.1f, 0.1f, -0.1f, 0.1f),

        // === Pose: RightFingerScale (199-223, pose index 154-178) ===
        new("scale_r_index1_length",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_middle1_length",    Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_ring1_length",      Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_pinky1_length",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_thumb1_length",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_index1_offset",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_middle1_offset",    Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_ring1_offset",      Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_pinky1_offset",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_thumb1_offset",     Po, RightFingerScale, -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_r_index2_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_middle2_length",    Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_ring2_length",      Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_pinky2_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_thumb2_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_index3_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_middle3_length",    Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_ring3_length",      Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_pinky3_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_thumb3_length",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_index_null_tx",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_middle_null_tx",    Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_ring_null_tx",      Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_pinky_null_tx",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_r_thumb_null_tx",     Po, RightFingerScale, -0.25f, 0.25f, -0.25f, 0.25f),

        // === Pose: LeftFingerScale (224-248, pose index 179-203) ===
        new("scale_l_index1_length",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_middle1_length",    Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_ring1_length",      Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_pinky1_length",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_thumb1_length",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_index1_offset",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_middle1_offset",    Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_ring1_offset",      Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_pinky1_offset",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_thumb1_offset",     Po, LeftFingerScale,  -0.5f, 0.5f, -0.5f, 0.5f),
        new("scale_l_index2_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_middle2_length",    Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_ring2_length",      Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_pinky2_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_thumb2_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_index3_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_middle3_length",    Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_ring3_length",      Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_pinky3_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_thumb3_length",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_index_null_tx",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_middle_null_tx",    Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_ring_null_tx",      Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_pinky_null_tx",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),
        new("scale_l_thumb_null_tx",     Po, LeftFingerScale,  -0.25f, 0.25f, -0.25f, 0.25f),

        // === Expression (249-320, expression index 0-71) ===
        new("browLowerer_L",           Ex, Brows,      -2f, 0.8189f),
        new("browLowerer_R",           Ex, Brows,      -2f, 0.8304f),
        new("cheekPuff_L",             Ex, CheeksNose, -2f, 2f),
        new("cheekPuff_R",             Ex, CheeksNose, -2f, 2f),
        new("cheekRaiser_L",           Ex, CheeksNose, -2f, 1.8247f),
        new("cheekRaiser_R",           Ex, CheeksNose, -2f, 1.7982f),
        new("cheekSuck_L",             Ex, CheeksNose, -2f, 2f),
        new("cheekSuck_R",             Ex, CheeksNose, -2f, 2f),
        new("chinRaiser_B",            Ex, JawChin,    -0.3147f, 1.428f),
        new("chinRaiser_T",            Ex, JawChin,    -1.0916f, 0.3827f),
        new("dimpler_L",               Ex, MouthLips,  -2f, 1.9575f),
        new("dimpler_R",               Ex, MouthLips,  -2f, 1.936f),
        new("eyesClosed_L",            Ex, Eyes,       -0.5439f, 1.0289f),
        new("eyesClosed_R",            Ex, Eyes,       -0.546f, 1.0328f),
        new("eyesLookDown_L",          Ex, Eyes,       -0.4668f, 1.5428f),
        new("eyesLookDown_R",          Ex, Eyes,       -0.4479f, 1.5483f),
        new("eyesLookLeft_L",          Ex, Eyes,       -2f, 2f),
        new("eyesLookLeft_R",          Ex, Eyes,       -2f, 2f),
        new("eyesLookRight_L",         Ex, Eyes,       -2f, 2f),
        new("eyesLookRight_R",         Ex, Eyes,       -2f, 2f),
        new("eyesLookUp_L",            Ex, Eyes,       -2f, 0.8296f),
        new("eyesLookUp_R",            Ex, Eyes,       -2f, 0.8694f),
        new("innerBrowRaiser_L",       Ex, Brows,      -0.8954f, 2f),
        new("innerBrowRaiser_R",       Ex, Brows,      -0.8927f, 2f),
        new("jawDrop",                 Ex, JawChin,    -0.6675f, 0.1095f),
        new("jawSidewaysLeft",         Ex, JawChin,    -1.7635f, 2f),
        new("jawSidewaysRight",        Ex, JawChin,    -2f, 2f),
        new("jawThrust",               Ex, JawChin,    -0.6026f, 2f),
        new("lidTightener_L",          Ex, Eyes,       -2f, 1.2943f),
        new("lidTightener_R",          Ex, Eyes,       -2f, 1.3069f),
        new("lipCornerDepressor_L",    Ex, MouthLips,  -2f, 2f),
        new("lipCornerDepressor_R",    Ex, MouthLips,  -2f, 2f),
        new("lipCornerPuller_L",       Ex, MouthLips,  -1.3084f, 1.7027f),
        new("lipCornerPuller_R",       Ex, MouthLips,  -1.269f, 1.7724f),
        new("lipFunneler_LB",          Ex, MouthLips,  -0.5916f, 1.0735f),
        new("lipFunneler_LT",          Ex, MouthLips,  -0.7962f, 1.0271f),
        new("lipFunneler_RB",          Ex, MouthLips,  -0.6014f, 0.9951f),
        new("lipFunneler_RT",          Ex, MouthLips,  -0.7598f, 1.0698f),
        new("lipPressor_L",            Ex, MouthLips,  -1.0503f, 2f),
        new("lipPressor_R",            Ex, MouthLips,  -1.0944f, 2f),
        new("lipPucker_L",             Ex, MouthLips,  -0.4019f, 1.0923f),
        new("lipPucker_R",             Ex, MouthLips,  -0.3947f, 1.089f),
        new("lipStretcher_L",          Ex, MouthLips,  -2f, 2f),
        new("lipStretcher_R",          Ex, MouthLips,  -2f, 2f),
        new("lipSuck_LB",              Ex, MouthLips,  -1.281f, 0.8283f),
        new("lipSuck_LT",              Ex, MouthLips,  -1.089f, 0.5693f),
        new("lipSuck_RB",              Ex, MouthLips,  -1.2317f, 0.8696f),
        new("lipSuck_RT",              Ex, MouthLips,  -1.0789f, 0.5693f),
        new("lipTightener_L",          Ex, MouthLips,  -2f, 2f),
        new("lipTightener_R",          Ex, MouthLips,  -2f, 2f),
        new("lipsToward_LB",           Ex, MouthLips,  -0.4059f, 1.3431f),
        new("lipsToward_LT",           Ex, MouthLips,  -0.7947f, 0.8535f),
        new("lipsToward_RB",           Ex, MouthLips,  -0.4112f, 1.3934f),
        new("lipsToward_RT",           Ex, MouthLips,  -0.7984f, 0.8366f),
        new("lowerLipDepressor_L",     Ex, MouthLips,  -2f, 2f),
        new("lowerLipDepressor_R",     Ex, MouthLips,  -2f, 2f),
        new("mouthLeft",               Ex, MouthLips,  -2f, 2f),
        new("mouthRight",              Ex, MouthLips,  -2f, 2f),
        new("nasolabialFurrow_L",      Ex, CheeksNose, -2f, 2f),
        new("nasolabialFurrow_R",      Ex, CheeksNose, -2f, 2f),
        new("noseWrinkler_L",          Ex, CheeksNose, -2f, 2f),
        new("noseWrinkler_R",          Ex, CheeksNose, -2f, 2f),
        new("nostrilCompressor_L",     Ex, CheeksNose, -2f, 2f),
        new("nostrilCompressor_R",     Ex, CheeksNose, -2f, 2f),
        new("nostrilDilator_L",        Ex, CheeksNose, -2f, 2f),
        new("nostrilDilator_R",        Ex, CheeksNose, -2f, 2f),
        new("outerBrowRaiser_L",       Ex, Brows,      -0.7274f, 2f),
        new("outerBrowRaiser_R",       Ex, Brows,      -0.7238f, 2f),
        new("upperLidRaiser_L",        Ex, Eyes,       -2f, 2f),
        new("upperLidRaiser_R",        Ex, Eyes,       -2f, 2f),
        new("upperLipRaiser_L",        Ex, MouthLips,  -2f, 0.7983f),
        new("upperLipRaiser_R",        Ex, MouthLips,  -2f, 0.7296f),
    ];

    /// <summary>
    /// Group definitions derived from All, in order of first appearance:
    /// (category, groupName, global parameter indices).
    /// </summary>
    public static readonly (string Category, string Group, int[] Indices)[] Groups = BuildGroups();

    /// <summary>
    /// Get the global index of a parameter by its name, or -1 if not found.
    /// </summary>
    public static int IndexOf(string name) => Array.FindIndex(All, p => p.Name == name);

    private static (string Category, string Group, int[] Indices)[] BuildGroups()
    {
        var groups = new List<(string Category, string Group, List<int> Indices)>();
        for (int i = 0; i < All.Length; i++)
        {
            int g = groups.FindIndex(x => x.Category == All[i].Category && x.Group == All[i].Group);
            if (g < 0)
            {
                groups.Add((All[i].Category, All[i].Group, new List<int>()));
                g = groups.Count - 1;
            }
            groups[g].Indices.Add(i);
        }
        return groups.Select(x => (x.Category, x.Group, x.Indices.ToArray())).ToArray();
    }
}
