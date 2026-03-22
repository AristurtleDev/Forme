// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Stores the atlas position, pixel offsets, and horizontal metrics for a single glyph
/// baked into a <see cref="FormeSpriteAtlas"/>.
/// </summary>
public readonly struct FormeSpriteAtlasGlyph
{
    /// <summary>
    /// Gets the Unicode code point of the glyph.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// Gets the position and size of the glyph's pixels within the atlas texture.
    /// </summary>
    /// <remarks>
    /// Width and Height are zero for glyphs with no visible outline, such as spaces.
    /// The atlas position (X, Y) for such glyphs is (0, 0) and should not be used for
    /// rendering; advance width is carried entirely through <see cref="Kerning"/>.
    /// </remarks>
    public FormeSpriteAtlasRegion AtlasRegion { get; }

    /// <summary>
    /// Gets the horizontal pixel offset from the glyph's layout origin to the left edge
    /// of its rendered bitmap.
    /// </summary>
    /// <remarks>
    /// Equivalent to the integer-pixel left side bearing. Negative values indicate the
    /// rendered pixels extend to the left of the layout origin.
    /// </remarks>
    public int BitmapOriginX { get; }

    /// <summary>
    /// Gets the vertical pixel offset from the baseline to the top edge of the rendered bitmap.
    /// </summary>
    /// <remarks>
    /// Negative for glyphs that extend above the baseline, such as capital letters and ascenders.
    /// Positive values indicate the top of the bitmap is below the baseline.
    /// </remarks>
    public int BitmapOriginY { get; }

    /// <summary>
    /// Gets the horizontal spacing metrics for this glyph.
    /// </summary>
    public FormeSpriteAtlasKerning Kerning { get; }

    /// <summary>
    /// Initializes a new <see cref="FormeSpriteAtlasGlyph"/>.
    /// </summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="atlasRegion">The position and size within the atlas texture.</param>
    /// <param name="bitmapOriginX">The horizontal pixel offset from the layout origin to the bitmap left edge.</param>
    /// <param name="bitmapOriginY">The vertical pixel offset from the baseline to the bitmap top edge.</param>
    /// <param name="kerning">The horizontal spacing metrics.</param>
    public FormeSpriteAtlasGlyph(int codePoint, FormeSpriteAtlasRegion atlasRegion, int bitmapOriginX, int bitmapOriginY, FormeSpriteAtlasKerning kerning)
    {
        CodePoint = codePoint;
        AtlasRegion = atlasRegion;
        BitmapOriginX = bitmapOriginX;
        BitmapOriginY = bitmapOriginY;
        Kerning = kerning;
    }
}
