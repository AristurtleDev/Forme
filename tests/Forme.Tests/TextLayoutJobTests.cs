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

    [Fact]
    public void LayoutText_JobWithSectionBaselineShift_OffsetsGlyphBaselines()
    {
        FormeFont font = LoadTestFont();
        TextFormat normal = new TextFormat(font, 20f);
        TextFormat raised = new TextFormat(font, 20f)
        {
            BaselineShift = -4f
        };
        TextLayoutJob shiftedJob = new TextLayoutJob(
            "ABCD",
            new TextSection[]
            {
                new TextSection(0, 2, normal),
                new TextSection(2, 2, raised)
            });

        TextLayoutResult shiftedResult = font.LayoutText(shiftedJob);

        Assert.Equal(0f, shiftedResult.Glyphs[0].BaselineY);
        Assert.Equal(0f, shiftedResult.Glyphs[1].BaselineY);
        Assert.Equal(-4f, shiftedResult.Glyphs[2].BaselineY);
        Assert.Equal(-4f, shiftedResult.Glyphs[3].BaselineY);
        Assert.True(shiftedResult.Runs[1].LogicalBounds.Y < shiftedResult.Runs[0].LogicalBounds.Y);
    }

    [Fact]
    public void LayoutText_JobWithSectionBaselineShift_DoesNotChangeFollowingLineBaseline()
    {
        FormeFont font = LoadTestFont();
        TextFormat normal = new TextFormat(font, 20f);
        TextFormat lowered = new TextFormat(font, 20f)
        {
            BaselineShift = 5f
        };
        TextLayoutJob shiftedJob = new TextLayoutJob(
            "AB\nCD",
            new TextSection[]
            {
                new TextSection(0, 2, lowered),
                new TextSection(2, 1, lowered),
                new TextSection(3, 2, normal)
            });
        TextLayoutJob normalJob = TextLayoutJob.CreatePlain("AB\nCD", normal);

        TextLayoutResult shiftedResult = font.LayoutText(shiftedJob);
        TextLayoutResult normalResult = font.LayoutText(normalJob);

        Assert.Equal(2, shiftedResult.Lines.Count);
        Assert.Equal(normalResult.Lines[1].BaselineY, shiftedResult.Lines[1].BaselineY);
        Assert.True(shiftedResult.Lines[0].LogicalBounds.Y2 > normalResult.Lines[0].LogicalBounds.Y2);
    }

    [Fact]
    public void LayoutText_JobWithAdjacentBackgroundRuns_MergesBackgroundRectsPerLine()
    {
        FormeFont font = LoadTestFont();
        TextColor background = new TextColor(20, 30, 40, 255);
        TextFormat first = new TextFormat(font, 20f)
        {
            Color = new TextColor(255, 0, 0),
            BackgroundColor = background,
            Decorations = TextDecorations.Background
        };
        TextFormat second = new TextFormat(font, 20f)
        {
            Color = new TextColor(0, 255, 0),
            BackgroundColor = background,
            Decorations = TextDecorations.Background
        };
        TextLayoutJob job = new TextLayoutJob(
            "ABCD",
            new TextSection[]
            {
                new TextSection(0, 2, first),
                new TextSection(2, 2, second)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Single(result.BackgroundRects);
        Assert.Equal(background, result.BackgroundRects[0].Color);
        Assert.Equal(0, result.BackgroundRects[0].TextStart);
        Assert.Equal(4, result.BackgroundRects[0].TextLength);
        Assert.Equal(0, result.BackgroundRects[0].LineIndex);
        Assert.Equal(result.Lines[0].LogicalBounds.Y, result.BackgroundRects[0].Bounds.Y);
        Assert.Equal(result.Lines[0].LogicalBounds.Y2, result.BackgroundRects[0].Bounds.Y2);
        Assert.Equal(result.Lines[0].LogicalBounds.X, result.BackgroundRects[0].Bounds.X);
        Assert.Equal(result.Lines[0].LogicalBounds.X2, result.BackgroundRects[0].Bounds.X2);
    }

    [Fact]
    public void LayoutText_JobWithBackgroundDecoration_SplitsBackgroundRectsAcrossLines()
    {
        FormeFont font = LoadTestFont();
        TextColor background = new TextColor(50, 60, 70, 255);
        TextFormat format = new TextFormat(font, 20f)
        {
            BackgroundColor = background,
            Decorations = TextDecorations.Background
        };
        TextLayoutJob job = TextLayoutJob.CreatePlain("AB\nCD", format);

        TextLayoutResult result = font.LayoutText(job);

        Assert.Equal(2, result.BackgroundRects.Count);
        Assert.Equal(0, result.BackgroundRects[0].LineIndex);
        Assert.Equal(0, result.BackgroundRects[0].TextStart);
        Assert.Equal(2, result.BackgroundRects[0].TextLength);
        Assert.Equal(result.Lines[0].LogicalBounds.Y, result.BackgroundRects[0].Bounds.Y);
        Assert.Equal(result.Lines[0].LogicalBounds.Y2, result.BackgroundRects[0].Bounds.Y2);
        Assert.Equal(1, result.BackgroundRects[1].LineIndex);
        Assert.Equal(3, result.BackgroundRects[1].TextStart);
        Assert.Equal(2, result.BackgroundRects[1].TextLength);
        Assert.Equal(result.Lines[1].LogicalBounds.Y, result.BackgroundRects[1].Bounds.Y);
        Assert.Equal(result.Lines[1].LogicalBounds.Y2, result.BackgroundRects[1].Bounds.Y2);
    }

    [Fact]
    public void LayoutText_JobWithAdjacentUnderlineRuns_MergesUnderlineSegmentsPerLine()
    {
        FormeFont font = LoadTestFont();
        TextColor color = new TextColor(200, 210, 220, 255);
        TextFormat first = new TextFormat(font, 20f)
        {
            Color = color,
            Decorations = TextDecorations.Underline
        };
        TextFormat second = new TextFormat(font, 20f)
        {
            Color = color,
            Decorations = TextDecorations.Underline
        };
        TextLayoutJob job = new TextLayoutJob(
            "ABCD",
            new TextSection[]
            {
                new TextSection(0, 2, first),
                new TextSection(2, 2, second)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Single(result.UnderlineLines);
        Assert.Equal(TextDecorations.Underline, result.UnderlineLines[0].Decoration);
        Assert.Equal(color, result.UnderlineLines[0].Color);
        Assert.Equal(0, result.UnderlineLines[0].TextStart);
        Assert.Equal(4, result.UnderlineLines[0].TextLength);
        Assert.Equal(0, result.UnderlineLines[0].LineIndex);
        Assert.Equal(result.Lines[0].LogicalBounds.X, result.UnderlineLines[0].X);
        Assert.Equal(result.Lines[0].LogicalBounds.X2, result.UnderlineLines[0].X2);
        Assert.Equal(result.Lines[0].LogicalBounds.Y2, result.UnderlineLines[0].Y);
        Assert.Equal(1f, result.UnderlineLines[0].Thickness);
    }

    [Fact]
    public void LayoutText_JobWithBaselineShiftedUnderline_KeepsSeparateUnderlineSegment()
    {
        FormeFont font = LoadTestFont();
        TextColor color = new TextColor(180, 120, 80, 255);
        TextFormat normal = new TextFormat(font, 20f)
        {
            Color = color,
            Decorations = TextDecorations.Underline
        };
        TextFormat lowered = new TextFormat(font, 20f)
        {
            Color = color,
            Decorations = TextDecorations.Underline,
            BaselineShift = 4f
        };
        TextLayoutJob job = new TextLayoutJob(
            "ABCD",
            new TextSection[]
            {
                new TextSection(0, 2, normal),
                new TextSection(2, 2, lowered)
            });

        TextLayoutResult result = font.LayoutText(job);

        Assert.Equal(2, result.UnderlineLines.Count);
        Assert.Equal(result.Glyphs[1].LogicalBounds.Y2, result.UnderlineLines[0].Y);
        Assert.Equal(result.Glyphs[2].LogicalBounds.Y2, result.UnderlineLines[1].Y);
        Assert.True(result.UnderlineLines[1].Y > result.UnderlineLines[0].Y);
        Assert.Equal(0, result.UnderlineLines[0].TextStart);
        Assert.Equal(2, result.UnderlineLines[0].TextLength);
        Assert.Equal(2, result.UnderlineLines[1].TextStart);
        Assert.Equal(2, result.UnderlineLines[1].TextLength);
    }
}
