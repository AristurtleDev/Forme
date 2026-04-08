// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes one laid-out line of text within a <see cref="TextLayoutResult"/>.
/// </summary>
public readonly struct TextLayoutLine
{
    /// <summary>
    /// Gets the baseline Y position of this line, relative to the layout origin.
    /// </summary>
    public float BaselineY { get; }

    /// <summary>
    /// Gets the baseline-to-baseline distance used for this line in pixels.
    /// </summary>
    public float LineHeight { get; }

    /// <summary>
    /// Gets the logical line width in pixels after wrapping, alignment, and pair positioning.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the logical bounds of this line in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds LogicalBounds { get; }

    /// <summary>
    /// Gets the visual bounds of this line in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds VisualBounds { get; }

    /// <summary>
    /// Gets the index of the first glyph in this line within <see cref="TextLayoutResult.Glyphs"/>.
    /// </summary>
    public int GlyphStart { get; }

    /// <summary>
    /// Gets the number of glyph placements that belong to this line.
    /// </summary>
    public int GlyphCount { get; }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutLine"/> with the given values.
    /// </summary>
    public TextLayoutLine(
        float baselineY,
        float lineHeight,
        float width,
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        int glyphStart,
        int glyphCount)
    {
        BaselineY = baselineY;
        LineHeight = lineHeight;
        Width = width;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        GlyphStart = glyphStart;
        GlyphCount = glyphCount;
    }
}
