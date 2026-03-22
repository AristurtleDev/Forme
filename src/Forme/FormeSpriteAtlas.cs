// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using StbTrueTypeSharp;

namespace Forme;

/// <summary>
/// A CPU-side texture atlas containing rasterized glyph bitmaps at a fixed pixel size, along
/// with the per-glyph metrics needed to render them correctly.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="Bake"/>. The atlas pixel data is a grayscale alpha mask stored in
/// <see cref="Pixels"/> as one byte per pixel, row-major. Each byte is an alpha coverage value
/// in [0, 255]. To upload to a GPU texture, convert to RGBA by setting R, G, B to 255 and
/// using this value as the alpha channel.
/// </para>
/// <para>
/// Glyph metrics are sourced directly from the font's own tables via StbTrueType, making them
/// consistent with the rasterized output and free from the platform-specific rounding issues
/// that affect GDI+ or FreeType-based pipelines.
/// </para>
/// <para>
/// To use with MonoGame, pass this atlas to <c>FormeSpriteFont.Create</c> in
/// <c>Forme.MonoGame</c>, which uploads the pixels to a <c>Texture2D</c> and constructs
/// a <c>SpriteFont</c>.
/// </para>
/// </remarks>
public sealed class FormeSpriteAtlas
{
    private const int PADDING = 1;

    /// <summary>
    /// Gets the width of the atlas texture in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the atlas texture in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the raw grayscale alpha pixel data for the atlas.
    /// </summary>
    /// <remarks>
    /// One byte per pixel, row-major order. Convert to RGBA for GPU upload by setting
    /// R, G, B to 255 and using each byte as the alpha channel.
    /// </remarks>
    public byte[] Pixels { get; }

    /// <summary>
    /// Gets the recommended line spacing in pixels.
    /// </summary>
    /// <remarks>
    /// Includes ascent, absolute descent, and the font's line gap. Use this as the vertical
    /// advance between lines of text.
    /// </remarks>
    public int LineSpacing { get; }

    /// <summary>
    /// Gets the distance in pixels from the top of a text line to the baseline.
    /// </summary>
    /// <remarks>
    /// Add <see cref="FormeSpriteAtlasGlyph.BitmapOriginY"/> to this value to get the
    /// screen-space y-coordinate of the top of a glyph's rendered bitmap relative to
    /// the top of its text line.
    /// </remarks>
    public int AscentPixels { get; }

    /// <summary>
    /// Gets the per-glyph atlas data, sorted by code point in ascending order.
    /// </summary>
    public IReadOnlyList<FormeSpriteAtlasGlyph> Glyphs { get; }

