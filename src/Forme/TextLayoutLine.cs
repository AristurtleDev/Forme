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
    /// Gets the zero-based UTF-16 start index of this line within the source text.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length of the source text represented by this line.
    /// </summary>
    /// <remarks>
    /// Synthetic glyphs introduced by layout policy, such as an ellipsis string, are not counted
    /// in this source range.
    /// </remarks>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index of the source text represented by this line.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the baseline Y position of this line, relative to the layout origin.
    /// </summary>
    public float BaselineY { get; }

    /// <summary>
    /// Gets the baseline-to-baseline distance used for this line in pixels.
    /// </summary>
    public float LineHeight { get; }

    /// <summary>
    /// Gets the ascent used for this line in pixels. This value is positive and extends upward
    /// from the baseline.
    /// </summary>
    public float Ascent { get; }

    /// <summary>
    /// Gets the descent used for this line in pixels. This value is typically negative and extends
    /// downward from the baseline.
    /// </summary>
    public float Descent { get; }

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
    /// Gets the exclusive end glyph index for this line within <see cref="TextLayoutResult.Glyphs"/>.
    /// </summary>
    public int GlyphEnd => GlyphStart + GlyphCount;

    /// <summary>
    /// Gets the index of the first run touching this line within <see cref="TextLayoutResult.Runs"/>.
    /// </summary>
    public int RunStart { get; }

    /// <summary>
    /// Gets the number of runs touching this line.
    /// </summary>
    public int RunCount { get; }

    /// <summary>
    /// Gets the exclusive end run index for this line within <see cref="TextLayoutResult.Runs"/>.
    /// </summary>
    public int RunEnd => RunStart + RunCount;

    /// <summary>
    /// Returns whether the given UTF-16 text index falls within this line's source-text range.
    /// </summary>
    public bool ContainsTextIndex(int textIndex)
    {
        return TextLength > 0 && textIndex >= TextStart && textIndex < TextEnd;
    }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutLine"/> with the given values.
    /// </summary>
    public TextLayoutLine(
        int textStart,
        int textLength,
        float baselineY,
        float lineHeight,
        float ascent,
        float descent,
        float width,
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        int glyphStart,
        int glyphCount,
        int runStart,
        int runCount)
    {
        TextStart = textStart;
        TextLength = textLength;
        BaselineY = baselineY;
        LineHeight = lineHeight;
        Ascent = ascent;
        Descent = descent;
        Width = width;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        GlyphStart = glyphStart;
        GlyphCount = glyphCount;
        RunStart = runStart;
        RunCount = runCount;
    }
}
