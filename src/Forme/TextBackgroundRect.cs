// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes a suggested background rectangle for a contiguous decorated text segment.
/// </summary>
public readonly struct TextBackgroundRect
{
    /// <summary>
    /// Gets the background color to use for this rectangle.
    /// </summary>
    public TextColor Color { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 start index covered by this rectangle.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length covered by this rectangle.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index covered by this rectangle.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the line index owning this rectangle within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the suggested rectangle bounds in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds Bounds { get; }

    /// <summary>
    /// Initializes a new <see cref="TextBackgroundRect"/> with the given values.
    /// </summary>
    public TextBackgroundRect(TextColor color, int textStart, int textLength, int lineIndex, FormeTextBounds bounds)
    {
        Color = color;
        TextStart = textStart;
        TextLength = textLength;
        LineIndex = lineIndex;
        Bounds = bounds;
    }
}
