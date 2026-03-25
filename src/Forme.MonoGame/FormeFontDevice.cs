// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Forme.MonoGame;

/// <summary>
/// Uploads a <see cref="FormeFont"/> to the GPU and owns the resulting texture resources.
/// </summary>
/// <remarks>
/// <para>
/// Create one <see cref="FormeFontDevice"/> per <see cref="FormeFont"/> and reuse it for
/// the lifetime of the font. Dispose when the font is no longer needed.
/// </para>
/// <para>
/// The curve texture uses <see cref="SurfaceFormat.Vector4"/> (RGBA32F) and stores
/// quadratic Bezier control points. The band texture also uses
/// <see cref="SurfaceFormat.Vector4"/> with RG32F data packed into XY and ZW set to zero;
/// this is required because SM3/MojoShader cannot sample RG32F directly.
/// </para>
/// <para>
/// The band LUT texture uses <see cref="SurfaceFormat.Vector4"/> (RGBA32F) and stores
/// per-glyph band transform data (bandScaleX, bandScaleY, bandOffsetX, bandOffsetY) indexed
/// by glyph.
/// </para>
/// </remarks>
public sealed class FormeFontDevice : IDisposable
{
    /// <summary>
    /// Gets the source font data used to create this device font.
    /// </summary>
    public FormeFont Font { get; }

    /// <summary>
    /// Gets the curve texture containing quadratic Bezier control points (RGBA32F).
    /// </summary>
    public Texture2D CurveTexture { get; }

    /// <summary>
    /// Gets the band texture containing the spatial acceleration structure (RGBA32F, RG used).
    /// </summary>
    public Texture2D BandTexture { get; }

    /// <summary>
    /// Gets the band LUT texture containing per-glyph band transform data (RGBA32F).
    /// </summary>
    /// <remarks>
    /// Each texel stores (bandScaleX, bandScaleY, bandOffsetX, bandOffsetY) for one glyph.
    /// The texture is at most 4096 texels wide; tall fonts use multiple rows.
    /// </remarks>
    public Texture2D BandLutTexture { get; }

    /// <summary>
    /// Gets the glyph metadata dictionary, keyed by Unicode code point.
    /// </summary>
    public IReadOnlyDictionary<int, FormeGlyph> Glyphs { get; }

    /// <summary>
    /// Gets the precomputed band LUT UV coordinates, keyed by Unicode code point.
    /// </summary>
    internal IReadOnlyDictionary<int, Vector2> GlyphLutUVs { get; }

    /// <summary>
    /// Gets the font metrics (ascent, descent, units per em).
    /// </summary>
    public FontMetrics Metrics { get; }

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    /// <remarks>
    /// Once <see langword="true"/>, the GPU textures owned by this instance have been released
    /// and the instance must not be used for rendering.
    /// </remarks>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="FormeFontDevice"/> by uploading font data to the GPU.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to create textures on.</param>
    /// <param name="font">The processed font data to upload.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="graphicsDevice"/> or <paramref name="font"/> is null.
    /// </exception>
    public FormeFontDevice(GraphicsDevice graphicsDevice, FormeFont font)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(font);

        Font = font;
        Glyphs = font.Glyphs;
        Metrics = font.Metrics;

        CurveTexture = UploadCurveTexture(graphicsDevice, font);
        BandTexture = UploadBandTexture(graphicsDevice, font);

        BandLutTexture = BuildBandLut(graphicsDevice, font, out Dictionary<int, Vector2> glyphLutUVs);
        GlyphLutUVs = glyphLutUVs;
    }

    private static Texture2D UploadCurveTexture(GraphicsDevice graphicsDevice, FormeFont font)
    {
        Texture2D texture = new Texture2D(
            graphicsDevice,
            font.CurveTexture.Width,
            font.CurveTexture.Height,
            false,
            SurfaceFormat.Vector4);

        int texelCount = font.CurveTexture.Width * font.CurveTexture.Height;
        Vector4[] data = new Vector4[texelCount];
        ReadOnlySpan<float> src = font.CurveTexture.Data.Span;

        for (int i = 0; i < texelCount; i++)
        {
            data[i] = new Vector4(src[i * 4], src[i * 4 + 1], src[i * 4 + 2], src[i * 4 + 3]);
        }

        texture.SetData(data);
        return texture;
    }

    private static Texture2D UploadBandTexture(GraphicsDevice graphicsDevice, FormeFont font)
    {
        // Band data is RG32F (2 floats per texel). SM3/MojoShader cannot sample RG32F
        // directly, so pack into RGBA32F with ZW = 0 and read only XY in the shader.
        Texture2D texture = new Texture2D(
            graphicsDevice,
            font.BandTexture.Width,
            font.BandTexture.Height,
            false,
            SurfaceFormat.Vector4);

        int texelCount = font.BandTexture.Width * font.BandTexture.Height;
        Vector4[] data = new Vector4[texelCount];
        ReadOnlySpan<float> src = font.BandTexture.Data.Span;
        int pairCount = src.Length / 2;

        for (int i = 0; i < pairCount; i++)
        {
            data[i] = new Vector4(src[i * 2], src[i * 2 + 1], 0f, 0f);
        }

        texture.SetData(data);
        return texture;
    }

    private static Texture2D BuildBandLut(
        GraphicsDevice graphicsDevice, FormeFont font, out Dictionary<int, Vector2> uvs)
    {
        int glyphCount = Math.Max(1, font.Glyphs.Count);
        int lutWidth   = Math.Min(glyphCount, 4096);
        int lutHeight  = (glyphCount + 4095) / 4096;

        Vector4[] data = new Vector4[lutWidth * lutHeight];
        uvs = new Dictionary<int, Vector2>(glyphCount);

        int index = 0;
        foreach (KeyValuePair<int, FormeGlyph> kvp in font.Glyphs)
        {
            FormeGlyph g = kvp.Value;
            float scaleX = 1.0f / Math.Max(1f, (float)g.BandInfo.DimX);
            float scaleY = 1.0f / Math.Max(1f, (float)g.BandInfo.DimY);
            data[index] = new Vector4(
                scaleX,
                scaleY,
                -(float)g.BoundingBox.X1 * scaleX,
                -(float)g.BoundingBox.Y1 * scaleY);

            float u = (index % 4096 + 0.5f) / lutWidth;
            float v = (index / 4096 + 0.5f) / lutHeight;
            uvs[kvp.Key] = new Vector2(u, v);
            index++;
        }

        Texture2D texture = new Texture2D(
            graphicsDevice, lutWidth, lutHeight, false, SurfaceFormat.Vector4);
        texture.SetData(data);
        return texture;
    }

    /// <summary>
    /// Releases the GPU textures owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        CurveTexture.Dispose();
        BandTexture.Dispose();
        BandLutTexture.Dispose();
    }
}
