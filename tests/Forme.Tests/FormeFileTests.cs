using System;
using System.IO;
using System.Reflection;
using Xunit;
using Forme;

namespace Forme.Tests;

public class FormeFileTests
{
    private static byte[] LoadEmbeddedFont(string resourceName)
    {
        Assembly assembly = typeof(FormeFileTests).Assembly;

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

    private static FormeFont BuildTestFont()
    {
        byte[] ttf = LoadTestFont();
        return FormeFont.FromTtf(ttf, CharacterSet.Range(65, 90));
    }

    private static FormeFont BuildPairAdjustingFont()
    {
        byte[] ttf = LoadUbuntuFont();
        return FormeFont.FromTtf(ttf, CharacterSet.FromString("AV"));
    }

    [Fact]
    public void RoundTrip_PreservesGlyphCount()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        Assert.Equal(original.Glyphs.Count, loaded.Glyphs.Count);
    }

    [Fact]
    public void RoundTrip_PreservesMetrics()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        Assert.Equal(original.Metrics.UnitsPerEm, loaded.Metrics.UnitsPerEm);
        Assert.Equal(original.Metrics.Ascent, loaded.Metrics.Ascent);
        Assert.Equal(original.Metrics.Descent, loaded.Metrics.Descent);
        Assert.Equal(original.Metrics.LineGap, loaded.Metrics.LineGap);
    }

    [Fact]
    public void RoundTrip_PreservesGlyphBoundingBox()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        FormeGlyph originalGlyph = original.Glyphs['A'];
        FormeGlyph loadedGlyph = loaded.Glyphs['A'];

        Assert.Equal(originalGlyph.BoundingBox.X1, loadedGlyph.BoundingBox.X1);
        Assert.Equal(originalGlyph.BoundingBox.Y1, loadedGlyph.BoundingBox.Y1);
        Assert.Equal(originalGlyph.BoundingBox.X2, loadedGlyph.BoundingBox.X2);
        Assert.Equal(originalGlyph.BoundingBox.Y2, loadedGlyph.BoundingBox.Y2);
    }

    [Fact]
    public void RoundTrip_PreservesGlyphBandData()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        FormeGlyph originalGlyph = original.Glyphs['A'];
        FormeGlyph loadedGlyph = loaded.Glyphs['A'];

        Assert.Equal(originalGlyph.BandInfo.Count, loadedGlyph.BandInfo.Count);
        Assert.Equal(originalGlyph.BandInfo.DimX, loadedGlyph.BandInfo.DimX);
        Assert.Equal(originalGlyph.BandInfo.DimY, loadedGlyph.BandInfo.DimY);
        Assert.Equal(originalGlyph.BandInfo.TexCoordX, loadedGlyph.BandInfo.TexCoordX);
        Assert.Equal(originalGlyph.BandInfo.TexCoordY, loadedGlyph.BandInfo.TexCoordY);
    }

    [Fact]
    public void RoundTrip_PreservesCurveTextureDimensions()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        Assert.Equal(original.CurveTexture.Width, loaded.CurveTexture.Width);
        Assert.Equal(original.CurveTexture.Height, loaded.CurveTexture.Height);
    }

    [Fact]
    public void RoundTrip_PreservesBandTextureDimensions()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        Assert.Equal(original.BandTexture.Width, loaded.BandTexture.Width);
        Assert.Equal(original.BandTexture.Height, loaded.BandTexture.Height);
    }

    [Fact]
    public void RoundTrip_PreservesCurveTextureData()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        System.ReadOnlySpan<float> originalSpan = original.CurveTexture.Data.Span;
        System.ReadOnlySpan<float> loadedSpan = loaded.CurveTexture.Data.Span;

        Assert.Equal(originalSpan.Length, loadedSpan.Length);
        Assert.True(originalSpan.SequenceEqual(loadedSpan), "Curve texture data must be byte-identical after round-trip.");
    }

    [Fact]
    public void RoundTrip_PreservesBandTextureData()
    {
        FormeFont original = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        System.ReadOnlySpan<float> originalSpan = original.BandTexture.Data.Span;
        System.ReadOnlySpan<float> loadedSpan = loaded.BandTexture.Data.Span;

        Assert.Equal(originalSpan.Length, loadedSpan.Length);
        Assert.True(originalSpan.SequenceEqual(loadedSpan), "Band texture data must be byte-identical after round-trip.");
    }

    [Fact]
    public void FromStream_ThrowsOnInvalidMagic()
    {
        byte[] garbage = new byte[64];
        garbage[0] = 0xFF;

        using MemoryStream ms = new MemoryStream(garbage);
        Assert.Throws<InvalidDataException>(() => FormeFont.FromStream(ms));
    }

    [Fact]
    public void FromStream_ThrowsOnFutureVersion()
    {
        FormeFont font = BuildTestFont();

        using MemoryStream ms = new MemoryStream();
        font.Save(ms);

        byte[] bytes = ms.ToArray();

        // Patch version field (bytes 8-9) to version 255
        bytes[8] = 255;
        bytes[9] = 0;

        using MemoryStream patched = new MemoryStream(bytes);
        Assert.Throws<NotSupportedException>(() => FormeFont.FromStream(patched));
    }

    [Fact]
    public void FromStream_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => FormeFont.FromStream(null!));
    }

    [Fact]
    public void Save_ThrowsOnNull()
    {
        FormeFont font = BuildTestFont();
        Assert.Throws<ArgumentNullException>(() => font.Save((Stream)null!));
    }

    [Fact]
    public void RoundTrip_ViaFilePath()
    {
        FormeFont original = BuildTestFont();
        string tempPath = Path.GetTempFileName() + ".forme";

        try
        {
            original.Save(tempPath);
            FormeFont loaded = FormeFont.FromFile(tempPath);

            Assert.Equal(original.Glyphs.Count, loaded.Glyphs.Count);
            Assert.Equal(original.Metrics.UnitsPerEm, loaded.Metrics.UnitsPerEm);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void RoundTrip_PreservesPairAdjustments()
    {
        FormeFont original = BuildPairAdjustingFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        Assert.True(original.TryGetPairAdvanceAdjustment('A', 'V', out int originalAdjustment));
        Assert.True(loaded.TryGetPairAdvanceAdjustment('A', 'V', out int adjustment));
        Assert.Equal(original.PairAdjustments.Count, loaded.PairAdjustments.Count);
        Assert.Equal(originalAdjustment, adjustment);
    }

    [Fact]
    public void RoundTrip_PreservesPairAdjustedLayout()
    {
        FormeFont original = BuildPairAdjustingFont();

        using MemoryStream ms = new MemoryStream();
        original.Save(ms);
        ms.Position = 0;

        FormeFont loaded = FormeFont.FromStream(ms);

        FormeTextBounds originalBounds = original.MeasureLogicalBounds("AV".AsSpan(), 32f);
        FormeTextBounds loadedBounds = loaded.MeasureLogicalBounds("AV".AsSpan(), 32f);

        Assert.Equal(originalBounds.Width, loadedBounds.Width);
    }
}
