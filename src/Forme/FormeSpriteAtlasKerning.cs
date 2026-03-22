// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Stores the three horizontal spacing components for a glyph baked into a <see cref="FormeSpriteAtlas"/>.
/// </summary>
/// <remarks>
/// The three values map directly to the per-character kerning Vector3 entries in a MonoGame
/// SpriteFont: <see cref="Left"/> is the left side bearing, <see cref="Width"/> is the rendered
/// pixel width of the glyph, and <see cref="Right"/> is the remaining advance past the right
/// edge of the rendered pixels. The sum of all three equals the full advance width.
/// </remarks>
public readonly struct FormeSpriteAtlasKerning
{
    /// <summary>
    /// Gets the left side bearing in pixels.
    /// </summary>
    public float Left { get; }

    /// <summary>
    /// Gets the rendered pixel width of the glyph.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the right-side spacing in pixels.
    /// </summary>
    /// <remarks>
    /// Equal to the full advance width minus <see cref="Left"/> minus <see cref="Width"/>.
    /// </remarks>
    public float Right { get; }

    /// <summary>
    /// Initializes a new <see cref="FormeSpriteAtlasKerning"/>.
    /// </summary>
    /// <param name="left">The left side bearing in pixels.</param>
    /// <param name="width">The rendered pixel width of the glyph.</param>
    /// <param name="right">The right-side spacing in pixels.</param>
    public FormeSpriteAtlasKerning(float left, float width, float right)
    {
        Left = left;
        Width = width;
        Right = right;
    }
}
