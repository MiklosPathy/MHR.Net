// MHR Body Generator - Test application
// Uses Meta's MHR model for AI-based 3D body mesh generation

using D3DShared;
using MHR.Net;
using System.Numerics;
using TorchSharp;
using Vortice.Direct3D12;
using Vortice.Mathematics;
// Redirect Console to Debug output
using Console = MHR.DebugConsole;

namespace MHR;

class Program
{
    // Window dimensions
    static int _width = 1280;
    static int _height = 720;

    // Renderer
    static D3D12Renderer? _renderer;

    // Body model resources
    static ID3D12Resource? _bodyVertexBuffer;
    static ID3D12Resource? _bodyIndexBuffer;
    static VertexBufferView _bodyVertexBufferView;
    static IndexBufferView _bodyIndexBufferView;
    static int _bodyIndexCount;
    static ID3D12Resource? _bodyConstantBuffer;

    // MHR model
    static MhrModel? _mhrModel;
    static MhrVertex[]? _currentMhrVertices;

    // Parameter counts
    const int IdentityParamCount = 45;    // 20 body + 20 head + 5 hands
    const int PoseParamCount = 204;        // Joint rotations, position, scale
    const int ExpressionParamCount = 72;   // Facial expressions
    const int TotalParamCount = IdentityParamCount + PoseParamCount + ExpressionParamCount;

    // Parameter arrays
    static float[] _identityParams = new float[IdentityParamCount];
    static float[] _poseParams = new float[PoseParamCount];
    static float[] _expressionParams = new float[ExpressionParamCount];

    // Parameter panel manager
    static ParameterPanelManager? _paramPanel;

    // Mouse rotation
    static float _rotationX;
    static float _rotationY = (float)Math.PI;  // Start facing forward (180 degrees)
    static bool _mouseDown;
    static int _lastMouseX;
    static int _lastMouseY;
    static float _cameraDistance = 4.0f;  // Good distance for full body (~1.7m tall)
    static float _cameraHeight = 0.85f;   // Look at body center (half of ~1.7m height)

    // FPS counter
    static readonly UIHelpers.FpsCounter _fpsCounter = new();

    // UI update flag
    static bool _needsUpdate = true;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Create main form
        var form = UIHelpers.CreateMainForm("MHR Body Generator", _width, _height);

        // Initialize TorchSharp
        Console.WriteLine("Initializing TorchSharp...");
        Console.WriteLine($"CUDA available: {torch.cuda.is_available()}");
        if (torch.cuda.is_available())
        {
            Console.WriteLine($"CUDA device count: {torch.cuda.device_count()}");
        }

        // Initialize renderer and enumerate adapters
        _renderer = new D3D12Renderer(_width, _height);
        _renderer.EnumerateAdapters();
        _renderer.Initialize(form.Handle, 0);

        // Create constant buffer
        _bodyConstantBuffer = _renderer.CreateConstantBuffer();

