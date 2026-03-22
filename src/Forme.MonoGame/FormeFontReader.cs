// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace Forme.MonoGame;

/// <summary>
/// Reads a <see cref="FormeFont"/> from an XNB content file produced by the Forme content
/// pipeline extension. Referenced at runtime by the string returned from
/// <c>FormeFontWriter.GetRuntimeReader</c>.
/// </summary>
public sealed class FormeFontReader : ContentTypeReader<FormeFont>
{
    /// <summary>
    /// Reads font metrics, glyph table, and texture data from <paramref name="input"/> and
    /// returns a <see cref="FormeFont"/> ready for use with <see cref="FormeFontDevice"/>.
    /// </summary>
    protected override FormeFont Read(ContentReader input, FormeFont existingInstance)
    {
        int unitsPerEm = input.ReadInt32();
        int ascent = input.ReadInt32();
        int descent = input.ReadInt32();
        int lineGap = input.ReadInt32();
        FontMetrics metrics = new FontMetrics(unitsPerEm, ascent, descent, lineGap);

        int glyphCount = input.ReadInt32();
        Dictionary<int, FormeGlyph> glyphs = new Dictionary<int, FormeGlyph>(glyphCount);
        for (int i = 0; i < glyphCount; i++)
        {
            int codePoint = input.ReadInt32();
            int bBoxX1 = input.ReadInt32();
            int bBoxY1 = input.ReadInt32();
            int bBoxX2 = input.ReadInt32();
            int bBoxY2 = input.ReadInt32();
            int advanceWidth = input.ReadInt32();
            int leftSideBearing = input.ReadInt32();
            int bandCount = input.ReadInt32();
            int bandDimX = input.ReadInt32();
            int bandDimY = input.ReadInt32();
            int bandsTexCoordX = input.ReadInt32();
            int bandsTexCoordY = input.ReadInt32();

            FormeGlyph glyph = new FormeGlyph(
                codePoint,
                new FormeBoundingBox(bBoxX1, bBoxY1, bBoxX2, bBoxY2),
                advanceWidth,
                leftSideBearing,
                new FormeBandInfo(bandCount, bandDimX, bandDimY, bandsTexCoordX, bandsTexCoordY));

            glyphs[codePoint] = glyph;
        }

        int curveWidth = input.ReadInt32();
        int curveHeight = input.ReadInt32();
        int curveDataLength = input.ReadInt32();
        float[] curveData = new float[curveDataLength];
        for (int i = 0; i < curveDataLength; i++)
        {
            curveData[i] = input.ReadSingle();
        }

        int bandWidth = input.ReadInt32();
        int bandHeight = input.ReadInt32();
        int bandDataLength = input.ReadInt32();
        float[] bandData = new float[bandDataLength];
        for (int i = 0; i < bandDataLength; i++)
        {
            bandData[i] = input.ReadSingle();
        }

        return new FormeFont(
            metrics,
            glyphs,
            new FormeTextureData(curveData, curveWidth, curveHeight),
            new FormeTextureData(bandData, bandWidth, bandHeight));
    }
}
