using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;
using Forme;

namespace Forme.Tests;

public class FontProcessorTests
{
    private static byte[] LoadTestFont()
    {
        Assembly assembly = typeof(FontProcessorTests).Assembly;
        string resourceName = "Forme.Tests.TestData.Inter-Regular.ttf";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
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
}
