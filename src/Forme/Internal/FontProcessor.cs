// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using StbTrueTypeSharp;

namespace Forme.Internal;

/// <summary>
/// Processes a TrueType font and builds the GPU texture data required by the Slug algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Curve texture (RGBA32F, 4096 texels wide): each curve occupies exactly 2 consecutive
/// texels in the same row. Texel 0 stores (P1.x, P1.y, P2.x, P2.y); Texel 1 stores
/// (P3.x, P3.y, 0, 0). All coordinates are in absolute font design units.
/// </para>
/// <para>
/// Band texture (RG32F, 4096 texels wide): values are stored as floats representing u16
/// integers (all representable exactly in float32). Per-glyph layout starting at
/// (BandsTexCoordX, BandsTexCoordY): [bandCount horiz headers][bandCount vert headers]
/// [curve-location entries]. Each header stores (curveCount, absoluteOffset). Each
/// curve-location entry stores (curveTexelX, curveTexelY).
/// </para>
/// </remarks>
internal sealed class FontProcessor : IDisposable
{
    private const int TextureWidth = 4096;

    private StbTrueType.stbtt_fontinfo? _fontInfo;
    private FontMetrics _metrics;
    private bool _loaded;

    private readonly List<float> _curveTexData = new(TextureWidth * 4);

    private readonly List<ushort> _bandHeaderCurveCount = [];
    private readonly List<int> _bandHeaderOffset = [];
    private readonly List<BandTexelCoord> _bandCurveLocs = [];
    private readonly List<GlyphBandRange> _glyphBandRanges = [];
    private readonly List<FormeGlyph> _glyphs = [];
    private readonly List<FormeCurve> _scratchCurves = [];

    public unsafe void Load(byte[] ttfData)
    {
        fixed (byte* ptr = ttfData)
        {
            _fontInfo = new StbTrueType.stbtt_fontinfo();

            if (StbTrueType.stbtt_InitFont(_fontInfo, ptr, 0) == 0)
            {
                throw new InvalidOperationException("Failed to initialize font. The TTF data may be corrupt or unsupported.");
            }

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(_fontInfo, &ascent, &descent, &lineGap);

            float scale = StbTrueType.stbtt_ScaleForMappingEmToPixels(_fontInfo, 1.0f);
            int unitsPerEm = scale > 0f ? (int)MathF.Round(1.0f / scale) : 1000;

            _metrics = new FontMetrics(unitsPerEm, ascent, descent, lineGap);
            _loaded = true;
        }
    }

    public unsafe void ProcessCodePoint(int codePoint)
    {
        if (!_loaded)
        {
            throw new InvalidOperationException("Font not loaded. Call Load before processing glyphs.");
        }

        int glyphIdx = StbTrueType.stbtt_FindGlyphIndex(_fontInfo, codePoint);
        if (glyphIdx == 0)
        {
            return;
        }

        StbTrueType.stbtt_vertex* verts;
        int vertCount = StbTrueType.stbtt_GetGlyphShape(_fontInfo, glyphIdx, &verts);

        if (vertCount == 0)
        {
            // No visible outline (e.g. space). Still record advance width so DrawString
            // can advance the cursor correctly without skipping whitespace.
            int blankAdvance, blankLsb;
            StbTrueType.stbtt_GetGlyphHMetrics(_fontInfo, glyphIdx, &blankAdvance, &blankLsb);
            _glyphs.Add(new FormeGlyph(
                codePoint: codePoint,
                boundingBox: new FormeBoundingBox(0, 0, 0, 0),
                advanceWidth: blankAdvance,
                leftSideBearing: blankLsb,
                bandInfo: new FormeBandInfo(0, 1, 1, 0, 0)));
            return;
        }

        for (int v = 0; v < vertCount; v++)
        {
            if (verts[v].type == StbTrueType.STBTT_vcubic)
            {
                StbTrueType.stbtt_FreeShape(_fontInfo, verts);
                return;
            }
        }

        int bx1, by1, bx2, by2;
        StbTrueType.stbtt_GetGlyphBox(_fontInfo, glyphIdx, &bx1, &by1, &bx2, &by2);

        int advanceWidth, lsb;
        StbTrueType.stbtt_GetGlyphHMetrics(_fontInfo, glyphIdx, &advanceWidth, &lsb);

        _scratchCurves.Clear();
        float curX = 0f, curY = 0f;
        bool nextIsFirst = false;

        for (int v = 0; v < vertCount; v++)
        {
            ref StbTrueType.stbtt_vertex vert = ref verts[v];

            switch ((int)vert.type)
            {
                case StbTrueType.STBTT_vmove:
                    curX = vert.x;
                    curY = vert.y;
                    nextIsFirst = true;
                    break;

                case StbTrueType.STBTT_vline:
                    {
                        float nx = vert.x;
                        float ny = vert.y;
                        _scratchCurves.Add(new FormeCurve
                        {
                            StartPoint = new Vector2(curX, curY),
                            ControlPoint = new Vector2((curX + nx) * 0.5f, (curY + ny) * 0.5f),
                            EndPoint = new Vector2(nx, ny),
                            IsFirst = nextIsFirst
                        });
                        curX = nx;
                        curY = ny;
                        nextIsFirst = false;
                        break;
                    }

                case StbTrueType.STBTT_vcurve:
                    {
                        float nx = vert.x;
                        float ny = vert.y;
                        _scratchCurves.Add(new FormeCurve
                        {
                            StartPoint = new Vector2(curX, curY),
                            ControlPoint = new Vector2(vert.cx, vert.cy),
                            EndPoint = new Vector2(nx, ny),
                            IsFirst = nextIsFirst
                        });
                        curX = nx;
                        curY = ny;
                        nextIsFirst = false;
                        break;
                    }
            }
        }

        StbTrueType.stbtt_FreeShape(_fontInfo, verts);

        if (_scratchCurves.Count == 0)
        {
            return;
        }

        FixDegenerateControlPoints();

        int bandHeaderStart = _bandHeaderCurveCount.Count;
        int bandCurveStart = _bandCurveLocs.Count;
        int bandsTexelIndex = bandHeaderStart;

        AppendCurveTexture();

        int sizeX = bx2 - bx1 + 1;
        int sizeY = by2 - by1 + 1;
        int bandCount = Math.Max(1, Math.Min(16, Math.Min(sizeX, sizeY) / 2));

        AppendBandData(bandCount, sizeX, sizeY, bx1, by1);

        int bandHeaderCount = _bandHeaderCurveCount.Count - bandHeaderStart;
        _glyphBandRanges.Add(new GlyphBandRange(bandHeaderStart, bandCurveStart, bandHeaderCount));

        FormeGlyph glyph = new FormeGlyph(
            codePoint: codePoint,
            boundingBox: new FormeBoundingBox(bx1, by1, bx2, by2),
            advanceWidth: advanceWidth,
            leftSideBearing: lsb,
            bandInfo: new FormeBandInfo(
                count: bandCount,
                dimX: (sizeX + bandCount - 1) / bandCount,
                dimY: (sizeY + bandCount - 1) / bandCount,
                texCoordX: bandsTexelIndex % TextureWidth,
                texCoordY: bandsTexelIndex / TextureWidth));

        _glyphs.Add(glyph);
    }

