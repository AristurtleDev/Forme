using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Forme.MonoGame;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FormeVertex
{
    // POSITION0: xy = screen position, zw = outward corner normal
    internal Vector4 Pos;

    // TEXCOORD0: xy = em-space sample coordinates, z = packed band texture location, w = band count
    internal Vector4 Tex;

    // TEXCOORD1: xyzw = normalized RGBA color (R/255, G/255, B/255, A/255)
    internal Vector4 Color;

    // TEXCOORD2: bandScaleX, bandScaleY, bandOffsetX, bandOffsetY
    internal Vector4 Bnd;

    internal const int SizeInBytes = 4 * 4 * 4;

    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement( 0, VertexElementFormat.Vector4, VertexElementUsage.Position,          0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2)
    );
}
