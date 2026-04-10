// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes one style-contiguous run of text within a <see cref="TextLayoutResult"/>.
/// </summary>
/// <remarks>
/// Runs are contiguous in both section format and resolved font. Rich-text layout may therefore
/// split a single <see cref="TextSection"/> into multiple runs when fallback font selection changes
/// within the section.
/// </remarks>
public readonly struct TextLayoutRun
{
    /// <summary>
    /// Gets the full format used for this run.
    /// </summary>
    public TextFormat Format { get; }

    /// <summary>
    /// Gets the font used for this run.
    /// </summary>
    public FormeFont Font { get; }

    /// <summary>
    /// Gets the em-square height used for this run in pixels.
    /// </summary>
    public float SizePixels => Format.SizePixels;

    /// <summary>
    /// Gets the text decorations requested for this run.
    /// </summary>
    public TextDecorations Decorations => Format.Decorations;

    /// <summary>
    /// Gets the zero-based UTF-16 start index of this run within the source text.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length of this run within the source text.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index of this run within the source text.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the index of the first glyph in this run within <see cref="TextLayoutResult.Glyphs"/>.
    /// </summary>
    public int GlyphStart { get; }

    /// <summary>
    /// Gets the number of glyph placements that belong to this run.
    /// </summary>
    public int GlyphCount { get; }

    /// <summary>
    /// Gets the exclusive end glyph index for this run within <see cref="TextLayoutResult.Glyphs"/>.
    /// </summary>
    public int GlyphEnd => GlyphStart + GlyphCount;

    /// <summary>
    /// Gets the logical bounds of this run in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds LogicalBounds { get; }

    /// <summary>
    /// Gets the visual bounds of this run in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds VisualBounds { get; }

    /// <summary>
    /// Gets the index of the first line touched by this run within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineStart { get; }

    /// <summary>
    /// Gets the number of lines touched by this run.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Gets the exclusive end line index for this run within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineEnd => LineStart + LineCount;

    /// <summary>
    /// Returns whether the given UTF-16 text index falls within this run's source-text range.
    /// </summary>
    public bool ContainsTextIndex(int textIndex)
    {
        return TextLength > 0 && textIndex >= TextStart && textIndex < TextEnd;
    }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutRun"/> with the given values.
    /// </summary>
    /// <param name="format">The full format used for the run.</param>
    /// <param name="font">The resolved font used for glyph layout within the run.</param>
    /// <param name="textStart">The zero-based UTF-16 start index of the run.</param>
    /// <param name="textLength">The UTF-16 length of the run.</param>
    /// <param name="glyphStart">The index of the first glyph in the run.</param>
    /// <param name="glyphCount">The number of glyphs in the run.</param>
    /// <param name="logicalBounds">The logical bounds of the run.</param>
    /// <param name="visualBounds">The visual bounds of the run.</param>
    /// <param name="lineStart">The index of the first line touched by the run.</param>
    /// <param name="lineCount">The number of lines touched by the run.</param>
    public TextLayoutRun(
        TextFormat format,
        FormeFont font,
        int textStart,
        int textLength,
        int glyphStart,
        int glyphCount,
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        int lineStart,
        int lineCount)
    {
        if (!format.IsValid)
        {
            throw new System.ArgumentException("Text layout runs require a valid TextFormat.", nameof(format));
        }

        System.ArgumentNullException.ThrowIfNull(font);

        Format = format;
        Font = font;
        TextStart = textStart;
        TextLength = textLength;
        GlyphStart = glyphStart;
        GlyphCount = glyphCount;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        LineStart = lineStart;
        LineCount = lineCount;
    }
}