    public FormeFont Build()
    {
        FormeTextureData curveTexture = FinalizeCurveTexture();
        FormeTextureData bandTexture = FinalizeBandTexture();

        Dictionary<int, FormeGlyph> glyphs = new Dictionary<int, FormeGlyph>(_glyphs.Count);
        foreach (FormeGlyph g in _glyphs)
        {
            glyphs[g.CodePoint] = g;
        }

        return new FormeFont(_metrics, glyphs, curveTexture, bandTexture);
    }

    private void FixDegenerateControlPoints()
    {
        for (int i = 0; i < _scratchCurves.Count; i++)
        {
            FormeCurve c = _scratchCurves[i];

            bool controlEqualStart = c.ControlPoint == c.StartPoint;
            bool controlEqualEnd = c.ControlPoint == c.EndPoint;

            if (controlEqualStart || controlEqualEnd)
            {
                c.ControlPoint = (c.StartPoint + c.EndPoint) * 0.5f;
                _scratchCurves[i] = c;
            }
        }
    }

    private void AppendCurveTexture()
    {
        for (int i = 0; i < _scratchCurves.Count; i++)
        {
            FormeCurve c = _scratchCurves[i];

            int nextTexel = _curveTexData.Count / 4;

            // Ensure both texels of this curve fit in the same row.
            if (nextTexel % TextureWidth == TextureWidth - 1)
            {
                _curveTexData.Add(0f);
                _curveTexData.Add(0f);
                _curveTexData.Add(0f);
                _curveTexData.Add(0f);
                nextTexel++;
            }

            c.TexelIndex = nextTexel;
            _scratchCurves[i] = c;

            _curveTexData.Add(c.StartPoint.X);
            _curveTexData.Add(c.StartPoint.Y);
            _curveTexData.Add(c.ControlPoint.X);
            _curveTexData.Add(c.ControlPoint.Y);

            _curveTexData.Add(c.EndPoint.X);
            _curveTexData.Add(c.EndPoint.Y);
            _curveTexData.Add(0f);
            _curveTexData.Add(0f);
        }
    }

