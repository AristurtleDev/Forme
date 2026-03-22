// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Forme;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Forme.MonoGame;

/// <summary>
/// Creates MonoGame <see cref="SpriteFont"/> instances from TrueType font data using
/// CPU-side rasterization and font metrics sourced directly from the font file.
/// </summary>
/// <remarks>
/// <para>
/// The resulting <see cref="SpriteFont"/> is a fixed-size bitmap font and behaves identically
/// to any other MonoGame <see cref="SpriteFont"/>: it works with
/// <c>SpriteBatch.DrawString(SpriteFont, ...)</c> and looks correct at the baked size. Using
/// it at other sizes will produce blurry or blocky output, as with any bitmap font.
/// </para>
/// <para>
/// Kerning and glyph metrics are read directly from the font file rather than from a
/// platform font engine, which avoids the rounding discrepancies that can affect
/// MonoGame's own content pipeline SpriteFont generation on some platforms.
/// </para>
/// </remarks>
public static class FormeSpriteFont
{
    /// <summary>
    /// Creates a <see cref="SpriteFont"/> from a TrueType font at a fixed pixel size.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create the atlas texture.</param>
    /// <param name="ttfData">The raw bytes of a TrueType or OpenType font file.</param>
    /// <param name="sizePixels">
    /// The target em-square height in pixels. Glyph dimensions scale proportionally so that
    /// one em equals this many pixels.
    /// </param>
    /// <param name="charset">The set of Unicode code points to include in the font.</param>
    /// <returns>
    /// A <see cref="SpriteFont"/> ready for use with <c>SpriteBatch.DrawString</c>.
    /// The font owns its atlas <see cref="Texture2D"/>; dispose it when no longer needed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="graphicsDevice"/>, <paramref name="ttfData"/>, or
    /// <paramref name="charset"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizePixels"/> is zero or negative.
    /// </exception>
    public static SpriteFont Create(GraphicsDevice graphicsDevice, byte[] ttfData, int sizePixels, CharacterSet charset)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(ttfData);
        ArgumentNullException.ThrowIfNull(charset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizePixels);

        FormeSpriteAtlas atlas = FormeSpriteAtlas.Bake(ttfData, sizePixels, charset);

        // Convert grayscale alpha to premultiplied RGBA. SpriteBatch uses BlendState.AlphaBlend
        // which expects premultiplied source pixels (src + dst*(1-src.a)). Setting R=G=B=A=alpha
        // produces a premultiplied white glyph that SpriteBatch then tints with the draw color.
        byte[] rgba = new byte[atlas.Width * atlas.Height * 4];

        for (int i = 0; i < atlas.Pixels.Length; i++)
        {
            byte a = atlas.Pixels[i];
            rgba[i * 4 + 0] = a;
            rgba[i * 4 + 1] = a;
            rgba[i * 4 + 2] = a;
            rgba[i * 4 + 3] = a;
        }

        Texture2D texture = new Texture2D(graphicsDevice, atlas.Width, atlas.Height, false, SurfaceFormat.Color);
        texture.SetData(rgba);

        List<Rectangle> glyphBounds = new List<Rectangle>(atlas.Glyphs.Count);
        List<Rectangle> cropping = new List<Rectangle>(atlas.Glyphs.Count);
        List<char> characters = new List<char>(atlas.Glyphs.Count);
        List<Vector3> kerning = new List<Vector3>(atlas.Glyphs.Count);

        foreach (FormeSpriteAtlasGlyph glyph in atlas.Glyphs)
        {
            // SpriteFont stores characters as char (BMP only); skip supplementary plane codepoints.
            if (glyph.CodePoint > char.MaxValue)
            {
                continue;
            }

            characters.Add((char)glyph.CodePoint);

            glyphBounds.Add(new Rectangle(
                glyph.AtlasRegion.X,
                glyph.AtlasRegion.Y,
                glyph.AtlasRegion.Width,
                glyph.AtlasRegion.Height));

            // croppingY positions the glyph top relative to the top of its text line.
            // BitmapOriginY is the signed pixel offset from the baseline (negative = above baseline),
            // and AscentPixels is the distance from line top to baseline.
            int croppingY = atlas.AscentPixels + glyph.BitmapOriginY;
            cropping.Add(new Rectangle(0, croppingY, glyph.AtlasRegion.Width, atlas.LineSpacing));

            kerning.Add(new Vector3(glyph.Kerning.Left, glyph.Kerning.Width, glyph.Kerning.Right));
        }

        // Use '?' as the fallback for unmapped characters; fall back to the first available char.
        char? defaultChar = null;

        if (characters.Contains('?'))
        {
            defaultChar = '?';
        }
        else if (characters.Count > 0)
        {
            defaultChar = characters[0];
        }

        return new SpriteFont(texture, glyphBounds, cropping, characters, atlas.LineSpacing, 0f, kerning, defaultChar);
    }
}
