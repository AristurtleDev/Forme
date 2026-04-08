// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Forme.MonoGame.Content.Pipeline;

/// <summary>
/// Writes <see cref="FormeFontContent"/> to an XNB stream for loading at runtime via
/// <c>Content.Load&lt;FormeFont&gt;()</c>.
/// </summary>
[ContentTypeWriter]
public sealed class FormeFontWriter : ContentTypeWriter<FormeFontContent>
{
    /// <summary>
    /// Returns the assembly-qualified name of <c>Forme.MonoGame.FormeFontReader</c>
    /// which resides in the <c>Forme.MonoGame</c> runtime assembly.
    /// </summary>
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        return "Forme.MonoGame.FormeFontReader, Forme.MonoGame";
    }

    /// <summary>
    /// Writes the font metrics, glyph table, and texture data to <paramref name="output"/>.
    /// </summary>
    protected override void Write(ContentWriter output, FormeFontContent value)
    {
        output.Write(value.Metrics.UnitsPerEm);
        output.Write(value.Metrics.Ascent);
        output.Write(value.Metrics.Descent);
        output.Write(value.Metrics.LineGap);
        output.Write(value.PairAdjustments.Count);

        output.Write(value.Glyphs.Count);
        foreach (FormeGlyph glyph in value.Glyphs)
        {
            output.Write(glyph.CodePoint);
            output.Write(glyph.BoundingBox.X1);
            output.Write(glyph.BoundingBox.Y1);
            output.Write(glyph.BoundingBox.X2);
            output.Write(glyph.BoundingBox.Y2);
            output.Write(glyph.AdvanceWidth);
            output.Write(glyph.LeftSideBearing);
            output.Write(glyph.BandInfo.Count);
            output.Write(glyph.BandInfo.DimX);
            output.Write(glyph.BandInfo.DimY);
            output.Write(glyph.BandInfo.TexCoordX);
            output.Write(glyph.BandInfo.TexCoordY);
        }

        List<ulong> pairKeys = new List<ulong>(value.PairAdjustments.Keys);
        pairKeys.Sort();
        foreach (ulong pairKey in pairKeys)
        {
            output.Write(pairKey);
            output.Write(value.PairAdjustments[pairKey]);
        }

        output.Write(value.CurveTextureWidth);
        output.Write(value.CurveTextureHeight);
        output.Write(value.CurveTextureData.Length);
        foreach (float f in value.CurveTextureData)
        {
            output.Write(f);
        }

        output.Write(value.BandTextureWidth);
        output.Write(value.BandTextureHeight);
        output.Write(value.BandTextureData.Length);
        foreach (float f in value.BandTextureData)
        {
            output.Write(f);
        }
    }
}
