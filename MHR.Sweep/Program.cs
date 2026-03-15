// MHR Parameter Sweep Tool
// Renders baseline + positive/negative for each parameter from multiple angles,
// saves images with metadata for vision model identification.

using D3DShared;
using MHR.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TorchSharp;
using Vortice.Direct3D12;
using Vortice.Mathematics;
using Console = MHR.Sweep.DebugConsole;

namespace MHR.Sweep;

class Program
{
    static int _width = 800;
    static int _height = 800;

    static D3D12Renderer? _renderer;
    static ID3D12Resource? _bodyVertexBuffer;
    static ID3D12Resource? _bodyIndexBuffer;
    static VertexBufferView _bodyVertexBufferView;
    static IndexBufferView _bodyIndexBufferView;
    static int _bodyIndexCount;
    static ID3D12Resource? _bodyConstantBuffer;

    static MhrModel? _mhrModel;
    static MhrVertex[]? _currentMhrVertices;

    const int IdentityParamCount = MhrParameters.IdentityCount;
    const int PoseParamCount = MhrParameters.PoseCount;
    const int ExpressionParamCount = MhrParameters.ExpressionCount;
    const int TotalParamCount = MhrParameters.TotalCount;

    static float[] _identityParams = new float[IdentityParamCount];
    static float[] _poseParams = new float[PoseParamCount];
    static float[] _expressionParams = new float[ExpressionParamCount];

    // Camera angles to capture from (name, rotationY, rotationX)
    static readonly (string Name, float RotY, float RotX)[] CameraAngles =
    [
        ("front",   MathF.PI, 0f),
        ("side_l",  MathF.PI + MathF.PI / 2f, 0f),
        ("side_r",  MathF.PI - MathF.PI / 2f, 0f),
        ("back",    0f, 0f),
    ];

    static float _rotationX;
    static float _rotationY = MathF.PI;
    static float _cameraDistance = 2.67f;  // ~4.0 / 1.5 for 1.5x zoom
    static float _cameraHeight = 0.85f;

    const int SweepStart = 0;
    const int SweepCount = MhrParameters.TotalCount;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Create form - no UI panels, just render area
        var form = new Form
        {
            Text = "MHR Sweep",
            ClientSize = new System.Drawing.Size(_width, _height),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            StartPosition = FormStartPosition.CenterScreen
        };

