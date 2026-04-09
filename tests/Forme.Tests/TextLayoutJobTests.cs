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
    public void Constructor_ThrowsWhenNonEmptyTextHasNoSections()
    {
        Assert.Throws<ArgumentException>(() => new TextLayoutJob("abcd", Array.Empty<TextSection>()));
    }

    [Fact]
    public void Constructor_ThrowsWhenFirstSectionDoesNotStartAtZero()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 18f);

        Assert.Throws<ArgumentException>(() =>
            new TextLayoutJob("abcd", new TextSection[] { new TextSection(1, 3, format) }));
    }

    [Fact]
    public void Constructor_ThrowsWhenSectionsLeaveGap()
    {
        FormeFont font = LoadTestFont();
        TextFormat format = new TextFormat(font, 18f);
        TextSection[] sections =
        [
            new TextSection(0, 1, format),
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

    [Fact]
    public void LayoutText_JobWithTwoSections_SplitsRunsAndUpdatesGlyphOwnership()
    {
        FormeFont font = LoadTestFont();
        TextFormat firstFormat = new TextFormat(font, 20f)
        {
            Color = new TextColor(255, 0, 0),
            Decorations = TextDecorations.Underline
        };
        TextFormat secondFormat = new TextFormat(font, 20f)
        {
            Color = new TextColor(0, 255, 0),
            BackgroundColor = new TextColor(20, 30, 40, 255),
            Decorations = TextDecorations.Strikethrough | TextDecorations.Background
        };
        TextLayoutJob job = new TextLayoutJob(
            "HelloWorld",
            new TextSection[]
            {
                new TextSection(0, 5, firstFormat),
                new TextSection(5, 5, secondFormat)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Equal(2, result.Runs.Count);
        Assert.Equal(firstFormat, result.Runs[0].Format);
        Assert.Equal(secondFormat, result.Runs[1].Format);
        Assert.Equal(0, result.Runs[0].TextStart);
        Assert.Equal(5, result.Runs[0].TextLength);
        Assert.Equal(5, result.Runs[1].TextStart);
        Assert.Equal(5, result.Runs[1].TextLength);
        Assert.Equal(0, result.Glyphs[0].RunIndex);
        Assert.Equal(0, result.Glyphs[4].RunIndex);
        Assert.Equal(1, result.Glyphs[5].RunIndex);
        Assert.Equal(1, result.Glyphs[9].RunIndex);
        Assert.Equal(2, result.Lines[0].RunCount);
    }

    [Fact]
    public void LayoutText_JobWithMismatchedSize_UsesSectionSizes()
    {
        FormeFont font = LoadTestFont();
        TextLayoutJob job = new TextLayoutJob(
            "HelloWorld",
            new TextSection[]
            {
                new TextSection(0, 5, new TextFormat(font, 18f)),
                new TextSection(5, 5, new TextFormat(font, 20f))
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Equal(2, result.Runs.Count);
        Assert.Equal(18f, result.Runs[0].SizePixels);
        Assert.Equal(20f, result.Runs[1].SizePixels);
        Assert.True(result.Runs[1].VisualBounds.Height >= result.Runs[0].VisualBounds.Height);
    }

    [Fact]
    public void LayoutText_JobWithSectionCharacterSpacing_AffectsRunWidth()
    {
        FormeFont font = LoadTestFont();
        TextFormat compact = new TextFormat(font, 20f);
        TextFormat loose = new TextFormat(font, 20f)
        {
            CharacterSpacing = 3f
        };
        TextLayoutJob compactJob = TextLayoutJob.CreatePlain("ABCD", compact);
        TextLayoutJob looseJob = TextLayoutJob.CreatePlain("ABCD", loose);

        TextLayoutResult compactResult = font.LayoutText(compactJob);
        TextLayoutResult looseResult = font.LayoutText(looseJob);

        Assert.True(looseResult.Runs[0].LogicalBounds.Width > compactResult.Runs[0].LogicalBounds.Width);
    }

    [Fact]
    public void LayoutText_JobWithSectionLineHeightOverride_UsesOverrideForLineHeight()
    {
        FormeFont font = LoadTestFont();
        TextFormat normal = new TextFormat(font, 20f);
        TextFormat tall = new TextFormat(font, 20f)
        {
            LineHeightPixels = 40f
        };
        TextLayoutJob job = new TextLayoutJob(
            "ABCD",
            new TextSection[]
            {
                new TextSection(0, 2, normal),
                new TextSection(2, 2, tall)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Single(result.Lines);
        Assert.Equal(40f, result.Lines[0].LineHeight);
        Assert.Equal(40f, result.Runs[1].Format.LineHeightPixels);
    }

    [Fact]
    public void LayoutText_JobWithSectionLineHeightOverride_AffectsFollowingBaseline()
    {
        FormeFont font = LoadTestFont();
        TextFormat tall = new TextFormat(font, 20f)
        {
            LineHeightPixels = 40f
        };
        TextFormat normal = new TextFormat(font, 20f);
        TextLayoutJob job = new TextLayoutJob(
            "AB\nCD",
            new TextSection[]
            {
                new TextSection(0, 2, tall),
                new TextSection(2, 1, tall),
                new TextSection(3, 2, normal)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(40f, result.Lines[0].LineHeight);
        Assert.Equal(40f, result.Lines[1].BaselineY);
        Assert.True(result.Lines[1].LineHeight < result.Lines[0].LineHeight);
    }
}
