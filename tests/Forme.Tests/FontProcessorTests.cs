using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;
using Forme;

namespace Forme.Tests;

public class FontProcessorTests
{
    private static byte[] LoadEmbeddedFont(string resourceName)
    {
        Assembly assembly = typeof(FontProcessorTests).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] LoadTestFont()
    {
        return LoadEmbeddedFont("Forme.Tests.TestData.Inter-Regular.ttf");
    }

    private static byte[] LoadUbuntuFont()
    {
        return LoadEmbeddedFont("Forme.Tests.TestData.Ubuntu-Light.ttf");
    }

    [Fact]
    public void FromTtf_AsciiCharset_ProducesExpectedGlyphCount()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        // ASCII charset has 95 codepoints (32-126). Space (32) has no outline and is skipped.
        // All other printable ASCII characters should be present in Inter.
        Assert.True(font.Glyphs.Count > 0);
        Assert.True(font.Glyphs.Count <= 95);
    }

    [Fact]
    public void FromTtf_ContainsGlyphForCapitalA()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        Assert.True(font.Glyphs.ContainsKey('A'), "Glyph for 'A' (codepoint 65) should be present.");
    }

    [Fact]
    public void FromTtf_GlyphA_HasValidBoundingBox()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        FormeGlyph glyph = font.Glyphs['A'];

        Assert.True(glyph.BoundingBox.X2 > glyph.BoundingBox.X1, "BoundingBox.X2 must be greater than BoundingBox.X1.");
        Assert.True(glyph.BoundingBox.Y2 > glyph.BoundingBox.Y1, "BoundingBox.Y2 must be greater than BoundingBox.Y1.");
        Assert.True(glyph.Width > 0);
        Assert.True(glyph.Height > 0);
    }

    [Fact]
    public void FromTtf_GlyphA_HasPositiveAdvanceWidth()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        FormeGlyph glyph = font.Glyphs['A'];
        Assert.True(glyph.AdvanceWidth > 0);
    }

    [Fact]
    public void FromTtf_GlyphA_HasValidBandData()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        FormeGlyph glyph = font.Glyphs['A'];

        Assert.True(glyph.BandInfo.Count >= 1 && glyph.BandInfo.Count <= 16);
        Assert.True(glyph.BandInfo.DimX > 0);
        Assert.True(glyph.BandInfo.DimY > 0);
        Assert.True(glyph.BandInfo.TexCoordX >= 0);
        Assert.True(glyph.BandInfo.TexCoordY >= 0);
    }

    [Fact]
    public void FromTtf_CurveTextureWidth_Is4096()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        Assert.Equal(4096, font.CurveTexture.Width);
    }

    [Fact]
    public void FromTtf_BandTextureWidth_Is4096()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        Assert.Equal(4096, font.BandTexture.Width);
    }

    [Fact]
    public void FromTtf_CurveTextureData_HasCorrectLength()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        int expectedLength = font.CurveTexture.Width * font.CurveTexture.Height * 4;
        Assert.Equal(expectedLength, font.CurveTexture.Data.Length);
    }

    [Fact]
    public void FromTtf_BandTextureData_HasCorrectLength()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        int expectedLength = font.BandTexture.Width * font.BandTexture.Height * 2;
        Assert.Equal(expectedLength, font.BandTexture.Data.Length);
    }

    [Fact]
    public void FromTtf_Metrics_HasPositiveUnitsPerEm()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        Assert.True(font.Metrics.UnitsPerEm > 0);
    }

    [Fact]
    public void FromTtf_Metrics_AscentIsPositive()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);

        Assert.True(font.Metrics.Ascent > 0);
    }

    [Fact]
    public void GetScaledMetrics_ScalesRawFontMetrics()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        float sizePixels = 32f;
        float scale = sizePixels / font.Metrics.UnitsPerEm;

        ScaledFontMetrics metrics = font.GetScaledMetrics(sizePixels);

        Assert.Equal(font.Metrics.Ascent * scale, metrics.Ascent);
        Assert.Equal(font.Metrics.Descent * scale, metrics.Descent);
        Assert.Equal(font.Metrics.LineGap * scale, metrics.LineGap);
        Assert.Equal(font.GetLineHeight(sizePixels), metrics.LineHeight);
        Assert.Equal(metrics.Ascent, metrics.BaselineToTop);
        Assert.Equal(-metrics.Descent, metrics.BaselineToBottom);
    }

    [Fact]
    public void FromTtf_ThrowsOnNullTtfData()
    {
        Assert.Throws<ArgumentNullException>(() => FormeFont.FromTtf(null!, CharacterSet.Ascii));
    }

    [Fact]
    public void FromTtf_ThrowsOnNullCharset()
    {
        Assert.Throws<ArgumentNullException>(() => FormeFont.FromTtf(Array.Empty<byte>(), null!));
    }

    [Fact]
    public void FromTtf_SmallCharset_OnlyContainsRequestedGlyphs()
    {
        byte[] ttf = LoadTestFont();
        CharacterSet charset = CharacterSet.FromString("ABC");
        FormeFont font = FormeFont.FromTtf(ttf, charset);

        Assert.True(font.Glyphs.Count <= 3);
        Assert.True(font.Glyphs.ContainsKey('A'));
        Assert.True(font.Glyphs.ContainsKey('B'));
        Assert.True(font.Glyphs.ContainsKey('C'));
        Assert.False(font.Glyphs.ContainsKey('D'));
    }

    [Fact]
    public void MeasureVisualBounds_MatchesUnionOfGlyphVisualBounds()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        TextLayoutOptions options = new TextLayoutOptions
        {
            MaxWidth = 80f
        };

        IReadOnlyList<GlyphPlacement> placements = font.GetGlyphs("AVATAR".AsSpan(), 32f, in options);
        FormeTextBounds visual = font.MeasureVisualBounds("AVATAR".AsSpan(), 32f, in options);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool foundVisible = false;

        foreach (GlyphPlacement placement in placements)
        {
            if (placement.VisualBounds.Width <= 0f || placement.VisualBounds.Height <= 0f)
            {
                continue;
            }

            if (placement.VisualBounds.X < minX)
            {
                minX = placement.VisualBounds.X;
            }
            if (placement.VisualBounds.Y < minY)
            {
                minY = placement.VisualBounds.Y;
            }
            if (placement.VisualBounds.X2 > maxX)
            {
                maxX = placement.VisualBounds.X2;
            }
            if (placement.VisualBounds.Y2 > maxY)
            {
                maxY = placement.VisualBounds.Y2;
            }

            foundVisible = true;
        }

        Assert.True(foundVisible);
        Assert.Equal(minX, visual.X);
        Assert.Equal(minY, visual.Y);
        Assert.Equal(maxX, visual.X2);
        Assert.Equal(maxY, visual.Y2);
    }

    [Fact]
    public void MeasureString_MatchesMeasureLogicalBounds()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        TextLayoutOptions options = new TextLayoutOptions
        {
            MaxWidth = 120f,
            Alignment = TextHorizontalAlignment.Center
        };

        FormeTextBounds aliasBounds = font.MeasureString("Hello, world!".AsSpan(), 32f, in options);
        FormeTextBounds logicalBounds = font.MeasureLogicalBounds("Hello, world!".AsSpan(), 32f, in options);

        Assert.Equal(logicalBounds.X, aliasBounds.X);
        Assert.Equal(logicalBounds.Y, aliasBounds.Y);
        Assert.Equal(logicalBounds.X2, aliasBounds.X2);
        Assert.Equal(logicalBounds.Y2, aliasBounds.Y2);
    }

    [Fact]
    public void MeasureLogicalBounds_UsesScaledFontMetricsForTopAndBottom()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.FromString("A"));
        float sizePixels = 32f;
        ScaledFontMetrics metrics = font.GetScaledMetrics(sizePixels);
        TextLayoutOptions options = new TextLayoutOptions
        {
            LineSpacing = 6f
        };

        FormeTextBounds bounds = font.MeasureLogicalBounds("A\nA".AsSpan(), sizePixels, in options);

        Assert.Equal(-metrics.BaselineToTop, bounds.Y);
        Assert.Equal(metrics.LineHeight + options.LineSpacing + metrics.BaselineToBottom, bounds.Y2);
    }

    [Fact]
    public void LayoutText_ExposesSameOverallBoundsAsLegacyApis()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        TextLayoutOptions options = new TextLayoutOptions
        {
            MaxWidth = 90f
        };

        TextLayoutResult result = font.LayoutText("AVATAR\nWIDE".AsSpan(), 32f, in options);

        Assert.Equal(font.MeasureLogicalBounds("AVATAR\nWIDE".AsSpan(), 32f, in options), result.LogicalBounds);
        Assert.Equal(font.MeasureVisualBounds("AVATAR\nWIDE".AsSpan(), 32f, in options), result.VisualBounds);
        Assert.Equal(font.GetGlyphs("AVATAR\nWIDE".AsSpan(), 32f, in options).Count, result.Glyphs.Count);
    }

    [Fact]
    public void LayoutText_WrappedText_ProducesMultipleLinesWithGlyphRanges()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        TextLayoutOptions options = new TextLayoutOptions
        {
            MaxWidth = 55f
        };

        TextLayoutResult result = font.LayoutText("Wrap here please".AsSpan(), 24f, in options);

        Assert.True(result.Lines.Count > 1);

        int totalGlyphs = 0;
        for (int i = 0; i < result.Lines.Count; i++)
        {
            TextLayoutLine line = result.Lines[i];
            Assert.True(line.GlyphStart >= totalGlyphs);
            Assert.True(line.GlyphCount >= 0);
            Assert.True(line.Width >= 0f);
            Assert.Equal(result.LogicalBounds.Y + i * line.LineHeight, line.LogicalBounds.Y);
            totalGlyphs += line.GlyphCount;
        }

        Assert.Equal(result.Glyphs.Count, totalGlyphs);
    }

    [Fact]
    public void LayoutText_RightAlignedLine_ReportsShiftedLogicalBounds()
    {
        byte[] ttf = LoadTestFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.Ascii);
        TextLayoutOptions options = new TextLayoutOptions
        {
            Alignment = TextHorizontalAlignment.Right
        };

        TextLayoutResult result = font.LayoutText("Right".AsSpan(), 32f, in options);
        TextLayoutLine line = result.Lines[0];

        Assert.True(line.LogicalBounds.X < 0f);
        Assert.Equal(0f, line.LogicalBounds.X2);
    }

    [Fact]
    public void FromTtf_UbuntuFont_ExtractsPairAdjustments()
    {
        byte[] ttf = LoadUbuntuFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.FromString("AV"));

        Assert.True(font.TryGetPairAdvanceAdjustment('A', 'V', out int adjustment));
        Assert.True(adjustment < 0);
    }

    [Fact]
    public void MeasureLogicalBounds_AppliesPairAdjustments()
    {
        byte[] ttf = LoadUbuntuFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.FromString("AV"));

        FormeTextBounds pairBounds = font.MeasureLogicalBounds("AV".AsSpan(), 32f);
        FormeTextBounds separateBoundsA = font.MeasureLogicalBounds("A".AsSpan(), 32f);
        FormeTextBounds separateBoundsV = font.MeasureLogicalBounds("V".AsSpan(), 32f);

        Assert.True(pairBounds.Width < separateBoundsA.Width + separateBoundsV.Width);
    }

    [Fact]
    public void GetGlyphs_AppliesPairAdjustmentsToPlacement()
    {
        byte[] ttf = LoadUbuntuFont();
        FormeFont font = FormeFont.FromTtf(ttf, CharacterSet.FromString("AV"));

        IReadOnlyList<GlyphPlacement> placements = font.GetGlyphs("AV".AsSpan(), 32f);

        Assert.Equal(2, placements.Count);
        Assert.True(placements[1].BaselineX < placements[0].AdvanceWidth);
    }
}
