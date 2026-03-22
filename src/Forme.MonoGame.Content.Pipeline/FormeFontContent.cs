// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Forme.MonoGame.Content.Pipeline;

/// <summary>
/// Intermediate content object that carries all processed font data through the MonoGame
/// content pipeline between the importer/processor and the writer.
/// </summary>
public sealed class FormeFontContent
{
    /// <summary>
    /// Gets or sets the font metrics produced by the font processor.
    /// </summary>
    public FontMetrics Metrics { get; set; }

    /// <summary>
    /// Gets or sets the list of processed glyphs, one entry per processed code point.
    /// </summary>
    public List<FormeGlyph> Glyphs { get; set; } = [];

    /// <summary>
    /// Gets or sets the raw RGBA32F texel data for the curve texture, stored as
    /// four floats per texel in row-major order.
    /// </summary>
    public float[] CurveTextureData { get; set; } = [];

    /// <summary>
    /// Gets or sets the width of the curve texture in texels.
    /// </summary>
    public int CurveTextureWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the curve texture in texels.
    /// </summary>
    public int CurveTextureHeight { get; set; }

    /// <summary>
    /// Gets or sets the raw RG32F texel data for the band texture, stored as
    /// two floats per texel in row-major order.
    /// </summary>
    public float[] BandTextureData { get; set; } = [];

    /// <summary>
    /// Gets or sets the width of the band texture in texels.
    /// </summary>
    public int BandTextureWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the band texture in texels.
    /// </summary>
    public int BandTextureHeight { get; set; }
}
