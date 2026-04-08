// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes one style-contiguous run of text within a <see cref="TextLayoutResult"/>.
/// </summary>
/// <remarks>
/// Current Forme layout uses a single font and style for the whole request, so a result typically
/// contains one run spanning the full source text. This type exists to make the output model stable
/// before richer multi-section layout is added.
/// </remarks>
public readonly struct TextLayoutRun
{
    /// <summary>
    /// Gets the zero-based UTF-16 start index of this run within the source text.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length of this run within the source text.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the index of the first glyph in this run within <see cref="TextLayoutResult.Glyphs"/>.
    /// </summary>
    public int GlyphStart { get; }

    /// <summary>
    /// Gets the number of glyph placements that belong to this run.
    /// </summary>
    public int GlyphCount { get; }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutRun"/> with the given values.
    /// </summary>
    public TextLayoutRun(int textStart, int textLength, int glyphStart, int glyphCount)
    {
        TextStart = textStart;
        TextLength = textLength;
        GlyphStart = glyphStart;
        GlyphCount = glyphCount;
    }
}
