// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Forme.Internal;

internal struct FormeCurve
{
    /// <summary>
    /// The on-curve start point of the quadratic Bezier, in font design units.
    /// </summary>
    internal Vector2 StartPoint;

    /// <summary>
    /// The off-curve control point of the quadratic Bezier, in font design units.
    /// </summary>
    internal Vector2 ControlPoint;

    /// <summary>
    /// The on-curve end point of the quadratic Bezier, in font design units.
    /// </summary>
    internal Vector2 EndPoint;

    /// <summary>
    /// X-texel index of this curve's first texel in the curve texture.
    /// The second texel is always at TexelIndex + 1 (guaranteed same row).
    /// </summary>
    internal int TexelIndex;

    /// <summary>
    /// True if this is the first curve of a new contour.
    /// </summary>
    internal bool IsFirst;
}
