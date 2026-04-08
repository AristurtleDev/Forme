// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes a caret position within a <see cref="TextLayoutResult"/>.
/// </summary>
public readonly struct TextCaret
{
    /// <summary>
    /// Gets the zero-based UTF-16 text index the caret was resolved from.
    /// </summary>
    public int TextIndex { get; }

    /// <summary>
    /// Gets the line index owning this caret within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the caret X position, relative to the layout origin.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the caret baseline Y position, relative to the layout origin.
    /// </summary>
    public float BaselineY { get; }

    /// <summary>
    /// Gets the caret top edge, relative to the layout origin.
    /// </summary>
    public float Top { get; }

    /// <summary>
    /// Gets the caret bottom edge, relative to the layout origin.
    /// </summary>
    public float Bottom { get; }

    /// <summary>
    /// Gets the caret height in pixels.
    /// </summary>
    public float Height => Bottom - Top;

    /// <summary>
    /// Gets the zero-width logical bounds of the caret.
    /// </summary>
    public FormeTextBounds LogicalBounds => new FormeTextBounds(X, Top, X, Bottom);

    /// <summary>
    /// Initializes a new <see cref="TextCaret"/> with the given values.
    /// </summary>
    public TextCaret(int textIndex, int lineIndex, float x, float baselineY, float top, float bottom)
    {
        TextIndex = textIndex;
        LineIndex = lineIndex;
        X = x;
        BaselineY = baselineY;
        Top = top;
        Bottom = bottom;
    }
}
