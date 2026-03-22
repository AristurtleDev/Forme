// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Defines the position and size of a glyph's pixels within a <see cref="FormeSpriteAtlas"/> texture.
/// </summary>
public readonly struct FormeSpriteAtlasRegion
{
    /// <summary>
    /// Gets the x-coordinate of the top-left corner of the region, in pixels.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the y-coordinate of the top-left corner of the region, in pixels.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the width of the region, in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the region, in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new <see cref="FormeSpriteAtlasRegion"/>.
    /// </summary>
    /// <param name="x">The x-coordinate of the top-left corner, in pixels.</param>
    /// <param name="y">The y-coordinate of the top-left corner, in pixels.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    public FormeSpriteAtlasRegion(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
