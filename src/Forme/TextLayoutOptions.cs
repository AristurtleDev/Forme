// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Configuration for text layout operations such as measurement, glyph enumeration, and wrapping.
/// </summary>
/// <remarks>
/// All properties default to values that produce single-line, left-aligned text with no additional
/// spacing and no width constraint, matching the behavior of a plain <c>DrawString</c> call.
/// </remarks>
public readonly struct TextLayoutOptions
{
    /// <summary>
    /// Gets the maximum line width in pixels. When <see langword="null"/>, lines are not constrained.
    /// </summary>
    /// <remarks>
    /// When set with <see cref="EllipsisMode"/> equal to <see cref="Forme.EllipsisMode.None"/>,
    /// text wraps to multiple lines at word boundaries. When set with any other
    /// <see cref="EllipsisMode"/>, text is truncated to a single line with the ellipsis string appended.
    /// </remarks>
    public float? MaxWidth { get; init; }

    /// <summary>
    /// Gets the maximum number of laid-out rows. When <see langword="null"/>, all rows are kept.
    /// </summary>
    /// <remarks>
    /// When the limit is reached, later rows are discarded and the overflow character is appended
    /// to the final retained row. A value of zero produces no rows and marks the result as elided.
    /// </remarks>
    public int? MaxRows { get; init; }

    /// <summary>
    /// Gets the character appended to the final row when <see cref="MaxRows"/> truncates text.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, U+2026 is used. Set this to <see cref="string.Empty"/> to
    /// suppress the marker while still truncating rows. Non-empty values must contain one Unicode
    /// scalar value.
    /// </remarks>
    public string? OverflowCharacter { get; init; }

    /// <summary>
    /// Gets whether wrapping may break between any two characters instead of preferring word boundaries.
    /// </summary>
    public bool BreakAnywhere { get; init; }

    /// <summary>
    /// Gets whether newline characters start new rows. When <see langword="null"/>, newlines break rows.
    /// </summary>
    /// <remarks>
    /// When this is <see langword="false"/>, newline characters remain on the current row and are
    /// laid out as U+FFFD replacement characters.
    /// </remarks>
    public bool? BreakOnNewline { get; init; }

    /// <summary>
    /// Gets the horizontal alignment of each line relative to the draw origin.
    /// </summary>
    public TextHorizontalAlignment Alignment { get; init; }

    /// <summary>
    /// Gets additional spacing in pixels added between each character, beyond the font's natural advance width.
    /// </summary>
    public float CharacterSpacing { get; init; }

    /// <summary>
    /// Gets additional spacing in pixels added between lines, beyond the font's natural line height.
    /// </summary>
    public float LineSpacing { get; init; }

    /// <summary>
    /// Gets the ellipsis truncation mode applied when text exceeds <see cref="MaxWidth"/>.
    /// </summary>
    public EllipsisMode EllipsisMode { get; init; }

    /// <summary>
    /// Gets the string appended to truncated text. When <see langword="null"/>, <c>"..."</c> is used.
    /// </summary>
    public string? EllipsisString { get; init; }

    /// <summary>
    /// Gets the pixel-snapping policy applied to the geometry in the resulting layout output.
    /// </summary>
    public TextGeometrySnap GeometrySnap { get; init; }

    /// <summary>
    /// Gets how missing glyphs should be handled during layout.
    /// </summary>
    public TextMissingGlyphPolicy MissingGlyphPolicy { get; init; }
}
