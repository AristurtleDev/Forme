// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// The screen-space bounding rectangle of a piece of laid-out text, expressed as pixel offsets
/// relative to the draw origin.
/// </summary>
/// <remarks>
/// Coordinates follow screen convention: X increases rightward and Y increases downward. The origin
/// (0, 0) corresponds to the baseline of the first line at the draw position passed to the layout
/// or measurement method.
/// </remarks>
public readonly struct FormeTextBounds
{
    /// <summary>
    /// Gets a <see cref="FormeTextBounds"/> with all values set to zero.
    /// </summary>
    public static readonly FormeTextBounds Empty;

    /// <summary>
    /// Gets the left edge of the bounding rectangle in pixels.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the top edge of the bounding rectangle in pixels.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the right edge of the bounding rectangle in pixels.
    /// </summary>
    public float X2 { get; }

    /// <summary>
    /// Gets the bottom edge of the bounding rectangle in pixels.
    /// </summary>
    public float Y2 { get; }

    /// <summary>
    /// Gets the width of the bounding rectangle in pixels.
    /// </summary>
    public float Width => X2 - X;

    /// <summary>
    /// Gets the height of the bounding rectangle in pixels.
    /// </summary>
    public float Height => Y2 - Y;

    /// <summary>
    /// Initializes a new <see cref="FormeTextBounds"/> with the given edges.
    /// </summary>
    /// <param name="x">Left edge in pixels.</param>
    /// <param name="y">Top edge in pixels.</param>
    /// <param name="x2">Right edge in pixels.</param>
    /// <param name="y2">Bottom edge in pixels.</param>
    public FormeTextBounds(float x, float y, float x2, float y2)
    {
        X = x;
        Y = y;
        X2 = x2;
        Y2 = y2;
    }
}
