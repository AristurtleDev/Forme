// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Forme.MonoGame;

/// <summary>
/// Queues and renders antialiased text using the Slug algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Wrap each group of text draw calls with <see cref="Begin"/> and <see cref="End"/>.
/// <see cref="Begin"/> saves <see cref="GraphicsDevice"/> state and <see cref="End"/>
/// flushes queued draws and restores that state. This allows safe interleaving with
/// <c>SpriteBatch</c> or other renderers.
/// </para>
/// <para>
/// Glyphs are batched by font in queue order. A batch is flushed automatically when the
/// font changes or the batch reaches <c>2048</c> glyphs.
/// </para>
/// </remarks>
public sealed class FormeRenderer : IDisposable
{
    private const int MaxGlyphsPerBatch = 2048;

    // 1/sqrt(2) for normalized diagonal corner normals
    private const float InvSqrt2 = 0.70710678f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly bool _ownsEffect;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly FormeVertex[] _vertices;
    private readonly int[] _indices;
    private readonly List<QueuedDraw> _queue;

    private int _glyphCount;

    private BlendState? _savedBlendState;
    private DepthStencilState? _savedDepthStencilState;
    private RasterizerState? _savedRasterizerState;
    private SamplerState? _savedSamplerState0;
    private SamplerState? _savedSamplerState1;

    private Matrix _transformMatrix;
    private bool _hasCustomTransform;

    private bool _inBeginEnd;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    /// <remarks>
    /// Once <see langword="true"/>, the GPU resources owned by this instance have been released
    /// and the instance must not be used for rendering.
    /// </remarks>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="FormeRenderer"/> and loads the embedded Slug shader.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="graphicsDevice"/> is null.
    /// </exception>
    public FormeRenderer(GraphicsDevice graphicsDevice) : this(graphicsDevice, null) { }

    /// <summary>
    /// Initializes a new <see cref="FormeRenderer"/> using a caller-supplied effect.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="effect">
    /// A pre-loaded <see cref="Effect"/> compiled from <c>FormeShader.fx</c>.
    /// When <see langword="null"/>, the embedded shader bytecode is used instead.
    /// The renderer does not take ownership of this effect; the caller is responsible for disposing it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="graphicsDevice"/> is null.
    /// </exception>
    public FormeRenderer(GraphicsDevice graphicsDevice, Effect? effect)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _graphicsDevice = graphicsDevice;

        if (effect != null)
        {
            _effect = effect;
            _ownsEffect = false;
        }
        else
        {
            byte[] bytecode = FormeEffectResource.GetBytecode();
            _effect = new Effect(graphicsDevice, bytecode);
            _ownsEffect = true;
        }

        int maxVertices = MaxGlyphsPerBatch * 4;
        int maxIndices = MaxGlyphsPerBatch * 6;

        _vertices = new FormeVertex[maxVertices];
        _indices = new int[maxIndices];

