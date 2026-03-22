// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Band partition metadata for a glyph, describing how the Slug algorithm divides the
/// glyph bounding box into a grid of bands and where that grid is stored in the band texture.
/// </summary>
public readonly struct FormeBandInfo
{
    /// <summary>
    /// Gets the number of bands per axis used to partition this glyph.
    /// Ranges from 1 to 16.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the width of each horizontal band partition in font units.
    /// Computed as <c>ceil((BBoxX2 - BBoxX1 + 1) / Count)</c>.
    /// </summary>
    public int DimX { get; }

    /// <summary>
    /// Gets the height of each vertical band partition in font units.
    /// Computed as <c>ceil((BBoxY2 - BBoxY1 + 1) / Count)</c>.
    /// </summary>
    public int DimY { get; }

    /// <summary>
    /// Gets the X texel coordinate in the band texture where this glyph's band data begins.
    /// The band texture is always 4096 texels wide.
    /// </summary>
    public int TexCoordX { get; }

    /// <summary>
    /// Gets the Y texel coordinate in the band texture where this glyph's band data begins.
    /// </summary>
    public int TexCoordY { get; }

    /// <summary>
    /// Initializes a new <see cref="FormeBandInfo"/> with the given values.
    /// </summary>
    public FormeBandInfo(int count, int dimX, int dimY, int texCoordX, int texCoordY)
    {
        Count = count;
        DimX = dimX;
        DimY = dimY;
        TexCoordX = texCoordX;
        TexCoordY = texCoordY;
    }
}