    private FormeSpriteAtlas(int width, int height, byte[] pixels, int lineSpacing, int ascentPixels, IReadOnlyList<FormeSpriteAtlasGlyph> glyphs)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
        LineSpacing = lineSpacing;
        AscentPixels = ascentPixels;
        Glyphs = glyphs;
    }

    /// <summary>
    /// Rasterizes the glyphs in <paramref name="charset"/> from the given TrueType font at
    /// the specified pixel size and packs them into a texture atlas.
    /// </summary>
    /// <param name="ttfData">The raw bytes of a TrueType or OpenType font file.</param>
    /// <param name="sizePixels">
    /// The target em-square height in pixels. Glyph dimensions scale proportionally so that
    /// one em equals this many pixels.
    /// </param>
    /// <param name="charset">The set of Unicode code points to rasterize.</param>
    /// <returns>
    /// A <see cref="FormeSpriteAtlas"/> containing the rasterized glyph bitmaps and their
    /// layout metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ttfData"/> or <paramref name="charset"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizePixels"/> is zero or negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the font data cannot be parsed.
    /// </exception>
    public static unsafe FormeSpriteAtlas Bake(byte[] ttfData, int sizePixels, CharacterSet charset)
    {
        ArgumentNullException.ThrowIfNull(ttfData);
        ArgumentNullException.ThrowIfNull(charset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizePixels);

        ReadOnlySpan<int> codepoints = charset.Codepoints;
        int count = codepoints.Length;

        int[] glyphIndices = new int[count];
        int[] bitmapX0 = new int[count];
        int[] bitmapY0 = new int[count];
        int[] bitmapX1 = new int[count];
        int[] bitmapY1 = new int[count];
        int[] advanceWidths = new int[count];
        int[] atlasX = new int[count];
        int[] atlasY = new int[count];

        int ascentPixels = 0;
        int lineSpacing = 0;
        float scale = 0f;
        int atlasWidth = 0;
        int atlasHeight = 0;
        byte[] pixels = [];

        fixed (byte* ttfPtr = ttfData)
        {
            StbTrueType.stbtt_fontinfo fontInfo = new StbTrueType.stbtt_fontinfo();

            if (StbTrueType.stbtt_InitFont(fontInfo, ttfPtr, 0) == 0)
            {
                throw new InvalidOperationException(
                    "Failed to initialize font. The TTF data may be corrupt or unsupported.");
            }

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

            scale = StbTrueType.stbtt_ScaleForMappingEmToPixels(fontInfo, sizePixels);
            ascentPixels = (int)Math.Ceiling(ascent * scale);
            int descentPixels = (int)Math.Ceiling(-descent * scale);
            int lineGapPixels = (int)Math.Ceiling(lineGap * scale);
            lineSpacing = ascentPixels + descentPixels + lineGapPixels;

            // First pass: collect glyph indices, pixel bounding boxes, and advance widths.
            for (int i = 0; i < count; i++)
            {
                int cp = codepoints[i];
                int glyphIdx = StbTrueType.stbtt_FindGlyphIndex(fontInfo, cp);
                glyphIndices[i] = glyphIdx;

                if (glyphIdx == 0)
                {
                    continue;
                }

                int ix0, iy0, ix1, iy1;
                StbTrueType.stbtt_GetGlyphBitmapBox(fontInfo, glyphIdx, scale, scale, &ix0, &iy0, &ix1, &iy1);
                bitmapX0[i] = ix0;
                bitmapY0[i] = iy0;
                bitmapX1[i] = ix1;
                bitmapY1[i] = iy1;

                int adv, lsb;
                StbTrueType.stbtt_GetGlyphHMetrics(fontInfo, glyphIdx, &adv, &lsb);
                advanceWidths[i] = adv;
            }

            // Estimate atlas width from the total glyph area with some overhead for padding.
            int totalArea = 0;
            for (int i = 0; i < count; i++)
            {
                int gw = Math.Max(0, bitmapX1[i] - bitmapX0[i]);
                int gh = Math.Max(0, bitmapY1[i] - bitmapY0[i]);

                if (gw > 0 && gh > 0)
                {
                    totalArea += (gw + PADDING) * (gh + PADDING);
                }
            }

            int estimated = (int)Math.Ceiling(Math.Sqrt(totalArea * 1.5));
            atlasWidth = NextPow2(Math.Max(256, Math.Min(4096, estimated)));

            // Pack glyphs into rows left-to-right, top-to-bottom.
            int curX = PADDING;
            int curY = PADDING;
            int rowHeight = 0;

            for (int i = 0; i < count; i++)
            {
                int gw = Math.Max(0, bitmapX1[i] - bitmapX0[i]);
                int gh = Math.Max(0, bitmapY1[i] - bitmapY0[i]);

                if (gw == 0 || gh == 0)
                {
                    continue;
                }

                if (curX + gw + PADDING > atlasWidth)
                {
                    curX = PADDING;
                    curY += rowHeight + PADDING;
                    rowHeight = 0;
                }

                atlasX[i] = curX;
                atlasY[i] = curY;
                curX += gw + PADDING;
                rowHeight = Math.Max(rowHeight, gh);
            }

            atlasHeight = Math.Max(1, curY + rowHeight + PADDING);
            pixels = new byte[atlasWidth * atlasHeight];

            // Second pass: rasterize each glyph into its assigned atlas region.
            fixed (byte* pixelsPtr = pixels)
            {
                for (int i = 0; i < count; i++)
                {
                    if (glyphIndices[i] == 0)
                    {
                        continue;
                    }

                    int gw = Math.Max(0, bitmapX1[i] - bitmapX0[i]);
                    int gh = Math.Max(0, bitmapY1[i] - bitmapY0[i]);

                    if (gw == 0 || gh == 0)
                    {
                        continue;
                    }

                    byte* dest = pixelsPtr + atlasY[i] * atlasWidth + atlasX[i];
                    StbTrueType.stbtt_MakeGlyphBitmap(
                        fontInfo, dest, gw, gh, atlasWidth, scale, scale, glyphIndices[i]);
                }
            }
        }

        // Build the glyph list from the computed per-glyph data.
        List<FormeSpriteAtlasGlyph> glyphs = new List<FormeSpriteAtlasGlyph>(count);

        for (int i = 0; i < count; i++)
        {
            // Skip codepoints not present in the font.
            if (glyphIndices[i] == 0)
            {
                continue;
            }

            int gw = Math.Max(0, bitmapX1[i] - bitmapX0[i]);
            int gh = Math.Max(0, bitmapY1[i] - bitmapY0[i]);

            FormeSpriteAtlasRegion region = new FormeSpriteAtlasRegion(atlasX[i], atlasY[i], gw, gh);

            float advPixels = advanceWidths[i] * scale;

            float kerLeft;
            float kerWidth;
            float kerRight;

            if (gw > 0)
            {
                // Visible glyph: left bearing is the pixel offset of the bitmap left edge from
                // the glyph origin. Right is whatever advance remains after left + bitmap width.
                kerLeft = bitmapX0[i];
                kerWidth = gw;
                kerRight = advPixels - bitmapX1[i];
            }
            else
            {
                // No visible pixels (e.g. space): entire advance goes to the right component
                // so SpriteBatch advances the cursor correctly without drawing anything.
                kerLeft = 0f;
                kerWidth = 0f;
                kerRight = advPixels;
            }

            FormeSpriteAtlasKerning kerning = new FormeSpriteAtlasKerning(kerLeft, kerWidth, kerRight);
            glyphs.Add(new FormeSpriteAtlasGlyph(codepoints[i], region, bitmapX0[i], bitmapY0[i], kerning));
        }

        // Charset codepoints are already sorted, but sort here as a defensive guarantee for
        // consumers that require ascending code point order (e.g. SpriteFont binary search).
        glyphs.Sort((a, b) => a.CodePoint.CompareTo(b.CodePoint));

        return new FormeSpriteAtlas(atlasWidth, atlasHeight, pixels, lineSpacing, ascentPixels, glyphs);
    }

    private static int NextPow2(int value)
    {
        if (value <= 1)
        {
            return 1;
        }

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }
}
