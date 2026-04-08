// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// The layout-computed position and metrics for a single glyph, expressed as pixel offsets relative
/// to the draw origin.
/// </summary>
/// <remarks>
/// Returned by <see cref="FormeFont.GetGlyphs(System.ReadOnlySpan{char}, float)"/> and its overloads.
/// Positions are relative to the draw origin (0, 0), which corresponds to the baseline of the first
/// line. Callers should add their actual draw position to <see cref="BaselineX"/> and
/// <see cref="BaselineY"/> to get screen-space coordinates.
/// </remarks>
public readonly struct GlyphPlacement
{
    /// <summary>
    /// Gets the zero-based index of the first UTF-16 code unit of this glyph in the source string.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the UTF-16 length of the source text represented by this glyph.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index of the source text represented by this glyph.
    /// </summary>
    public int TextEnd => Index + TextLength;

    /// <summary>
    /// Gets the Unicode code point this glyph represents.
    /// </summary>
    public int CodePoint { get; }

    /// <summary>
    /// Gets the index of the line that owns this glyph within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the index of the run that owns this glyph within <see cref="TextLayoutResult.Runs"/>.
    /// </summary>
    public int RunIndex { get; }

    /// <summary>
    /// Gets the baseline X position of this glyph in pixels, relative to the draw origin.
    /// </summary>
    public float BaselineX { get; }

    /// <summary>
    /// Gets the baseline Y position of this glyph in pixels, relative to the draw origin.
    /// </summary>
    /// <remarks>
    /// This value is 0 for glyphs on the first line and increases by one line height for each
    /// subsequent line.
    /// </remarks>
    public float BaselineY { get; }

    /// <summary>
    /// Gets the logical bounding rectangle of this glyph in pixels, relative to the draw origin.
    /// </summary>
    /// <remarks>
    /// This box reflects the glyph's allocated layout cell rather than its outline overhang.
    /// </remarks>
    public FormeTextBounds LogicalBounds { get; }

    /// <summary>
    /// Gets the visual bounding rectangle of this glyph in pixels, relative to the draw origin.
    /// </summary>
    /// <remarks>
    /// For glyphs with no visible outline (such as spaces), this is a zero-size rectangle at the
    /// baseline position.
    /// </remarks>
    public FormeTextBounds VisualBounds { get; }

    /// <summary>
    /// Gets the pixel advance width of this glyph, including any <see cref="TextLayoutOptions.CharacterSpacing"/>.
    /// </summary>
    public float AdvanceWidth { get; }

    /// <summary>
    /// Returns whether the given UTF-16 text index falls within this glyph's source-text range.
    /// </summary>
    public bool ContainsTextIndex(int textIndex)
    {
        return TextLength > 0 && textIndex >= Index && textIndex < TextEnd;
    }

    /// <summary>
    /// Initializes a new <see cref="GlyphPlacement"/> with the given values.
    /// </summary>
    public GlyphPlacement(
        int index,
        int textLength,
        int codePoint,
        int lineIndex,
        int runIndex,
        float baselineX,
        float baselineY,
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        float advanceWidth)
    {
        Index = index;
        TextLength = textLength;
        CodePoint = codePoint;
        LineIndex = lineIndex;
        RunIndex = runIndex;
        BaselineX = baselineX;
        BaselineY = baselineY;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        AdvanceWidth = advanceWidth;
    }
}