        _vertexBuffer = new VertexBuffer(graphicsDevice, FormeVertex.Declaration, maxVertices, BufferUsage.WriteOnly);

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, maxIndices, BufferUsage.WriteOnly);

        _queue = new List<QueuedDraw>(256);
    }

    /// <inheritdoc/>
    ~FormeRenderer() => Dispose(false);

    /// <summary>
    /// Saves <see cref="GraphicsDevice"/> render state and begins a draw session.
    /// Must be called before <c>DrawString</c> or <see cref="DrawGlyph"/>.
    /// </summary>
    /// <param name="transformMatrix">
    /// An optional matrix applied to all glyph geometry before rendering. Glyph positions
    /// are expressed in screen pixels (origin top-left, Y down), so this matrix should map
    /// from that coordinate space to clip space. When <see langword="null"/>, a standard
    /// orthographic projection covering the current viewport is used.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Begin"/> is called without a preceding <see cref="End"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this instance has been disposed.
    /// </exception>
    public void Begin(Matrix? transformMatrix = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_inBeginEnd)
        {
            throw new InvalidOperationException("End() must be called before calling Begin() again.");
        }

        _savedBlendState = _graphicsDevice.BlendState;
        _savedDepthStencilState = _graphicsDevice.DepthStencilState;
        _savedRasterizerState = _graphicsDevice.RasterizerState;
        _savedSamplerState0 = _graphicsDevice.SamplerStates[0];
        _savedSamplerState1 = _graphicsDevice.SamplerStates[1];

        if (transformMatrix.HasValue)
        {
            _transformMatrix = transformMatrix.Value;
            _hasCustomTransform = true;
        }
        else
        {
            _hasCustomTransform = false;
        }

        _inBeginEnd = true;
    }

    /// <summary>
    /// Queues a string of text for rendering at the specified baseline origin.
    /// </summary>
    /// <param name="font">The GPU font containing the glyphs to draw.</param>
    /// <param name="text">The text to render. Characters without a matching glyph are skipped.</param>
    /// <param name="position">
    /// The baseline origin in screen pixels. X is the left edge of the first character;
    /// Y is the baseline (font Y-up is applied internally).
    /// </param>
    /// <param name="sizePixels">The desired em-square height in pixels.</param>
    /// <param name="color">The text color.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font"/> or <paramref name="text"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside a <see cref="Begin"/>/<see cref="End"/> pair.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this instance has been disposed.
    /// </exception>
    public void DrawString(FormeFontDevice font, string text, Vector2 position, float sizePixels, Color color)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        if (!_inBeginEnd)
        {
            throw new InvalidOperationException(
                "Begin() must be called before DrawString().");
        }

        float scale = sizePixels / Math.Max(1, font.Metrics.UnitsPerEm);
        float cursorX = position.X;

        int i = 0;
        while (i < text.Length)
        {
            Rune.DecodeFromUtf16(text.AsSpan(i), out Rune rune, out int charsConsumed);
            i += charsConsumed;

            if (font.Glyphs.TryGetValue(rune.Value, out FormeGlyph glyph))
            {
                _queue.Add(new QueuedDraw(font, glyph, new Vector2(cursorX, position.Y), sizePixels, color));
                cursorX += glyph.AdvanceWidth * scale;
            }
        }
    }

    /// <summary>
    /// Queues a string of text for rendering at the specified baseline origin, applying the
    /// provided layout options for wrapping, alignment, spacing, and ellipsis.
    /// </summary>
    /// <param name="font">The GPU font containing the glyphs to draw.</param>
    /// <param name="text">The text to render. Characters without a matching glyph are skipped.</param>
    /// <param name="position">
    /// The baseline origin in screen pixels. X is the left edge of the first character for
    /// left-aligned text; Y is the baseline of the first line.
    /// </param>
    /// <param name="sizePixels">The desired em-square height in pixels.</param>
    /// <param name="color">The text color.</param>
    /// <param name="options">Layout options controlling wrapping, alignment, spacing, and ellipsis.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font"/> or <paramref name="text"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside a <see cref="Begin"/>/<see cref="End"/> pair.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this instance has been disposed.
    /// </exception>
    public void DrawString(FormeFontDevice font, string text, Vector2 position, float sizePixels, Color color, in TextLayoutOptions options)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);

        if (!_inBeginEnd)
        {
            throw new InvalidOperationException("Begin() must be called before DrawString().");
        }

        IReadOnlyList<GlyphPlacement> placements = font.Font.GetGlyphs(text.AsSpan(), sizePixels, in options);

        foreach (GlyphPlacement placement in placements)
        {
            if (!font.Glyphs.TryGetValue(placement.CodePoint, out FormeGlyph glyph) || glyph.BandInfo.Count == 0)
            {
                continue;
            }

            Vector2 glyphPos = new(position.X + placement.BaselineX, position.Y + placement.BaselineY);
            _queue.Add(new QueuedDraw(font, glyph, glyphPos, sizePixels, color));
        }
    }

    /// <summary>
    /// Queues a single glyph for rendering at the specified baseline origin.
    /// </summary>
    /// <param name="font">The GPU font containing the glyph.</param>
    /// <param name="codepoint">The Unicode code point of the glyph to draw.</param>
    /// <param name="position">
    /// The baseline origin in screen pixels. X is the glyph origin; Y is the baseline.
    /// </param>
    /// <param name="sizePixels">The desired em-square height in pixels.</param>
    /// <param name="color">The glyph color.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside a <see cref="Begin"/>/<see cref="End"/> pair.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this instance has been disposed.
    /// </exception>
    public void DrawGlyph(FormeFontDevice font, int codepoint, Vector2 position, float sizePixels, Color color)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(font);

        if (!_inBeginEnd)
        {
            throw new InvalidOperationException("Begin() must be called before DrawGlyph().");
        }

        if (font.Glyphs.TryGetValue(codepoint, out FormeGlyph glyph))
        {
            _queue.Add(new QueuedDraw(font, glyph, position, sizePixels, color));
        }
    }

    /// <summary>
    /// Flushes all queued draws to the GPU and restores the saved <see cref="GraphicsDevice"/> state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called without a preceding <see cref="Begin"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when this instance has been disposed.
    /// </exception>
    public void End()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_inBeginEnd)
        {
            throw new InvalidOperationException("Begin() must be called before End().");
        }

        _inBeginEnd = false;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        _graphicsDevice.SamplerStates[1] = SamplerState.PointClamp;

        FormeFontDevice? currentFont = null;
        _glyphCount = 0;

        foreach (QueuedDraw draw in _queue)
        {
            if (draw.Font != currentFont || _glyphCount >= MaxGlyphsPerBatch)
            {
                if (_glyphCount > 0)
                {
                    FlushBatch(currentFont!);
                    _glyphCount = 0;
                }

                currentFont = draw.Font;
            }

            if (AppendGlyphQuad(in draw))
            {
                _glyphCount++;
            }
        }

        if (_glyphCount > 0 && currentFont != null)
        {
            FlushBatch(currentFont);
        }

        _queue.Clear();
        _glyphCount = 0;

        _graphicsDevice.BlendState = _savedBlendState!;
        _graphicsDevice.DepthStencilState = _savedDepthStencilState!;
        _graphicsDevice.RasterizerState = _savedRasterizerState!;
        _graphicsDevice.SamplerStates[0] = _savedSamplerState0!;
        _graphicsDevice.SamplerStates[1] = _savedSamplerState1!;
    }

    private void FlushBatch(FormeFontDevice font)
    {
        if (_glyphCount == 0)
        {
            return;
        }

        Viewport vp = _graphicsDevice.Viewport;

        Matrix matrix;
        if (_hasCustomTransform)
        {
            matrix = _transformMatrix;
        }
        else
        {
            matrix = Matrix.CreateOrthographicOffCenter(
                0f, vp.Width, vp.Height, 0f, -1f, 1f);
        }

        _effect.Parameters["forme_matrix"].SetValue(matrix);
        _effect.Parameters["curveTexture"].SetValue(font.CurveTexture);
        _effect.Parameters["bandTexture"].SetValue(font.BandTexture);

        // curveTexSize and bandTexSize are used by the OpenGL shader for UV-based texel
        // sampling but are not present in the DirectX 11 shader, which uses Load() instead.
        EffectParameter curveTexSizeParam = _effect.Parameters["curveTexSize"];
        if (curveTexSizeParam != null)
        {
            curveTexSizeParam.SetValue(new Vector2(font.CurveTexture.Width, font.CurveTexture.Height));
        }

        EffectParameter bandTexSizeParam = _effect.Parameters["bandTexSize"];
        if (bandTexSizeParam != null)
        {
            bandTexSizeParam.SetValue(new Vector2(font.BandTexture.Width, font.BandTexture.Height));
        }

        int vertexCount = _glyphCount * 4;
        int indexCount = _glyphCount * 6;

        _vertexBuffer.SetData(_vertices, 0, vertexCount);
        _indexBuffer.SetData(_indices, 0, indexCount);

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0, 0, indexCount / 3);
        }
    }

    private bool AppendGlyphQuad(in QueuedDraw draw)
    {
        FormeGlyph g = draw.Glyph;
        float scale = draw.SizePixels / Math.Max(1, draw.Font.Metrics.UnitsPerEm);

        // Screen-space bounding box. Font Y is up; screen Y is down, so Y-axis flips.
        float px0 = draw.Position.X + g.BoundingBox.X1 * scale;
        float py0 = draw.Position.Y - g.BoundingBox.Y2 * scale;
        float px1 = draw.Position.X + g.BoundingBox.X2 * scale;
        float py1 = draw.Position.Y - g.BoundingBox.Y1 * scale;

        if (px1 <= px0 || py1 <= py0)
        {
            return false;
        }

        float ex0 = g.BoundingBox.X1;
        float ey0 = g.BoundingBox.Y1;
        float ex1 = g.BoundingBox.X2;
        float ey1 = g.BoundingBox.Y2;

        float bandScaleX = 1.0f / Math.Max(1f, (float)g.BandInfo.DimX);
        float bandScaleY = 1.0f / Math.Max(1f, (float)g.BandInfo.DimY);
        float bandOffsetX = -(float)g.BoundingBox.X1 * bandScaleX;
        float bandOffsetY = -(float)g.BoundingBox.Y1 * bandScaleY;

        // Pack band texture origin using the texture width as the row stride.
        // The shader unpacks this using the runtime uniform bandTexSize.x, avoiding
        // compile-time constant folding that the MGCB/MojoShader transpiler gets wrong.
        float packedBandTexLoc = (float)g.BandInfo.TexCoordY * 4096f + (float)g.BandInfo.TexCoordX;

        // Store band count directly; shader computes bandMax = bandCount - 1 itself.
        // Storing bandCount avoids a (bandMax+1) expression the transpiler folds incorrectly.
        float packedBandCount = (float)g.BandInfo.Count;

        // Pre-normalize color to [0..1] per channel. Passing normalized values sidesteps
        // MojoShader GLSL precision issues that arise when unpacking large packed floats
        // (up to 65535) via floor/mod in the shader.
        Vector4 color = new Vector4(
            draw.Color.R / 255f,
            draw.Color.G / 255f,
            draw.Color.B / 255f,
            draw.Color.A / 255f);

        Vector4 bnd = new Vector4(bandScaleX, bandScaleY, bandOffsetX, bandOffsetY);

        int baseVertex = _glyphCount * 4;

        _vertices[baseVertex + 0] = new FormeVertex
        {
            Pos = new Vector4(px0, py0, -InvSqrt2, -InvSqrt2),
            Tex = new Vector4(ex0, ey1, packedBandTexLoc, packedBandCount),
            Color = color,
            Bnd = bnd
        };

        _vertices[baseVertex + 1] = new FormeVertex
        {
            Pos = new Vector4(px1, py0, InvSqrt2, -InvSqrt2),
            Tex = new Vector4(ex1, ey1, packedBandTexLoc, packedBandCount),
            Color = color,
            Bnd = bnd
        };

        _vertices[baseVertex + 2] = new FormeVertex
        {
            Pos = new Vector4(px1, py1, InvSqrt2, InvSqrt2),
            Tex = new Vector4(ex1, ey0, packedBandTexLoc, packedBandCount),
            Color = color,
            Bnd = bnd
        };

        _vertices[baseVertex + 3] = new FormeVertex
        {
            Pos = new Vector4(px0, py1, -InvSqrt2, InvSqrt2),
            Tex = new Vector4(ex0, ey0, packedBandTexLoc, packedBandCount),
            Color = color,
            Bnd = bnd
        };

        int baseIndex = _glyphCount * 6;
        _indices[baseIndex + 0] = baseVertex + 0;
        _indices[baseIndex + 1] = baseVertex + 1;
        _indices[baseIndex + 2] = baseVertex + 2;
        _indices[baseIndex + 3] = baseVertex + 0;
        _indices[baseIndex + 4] = baseVertex + 2;
        _indices[baseIndex + 5] = baseVertex + 3;

        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        if (disposing)
        {
            if (_ownsEffect)
            {
                _effect.Dispose();
            }
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        IsDisposed = true;
    }

    private readonly struct QueuedDraw
    {
        internal FormeFontDevice Font { get; }
        internal FormeGlyph Glyph { get; }
        internal Vector2 Position { get; }
        internal float SizePixels { get; }
        internal Color Color { get; }

        internal QueuedDraw(FormeFontDevice font, FormeGlyph glyph, Vector2 position, float sizePixels, Color color)
        {
            Font = font;
            Glyph = glyph;
            Position = position;
            SizePixels = sizePixels;
            Color = color;
        }
    }
}