        // Output directory
        var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sweep_output");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Initializing...");

        _renderer = new D3D12Renderer(_width, _height);
        _renderer.EnumerateAdapters();
        _renderer.Initialize(form.Handle, 0);
        _bodyConstantBuffer = _renderer.CreateConstantBuffer();

        try
        {
            var assetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            _mhrModel = MhrModel.Load(
                device: torch.cuda.is_available() ? torch.CUDA : torch.CPU,
                lod: MhrLod.LOD1,
                assetFolder: assetFolder);
            Console.WriteLine($"Model loaded. Vertices: {_mhrModel.NumVertices}");

            GenerateBody();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load MHR model:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        form.Show();

        // Let the window fully render first
        Application.DoEvents();
        Render();

        Console.WriteLine($"Starting sweep of first {SweepCount} parameters...");
        Console.WriteLine($"Output directory: {outputDir}");

        // Run the sweep
        var metadata = new List<ParamSweepEntry>();

        // Capture baseline images from all angles (once, reused for every param)
        Console.WriteLine("Capturing baseline from all angles...");
        var baselineImages = new Dictionary<string, Bitmap>();
        ResetAllParams();
        GenerateBody();
        foreach (var (angleName, rotY, rotX) in CameraAngles)
        {
            _rotationY = rotY;
            _rotationX = rotX;
            RenderStable(form);
            baselineImages[angleName] = CaptureClientArea(form);
            baselineImages[angleName].Save(Path.Combine(outputDir, $"baseline_{angleName}.png"), ImageFormat.Png);
        }

        for (int paramIdx = SweepStart; paramIdx < SweepStart + SweepCount; paramIdx++)
        {
            var paramName = MhrParameters.All[paramIdx].Name;
            var category = MhrParameters.All[paramIdx].Category;
            Console.WriteLine($"[{paramIdx + 1}/{SweepCount}] Sweeping param {paramIdx}: {paramName} ({category})");

            var entry = new ParamSweepEntry
            {
                ParamIndex = paramIdx,
                CurrentName = paramName,
                Category = category,
                Images = []
            };

            // Capture variants: half and full in both directions
            var variantImages = new Dictionary<(string variant, string angle), Bitmap>();

            var paramDef = MhrParameters.All[paramIdx];
            Console.WriteLine($"  Range: {paramDef.RangeMin:F4} .. {paramDef.RangeMax:F4}");

            var variants = new[]
            {
                ("neg_full", paramDef.RangeMin),
                ("neg_half", paramDef.RangeMin * 0.5f),
                ("pos_half", paramDef.RangeMax * 0.5f),
                ("pos_full", paramDef.RangeMax),
            };

            foreach (var (variant, value) in variants)
            {
                ResetAllParams();
                SetParam(paramIdx, value);
                GenerateBody();

                foreach (var (angleName, rotY, rotX) in CameraAngles)
                {
                    _rotationY = rotY;
                    _rotationX = rotX;
                    RenderStable(form);

                    var bmp = CaptureClientArea(form);
                    variantImages[(variant, angleName)] = bmp;

                    var fileName = $"p{paramIdx:D3}_{variant}_{angleName}.png";
                    bmp.Save(Path.Combine(outputDir, fileName), ImageFormat.Png);

                    entry.Images.Add(new ImageEntry
                    {
                        FileName = fileName,
                        Variant = variant,
                        Value = value,
                        Angle = angleName
                    });
                }
            }

            // Generate heatmaps per angle: sum of diffs across all variants
            foreach (var (angleName, _, _) in CameraAngles)
            {
                var baseline = baselineImages[angleName];
                var variantBmps = variants.Select(v => variantImages[(v.Item1, angleName)]).ToArray();

                using var heatmap = GenerateHeatmap(baseline, variantBmps);
                var heatFileName = $"p{paramIdx:D3}_heatmap_{angleName}.png";
                heatmap.Save(Path.Combine(outputDir, heatFileName), ImageFormat.Png);

                entry.Images.Add(new ImageEntry
                {
                    FileName = heatFileName,
                    Variant = "heatmap",
                    Value = 0,
                    Angle = angleName
                });
            }

            // Dispose variant bitmaps
            foreach (var bmp in variantImages.Values)
                bmp.Dispose();

            metadata.Add(entry);

            // Reset after each param
            ResetAllParams();
            GenerateBody();
        }

        // Dispose baseline bitmaps
        foreach (var bmp in baselineImages.Values)
            bmp.Dispose();

        // Save metadata JSON
        var metadataPath = Path.Combine(outputDir, "sweep_metadata.json");
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metadataPath, json);

        Console.WriteLine($"Done! Saved {metadata.Count} parameter sweeps to {outputDir}");
        Console.WriteLine($"Metadata: {metadataPath}");

        MessageBox.Show(
            $"Sweep complete!\n\n{metadata.Count} parameters swept\n{metadata.Sum(m => m.Images.Count)} images saved\n\nOutput: {outputDir}",
            "MHR Sweep", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Cleanup
        _bodyVertexBuffer?.Dispose();
        _bodyIndexBuffer?.Dispose();
        _bodyConstantBuffer?.Dispose();
        _mhrModel?.Dispose();
        _renderer.Dispose();
    }

    static void ResetAllParams()
    {
        Array.Clear(_identityParams);
        Array.Clear(_poseParams);
        Array.Clear(_expressionParams);
    }

    static void SetParam(int globalIndex, float value)
    {
        if (globalIndex < IdentityParamCount)
            _identityParams[globalIndex] = value;
        else if (globalIndex < IdentityParamCount + PoseParamCount)
            _poseParams[globalIndex - IdentityParamCount] = value;
        else
            _expressionParams[globalIndex - IdentityParamCount - PoseParamCount] = value;
    }


    static void RenderStable(Form form)
    {
        Application.DoEvents();
        Render();
    }

    static Bitmap CaptureClientArea(Form form)
    {
        var rect = form.RectangleToScreen(form.ClientRectangle);
        var bmp = new Bitmap(rect.Width, rect.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
        }
        return bmp;
    }

    static unsafe Bitmap GenerateHeatmap(Bitmap baseline, Bitmap[] variants)
    {
        int w = baseline.Width, h = baseline.Height;
        var heatmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        var rect = new Rectangle(0, 0, w, h);

        // Lock all bitmaps for direct pixel access
        var baseData = baseline.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var varData = new BitmapData[variants.Length];
        for (int i = 0; i < variants.Length; i++)
            varData[i] = variants[i].LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var heatData = heatmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        int varCount = variants.Length;
        int scale = 200 * varCount;

        byte* baseScan0 = (byte*)baseData.Scan0;
        byte* heatScan0 = (byte*)heatData.Scan0;
        int baseStride = baseData.Stride;
        int heatStride = heatData.Stride;

        // Cache variant scan pointers and strides
        var varScan0 = new byte*[varCount];
        var varStrides = new int[varCount];
        for (int i = 0; i < varCount; i++)
        {
            varScan0[i] = (byte*)varData[i].Scan0;
            varStrides[i] = varData[i].Stride;
        }

        Parallel.For(0, h, y =>
        {
            byte* baseRow = baseScan0 + y * baseStride;
            byte* heatRow = heatScan0 + y * heatStride;

            for (int x = 0; x < w; x++)
            {
                int bx = x * 4;
                byte bB = baseRow[bx];
                byte bG = baseRow[bx + 1];
                byte bR = baseRow[bx + 2];

                int diff = 0;
                for (int v = 0; v < varCount; v++)
                {
                    byte* varRow = varScan0[v] + y * varStrides[v];
                    diff += Math.Abs(varRow[bx + 2] - bR)
                          + Math.Abs(varRow[bx + 1] - bG)
                          + Math.Abs(varRow[bx] - bB);
                }

                int intensity = Math.Min(255, diff * 255 / scale);

                int r, g, b;
                if (intensity < 85)
                {
                    r = intensity * 3; g = 0; b = 0;
                }
                else if (intensity < 170)
                {
                    r = 255; g = (intensity - 85) * 3; b = 0;
                }
                else
                {
                    r = 255; g = 255; b = (intensity - 170) * 3;
                }

                heatRow[bx]     = (byte)Math.Min(255, b);
                heatRow[bx + 1] = (byte)Math.Min(255, g);
                heatRow[bx + 2] = (byte)Math.Min(255, r);
                heatRow[bx + 3] = 255; // alpha
            }
        });

        // Unlock all
        baseline.UnlockBits(baseData);
        for (int i = 0; i < variants.Length; i++)
            variants[i].UnlockBits(varData[i]);
        heatmap.UnlockBits(heatData);

        return heatmap;
    }

    static void GenerateBody()
    {
        if (_mhrModel == null || _renderer == null) return;

        try
        {
            var identityTensor = torch.tensor(_identityParams, dtype: torch.ScalarType.Float32);
            var expressionTensor = torch.tensor(_expressionParams, dtype: torch.ScalarType.Float32);
            var poseTensor = torch.tensor(_poseParams, dtype: torch.ScalarType.Float32);

            var output = _mhrModel.Forward(identityTensor, poseTensor, expressionTensor);
            _currentMhrVertices = _mhrModel.ToVertexArray(output);

            uint[]? indices = null;
            if (_mhrModel.Indices != null && _mhrModel.Indices.Length > 0 &&
                _mhrModel.Indices.Max() < _currentMhrVertices.Length)
            {
                indices = _mhrModel.Indices;
            }
            indices ??= _mhrModel.FindMatchingIndices(_currentMhrVertices.Length);
            indices ??= _mhrModel.GenerateFallbackIndices(_currentMhrVertices.Length);

            CreateBuffers(_currentMhrVertices, indices);

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
        _bodyVertexBuffer?.Dispose();
        _bodyIndexBuffer?.Dispose();

        var vertices = new Vertex[mhrVertices.Length];
        for (int i = 0; i < mhrVertices.Length; i++)
            vertices[i] = new Vertex(mhrVertices[i].Position, mhrVertices[i].Normal);

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

        _renderer.View = Matrix4x4.CreateLookAt(
            new Vector3(0, _cameraHeight, -_cameraDistance),
            new Vector3(0, _cameraHeight, 0),
            Vector3.UnitY);

        _renderer.BeginFrame(new Color4(0.1f, 0.1f, 0.2f, 1.0f));

        float pivotY = 0.85f;
        var bodyWorld = Matrix4x4.CreateTranslation(0, -pivotY, 0) *
                        Matrix4x4.CreateRotationX(_rotationX) *
                        Matrix4x4.CreateRotationY(_rotationY) *
                        Matrix4x4.CreateTranslation(0, pivotY, 0);

        _renderer.UpdateConstantBuffer(_bodyConstantBuffer!, bodyWorld,
            new Vector4(0.85f, 0.72f, 0.62f, 1.0f));

        _renderer.CommandList!.SetGraphicsRootConstantBufferView(0, _bodyConstantBuffer!.GPUVirtualAddress);
        _renderer.CommandList.IASetVertexBuffers(0, _bodyVertexBufferView);
        _renderer.CommandList.IASetIndexBuffer(_bodyIndexBufferView);
        _renderer.CommandList.DrawIndexedInstanced((uint)_bodyIndexCount, 1, 0, 0, 0);

        _renderer.EndFrame();
    }

}

// Metadata types
record ParamSweepEntry
{
    public int ParamIndex { get; set; }
    public string CurrentName { get; set; } = "";
    public string Category { get; set; } = "";
    public List<ImageEntry> Images { get; set; } = [];
}

record ImageEntry
{
    public string FileName { get; set; } = "";
    public string Variant { get; set; } = "";
    public float Value { get; set; }
    public string Angle { get; set; } = "";
}
