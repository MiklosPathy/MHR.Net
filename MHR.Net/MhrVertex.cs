// Vertex structure for MHR mesh output
// Independent of rendering framework

using System.Numerics;
using System.Runtime.InteropServices;

namespace MHR.Net;

/// <summary>
/// Vertex with position and normal for MHR mesh output.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MhrVertex
{
    /// <summary>
    /// Vertex position in world space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Vertex normal (unit vector).
    /// </summary>
    public Vector3 Normal;

    public MhrVertex(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }
}