    private void AppendBandData(int bandCount, int sizeX, int sizeY, int originX, int originY)
    {
        List<BandTexelCoord> localLocs = new List<BandTexelCoord>(_scratchCurves.Count * 2);

        int bandDimY = (sizeY + bandCount - 1) / bandCount;
        int bandDimX = (sizeX + bandCount - 1) / bandCount;

        // Horizontal bands: partition the glyph's Y extent into bandCount strips.
        // Curves sorted by max-x descending for early-exit when the shader casts +X rays.
        _scratchCurves.Sort((a, b) =>
        {
            float maxA = Math.Max(Math.Max(a.StartPoint.X, a.ControlPoint.X), a.EndPoint.X);
            float maxB = Math.Max(Math.Max(b.StartPoint.X, b.ControlPoint.X), b.EndPoint.X);
            return maxB.CompareTo(maxA);
        });

        for (int band = 0; band < bandCount; band++)
        {
            float minY = originY + band * bandDimY;
            float maxY = minY + bandDimY;

            int localOffset = localLocs.Count;
            ushort count = 0;

            foreach (FormeCurve c in _scratchCurves)
            {
                if (c.StartPoint.Y == c.ControlPoint.Y && c.ControlPoint.Y == c.EndPoint.Y)
                {
                    continue;
                }

                float cMinY = Math.Min(Math.Min(c.StartPoint.Y, c.ControlPoint.Y), c.EndPoint.Y);
                float cMaxY = Math.Max(Math.Max(c.StartPoint.Y, c.ControlPoint.Y), c.EndPoint.Y);

                if (cMinY > maxY || cMaxY < minY)
                {
                    continue;
                }

                localLocs.Add(new BandTexelCoord((ushort)(c.TexelIndex % TextureWidth), (ushort)(c.TexelIndex / TextureWidth)));
                count++;
            }

            _bandHeaderCurveCount.Add(count);
            _bandHeaderOffset.Add(localOffset);
        }

        // Vertical bands: partition the glyph's X extent into bandCount strips.
        // Curves sorted by max-y descending for early-exit when the shader casts +Y rays.
        _scratchCurves.Sort((a, b) =>
        {
            float maxA = Math.Max(Math.Max(a.StartPoint.Y, a.ControlPoint.Y), a.EndPoint.Y);
            float maxB = Math.Max(Math.Max(b.StartPoint.Y, b.ControlPoint.Y), b.EndPoint.Y);
            return maxB.CompareTo(maxA);
        });

        for (int band = 0; band < bandCount; band++)
        {
            float minX = originX + band * bandDimX;
            float maxX = minX + bandDimX;

            int localOffset = localLocs.Count;
            ushort count = 0;

            foreach (FormeCurve c in _scratchCurves)
            {
                if (c.StartPoint.X == c.ControlPoint.X && c.ControlPoint.X == c.EndPoint.X)
                {
                    continue;
                }

                float cMinX = Math.Min(Math.Min(c.StartPoint.X, c.ControlPoint.X), c.EndPoint.X);
                float cMaxX = Math.Max(Math.Max(c.StartPoint.X, c.ControlPoint.X), c.EndPoint.X);

                if (cMinX > maxX || cMaxX < minX)
                {
                    continue;
                }

                localLocs.Add(new BandTexelCoord((ushort)(c.TexelIndex % TextureWidth), (ushort)(c.TexelIndex / TextureWidth)));
                count++;
            }

            _bandHeaderCurveCount.Add(count);
            _bandHeaderOffset.Add(localOffset);
        }

        foreach (BandTexelCoord loc in localLocs)
        {
            _bandCurveLocs.Add(loc);
        }
    }

    private FormeTextureData FinalizeBandTexture()
    {
        int totalHeaderTexels = _bandHeaderCurveCount.Count;

        for (int gi = 0; gi < _glyphBandRanges.Count; gi++)
        {
            GlyphBandRange range = _glyphBandRanges[gi];

            for (int hi = range.HeaderStart; hi < range.HeaderStart + range.HeaderCount; hi++)
            {
                _bandHeaderOffset[hi] = _bandHeaderOffset[hi] + totalHeaderTexels + range.CurveStart;
            }
        }

        int totalBandTexels = totalHeaderTexels + _bandCurveLocs.Count;
        int bandTexWidth = TextureWidth;
        int bandTexHeight = Math.Max(1, (totalBandTexels + TextureWidth - 1) / TextureWidth);

        float[] bandTexData = new float[bandTexWidth * bandTexHeight * 2];

        for (int i = 0; i < totalHeaderTexels; i++)
        {
            bandTexData[i * 2 + 0] = _bandHeaderCurveCount[i];
            bandTexData[i * 2 + 1] = _bandHeaderOffset[i];
        }

        for (int i = 0; i < _bandCurveLocs.Count; i++)
        {
            bandTexData[(totalHeaderTexels + i) * 2 + 0] = _bandCurveLocs[i].X;
            bandTexData[(totalHeaderTexels + i) * 2 + 1] = _bandCurveLocs[i].Y;
        }

        return new FormeTextureData(bandTexData, bandTexWidth, bandTexHeight);
    }

    private FormeTextureData FinalizeCurveTexture()
    {
        int totalCurveTexels = (_curveTexData.Count + 3) / 4;
        int curveTexWidth = TextureWidth;
        int curveTexHeight = Math.Max(1, (totalCurveTexels + TextureWidth - 1) / TextureWidth);

        float[] curveTexData = new float[curveTexWidth * curveTexHeight * 4];
        _curveTexData.CopyTo(curveTexData);

        return new FormeTextureData(curveTexData, curveTexWidth, curveTexHeight);
    }

    public void Dispose()
    {
        _fontInfo?.Dispose();
    }
}
