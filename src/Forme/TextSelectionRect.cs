// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes a suggested selection rectangle for a contiguous segment of laid-out text.
/// </summary>
public readonly struct TextSelectionRect
{
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
    /// Gets the suggested selection bounds in pixels, relative to the layout origin.
    /// </summary>
    public FormeTextBounds Bounds { get; }

    /// <summary>
    /// Initializes a new <see cref="TextSelectionRect"/> with the given values.
    /// </summary>
    /// <param name="textStart">The zero-based UTF-16 start index covered by this rectangle.</param>
    /// <param name="textLength">The UTF-16 length covered by this rectangle.</param>
    /// <param name="lineIndex">The line index owning this rectangle.</param>
    /// <param name="bounds">The suggested selection bounds.</param>
    public TextSelectionRect(int textStart, int textLength, int lineIndex, FormeTextBounds bounds)
    {
        TextStart = textStart;
        TextLength = textLength;
        LineIndex = lineIndex;
        Bounds = bounds;
    }
}
