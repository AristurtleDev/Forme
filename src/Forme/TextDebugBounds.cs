// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes debug bounds for a laid-out text row or glyph.
/// </summary>
public readonly struct TextDebugBounds
{
    /// <summary>
    /// Gets whether this entry represents a row or a glyph.
    /// </summary>
    public TextDebugBoundsKind Kind { get; }

    /// <summary>
    /// Gets the zero-based row or glyph index within the owning layout result.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 start index covered by this entry.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length covered by this entry.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index covered by this entry.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the logical bounds of this entry in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds LogicalBounds { get; }

    /// <summary>
    /// Gets the visual bounds of this entry in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds VisualBounds { get; }

    /// <summary>
    /// Initializes a new <see cref="TextDebugBounds"/> with the given values.
    /// </summary>
    /// <param name="kind">Whether this entry represents a row or a glyph.</param>
    /// <param name="index">The zero-based row or glyph index.</param>
    /// <param name="textStart">The zero-based UTF-16 start index covered by this entry.</param>
    /// <param name="textLength">The UTF-16 length covered by this entry.</param>
    /// <param name="logicalBounds">The logical bounds of this entry.</param>
    /// <param name="visualBounds">The visual bounds of this entry.</param>
    public TextDebugBounds(TextDebugBoundsKind kind, int index, int textStart, int textLength, FormeTextBounds logicalBounds, FormeTextBounds visualBounds)
    {
        Kind = kind;
        Index = index;
        TextStart = textStart;
        TextLength = textLength;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
    }
}
