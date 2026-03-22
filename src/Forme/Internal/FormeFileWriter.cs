// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Forme.Internal;

internal static class FormeFileWriter
{
    internal static void Write(FormeFont font, Stream stream)
    {
        using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(FormeFileConstants.Magic);
        writer.Write(FormeFileConstants.Version);
        writer.Write(FormeFileConstants.Flags);
        writer.Write((ushort)font.Glyphs.Count);
        writer.Write((ushort)font.Metrics.UnitsPerEm);
        writer.Write(font.Metrics.Ascent);
        writer.Write(font.Metrics.Descent);
        writer.Write(font.Metrics.LineGap);

        foreach (KeyValuePair<int, FormeGlyph> pair in font.Glyphs)
        {
            FormeGlyph g = pair.Value;
            writer.Write((uint)g.CodePoint);
            writer.Write(g.BoundingBox.X1);
            writer.Write(g.BoundingBox.Y1);
            writer.Write(g.BoundingBox.X2);
            writer.Write(g.BoundingBox.Y2);
            writer.Write(g.AdvanceWidth);
            writer.Write(g.LeftSideBearing);
            writer.Write((uint)g.BandInfo.Count);
            writer.Write((uint)g.BandInfo.DimX);
            writer.Write((uint)g.BandInfo.DimY);
            writer.Write((ushort)g.BandInfo.TexCoordX);
            writer.Write((ushort)g.BandInfo.TexCoordY);
        }

        WriteFloatTexture(writer, font.CurveTexture.Data.Span, font.CurveTexture.Width, font.CurveTexture.Height);
        WriteFloatTexture(writer, font.BandTexture.Data.Span, font.BandTexture.Width, font.BandTexture.Height);
    }

    private static void WriteFloatTexture(BinaryWriter writer, ReadOnlySpan<float> data, int width, int height)
    {
        writer.Write((ushort)width);
        writer.Write((ushort)height);
        writer.Write((uint)(data.Length * sizeof(float)));

        foreach (float f in data)
        {
            writer.Write(f);
        }
    }
}
