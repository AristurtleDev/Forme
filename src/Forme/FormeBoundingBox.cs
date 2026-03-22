// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// An axis-aligned bounding box expressed as two corners in font design units.
/// </summary>
/// <remarks>
/// Coordinates use TrueType convention where Y increases upward. <see cref="Y1"/> is the
/// bottom (lower) edge and <see cref="Y2"/> is the top (upper) edge.
/// </remarks>
public readonly struct FormeBoundingBox
{
    /// <summary>
    /// Gets the left edge of the bounding box, in font units.
    /// </summary>
    public int X1 { get; }

    /// <summary>
    /// Gets the bottom edge of the bounding box, in font units.
    /// </summary>
    public int Y1 { get; }

    /// <summary>
    /// Gets the right edge of the bounding box, in font units.
    /// </summary>
    public int X2 { get; }

    /// <summary>
    /// Gets the top edge of the bounding box, in font units.
    /// </summary>
    public int Y2 { get; }

    /// <summary>
    /// Gets the width of the bounding box in font units.
    /// </summary>
    public int Width => X2 - X1;

    /// <summary>
    /// Gets the height of the bounding box in font units.
    /// </summary>
    public int Height => Y2 - Y1;

    /// <summary>
    /// Initializes a new <see cref="FormeBoundingBox"/> with the given corner coordinates.
    /// </summary>
    /// <param name="x1">The left edge, in font design units.</param>
    /// <param name="y1">The bottom edge, in font design units. Y increases upward.</param>
    /// <param name="x2">The right edge, in font design units.</param>
    /// <param name="y2">The top edge, in font design units. Y increases upward.</param>
    public FormeBoundingBox(int x1, int y1, int x2, int y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}
