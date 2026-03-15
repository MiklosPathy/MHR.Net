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

    // Parameter counts (from central registry)
    const int IdentityParamCount = MhrParameters.IdentityCount;
    const int PoseParamCount = MhrParameters.PoseCount;
    const int ExpressionParamCount = MhrParameters.ExpressionCount;
    const int TotalParamCount = MhrParameters.TotalCount;

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

        // Build UI from central parameter registry
        string? currentTab = null;
        ParameterGroup? firstGroupPerTab = null;

        foreach (var (category, groupName, names, startIndex) in MhrParameters.Groups)
        {
            // Add tab when category changes
            if (category != currentTab)
            {
                if (firstGroupPerTab != null) firstGroupPerTab.Expand();
                currentTab = category;
                firstGroupPerTab = null;
                _paramPanel.AddTab(category);
            }

            var group = _paramPanel.AddGroup(category, groupName);
            firstGroupPerTab ??= group;

            for (int i = 0; i < names.Length; i++)
            {
                var p = MhrParameters.All[startIndex + i];
                group.AddSlider(names[i], startIndex + i, rangeMin: p.RangeMin, rangeMax: p.RangeMax);
            }
        }
        firstGroupPerTab?.Expand();

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

        // Slider values are now direct parameter values (fixed-point /100)
        for (int i = 0; i < IdentityParamCount; i++)
            _identityParams[i] = allValues[i];

        for (int i = 0; i < PoseParamCount; i++)
            _poseParams[i] = allValues[IdentityParamCount + i];

        for (int i = 0; i < ExpressionParamCount; i++)
            _expressionParams[i] = allValues[IdentityParamCount + PoseParamCount + i];
    }

    static void GenerateBody()
    {
        if (_mhrModel == null || _renderer == null) return;

        try
        {
            // Create parameter tensors
            var identityTensor = torch.tensor(_identityParams, dtype: torch.ScalarType.Float32);
            var expressionTensor = torch.tensor(_expressionParams, dtype: torch.ScalarType.Float32);
            var poseTensor = torch.tensor(_poseParams, dtype: torch.ScalarType.Float32);

            // Run inference
            var output = _mhrModel.Forward(identityTensor, poseTensor, expressionTensor);

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
