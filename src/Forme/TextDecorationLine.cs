// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Describes a suggested horizontal text-decoration segment.
/// </summary>
public readonly struct TextDecorationLine
{
    /// <summary>
    /// Gets the decoration represented by this segment.
    /// </summary>
    public TextDecorations Decoration { get; }

    /// <summary>
    /// Gets the color to use for this segment.
    /// </summary>
    public TextColor Color { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 start index covered by this segment.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length covered by this segment.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index covered by this segment.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the line index owning this segment within <see cref="TextLayoutResult.Lines"/>.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the segment start X position in pixels, relative to the layout origin.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the segment end X position in pixels, relative to the layout origin.
    /// </summary>
    public float X2 { get; }

    /// <summary>
    /// Gets the Y position of this horizontal segment in pixels, relative to the layout origin.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the suggested stroke thickness in pixels.
    /// </summary>
    public float Thickness { get; }

    /// <summary>
    /// Gets the segment width in pixels.
    /// </summary>
    public float Width => X2 - X;

    /// <summary>
    /// Initializes a new <see cref="TextDecorationLine"/> with the given values.
    /// </summary>
    /// <param name="decoration">The decoration represented by this segment.</param>
    /// <param name="color">The color to use for this segment.</param>
    /// <param name="textStart">The zero-based UTF-16 start index covered by this segment.</param>
    /// <param name="textLength">The UTF-16 length covered by this segment.</param>
    /// <param name="lineIndex">The line index owning this segment.</param>
    /// <param name="x">The segment start X position in pixels.</param>
    /// <param name="x2">The segment end X position in pixels.</param>
    /// <param name="y">The Y position of this horizontal segment in pixels.</param>
    /// <param name="thickness">The suggested stroke thickness in pixels.</param>
    public TextDecorationLine(TextDecorations decoration, TextColor color, int textStart, int textLength, int lineIndex, float x, float x2, float y, float thickness)
    {
        Decoration = decoration;
        Color = color;
        TextStart = textStart;
        TextLength = textLength;
        LineIndex = lineIndex;
        X = x;
        X2 = x2;
        Y = y;
        Thickness = thickness;
    }
}
