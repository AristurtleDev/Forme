// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Per-glyph metadata produced by font processing, expressed in font design units.
/// </summary>
/// <remarks>
/// All coordinate and dimension values are in the font's native design unit system.
/// To convert to pixels, multiply by <c>sizePixels / font.Metrics.UnitsPerEm</c>.
/// </remarks>
public readonly struct FormeGlyph
{
    /// <summary>
    /// Gets the Unicode code point this glyph represents.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// Gets the axis-aligned bounding box of the glyph in font design units.
    /// </summary>
    public FormeBoundingBox BoundingBox { get; }

    /// <summary>
    /// Gets the width of the glyph bounding box in font units.
    /// </summary>
    public int Width
    {
        get { return BoundingBox.Width; }
    }

    /// <summary>
    /// Gets the height of the glyph bounding box in font units.
    /// </summary>
    public int Height
    {
        get { return BoundingBox.Height; }
    }

    /// <summary>
    /// Gets the horizontal advance width in font units.
    /// </summary>
    public int AdvanceWidth { get; }

    /// <summary>
    /// Gets the left-side bearing in font units. May be negative for glyphs that extend
    /// to the left of the origin.
    /// </summary>
    public int LeftSideBearing { get; }

    /// <summary>
    /// Gets the band partition metadata for this glyph.
    /// </summary>
    public FormeBandInfo BandInfo { get; }

    /// <summary>
    /// Initializes a new <see cref="FormeGlyph"/> with the given values.
    /// </summary>
    public FormeGlyph(int codePoint, FormeBoundingBox boundingBox, int advanceWidth, int leftSideBearing, FormeBandInfo bandInfo)
    {
        CodePoint = codePoint;
        BoundingBox = boundingBox;
        AdvanceWidth = advanceWidth;
        LeftSideBearing = leftSideBearing;
        BandInfo = bandInfo;
    }
}
