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

        // === Pose: Spine & Torso (45-74) ===
        new("Pelvis Tilt",    Po, SpineTorso, -3.14f, 3.14f),
        new("Pelvis Roll",    Po, SpineTorso, -3.14f, 3.14f),
        new("Pelvis Twist",   Po, SpineTorso, -3.14f, 3.14f),
        new("Pelvis X",       Po, SpineTorso, -1.5708f, 1.5708f),
        new("Pelvis Y",       Po, SpineTorso, -1.5708f, 1.5708f),
        new("Pelvis Z",       Po, SpineTorso, -1.5708f, 1.5708f),
        new("Spine1 Bend",    Po, SpineTorso, -1.2934f, 1.2336f),
        new("Spine1 Side",    Po, SpineTorso, -3.14f, 3.14f),
        new("Spine1 Twist",   Po, SpineTorso, -0.8059f, 0.7839f),
        new("Spine2 Bend",    Po, SpineTorso, -2.3004f, 2.464f),
        new("Spine2 Side",    Po, SpineTorso, -0.777f, 0.7754f),
        new("Spine2 Twist",   Po, SpineTorso, -2.1377f, 1.7368f),
        new("Spine3 Bend",    Po, SpineTorso, -1.4564f, 1.5041f),
        new("Spine3 Side",    Po, SpineTorso, -2.2048f, 2.1947f),
        new("Spine3 Twist",   Po, SpineTorso, -0.931f, 1.0268f),
        new("Chest Bend",     Po, SpineTorso, -2.5236f, 2.3792f),
        new("Chest Side",     Po, SpineTorso, -1.2454f, 0.9815f),
        new("Chest Twist",    Po, SpineTorso, -3.14f, 3.1134f),
        new("Torso X",        Po, SpineTorso, -1.395f, 1.4978f),
        new("Torso Y",        Po, SpineTorso, -1.1988f, 1.1791f),
        new("Torso Z",        Po, SpineTorso, -1.3395f, 0.8442f),
        new("Torso Rot X",    Po, SpineTorso, -0.9239f, 1.0222f),
        new("Torso Rot Y",    Po, SpineTorso, -0.7543f, 0.6728f),
        new("Torso Rot Z",    Po, SpineTorso, -1.2145f, 0.9363f),
        new("Hip Thrust",     Po, SpineTorso, -1.5076f, 1.5378f),
        new("Belly",          Po, SpineTorso, -0.7629f, 0.8504f),
        new("Back Arch",      Po, SpineTorso, -0.9878f, 1.0335f),
        new("Shoulder Shrug", Po, SpineTorso, -1.5119f, 1.4983f),
        new("Rib Expand",     Po, SpineTorso, -1.1673f, 1.2073f),
        new("Core Twist",     Po, SpineTorso, -1.2377f, 0.8676f),

        // === Pose: Head & Neck (75-94) ===
        new("Neck Bend",    Po, HeadNeck, -1.5323f, 1.497f),
        new("Neck Side",    Po, HeadNeck, -0.6174f, 0.8106f),
        new("Neck Twist",   Po, HeadNeck, -0.7652f, 1.394f),
        new("Head Nod",     Po, HeadNeck, -1.5708f, 1.5708f),
        new("Head Tilt",    Po, HeadNeck, -1.1766f, 1.4984f),
        new("Head Turn",    Po, HeadNeck, -1.4477f, 1.499f),
        new("Head X",       Po, HeadNeck, -1.4565f, 1.4445f),
        new("Head Y",       Po, HeadNeck, -1.5501f, 1.5473f),
        new("Head Z",       Po, HeadNeck, -1.3891f, 1.2321f),
        new("Jaw Pose",     Po, HeadNeck, -1.4786f, 1.1232f),
        new("Eye L X",      Po, HeadNeck, -1.4752f, 1.3457f),
        new("Eye L Y",      Po, HeadNeck, -0.6146f, 0.8031f),
        new("Eye R X",      Po, HeadNeck, -0.7669f, 1.3193f),
        new("Eye R Y",      Po, HeadNeck, -1.5708f, 1.5708f),
        new("Neck Base",    Po, HeadNeck, -1.0798f, 1.4869f),
        new("Neck Mid",     Po, HeadNeck, -1.4542f, 1.4943f),
        new("Head Roll",    Po, HeadNeck, -1.42f, 1.443f),
        new("Skull",        Po, HeadNeck, -1.5474f, 1.5401f),
        new("Face Pose 1",  Po, HeadNeck, -1.4043f, 1.2666f),
        new("Face Pose 2",  Po, HeadNeck, -1.4841f, 1.1394f),

        // === Pose: Left Arm (95-124) ===
        new("L Clav Fwd",       Po, LeftArm, -1.5624f, 1.5321f),
        new("L Clav Up",        Po, LeftArm, -1.4304f, 0.7977f),
        new("L Clav Twist",     Po, LeftArm, -1.1705f, 1.0961f),
        new("L Shoulder Fwd",   Po, LeftArm, -1.3401f, 1.4335f),
        new("L Shoulder Out",   Po, LeftArm, -1.4937f, 1.5382f),
        new("L Shoulder Twist", Po, LeftArm, -1.0904f, 1.445f),
        new("L Elbow Bend",     Po, LeftArm, -1.4664f, 1.423f),
        new("L Elbow Twist",    Po, LeftArm, -1.3785f, 0.7977f),
        new("L Wrist Bend",     Po, LeftArm, -1.6858f, 1.2644f),
        new("L Wrist Side",     Po, LeftArm, -1.5374f, 1.5586f),
        new("L Wrist Twist",    Po, LeftArm, -1.4466f, 0.8231f),
        new("L Arm X",          Po, LeftArm, -1.2096f, 1.0518f),
        new("L Arm Y",          Po, LeftArm, -1.4407f, 1.4843f),
        new("L Arm Z",          Po, LeftArm, -1.4919f, 1.5191f),
        new("L Upper Rot",      Po, LeftArm, -1.2129f, 1.417f),
        new("L Forearm Rot",    Po, LeftArm, -1.4874f, 1.4658f),
        new("L Hand Rot",       Po, LeftArm, -1.267f, 1.016f),
        new("L Shoulder Roll",  Po, LeftArm, -1.5083f, 1.2557f),
        new("L Bicep",          Po, LeftArm, -0.6938f, 1.4086f),
        new("L Tricep",         Po, LeftArm, -1.3768f, 1.0746f),
        new("L Forearm 1",      Po, LeftArm, -1.4637f, 1.4754f),
        new("L Forearm 2",      Po, LeftArm, -1.2114f, 1.4631f),
        new("L Elbow Pose",     Po, LeftArm, -1.3263f, 1.3013f),
        new("L Wrist Pose",     Po, LeftArm, -1.5f, 1.4881f),
        new("L Arm IK 1",       Po, LeftArm, -1.444f, 1.6454f),
        new("L Arm IK 2",       Po, LeftArm, -1.1272f, 1.5617f),
        new("L Arm IK 3",       Po, LeftArm, -1.2032f, 1.3792f),
        new("L Arm IK 4",       Po, LeftArm, -1.5657f, 1.3363f),
        new("L Arm IK 5",       Po, LeftArm, -1.2442f, 1.2822f),
        new("L Arm IK 6",       Po, LeftArm, -1.5382f, 1.5403f),

        // === Pose: Right Arm (125-154) ===
        new("R Clav Fwd",       Po, RightArm, -1.4638f, 1.5506f),
        new("R Clav Up",        Po, RightArm, -1.4536f, 1.5146f),
        new("R Clav Twist",     Po, RightArm, -1.5559f, 1.3998f),
        new("R Shoulder Fwd",   Po, RightArm, -1.5208f, 1.5488f),
        new("R Shoulder Out",   Po, RightArm, -1.4742f, 1.5006f),
        new("R Shoulder Twist", Po, RightArm, -1.5601f, 1.4564f),
        new("R Elbow Bend",     Po, RightArm, -1.4564f, 1.5551f),
        new("R Elbow Twist",    Po, RightArm, -1.4528f, 1.4528f),
        new("R Wrist Bend",     Po, RightArm, -1.5565f, 1.5253f),
        new("R Wrist Side",     Po, RightArm, -1.4823f, 1.5631f),
        new("R Wrist Twist",    Po, RightArm, -1.527f, 1.5695f),
        new("R Arm X",          Po, RightArm, -1.5311f, 1.4916f),
        new("R Arm Y",          Po, RightArm, -1.5284f, 1.49f),
        new("R Arm Z",          Po, RightArm, -1.5456f, 1.463f),
        new("R Upper Rot",      Po, RightArm, -1.5301f, 1.5248f),
        new("R Forearm Rot",    Po, RightArm, -0.6982f, 1.4019f),
        new("R Hand Rot",       Po, RightArm, -1.356f, 1.1111f),
        new("R Shoulder Roll",  Po, RightArm, -1.4829f, 1.4497f),
        new("R Bicep",          Po, RightArm, -1.2215f, 1.4961f),
        new("R Tricep",         Po, RightArm, -1.3504f, 1.2962f),
        new("R Forearm 1",      Po, RightArm, -1.4916f, 1.4883f),
        new("R Forearm 2",      Po, RightArm, -1.4978f, 1.6012f),
        new("R Elbow Pose",     Po, RightArm, -1.1299f, 1.5645f),
        new("R Wrist Pose",     Po, RightArm, -1.2222f, 1.3845f),
        new("R Arm IK 1",       Po, RightArm, -1.5667f, 1.3665f),
        new("R Arm IK 2",       Po, RightArm, -1.2767f, 1.2784f),
        new("R Arm IK 3",       Po, RightArm, -1.5428f, 1.5333f),
        new("R Arm IK 4",       Po, RightArm, -1.4749f, 1.5472f),
        new("R Arm IK 5",       Po, RightArm, -1.4699f, 1.5433f),
        new("R Arm IK 6",       Po, RightArm, -1.5551f, 1.3878f),

        // === Pose: Left Leg (155-184) ===
        new("L Hip Fwd",    Po, LeftLeg, -1.5176f, 1.5493f),
        new("L Hip Out",    Po, LeftLeg, -1.4671f, 1.4963f),
        new("L Hip Twist",  Po, LeftLeg, -1.571f, 1.4267f),
        new("L Knee Bend",  Po, LeftLeg, -1.4814f, 1.5456f),
        new("L Knee Twist", Po, LeftLeg, -1.5222f, 1.4814f),
        new("L Ankle Bend", Po, LeftLeg, -1.5458f, 1.5246f),
        new("L Ankle Side",  Po, LeftLeg, -1.5067f, 1.5805f),
        new("L Ankle Twist", Po, LeftLeg, -1.5201f, 1.5484f),
        new("L Toe Bend",   Po, LeftLeg, -1.5314f, 1.4803f),
        new("L Toe Twist",  Po, LeftLeg, -1.5326f, 1.511f),
        new("L Leg X",      Po, LeftLeg, -1.5303f, 1.4965f),
        new("L Leg Y",      Po, LeftLeg, -1.513f, 1.5219f),
        new("L Leg Z",      Po, LeftLeg, -1.3264f, 1.395f),
        new("L Thigh Rot",  Po, LeftLeg, -0.9606f, 1.2092f),
        new("L Calf Rot",   Po, LeftLeg, -1.4625f, 1.4294f),
        new("L Foot Rot",   Po, LeftLeg, -1.0144f, 1.1297f),
        new("L Hip Roll",   Po, LeftLeg, -1.2644f, 1.2188f),
        new("L Glute",      Po, LeftLeg, -1.0142f, 1.2631f),
        new("L Quad",       Po, LeftLeg, -1.4288f, 1.4719f),
        new("L Hamstring",  Po, LeftLeg, -1.1732f, 0.9112f),
        new("L Calf 1",     Po, LeftLeg, -2.6613f, 3.14f),
        new("L Calf 2",     Po, LeftLeg, -0.7242f, 3.14f),
        new("L Knee Pose",  Po, LeftLeg, -0.5866f, 3.14f),
        new("L Ankle Pose", Po, LeftLeg, -2.6571f, 3.14f),
        new("L Leg IK 1",   Po, LeftLeg, -0.6014f, 3.14f),
        new("L Leg IK 2",   Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg IK 3",   Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg IK 4",   Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg IK 5",   Po, LeftLeg, -3.14f, 3.14f),
        new("L Leg IK 6",   Po, LeftLeg, -2.6613f, 3.14f),

        // === Pose: Right Leg (185-214) ===
        new("R Hip Fwd",    Po, RightLeg, -0.7242f, 3.14f),
        new("R Hip Out",    Po, RightLeg, -0.5866f, 3.14f),
        new("R Hip Twist",  Po, RightLeg, -1.3871f, 3.14f),
        new("R Knee Bend",  Po, RightLeg, -1.7285f, 3.14f),
        new("R Knee Twist", Po, RightLeg, -2.4875f, 2.9829f),
        new("R Ankle Bend", Po, RightLeg, -2.4875f, 2.9829f),
        new("R Ankle Side",  Po, RightLeg, -0.6014f, 3.14f),
        new("R Ankle Twist", Po, RightLeg, -3.14f, 1.3952f),
        new("R Toe Bend",   Po, RightLeg, -3.14f, 3.14f),
        new("R Toe Twist",  Po, RightLeg, -2.258f, 3.14f),
        new("R Leg X",      Po, RightLeg, -3.1013f, 3.14f),
        new("R Leg Y",      Po, RightLeg, -1.4264f, 1.4641f),
        new("R Leg Z",      Po, RightLeg, -3.14f, 0.6754f),
        new("R Thigh Rot",  Po, RightLeg, -0.8289f, 0.3265f),
        new("R Calf Rot",   Po, RightLeg, -3.14f, 2.5655f),
        new("R Foot Rot",   Po, RightLeg, -3.14f, 2.5494f),
        new("R Hip Roll",   Po, RightLeg, -3.14f, 2.237f),
        new("R Glute",      Po, RightLeg, -3.14f, 2.5139f),
        new("R Quad",       Po, RightLeg, -3.14f, 3.14f),
        new("R Hamstring",  Po, RightLeg, -2.2023f, 3.14f),
        new("R Calf 1",     Po, RightLeg, -2.6608f, 2.3119f),
        new("R Calf 2",     Po, RightLeg, -2.7974f, 2.1929f),
        new("R Knee Pose",  Po, RightLeg, -3.14f, 1.8424f),
        new("R Ankle Pose", Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 1",   Po, RightLeg, -3.14f, 2.0615f),
        new("R Leg IK 2",   Po, RightLeg, -3.14f, 2.6948f),
        new("R Leg IK 3",   Po, RightLeg, -3.14f, 2.174f),
        new("R Leg IK 4",   Po, RightLeg, -3.14f, 2.0596f),
        new("R Leg IK 5",   Po, RightLeg, -3.14f, 3.14f),
        new("R Leg IK 6",   Po, RightLeg, -3.14f, 1.5999f),

        // === Pose: Hands Detail (215-248) ===
        new("L Thumb 1",       Po, HandsDetail, -3.14f, 1.9945f),
        new("L Thumb 2",       Po, HandsDetail, -3.14f, 1.5354f),
        new("L Thumb 3",       Po, HandsDetail, -3.14f, 1.6335f),
        new("L Index 1",       Po, HandsDetail, -3.14f, 2.0273f),
        new("L Index 2",       Po, HandsDetail, -3.14f, 1.3943f),
        new("L Index 3",       Po, HandsDetail, -3.14f, 1.5065f),
        new("L Middle 1",      Po, HandsDetail, -3.14f, 1.4174f),
        new("L Middle 2",      Po, HandsDetail, -3.14f, 1.3459f),
        new("L Middle 3",      Po, HandsDetail, -3.14f, 1.6219f),
        new("L Ring 1",        Po, HandsDetail, -3.14f, 2.5655f),
        new("L Ring 2",        Po, HandsDetail, -3.14f, 2.5494f),
        new("L Ring 3",        Po, HandsDetail, -3.14f, 2.237f),
        new("L Pinky 1",       Po, HandsDetail, -3.14f, 2.5139f),
        new("L Pinky 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("L Pinky 3",       Po, HandsDetail, -2.2023f, 3.14f),
        new("L Palm",          Po, HandsDetail, -2.6608f, 2.312f),
        new("L Finger Splay",  Po, HandsDetail, -2.7974f, 2.193f),
        new("R Thumb 1",       Po, HandsDetail, -3.14f, 1.8425f),
        new("R Thumb 2",       Po, HandsDetail, -3.14f, 3.14f),
        new("R Thumb 3",       Po, HandsDetail, -3.14f, 2.0615f),
        new("R Index 1",       Po, HandsDetail, -3.14f, 2.6948f),
        new("R Index 2",       Po, HandsDetail, -3.14f, 2.174f),
        new("R Index 3",       Po, HandsDetail, -3.14f, 2.0596f),
        new("R Middle 1",      Po, HandsDetail, -3.14f, 3.14f),
        new("R Middle 2",      Po, HandsDetail, -3.14f, 1.5999f),
        new("R Middle 3",      Po, HandsDetail, -3.14f, 1.9946f),
        new("R Ring 1",        Po, HandsDetail, -3.14f, 1.5355f),
        new("R Ring 2",        Po, HandsDetail, -3.14f, 1.6335f),
        new("R Ring 3",        Po, HandsDetail, -3.14f, 2.0273f),
        new("R Pinky 1",       Po, HandsDetail, -3.14f, 1.3943f),
        new("R Pinky 2",       Po, HandsDetail, -3.14f, 1.5065f),
        new("R Pinky 3",       Po, HandsDetail, -3.14f, 1.4174f),
        new("R Palm",          Po, HandsDetail, -3.14f, 1.3459f),
        new("R Finger Splay",  Po, HandsDetail, -3.14f, 1.6219f),

        // === Expression: Eyes (249-260) ===
        new("Blink L",    Ex, Eyes, -2f, 0.8189f),
        new("Blink R",    Ex, Eyes, -2f, 0.8304f),
        new("Squint L",   Ex, Eyes, -2f, 2f),
        new("Squint R",   Ex, Eyes, -2f, 2f),
        new("Wide L",     Ex, Eyes, -2f, 1.8247f),
        new("Wide R",     Ex, Eyes, -2f, 1.7982f),
        new("Look Up",    Ex, Eyes, -2f, 2f),
        new("Look Down",  Ex, Eyes, -2f, 2f),
        new("Look Left",  Ex, Eyes, -0.3147f, 1.428f),
        new("Look Right", Ex, Eyes, -1.0916f, 0.3827f),
        new("Pupil L",    Ex, Eyes, -2f, 1.9575f),
        new("Pupil R",    Ex, Eyes, -2f, 1.936f),

        // === Expression: Eyebrows (261-268) ===
        new("Brow Up L",   Ex, Eyebrows, -0.5439f, 1.0289f),
        new("Brow Up R",   Ex, Eyebrows, -0.546f, 1.0328f),
        new("Brow Down L", Ex, Eyebrows, -0.4668f, 1.5428f),
        new("Brow Down R", Ex, Eyebrows, -0.4479f, 1.5483f),
        new("Brow In L",   Ex, Eyebrows, -2f, 2f),
        new("Brow In R",   Ex, Eyebrows, -2f, 2f),
        new("Brow Out L",  Ex, Eyebrows, -2f, 2f),
        new("Brow Out R",  Ex, Eyebrows, -2f, 2f),

        // === Expression: Mouth (269-292) ===
        new("Smile L",        Ex, Mouth, -2f, 0.8296f),
        new("Smile R",        Ex, Mouth, -2f, 0.8694f),
        new("Frown L",        Ex, Mouth, -0.8954f, 2f),
        new("Frown R",        Ex, Mouth, -0.8927f, 2f),
        new("Mouth Open",     Ex, Mouth, -0.6675f, 0.1095f),
        new("Mouth Close",    Ex, Mouth, -1.7635f, 2f),
        new("Lips Pucker",    Ex, Mouth, -2f, 2f),
        new("Lips Funnel",    Ex, Mouth, -0.6026f, 2f),
        new("Lips Tight",     Ex, Mouth, -2f, 1.2943f),
        new("Lips Press",     Ex, Mouth, -2f, 1.3069f),
        new("Upper Lip Up",   Ex, Mouth, -2f, 2f),
        new("Lower Lip Down", Ex, Mouth, -2f, 2f),
        new("Mouth Left",     Ex, Mouth, -1.3084f, 1.7027f),
        new("Mouth Right",    Ex, Mouth, -1.269f, 1.7724f),
        new("Mouth Stretch",  Ex, Mouth, -0.5916f, 1.0735f),
        new("Mouth Roll",     Ex, Mouth, -0.7962f, 1.0271f),
        new("Teeth Show",     Ex, Mouth, -0.6014f, 0.9951f),
        new("Tongue Out",     Ex, Mouth, -0.7598f, 1.0698f),
        new("Lip Bite",       Ex, Mouth, -1.0503f, 2f),
        new("Lip Suck",       Ex, Mouth, -1.0944f, 2f),
        new("Dimple L",       Ex, Mouth, -0.4019f, 1.0923f),
        new("Dimple R",       Ex, Mouth, -0.3947f, 1.089f),
        new("Sneer L",        Ex, Mouth, -2f, 2f),
        new("Sneer R",        Ex, Mouth, -2f, 2f),

        // === Expression: Jaw (293-300) ===
        new("Jaw Open",    Ex, Jaw, -1.281f, 0.8283f),
        new("Jaw Forward", Ex, Jaw, -1.089f, 0.5693f),
        new("Jaw Back",    Ex, Jaw, -1.2317f, 0.8696f),
        new("Jaw Left",    Ex, Jaw, -1.0789f, 0.5693f),
        new("Jaw Right",   Ex, Jaw, -2f, 2f),
        new("Jaw Clench",  Ex, Jaw, -2f, 2f),
        new("Chin Up",     Ex, Jaw, -0.4059f, 1.3431f),
        new("Chin Down",   Ex, Jaw, -0.7947f, 0.8535f),

        // === Expression: Cheeks/Nose (301-320) ===
        new("Cheek Puff L",   Ex, CheeksNose, -0.4112f, 1.3934f),
        new("Cheek Puff R",   Ex, CheeksNose, -0.7984f, 0.8366f),
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
        new("Puff Exhale",    Ex, CheeksNose, -0.7274f, 2f),
        new("Gulp",           Ex, CheeksNose, -0.7238f, 2f),
        new("Swallow",        Ex, CheeksNose, -2f, 2f),
        new("Throat",         Ex, CheeksNose, -2f, 2f),
        new("Adam Apple",     Ex, CheeksNose, -2f, 0.7983f),
        new("Neck Tense",     Ex, CheeksNose, -2f, 0.7296f),
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
