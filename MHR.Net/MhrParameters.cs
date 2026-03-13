namespace MHR.Net;

/// <summary>
/// Defines a single MHR parameter with its name, category, group, and value range.
/// </summary>
public struct MhrParamDef
{
    public MhrParamDef(string Name, string Category, string Group, float RangeMin, float RangeMax)
    {
        this.Name = Name;
        this.Category = Category;
        this.Group = Group;
        this.RangeMin = RangeMin;
        this.RangeMax = RangeMax;
    }

    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Group { get; init; } = "";
    public float RangeMin { get; init; }
    public float RangeMax { get; init; }
}

/// <summary>
/// Central registry of all MHR parameter names, categories, and value ranges.
/// Single source of truth used by MHR, MHR.Sweep, MHR.Identify, and MHR.Range.
/// </summary>
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

    // Group constants
    const string BodyShape   = "Body Shape (20)";
    const string HeadShape   = "Head Shape (20)";
    const string Hands       = "Hands (5)";
    const string SpineTorso  = "Spine & Torso (30)";
    const string HeadNeck    = "Head & Neck (20)";
    const string LeftArm     = "Left Arm (30)";
    const string RightArm    = "Right Arm (30)";
    const string LeftLeg     = "Left Leg (30)";
    const string RightLeg    = "Right Leg (30)";
    const string HandsDetail = "Hands Detail (34)";
    const string Eyes        = "Eyes (12)";
    const string Eyebrows    = "Eyebrows (8)";
    const string Mouth       = "Mouth (24)";
    const string Jaw         = "Jaw (8)";
    const string CheeksNose  = "Cheeks/Nose (20)";

    /// <summary>
    /// All 321 parameter definitions.
    /// </summary>
    public static readonly MhrParamDef[] All =
    [
        // === Identity: Body Shape (0-19) ===
        new("Height",       Id, BodyShape, -1.5256f, 1.5256f),
        new("Mass",         Id, BodyShape, -3.976f,  3.976f),
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
        new("Head Size",    Id, HeadShape, -4.7252f, 4.7252f),
        new("Face Length",  Id, HeadShape, -5f, 5f),
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

        // === Pose: Spine & Torso (45-74) ===
        new("Pelvis Tilt",    Po, SpineTorso, -0.5f, 0.5f),
        new("Pelvis Roll",    Po, SpineTorso, -0.5f, 0.5f),
        new("Pelvis Twist",   Po, SpineTorso, -0.5f, 0.5f),
        new("Pelvis X",       Po, SpineTorso, -0.0538f, 0.0538f),
        new("Pelvis Y",       Po, SpineTorso, -0.0723f, 0.0723f),
        new("Pelvis Z",       Po, SpineTorso, -0.0524f, 0.0524f),
        new("Spine1 Bend",    Po, SpineTorso, -0.071f, 0.071f),
        new("Spine1 Side",    Po, SpineTorso, -0.203f, 0.203f),
        new("Spine1 Twist",   Po, SpineTorso, -0.0642f, 0.0642f),
        new("Spine2 Bend",    Po, SpineTorso, -1.5f, 1.5f),//-0.1532f, 0.1532f),
        new("Spine2 Side",    Po, SpineTorso, -0.0636f, 0.0636f),
        new("Spine2 Twist",   Po, SpineTorso, -0.1128f, 0.1128f),
        new("Spine3 Bend",    Po, SpineTorso, -0.0716f, 0.0716f),
        new("Spine3 Side",    Po, SpineTorso, -0.1093f, 0.1093f),
        new("Spine3 Twist",   Po, SpineTorso, -0.0748f, 0.0748f),
        new("Chest Bend",     Po, SpineTorso, -0.1367f, 0.1367f),
        new("Chest Side",     Po, SpineTorso, -0.0741f, 0.0741f),
        new("Chest Twist",    Po, SpineTorso, -0.2209f, 0.2209f),
        new("Torso X",        Po, SpineTorso, -0.0708f, 0.0708f),
        new("Torso Y",        Po, SpineTorso, -0.0744f, 0.0744f),
        new("Torso Z",        Po, SpineTorso, -0.0876f, 0.0876f),
        new("Torso Rot X",    Po, SpineTorso, -0.0715f, 0.0715f),
        new("Torso Rot Y",    Po, SpineTorso, -0.0677f, 0.0677f),
        new("Torso Rot Z",    Po, SpineTorso, -0.1169f, 0.1169f),
        new("Hip Thrust",     Po, SpineTorso, -0.4132f, 0.4132f),
        new("Belly",          Po, SpineTorso, -0.1741f, 0.1741f),
        new("Back Arch",      Po, SpineTorso, -0.1738f, 0.1738f),
        new("Shoulder Shrug", Po, SpineTorso, -0.416f, 0.416f),
        new("Rib Expand",     Po, SpineTorso, -0.2643f, 0.2643f),
        new("Core Twist",     Po, SpineTorso, -0.264f, 0.264f),

        // === Pose: Head & Neck (75-94) ===
        new("Neck Bend",    Po, HeadNeck, -0.0783f, 0.0783f),
        new("Neck Side",    Po, HeadNeck, -0.083f, 0.083f),
        new("Neck Twist",   Po, HeadNeck, -0.0799f, 0.0799f),
        new("Head Nod",     Po, HeadNeck, -0.191f, 0.191f),
        new("Head Tilt",    Po, HeadNeck, -0.0785f, 0.0785f),
        new("Head Turn",    Po, HeadNeck, -0.074f, 0.074f),
        new("Head X",       Po, HeadNeck, -0.1121f, 0.1121f),
        new("Head Y",       Po, HeadNeck, -0.4794f, 0.4794f),
        new("Head Z",       Po, HeadNeck, -0.2818f, 0.2818f),
        new("Jaw Pose",     Po, HeadNeck, -0.2818f, 0.2818f),
        new("Eye L X",      Po, HeadNeck, -0.0783f, 0.0783f),
        new("Eye L Y",      Po, HeadNeck, -0.083f, 0.083f),
        new("Eye R X",      Po, HeadNeck, -0.0799f, 0.0799f),
        new("Eye R Y",      Po, HeadNeck, -0.191f, 0.191f),
        new("Neck Base",    Po, HeadNeck, -0.0785f, 0.0785f),
        new("Neck Mid",     Po, HeadNeck, -0.074f, 0.074f),
        new("Head Roll",    Po, HeadNeck, -0.1121f, 0.1121f),
        new("Skull",        Po, HeadNeck, -0.4794f, 0.4794f),
        new("Face Pose 1",  Po, HeadNeck, -0.2818f, 0.2818f),
        new("Face Pose 2",  Po, HeadNeck, -0.2818f, 0.2818f),

        // === Pose: Left Arm (95-124) ===
        new("L Clav Fwd",       Po, LeftArm, -0.2607f, 0.2607f),
        new("L Clav Up",        Po, LeftArm, -0.0545f, 0.0545f),
        new("L Clav Twist",     Po, LeftArm, -0.0543f, 0.0543f),
        new("L Shoulder Fwd",   Po, LeftArm, -0.0977f, 0.0977f),
        new("L Shoulder Out",   Po, LeftArm, -0.2549f, 0.2549f),
        new("L Shoulder Twist", Po, LeftArm, -0.2487f, 0.2487f),
        new("L Elbow Bend",     Po, LeftArm, -0.5913f, 0.5913f),
        new("L Elbow Twist",    Po, LeftArm, -0.4846f, 0.4846f),
        new("L Wrist Bend",     Po, LeftArm, -0.9681f, 0.9681f),
        new("L Wrist Side",     Po, LeftArm, -0.2607f, 0.2607f),
        new("L Wrist Twist",    Po, LeftArm, -0.0545f, 0.0545f),
        new("L Arm X",          Po, LeftArm, -0.0543f, 0.0543f),
        new("L Arm Y",          Po, LeftArm, -0.0976f, 0.0976f),
        new("L Arm Z",          Po, LeftArm, -0.2567f, 0.2567f),
        new("L Upper Rot",      Po, LeftArm, -0.2455f, 0.2455f),
        new("L Forearm Rot",    Po, LeftArm, -0.5977f, 0.5977f),
        new("L Hand Rot",       Po, LeftArm, -0.4972f, 0.4972f),
        new("L Shoulder Roll",  Po, LeftArm, -0.9805f, 0.9805f),
        new("L Bicep",          Po, LeftArm, -0.427f, 0.427f),
        new("L Tricep",         Po, LeftArm, -0.4261f, 0.4261f),
        new("L Forearm 1",      Po, LeftArm, -2.676f, 2.676f),
        new("L Forearm 2",      Po, LeftArm, -0.5353f, 0.5353f),
        new("L Elbow Pose",     Po, LeftArm, -0.5383f, 0.5383f),
        new("L Wrist Pose",     Po, LeftArm, -0.822f, 0.822f),
        new("L Arm IK 1",       Po, LeftArm, -1.9052f, 1.9052f),
        new("L Arm IK 2",       Po, LeftArm, -0.5808f, 0.5808f),
        new("L Arm IK 3",       Po, LeftArm, -0.5538f, 0.5538f),
        new("L Arm IK 4",       Po, LeftArm, -0.6784f, 0.6784f),
        new("L Arm IK 5",       Po, LeftArm, -0.5199f, 0.5199f),
        new("L Arm IK 6",       Po, LeftArm, -0.5813f, 0.5813f),

        // === Pose: Right Arm (125-154) ===
        new("R Clav Fwd",       Po, RightArm, -1.0911f, 1.0911f),
        new("R Clav Up",        Po, RightArm, -2.1442f, 2.1442f),
        new("R Clav Twist",     Po, RightArm, -0.5209f, 0.5209f),
        new("R Shoulder Fwd",   Po, RightArm, -0.941f, 0.941f),
        new("R Shoulder Out",   Po, RightArm, -1.9858f, 1.9858f),
        new("R Shoulder Twist", Po, RightArm, -0.5536f, 0.5536f),
        new("R Elbow Bend",     Po, RightArm, -0.9683f, 0.9683f),
        new("R Elbow Twist",    Po, RightArm, -2.0823f, 2.0823f),
        new("R Wrist Bend",     Po, RightArm, -0.6754f, 0.6754f),
        new("R Wrist Side",     Po, RightArm, -1.2205f, 1.2205f),
        new("R Wrist Twist",    Po, RightArm, -2.2359f, 2.2359f),
        new("R Arm X",          Po, RightArm, -3.14f, 3.14f),
        new("R Arm Y",          Po, RightArm, -3.14f, 3.14f),
        new("R Arm Z",          Po, RightArm, -3.14f, 3.14f),
        new("R Upper Rot",      Po, RightArm, -3.14f, 3.14f),
        new("R Forearm Rot",    Po, RightArm, -0.427f, 0.427f),
        new("R Hand Rot",       Po, RightArm, -0.4261f, 0.4261f),
        new("R Shoulder Roll",  Po, RightArm, -2.6864f, 2.6864f),
        new("R Bicep",          Po, RightArm, -0.5365f, 0.5365f),
        new("R Tricep",         Po, RightArm, -0.5393f, 0.5393f),
        new("R Forearm 1",      Po, RightArm, -0.8261f, 0.8261f),
        new("R Forearm 2",      Po, RightArm, -1.9004f, 1.9004f),
        new("R Elbow Pose",     Po, RightArm, -0.5809f, 0.5809f),
        new("R Wrist Pose",     Po, RightArm, -0.5538f, 0.5538f),
        new("R Arm IK 1",       Po, RightArm, -0.6765f, 0.6765f),
        new("R Arm IK 2",       Po, RightArm, -0.5199f, 0.5199f),
        new("R Arm IK 3",       Po, RightArm, -0.5811f, 0.5811f),
        new("R Arm IK 4",       Po, RightArm, -1.0892f, 1.0892f),
        new("R Arm IK 5",       Po, RightArm, -2.1497f, 2.1497f),
        new("R Arm IK 6",       Po, RightArm, -0.5213f, 0.5213f),

        // === Pose: Left Leg (155-184) ===
        new("L Hip Fwd",    Po, LeftLeg, -0.9426f, 0.9426f),
        new("L Hip Out",    Po, LeftLeg, -2.0097f, 2.0097f),
        new("L Hip Twist",  Po, LeftLeg, -0.5534f, 0.5534f),
        new("L Knee Bend",  Po, LeftLeg, -0.9685f, 0.9685f),
        new("L Knee Twist", Po, LeftLeg, -2.0988f, 2.0988f),
        new("L Ankle Bend", Po, LeftLeg, -0.6738f, 0.6738f),
        new("L Ankle Side",  Po, LeftLeg, -1.2151f, 1.2151f),
        new("L Ankle Twist", Po, LeftLeg, -2.2445f, 2.2445f),
        new("L Toe Bend",   Po, LeftLeg, -3.14f, 3.14f),
        new("L Toe Twist",  Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg X",      Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg Y",      Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg Z",      Po, LeftLeg, -0.5736f, 0.5736f),
        new("L Thigh Rot",  Po, LeftLeg, -0.2601f, 0.2601f),
        new("L Calf Rot",   Po, LeftLeg, -0.2486f, 0.2486f),
        new("L Foot Rot",   Po, LeftLeg, -0.7863f, 0.7863f),
        new("L Hip Roll",   Po, LeftLeg, -0.5623f, 0.5623f),
        new("L Glute",      Po, LeftLeg, -0.2626f, 0.2626f),
        new("L Quad",       Po, LeftLeg, -0.2505f, 0.2505f),
        new("L Hamstring",  Po, LeftLeg, -0.6787f, 0.6787f),
        new("L Calf 1",     Po, LeftLeg, -0.5f, 0.5f),
        new("L Calf 2",     Po, LeftLeg, -0.5f, 0.5f),
        new("L Knee Pose",  Po, LeftLeg, -0.5003f, 0.5003f),
        new("L Ankle Pose", Po, LeftLeg, -0.5234f, 0.5234f),
        new("L Leg IK 1",   Po, LeftLeg, -0.5f, 0.5f),
        new("L Leg IK 2",   Po, LeftLeg, -0.5f, 0.5f),
        new("L Leg IK 3",   Po, LeftLeg, -1f, 1f),
        new("L Leg IK 4",   Po, LeftLeg, -1f, 1f),
        new("L Leg IK 5",   Po, LeftLeg, -1f, 1f),
        new("L Leg IK 6",   Po, LeftLeg, -0.5f, 0.5f),

        // === Pose: Right Leg (185-214) ===
        new("R Hip Fwd",    Po, RightLeg, -0.5f, 0.5f),
        new("R Hip Out",    Po, RightLeg, -0.5003f, 0.5003f),
        new("R Hip Twist",  Po, RightLeg, -0.5f, 0.5f),
        new("R Knee Bend",  Po, RightLeg, -0.5f, 0.5f),
        new("R Knee Twist", Po, RightLeg, -0.392f, 0.392f),
        new("R Ankle Bend", Po, RightLeg, -0.392f, 0.392f),
        new("R Ankle Side",  Po, RightLeg, -0.5f, 0.5f),
        new("R Ankle Twist", Po, RightLeg, -0.5f, 0.5f),
        new("R Toe Bend",   Po, RightLeg, -0.5f, 0.5f),
        new("R Toe Twist",  Po, RightLeg, -0.5f, 0.5f),
        new("R Leg X",      Po, RightLeg, -0.5f, 0.5f),
        new("R Leg Y",      Po, RightLeg, -0.1003f, 0.1003f),
        new("R Leg Z",      Po, RightLeg, -0.4942f, 0.4942f),
        new("R Thigh Rot",  Po, RightLeg, -0.3202f, 0.3202f),
        new("R Calf Rot",   Po, RightLeg, -3.14f, 3.14f),
        new("R Foot Rot",   Po, RightLeg, -3.14f, 3.14f),
        new("R Hip Roll",   Po, RightLeg, -3.14f, 3.14f),
        new("R Glute",      Po, RightLeg, -3.14f, 3.14f),
        new("R Quad",       Po, RightLeg, -3.14f, 3.14f),
        new("R Hamstring",  Po, RightLeg, -3.14f, 3.14f),
        new("R Calf 1",     Po, RightLeg, -3.14f, 3.14f),
        new("R Calf 2",     Po, RightLeg, -3.14f, 3.14f),
        new("R Knee Pose",  Po, RightLeg, -3.14f, 3.14f),
        new("R Ankle Pose", Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 1",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 2",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 3",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 4",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 5",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 6",   Po, RightLeg, -3.14f, 3.14f),

        // === Pose: Hands Detail (215-248) ===
        new("L Thumb 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Thumb 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Thumb 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Index 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Index 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Index 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Middle 1",      Po, HandsDetail, -3.14f, 3.14f),
        new("L Middle 2",      Po, HandsDetail, -3.14f, 3.14f),
        new("L Middle 3",      Po, HandsDetail, -3.14f, 3.14f),
        new("L Ring 1",        Po, HandsDetail, -3.14f, 3.14f),
        new("L Ring 2",        Po, HandsDetail, -3.14f, 3.14f),
        new("L Ring 3",        Po, HandsDetail, -3.14f, 3.14f),
        new("L Pinky 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Pinky 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Pinky 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Palm",          Po, HandsDetail, -3.14f, 3.14f),
        new("L Finger Splay",  Po, HandsDetail, -3.14f, 3.14f),
        new("R Thumb 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Thumb 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Thumb 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Index 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Index 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Index 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Middle 1",      Po, HandsDetail, -3.14f, 3.14f),
        new("R Middle 2",      Po, HandsDetail, -3.14f, 3.14f),
        new("R Middle 3",      Po, HandsDetail, -3.14f, 3.14f),
        new("R Ring 1",        Po, HandsDetail, -3.14f, 3.14f),
        new("R Ring 2",        Po, HandsDetail, -3.14f, 3.14f),
        new("R Ring 3",        Po, HandsDetail, -3.14f, 3.14f),
        new("R Pinky 1",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Pinky 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Pinky 3",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Palm",          Po, HandsDetail, -3.14f, 3.14f),
        new("R Finger Splay",  Po, HandsDetail, -3.14f, 3.14f),

        // === Expression: Eyes (249-260) ===
        new("Blink L",    Ex, Eyes, -2f, 2f),
        new("Blink R",    Ex, Eyes, -2f, 2f),
        new("Squint L",   Ex, Eyes, -2f, 2f),
        new("Squint R",   Ex, Eyes, -2f, 2f),
        new("Wide L",     Ex, Eyes, -2f, 2f),
        new("Wide R",     Ex, Eyes, -2f, 2f),
        new("Look Up",    Ex, Eyes, -2f, 2f),
        new("Look Down",  Ex, Eyes, -2f, 2f),
        new("Look Left",  Ex, Eyes, -2f, 2f),
        new("Look Right", Ex, Eyes, -2f, 2f),
        new("Pupil L",    Ex, Eyes, -2f, 2f),
        new("Pupil R",    Ex, Eyes, -2f, 2f),

        // === Expression: Eyebrows (261-268) ===
        new("Brow Up L",   Ex, Eyebrows, -2f, 2f),
        new("Brow Up R",   Ex, Eyebrows, -2f, 2f),
        new("Brow Down L", Ex, Eyebrows, -2f, 2f),
        new("Brow Down R", Ex, Eyebrows, -2f, 2f),
        new("Brow In L",   Ex, Eyebrows, -2f, 2f),
        new("Brow In R",   Ex, Eyebrows, -2f, 2f),
        new("Brow Out L",  Ex, Eyebrows, -2f, 2f),
        new("Brow Out R",  Ex, Eyebrows, -2f, 2f),

        // === Expression: Mouth (269-292) ===
        new("Smile L",        Ex, Mouth, -2f, 2f),
        new("Smile R",        Ex, Mouth, -2f, 2f),
        new("Frown L",        Ex, Mouth, -2f, 2f),
        new("Frown R",        Ex, Mouth, -2f, 2f),
        new("Mouth Open",     Ex, Mouth, -1.2593f, 1.2593f),
        new("Mouth Close",    Ex, Mouth, -2f, 2f),
        new("Lips Pucker",    Ex, Mouth, -2f, 2f),
        new("Lips Funnel",    Ex, Mouth, -2f, 2f),
        new("Lips Tight",     Ex, Mouth, -2f, 2f),
        new("Lips Press",     Ex, Mouth, -2f, 2f),
        new("Upper Lip Up",   Ex, Mouth, -2f, 2f),
        new("Lower Lip Down", Ex, Mouth, -2f, 2f),
        new("Mouth Left",     Ex, Mouth, -2f, 2f),
        new("Mouth Right",    Ex, Mouth, -2f, 2f),
        new("Mouth Stretch",  Ex, Mouth, -2f, 2f),
        new("Mouth Roll",     Ex, Mouth, -2f, 2f),
        new("Teeth Show",     Ex, Mouth, -2f, 2f),
        new("Tongue Out",     Ex, Mouth, -2f, 2f),
        new("Lip Bite",       Ex, Mouth, -2f, 2f),
        new("Lip Suck",       Ex, Mouth, -2f, 2f),
        new("Dimple L",       Ex, Mouth, -2f, 2f),
        new("Dimple R",       Ex, Mouth, -2f, 2f),
        new("Sneer L",        Ex, Mouth, -2f, 2f),
        new("Sneer R",        Ex, Mouth, -2f, 2f),

        // === Expression: Jaw (293-300) ===
        new("Jaw Open",    Ex, Jaw, -2f, 2f),
        new("Jaw Forward", Ex, Jaw, -2f, 2f),
        new("Jaw Back",    Ex, Jaw, -2f, 2f),
        new("Jaw Left",    Ex, Jaw, -2f, 2f),
        new("Jaw Right",   Ex, Jaw, -2f, 2f),
        new("Jaw Clench",  Ex, Jaw, -2f, 2f),
        new("Chin Up",     Ex, Jaw, -2f, 2f),
        new("Chin Down",   Ex, Jaw, -2f, 2f),

        // === Expression: Cheeks/Nose (301-320) ===
        new("Cheek Puff L",   Ex, CheeksNose, -2f, 2f),
        new("Cheek Puff R",   Ex, CheeksNose, -2f, 2f),
        new("Cheek Suck L",   Ex, CheeksNose, -2f, 2f),
        new("Cheek Suck R",   Ex, CheeksNose, -2f, 2f),
        new("Nose Wrinkle",   Ex, CheeksNose, -2f, 2f),
        new("Nose Sneer L",   Ex, CheeksNose, -2f, 2f),
        new("Nose Sneer R",   Ex, CheeksNose, -2f, 2f),
        new("Nose Flare L",   Ex, CheeksNose, -2f, 2f),
        new("Nose Flare R",   Ex, CheeksNose, -2f, 2f),
        new("Nostril Dilate", Ex, CheeksNose, -2f, 2f),
        new("Cheek Raise L",  Ex, CheeksNose, -2f, 2f),
        new("Cheek Raise R",  Ex, CheeksNose, -2f, 2f),
        new("Face Tense",     Ex, CheeksNose, -2f, 2f),
        new("Face Relax",     Ex, CheeksNose, -2f, 2f),
        new("Puff Exhale",    Ex, CheeksNose, -2f, 2f),
        new("Gulp",           Ex, CheeksNose, -2f, 2f),
        new("Swallow",        Ex, CheeksNose, -2f, 2f),
        new("Throat",         Ex, CheeksNose, -2f, 2f),
        new("Adam Apple",     Ex, CheeksNose, -2f, 2f),
        new("Neck Tense",     Ex, CheeksNose, -2f, 2f),
    ];

    /// <summary>
    /// Group definitions derived from All: (category, groupName, nameArray, globalStartIndex).
    /// </summary>
    public static readonly (string Category, string Group, string[] Names, int StartIndex)[] Groups = BuildGroups();

    private static (string Category, string Group, string[] Names, int StartIndex)[] BuildGroups()
    {
        var groups = new List<(string, string, string[], int)>();
        int i = 0;
        while (i < All.Length)
        {
            var cat = All[i].Category;
            var grp = All[i].Group;
            int start = i;
            while (i < All.Length && All[i].Category == cat && All[i].Group == grp) i++;
            var names = new string[i - start];
            for (int j = start; j < i; j++) names[j - start] = All[j].Name;
            groups.Add((cat, grp, names, start));
        }
        return groups.ToArray();
    }
}
