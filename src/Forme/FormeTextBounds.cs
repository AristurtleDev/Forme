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
    /// Returns whether the given point lies within this rectangle.
    /// </summary>
    /// <param name="x">The X position to test.</param>
    /// <param name="y">The Y position to test.</param>
    /// <returns>
    /// <see langword="true"/> when the point is inside the rectangle's half-open bounds;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(float x, float y)
    {
        return x >= X && x < X2 && y >= Y && y < Y2;
    }

    /// <summary>
    /// Returns the squared distance from the given point to this rectangle.
    /// </summary>
    /// <param name="x">The X position to test.</param>
    /// <param name="y">The Y position to test.</param>
    /// <returns>
    /// Zero when the point lies within the rectangle; otherwise, the squared distance to the
    /// rectangle's nearest edge or corner.
    /// </returns>
    public float DistanceSquaredTo(float x, float y)
    {
        float dx = 0f;
        if (x < X)
        {
            dx = X - x;
        }
        else if (x >= X2)
        {
            dx = x - X2;
        }

        float dy = 0f;
        if (y < Y)
        {
            dy = Y - y;
        }
        else if (y >= Y2)
        {
            dy = y - Y2;
        }

        return dx * dx + dy * dy;
    }

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