        // Load MHR model
        try
        {
            var assetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            Console.WriteLine("Loading MHR model...");
            // Note: TorchScript model only supports LOD1 per MHR documentation
            _mhrModel = MhrModel.Load(
                device: torch.cuda.is_available() ? torch.CUDA : torch.CPU,
                lod: MhrLod.LOD1,  // TorchScript model is fixed at LOD1
                assetFolder: assetFolder);

            Console.WriteLine($"Model loaded! Vertices: {_mhrModel.NumVertices}");
            Console.WriteLine($"Indices available: {_mhrModel.Indices != null} (count: {_mhrModel.Indices?.Length ?? 0})");

            // Generate initial mesh
            GenerateBody();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load MHR model:\n\n{ex.Message}\n\nMake sure Assets folder contains the extracted assets.zip contents.",
                "Model Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // === UI Controls - Tabbed Parameter Panel ===

        _paramPanel = new ParameterPanelManager(form, 5, 5, 200, form.ClientSize.Height - 50, TotalParamCount);

        // --- Identity Tab (45 params: 0-44) ---
        _paramPanel.AddTab("Identity");

        var bodyGroup = _paramPanel.AddGroup("Identity", "Body Shape (20)");
        // Body shape PCA components - names based on typical body model semantics
        string[] bodyNames = {
            "Height", "Mass", "Shoulders", "Hips", "Chest",
            "Waist", "Arms Length", "Legs Length", "Torso", "Muscle",
            "Body Fat", "Limb Thick", "Neck", "Back Width", "Posture",
            "Ribcage", "Pelvis", "Proportion", "Build", "Frame"
        };
        for (int i = 0; i < 20; i++)
            bodyGroup.AddSlider(bodyNames[i], i);

        var headGroup = _paramPanel.AddGroup("Identity", "Head Shape (20)");
        string[] headNames = {
            "Head Size", "Face Length", "Face Width", "Jaw Width", "Forehead",
            "Cheekbones", "Chin", "Nose Size", "Nose Bridge", "Nose Width",
            "Eye Distance", "Eye Size", "Brow Ridge", "Ears", "Skull Shape",
            "Temple", "Face Depth", "Mouth Width", "Lip Size", "Neck Thick"
        };
        for (int i = 0; i < 20; i++)
            headGroup.AddSlider(headNames[i], 20 + i);

        var handsGroup = _paramPanel.AddGroup("Identity", "Hands (5)");
        string[] handNames = { "Hand Size", "Palm Width", "Finger Length", "Finger Thick", "Knuckles" };
        for (int i = 0; i < 5; i++)
            handsGroup.AddSlider(handNames[i], 40 + i);

        // Expand first group by default
        bodyGroup.Expand();

        // --- Expression Tab (72 params: 45+204 = 249 to 320) ---
        // Note: Expression params start at index 249 in the combined array
        const int exprOffset = IdentityParamCount + PoseParamCount;
        _paramPanel.AddTab("Expression");

        var eyesGroup = _paramPanel.AddGroup("Expression", "Eyes (12)");
        string[] eyeNames = {
            "Blink L", "Blink R", "Squint L", "Squint R", "Wide L", "Wide R",
            "Look Up", "Look Down", "Look Left", "Look Right", "Pupil L", "Pupil R"
        };
        for (int i = 0; i < 12; i++)
            eyesGroup.AddSlider(eyeNames[i], exprOffset + i);

        var browsGroup = _paramPanel.AddGroup("Expression", "Eyebrows (8)");
        string[] browNames = {
            "Brow Up L", "Brow Up R", "Brow Down L", "Brow Down R",
            "Brow In L", "Brow In R", "Brow Out L", "Brow Out R"
        };
        for (int i = 0; i < 8; i++)
            browsGroup.AddSlider(browNames[i], exprOffset + 12 + i);

        var mouthGroup = _paramPanel.AddGroup("Expression", "Mouth (24)");
        string[] mouthNames = {
            "Smile L", "Smile R", "Frown L", "Frown R", "Mouth Open", "Mouth Close",
            "Lips Pucker", "Lips Funnel", "Lips Tight", "Lips Press", "Upper Lip Up", "Lower Lip Down",
            "Mouth Left", "Mouth Right", "Mouth Stretch", "Mouth Roll", "Teeth Show", "Tongue Out",
            "Lip Bite", "Lip Suck", "Dimple L", "Dimple R", "Sneer L", "Sneer R"
        };
        for (int i = 0; i < 24; i++)
            mouthGroup.AddSlider(mouthNames[i], exprOffset + 20 + i);

        var jawGroup = _paramPanel.AddGroup("Expression", "Jaw (8)");
        string[] jawNames = {
            "Jaw Open", "Jaw Forward", "Jaw Back", "Jaw Left",
            "Jaw Right", "Jaw Clench", "Chin Up", "Chin Down"
        };
        for (int i = 0; i < 8; i++)
            jawGroup.AddSlider(jawNames[i], exprOffset + 44 + i);

        var cheeksGroup = _paramPanel.AddGroup("Expression", "Cheeks/Nose (20)");
        string[] cheekNames = {
            "Cheek Puff L", "Cheek Puff R", "Cheek Suck L", "Cheek Suck R",
            "Nose Wrinkle", "Nose Sneer L", "Nose Sneer R", "Nose Flare L", "Nose Flare R", "Nostril Dilate",
            "Cheek Raise L", "Cheek Raise R", "Face Tense", "Face Relax", "Puff Exhale",
            "Gulp", "Swallow", "Throat", "Adam Apple", "Neck Tense"
        };
        for (int i = 0; i < 20; i++)
            cheeksGroup.AddSlider(cheekNames[i], exprOffset + 52 + i);

        // Expand first group by default
        eyesGroup.Expand();

        // --- Pose Tab (204 params: 45-248) ---
        // Note: Pose params start at index 45 in the combined array
        // Pose params are joint rotations - exact mapping depends on MHR skeleton
        const int poseOffset = IdentityParamCount;
        _paramPanel.AddTab("Pose");

        var spineGroup = _paramPanel.AddGroup("Pose", "Spine & Torso (30)");
        string[] spineNames = {
            "Pelvis Tilt", "Pelvis Roll", "Pelvis Twist", "Pelvis X", "Pelvis Y", "Pelvis Z",
            "Spine1 Bend", "Spine1 Side", "Spine1 Twist", "Spine2 Bend", "Spine2 Side", "Spine2 Twist",
            "Spine3 Bend", "Spine3 Side", "Spine3 Twist", "Chest Bend", "Chest Side", "Chest Twist",
            "Torso X", "Torso Y", "Torso Z", "Torso Rot X", "Torso Rot Y", "Torso Rot Z",
            "Hip Thrust", "Belly", "Back Arch", "Shoulder Shrug", "Rib Expand", "Core Twist"
        };
        for (int i = 0; i < 30; i++)
            spineGroup.AddSlider(spineNames[i], poseOffset + i);

        var neckGroup = _paramPanel.AddGroup("Pose", "Head & Neck (20)");
        string[] neckNames = {
            "Neck Bend", "Neck Side", "Neck Twist", "Head Nod", "Head Tilt", "Head Turn",
            "Head X", "Head Y", "Head Z", "Jaw Pose", "Eye L X", "Eye L Y",
            "Eye R X", "Eye R Y", "Neck Base", "Neck Mid", "Head Roll", "Skull",
            "Face Pose 1", "Face Pose 2"
        };
        for (int i = 0; i < 20; i++)
            neckGroup.AddSlider(neckNames[i], poseOffset + 30 + i);

        var leftArmGroup = _paramPanel.AddGroup("Pose", "Left Arm (30)");
        string[] leftArmNames = {
            "L Clav Fwd", "L Clav Up", "L Clav Twist", "L Shoulder Fwd", "L Shoulder Out", "L Shoulder Twist",
            "L Elbow Bend", "L Elbow Twist", "L Wrist Bend", "L Wrist Side", "L Wrist Twist", "L Arm X",
            "L Arm Y", "L Arm Z", "L Upper Rot", "L Forearm Rot", "L Hand Rot", "L Shoulder Roll",
            "L Bicep", "L Tricep", "L Forearm 1", "L Forearm 2", "L Elbow Pose", "L Wrist Pose",
            "L Arm IK 1", "L Arm IK 2", "L Arm IK 3", "L Arm IK 4", "L Arm IK 5", "L Arm IK 6"
        };
        for (int i = 0; i < 30; i++)
            leftArmGroup.AddSlider(leftArmNames[i], poseOffset + 50 + i);

        var rightArmGroup = _paramPanel.AddGroup("Pose", "Right Arm (30)");
        string[] rightArmNames = {
            "R Clav Fwd", "R Clav Up", "R Clav Twist", "R Shoulder Fwd", "R Shoulder Out", "R Shoulder Twist",
            "R Elbow Bend", "R Elbow Twist", "R Wrist Bend", "R Wrist Side", "R Wrist Twist", "R Arm X",
            "R Arm Y", "R Arm Z", "R Upper Rot", "R Forearm Rot", "R Hand Rot", "R Shoulder Roll",
            "R Bicep", "R Tricep", "R Forearm 1", "R Forearm 2", "R Elbow Pose", "R Wrist Pose",
            "R Arm IK 1", "R Arm IK 2", "R Arm IK 3", "R Arm IK 4", "R Arm IK 5", "R Arm IK 6"
        };
        for (int i = 0; i < 30; i++)
            rightArmGroup.AddSlider(rightArmNames[i], poseOffset + 80 + i);

        var leftLegGroup = _paramPanel.AddGroup("Pose", "Left Leg (30)");
        string[] leftLegNames = {
            "L Hip Fwd", "L Hip Out", "L Hip Twist", "L Knee Bend", "L Knee Twist", "L Ankle Bend",
            "L Ankle Side", "L Ankle Twist", "L Toe Bend", "L Toe Twist", "L Leg X", "L Leg Y",
            "L Leg Z", "L Thigh Rot", "L Calf Rot", "L Foot Rot", "L Hip Roll", "L Glute",
            "L Quad", "L Hamstring", "L Calf 1", "L Calf 2", "L Knee Pose", "L Ankle Pose",
            "L Leg IK 1", "L Leg IK 2", "L Leg IK 3", "L Leg IK 4", "L Leg IK 5", "L Leg IK 6"
        };
        for (int i = 0; i < 30; i++)
            leftLegGroup.AddSlider(leftLegNames[i], poseOffset + 110 + i);

        var rightLegGroup = _paramPanel.AddGroup("Pose", "Right Leg (30)");
        string[] rightLegNames = {
            "R Hip Fwd", "R Hip Out", "R Hip Twist", "R Knee Bend", "R Knee Twist", "R Ankle Bend",
            "R Ankle Side", "R Ankle Twist", "R Toe Bend", "R Toe Twist", "R Leg X", "R Leg Y",
            "R Leg Z", "R Thigh Rot", "R Calf Rot", "R Foot Rot", "R Hip Roll", "R Glute",
            "R Quad", "R Hamstring", "R Calf 1", "R Calf 2", "R Knee Pose", "R Ankle Pose",
            "R Leg IK 1", "R Leg IK 2", "R Leg IK 3", "R Leg IK 4", "R Leg IK 5", "R Leg IK 6"
        };
        for (int i = 0; i < 30; i++)
            rightLegGroup.AddSlider(rightLegNames[i], poseOffset + 140 + i);

        var handsDetailGroup = _paramPanel.AddGroup("Pose", "Hands Detail (34)");
        string[] fingerNames = {
            "L Thumb 1", "L Thumb 2", "L Thumb 3", "L Index 1", "L Index 2", "L Index 3",
            "L Middle 1", "L Middle 2", "L Middle 3", "L Ring 1", "L Ring 2", "L Ring 3",
            "L Pinky 1", "L Pinky 2", "L Pinky 3", "L Palm", "L Finger Splay",
            "R Thumb 1", "R Thumb 2", "R Thumb 3", "R Index 1", "R Index 2", "R Index 3",
            "R Middle 1", "R Middle 2", "R Middle 3", "R Ring 1", "R Ring 2", "R Ring 3",
            "R Pinky 1", "R Pinky 2", "R Pinky 3", "R Palm", "R Finger Splay"
        };
        for (int i = 0; i < 34; i++)
            handsDetailGroup.AddSlider(fingerNames[i], poseOffset + 170 + i);

        // Wire up parameter changes
        _paramPanel.ParametersChanged += () =>
        {
            UpdateParameterArrays();
            _needsUpdate = true;
        };

        // Add reset button
        _paramPanel.AddResetButton(form);

        // === Right side UI ===

        // FPS counter label
        var fpsLabel = UIHelpers.CreateFpsLabel(form);
        form.Controls.Add(fpsLabel);

        // Vertex count label
        var vertexCountLabel = UIHelpers.CreateCounterLabel(form, "Verts: --");
        form.Controls.Add(vertexCountLabel);

        // Device selector
        var deviceComboBox = UIHelpers.CreateDeviceSelector(form, _renderer, newIndex =>
        {
            _renderer.SwitchAdapter(newIndex,
                onBeforeReinit: () =>
                {
                    _bodyVertexBuffer?.Dispose();
                    _bodyIndexBuffer?.Dispose();
                    _bodyConstantBuffer?.Dispose();
                },
                onAfterReinit: () =>
                {
                    _bodyConstantBuffer = _renderer.CreateConstantBuffer();
                    if (_currentMhrVertices != null && _mhrModel?.Indices != null)
                    {
                        CreateBuffers(_currentMhrVertices, _mhrModel.Indices);
                    }
                });
        });
        form.Controls.Add(deviceComboBox);

        // === Event handlers ===

        form.Resize += (s, e) =>
        {
            if (form.ClientSize.Width > 0 && form.ClientSize.Height > 0)
            {
                _width = form.ClientSize.Width;
                _height = form.ClientSize.Height;
                _renderer.ResizePending = true;
            }
        };

        form.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = true;
                _lastMouseX = e.X;
                _lastMouseY = e.Y;
            }
        };

        form.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = false;
            }
        };

        form.MouseMove += (s, e) =>
        {
            if (_mouseDown)
            {
                int dx = e.X - _lastMouseX;
                int dy = e.Y - _lastMouseY;

                _rotationY += dx * 0.01f;
                _rotationX += dy * 0.01f;

                _lastMouseX = e.X;
                _lastMouseY = e.Y;
            }
        };

        form.MouseWheel += (s, e) =>
        {
            _cameraDistance -= e.Delta * 0.005f;
            _cameraDistance = Math.Clamp(_cameraDistance, 0.5f, 50.0f);  // Allow zooming out much further
        };

        form.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.R)
            {
                _rotationX = 0;
                _rotationY = (float)Math.PI;  // Face forward
                _cameraDistance = 5.0f;
                _cameraHeight = 1.0f;
            }
            else if (e.KeyCode == Keys.Up)
            {
                _cameraHeight += 0.2f;
            }
            else if (e.KeyCode == Keys.Down)
            {
                _cameraHeight -= 0.2f;
            }
        };

        form.Show();

        // Main render loop
        while (form.Visible)
        {
            Application.DoEvents();

            // Handle resize
            if (_renderer.ResizePending)
            {
                _renderer.ResizeBuffers(_width, _height);
            }

            // Update mesh if parameters changed
            if (_needsUpdate)
            {
                GenerateBody();
                _needsUpdate = false;
            }

            // Update FPS
            if (_fpsCounter.Update())
            {
                fpsLabel.Text = $"FPS: {_fpsCounter.CurrentFps:F0}";
                vertexCountLabel.Text = $"Verts: {_mhrModel?.NumVertices:N0}";
            }

            Render();
        }

        // Cleanup
        _bodyVertexBuffer?.Dispose();
        _bodyIndexBuffer?.Dispose();
        _bodyConstantBuffer?.Dispose();
        _mhrModel?.Dispose();
        _renderer.Dispose();
    }

    static void UpdateParameterArrays()
    {
        if (_paramPanel == null) return;

        var allValues = _paramPanel.GetAllValues();

        // Copy identity params (0-44) with scaling
        for (int i = 0; i < IdentityParamCount; i++)
        {
            _identityParams[i] = (allValues[i] - 0.5f) * 4f;  // Scale to roughly -2 to +2
        }

        // Copy pose params (45-248) with scaling
        for (int i = 0; i < PoseParamCount; i++)
        {
            _poseParams[i] = (allValues[IdentityParamCount + i] - 0.5f) * 2f;  // Scale to -1 to +1
        }

        // Copy expression params (249-320) with scaling
        for (int i = 0; i < ExpressionParamCount; i++)
        {
            _expressionParams[i] = (allValues[IdentityParamCount + PoseParamCount + i] - 0.5f) * 2f;  // Scale to -1 to +1
        }
    }

    static void GenerateBody()
    {
        if (_mhrModel == null || _renderer == null) return;

        try
        {
            // Create parameter tensors
            var identityTensor = torch.tensor(_identityParams, dtype: torch.ScalarType.Float32);
            var expressionTensor = torch.tensor(_expressionParams, dtype: torch.ScalarType.Float32);
            var modelParams = torch.tensor(_poseParams, dtype: torch.ScalarType.Float32).unsqueeze(0);

            // Run inference
            var output = _mhrModel.Forward(identityTensor, modelParams.squeeze(0), expressionTensor);

            // Convert to vertex array
            _currentMhrVertices = _mhrModel.ToVertexArray(output);

            // Get indices - try loaded FBX first, then find matching, then fallback
            uint[]? indices = null;
            if (_mhrModel.Indices != null && _mhrModel.Indices.Length > 0 &&
                _mhrModel.Indices.Max() < _currentMhrVertices.Length)
            {
                indices = _mhrModel.Indices;
            }
            indices ??= _mhrModel.FindMatchingIndices(_currentMhrVertices.Length);
            indices ??= _mhrModel.GenerateFallbackIndices(_currentMhrVertices.Length);

            CreateBuffers(_currentMhrVertices, indices);

            // Dispose tensors
            output.Vertices.Dispose();
            output.SkeletonState.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating body: {ex.Message}");
        }
    }

    static void CreateBuffers(MhrVertex[] mhrVertices, uint[] indices)
    {
        // Dispose old buffers
        _bodyVertexBuffer?.Dispose();
        _bodyIndexBuffer?.Dispose();

        // Convert MhrVertex to D3DShared.Vertex
        var vertices = new Vertex[mhrVertices.Length];
        for (int i = 0; i < mhrVertices.Length; i++)
        {
            vertices[i] = new Vertex(mhrVertices[i].Position, mhrVertices[i].Normal);
        }

        // Create new buffers
        var (vb, vbv) = _renderer!.CreateVertexBuffer(vertices);
        var (ib, ibv) = _renderer.CreateIndexBuffer(indices);

        _bodyVertexBuffer = vb;
        _bodyVertexBufferView = vbv;
        _bodyIndexBuffer = ib;
        _bodyIndexBufferView = ibv;
        _bodyIndexCount = indices.Length;
    }

    static void Render()
    {
        if (_renderer == null || _bodyVertexBuffer == null) return;

        // Update view matrix based on zoom level - camera looks at body center
        _renderer.View = Matrix4x4.CreateLookAt(
            new Vector3(0, _cameraHeight, -_cameraDistance),
            new Vector3(0, _cameraHeight, 0),
            Vector3.UnitY);

        // Begin frame with dark blue background
        _renderer.BeginFrame(new Color4(0.1f, 0.1f, 0.2f, 1.0f));

        // Body transform with mouse rotation - pivot around model center (not feet)
        float pivotY = 0.85f;  // Model center height (half of ~1.7m body height)
        var bodyWorld = Matrix4x4.CreateTranslation(0, -pivotY, 0) *  // Move center to origin
                        Matrix4x4.CreateRotationX(_rotationX) *
                        Matrix4x4.CreateRotationY(_rotationY) *
                        Matrix4x4.CreateTranslation(0, pivotY, 0);    // Move back

        // Update and draw body (uses default front lighting from D3DShared)
        _renderer.UpdateConstantBuffer(_bodyConstantBuffer!, bodyWorld,
            new Vector4(0.85f, 0.72f, 0.62f, 1.0f)); // Skin color

        _renderer.CommandList!.SetGraphicsRootConstantBufferView(0, _bodyConstantBuffer!.GPUVirtualAddress);
        _renderer.CommandList.IASetVertexBuffers(0, _bodyVertexBufferView);
        _renderer.CommandList.IASetIndexBuffer(_bodyIndexBufferView);
        _renderer.CommandList.DrawIndexedInstanced((uint)_bodyIndexCount, 1, 0, 0, 0);

        // End frame
        _renderer.EndFrame();
    }
}
