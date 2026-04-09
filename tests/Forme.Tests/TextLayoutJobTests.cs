using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Forme.Tests;

public class TextLayoutJobTests
{
    private static byte[] LoadEmbeddedFont(string resourceName)
    {
        Assembly assembly = typeof(TextLayoutJobTests).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static FormeFont LoadTestFont()
    {
        byte[] ttf = LoadEmbeddedFont("Forme.Tests.TestData.Inter-Regular.ttf");
        return FormeFont.FromTtf(ttf, CharacterSet.Ascii);
    }

    [Fact]
    public void CreatePlain_NonEmptyText_CreatesSingleFullLengthSection()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 24f);

        TextLayoutJob job = TextLayoutJob.CreatePlain("Hello", format);

        Assert.Equal("Hello", job.Text);
        Assert.Single(job.Sections);
        Assert.Equal(0, job.Sections[0].TextStart);
        Assert.Equal(5, job.Sections[0].TextLength);
        Assert.Equal(format, job.Sections[0].Format);
    }

    [Fact]
    public void CreatePlain_EmptyText_CreatesNoSections()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 24f);

        TextLayoutJob job = TextLayoutJob.CreatePlain(string.Empty, format);

        Assert.Empty(job.Sections);
    }

    [Fact]
    public void Constructor_ThrowsWhenSectionExtendsPastText()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 18f);

        Assert.Throws<ArgumentException>(() =>
            new TextLayoutJob("abcd", new TextSection[] { new TextSection(2, 3, format) }));
    }

    [Fact]
    public void Constructor_ThrowsWhenSectionsOverlap()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 18f);
        TextSection[] sections =
        [
            new TextSection(0, 3, format),
            new TextSection(2, 2, format)
        ];

        Assert.Throws<ArgumentException>(() => new TextLayoutJob("abcd", sections));
    }

    [Fact]
    public void LayoutText_PlainPath_ProducesRunWithDefaultFormatMetadata()
    {
        FormeFont font = LoadTestFont();

        TextLayoutResult result = font.LayoutText("Hello".AsSpan(), 20f);

        Assert.Single(result.Runs);
        Assert.Same(font, result.Runs[0].Font);
        Assert.Equal(20f, result.Runs[0].SizePixels);
        Assert.Equal(TextDecorations.None, result.Runs[0].Decorations);
        Assert.Equal(TextColor.White, result.Runs[0].Format.Color);
        Assert.Equal(TextColor.Transparent, result.Runs[0].Format.BackgroundColor);
        Assert.Null(result.Runs[0].Format.LineHeightPixels);
        Assert.Equal(0f, result.Runs[0].Format.BaselineShift);
    }
}
