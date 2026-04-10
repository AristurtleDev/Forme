// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Forme.Internal;

namespace Forme;

/// <summary>
/// Represents a processed font containing all data required to render glyphs using
/// the Slug algorithm.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FormeFont"/> holds two GPU texture datasets: a curve texture (RGBA32F)
/// encoding the quadratic Bezier control points for each glyph, and a band texture
/// (RG32F) encoding the spatial acceleration structure. Both textures are 4096 texels
/// wide.
/// </para>
/// <para>
/// Create instances via <see cref="FromTtf"/> or <see cref="FromFile"/>. Persist
/// processed data with <see cref="Save(string)"/> to avoid reprocessing the TTF at
/// startup.
/// </para>
/// </remarks>
public sealed class FormeFont
{
    /// <summary>
    /// Gets the vertical metrics for this font.
    /// </summary>
    public FontMetrics Metrics { get; }

    /// <summary>
    /// Gets the processed glyphs, keyed by Unicode code point.
    /// </summary>
    /// <remarks>
    /// Only glyphs in the <see cref="CharacterSet"/> specified during processing are
    /// present. Glyphs with no outline (e.g. space) and glyphs not found in the font
    /// are omitted.
    /// </remarks>
    public IReadOnlyDictionary<int, FormeGlyph> Glyphs { get; }

    /// <summary>
    /// Gets the sparse pair-advance adjustments extracted for this font, keyed by packed
    /// previous/current Unicode code point pairs.
    /// </summary>
    /// <remarks>
    /// Values are stored in the font's design units, not pixels. Call
    /// <see cref="TryGetPairAdvanceAdjustment"/> for point lookups instead of decoding keys
    /// manually.
    /// </remarks>
    public IReadOnlyDictionary<ulong, int> PairAdjustments { get; }

    /// <summary>
    /// Gets the curve texture dataset (RGBA32F, 4 floats per texel). Always 4096 texels wide.
    /// </summary>
    public FormeTextureData CurveTexture { get; }

    /// <summary>
    /// Gets the band texture dataset (RG32F, 2 floats per texel). Always 4096 texels wide.
    /// </summary>
    public FormeTextureData BandTexture { get; }

    internal FormeFont(
        FontMetrics metrics,
        Dictionary<int, FormeGlyph> glyphs,
        Dictionary<ulong, int> pairAdjustments,
        FormeTextureData curveTexture,
        FormeTextureData bandTexture)
    {
        Metrics = metrics;
        Glyphs = glyphs;
        PairAdjustments = pairAdjustments;
        CurveTexture = curveTexture;
        BandTexture = bandTexture;
    }

    /// <summary>
    /// Processes the specified glyphs from a TrueType font and returns a
    /// <see cref="FormeFont"/> ready for rendering.
    /// </summary>
    /// <param name="ttfData">Raw bytes of a TrueType (.ttf) or OpenType (.otf) font file.</param>
    /// <param name="charset">The set of Unicode code points to process.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ttfData"/> or <paramref name="charset"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="ttfData"/> cannot be parsed as a valid font.
    /// </exception>
    public static FormeFont FromTtf(byte[] ttfData, CharacterSet charset)
    {
        ArgumentNullException.ThrowIfNull(ttfData);
        ArgumentNullException.ThrowIfNull(charset);

        using FontProcessor processor = new FontProcessor();
        processor.Load(ttfData);

        foreach (int codePoint in charset.Codepoints)
        {
            processor.ProcessCodePoint(codePoint);
        }

        return processor.Build();
    }

    /// <summary>
    /// Loads a <see cref="FormeFont"/> from a <c>.forme</c> file on disk.
    /// </summary>
    /// <param name="path">The path to the <c>.forme</c> file.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is <see langword="null"/> or an empty string.
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the file does not contain valid <c>.forme</c> data.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the file was written by a newer version of Forme.
    /// </exception>
    public static FormeFont FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using FileStream stream = File.OpenRead(path);
        return FormeFileReader.Read(stream);
    }

    /// <summary>
    /// Loads a <see cref="FormeFont"/> from a stream containing <c>.forme</c> binary data.
    /// </summary>
    /// <param name="stream">A readable stream positioned at the start of <c>.forme</c> data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the stream does not contain valid <c>.forme</c> data.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the data was written by a newer version of Forme.
    /// </exception>
    public static FormeFont FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return FormeFileReader.Read(stream);
    }

    /// <summary>
    /// Saves this <see cref="FormeFont"/> to a <c>.forme</c> file on disk.
    /// </summary>
    /// <param name="path">The destination file path. The file is created or overwritten.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="path"/> is <see langword="null"/> or an empty string.
    /// </exception>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using FileStream stream = File.Create(path);
        FormeFileWriter.Write(this, stream);
    }

    /// <summary>
    /// Saves this <see cref="FormeFont"/> to the given stream in <c>.forme</c> binary format.
    /// </summary>
    /// <param name="stream">A writable stream. The stream is not closed after writing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        FormeFileWriter.Write(this, stream);
    }

    /// <summary>
    /// Returns the vertical font metrics in pixels for the given size.
    /// </summary>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <returns>
    /// A <see cref="ScaledFontMetrics"/> value whose ascent, descent, line gap, and derived line
    /// height all use the same baseline-relative coordinate system as Forme's layout API.
    /// </returns>
    public ScaledFontMetrics GetScaledMetrics(float sizePixels)
    {
        float scale = sizePixels / Math.Max(1, Metrics.UnitsPerEm);
        return new ScaledFontMetrics(
            Metrics.Ascent * scale,
            Metrics.Descent * scale,
            Metrics.LineGap * scale);
    }

    /// <summary>
    /// Returns the natural line height in pixels at the given size, based solely on font metrics.
    /// </summary>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <returns>
    /// The distance in pixels from one baseline to the next, equal to
    /// <c>(Ascent - Descent + LineGap) * (sizePixels / UnitsPerEm)</c>.
    /// </returns>
    public float GetLineHeight(float sizePixels)
        => GetScaledMetrics(sizePixels).LineHeight;

    /// <summary>
    /// Gets the horizontal advance adjustment for a specific neighboring glyph pair.
    /// </summary>
    /// <param name="previousCodePoint">The Unicode code point of the previous glyph.</param>
    /// <param name="currentCodePoint">The Unicode code point of the current glyph.</param>
    /// <param name="adjustment">
    /// When this method returns <see langword="true"/>, contains the design-unit adjustment to
    /// add to the normal glyph advance between the two glyphs.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the font contains a non-zero pair adjustment for the given
    /// neighboring glyphs; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetPairAdvanceAdjustment(int previousCodePoint, int currentCodePoint, out int adjustment)
    {
        return PairAdjustments.TryGetValue(MakePairAdjustmentKey(previousCodePoint, currentCodePoint), out adjustment);
    }

    /// <summary>
    /// Returns whether this <see cref="FormeFont"/> contains a glyph entry for the given Unicode
    /// code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to query.</param>
    /// <returns>
    /// <see langword="true"/> when the font contains the code point; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool SupportsCodePoint(int codePoint)
    {
        return Glyphs.ContainsKey(codePoint);
    }

    /// <summary>
    /// Measures the logical layout bounds of the given text at the specified size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <returns>
    /// Logical bounds in pixels relative to a draw origin at (0, 0). This is a compatibility alias
    /// for <see cref="MeasureLogicalBounds(ReadOnlySpan{char}, float)"/>. X is always 0 for
    /// left-aligned text, Y is derived from font ascent, and X2 represents the line layout width
    /// rather than the visual overhang of specific glyph outlines.
    /// </returns>
    public FormeTextBounds MeasureString(ReadOnlySpan<char> text, float sizePixels)
    {
        TextLayoutOptions options = default;
        return MeasureLogicalBounds(text, sizePixels, in options);
    }

    /// <summary>
    /// Measures the logical layout bounds of the given text at the specified size using the
    /// provided layout options.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <param name="options">Layout options controlling wrapping, spacing, and ellipsis.</param>
    /// <returns>
    /// Logical bounds in pixels relative to a draw origin at (0, 0). Width reflects the widest
    /// line layout width after pair positioning, spacing, wrapping, alignment, and ellipsis policy have
    /// been applied.
    /// </returns>
    public FormeTextBounds MeasureString(ReadOnlySpan<char> text, float sizePixels, in TextLayoutOptions options)
    {
        return MeasureLogicalBounds(text, sizePixels, in options);
    }

    /// <summary>
    /// Measures the logical layout bounds of the given text at the specified size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    public FormeTextBounds MeasureLogicalBounds(ReadOnlySpan<char> text, float sizePixels)
    {
        TextLayoutOptions options = default;
        return MeasureLogicalBounds(text, sizePixels, in options);
    }

    /// <summary>
    /// Measures the logical layout bounds of the given text at the specified size using the
    /// provided layout options.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <param name="options">Layout options controlling wrapping, spacing, and ellipsis.</param>
    /// <returns>
    /// Logical bounds in pixels relative to a draw origin at (0, 0). This is a compatibility alias
    /// for <see cref="MeasureLogicalBounds(ReadOnlySpan{char}, float, in TextLayoutOptions)"/>.
    /// Width reflects the widest line layout width after pair positioning, spacing, wrapping,
    /// alignment, and ellipsis policy have been applied.
    /// </returns>
    public FormeTextBounds MeasureLogicalBounds(ReadOnlySpan<char> text, float sizePixels, in TextLayoutOptions options)
    {
        return LayoutText(text, sizePixels, in options).LogicalBounds;
    }

    /// <summary>
    /// Measures the visual bounds of the given text at the specified size.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <returns>
    /// Visual bounds in pixels relative to a draw origin at (0, 0), derived from the union of
    /// visible glyph outline bounds after layout is applied.
    /// </returns>
    public FormeTextBounds MeasureVisualBounds(ReadOnlySpan<char> text, float sizePixels)
    {
        TextLayoutOptions options = default;
        return MeasureVisualBounds(text, sizePixels, in options);
    }

    /// <summary>
    /// Measures the visual bounds of the given text at the specified size using the provided
    /// layout options.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <param name="options">Layout options controlling wrapping, spacing, and ellipsis.</param>
    /// <returns>
    /// Visual bounds in pixels relative to a draw origin at (0, 0), derived from the union of
    /// visible glyph outline bounds after layout is applied.
    /// </returns>
    public FormeTextBounds MeasureVisualBounds(ReadOnlySpan<char> text, float sizePixels, in TextLayoutOptions options)
    {
        return LayoutText(text, sizePixels, in options).VisualBounds;
    }

    /// <summary>
    /// Tries to find the first code point in <paramref name="text"/> that is not available in
    /// this <see cref="FormeFont"/>.
    /// </summary>
    /// <param name="text">The text to inspect.</param>
    /// <param name="textIndex">
    /// When this method returns <see langword="true"/>, contains the zero-based UTF-16 index of
    /// the first missing code point.
    /// </param>
    /// <param name="codePoint">
    /// When this method returns <see langword="true"/>, contains the missing Unicode code point.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a missing code point is found; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryFindMissingCodePoint(ReadOnlySpan<char> text, out int textIndex, out int codePoint)
    {
        int currentTextIndex = 0;
        while (currentTextIndex < text.Length)
        {
            OperationStatus status = Rune.DecodeFromUtf16(text[currentTextIndex..], out Rune rune, out int charsConsumed);
            if (status != OperationStatus.Done)
            {
                break;
            }

            if (rune.Value != '\n' && !SupportsCodePoint(rune.Value))
            {
                textIndex = currentTextIndex;
                codePoint = rune.Value;
                return true;
            }

            currentTextIndex += charsConsumed;
        }

        textIndex = -1;
        codePoint = 0;
        return false;
    }

    /// <summary>
    /// Returns the full reusable layout result for the given text.
    /// </summary>
    /// <param name="text">The text to lay out.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    public TextLayoutResult LayoutText(ReadOnlySpan<char> text, float sizePixels)
    {
        TextLayoutOptions options = default;
        return LayoutText(text, sizePixels, in options);
    }

    /// <summary>
    /// Returns the full reusable layout result for the given rich-text job.
    /// </summary>
    /// <param name="job">The rich-text layout job to process.</param>
    /// <remarks>
    /// This rich-text entry point supports per-section primary fonts and fallback chains.
    /// Per-section size, character spacing, line-height overrides, and baseline shifts are
    /// also supported.
    /// </remarks>
    public TextLayoutResult LayoutText(TextLayoutJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.Text.Length == 0)
        {
            return TextLayoutResult.Empty;
        }

        TextFormat baseFormat = ValidateLayoutJob(job);
        ValidateMissingGlyphPolicy(job, job.LayoutOptions.MissingGlyphPolicy);
        TextLayoutResult baseResult = LayoutTextCore(job, baseFormat);
        TextLayoutResult result = ApplySectionsToLayout(baseResult, job.Sections);
        return ApplyGeometrySnap(result, job.LayoutOptions.GeometrySnap);
    }

    /// <summary>
    /// Returns the full reusable layout result for the given text, applying the provided layout
    /// options.
    /// </summary>
    /// <param name="text">The text to lay out.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <param name="options">Layout options controlling wrapping, spacing, alignment, and ellipsis.</param>
    public TextLayoutResult LayoutText(ReadOnlySpan<char> text, float sizePixels, in TextLayoutOptions options)
    {
        if (text.IsEmpty)
        {
            return TextLayoutResult.Empty;
        }

        ValidateMissingGlyphPolicy(text, options.MissingGlyphPolicy);

        ScaledFontMetrics scaledMetrics = GetScaledMetrics(sizePixels);
        float scale = sizePixels / Math.Max(1, Metrics.UnitsPerEm);
        float lineHeight = scaledMetrics.LineHeight + options.LineSpacing;

        List<LineLayoutInfo> codePointLines = BuildLines(text, scale, in options);
        if (codePointLines.Count == 0)
        {
            return TextLayoutResult.Empty;
        }

        List<GlyphPlacement> placements = new();
        List<TextLayoutLine> lines = new(codePointLines.Count);

        float maxLineWidth = 0f;
        bool hasVisibleBounds = false;
        float visualMinX = 0f;
        float visualMinY = 0f;
        float visualMaxX = 0f;
        float visualMaxY = 0f;
        float cursorY = 0f;

        foreach (LineLayoutInfo codePointLine in codePointLines)
        {
            int lineIndex = lines.Count;
            float lineWidth = MeasureLineWidth(codePointLine.Entries, scale, options.CharacterSpacing);
            if (lineWidth > maxLineWidth)
            {
                maxLineWidth = lineWidth;
            }

            float lineOriginX = options.Alignment switch
            {
                TextHorizontalAlignment.Center => -lineWidth * 0.5f,
                TextHorizontalAlignment.Right => -lineWidth,
                _ => 0f
            };

            int glyphStart = placements.Count;
            float cursorX = lineOriginX;
            int previousCodePoint = 0;
            bool hasPreviousGlyph = false;
            bool lineHasVisibleBounds = false;
            float lineMinX = 0f;
            float lineMinY = 0f;
            float lineMaxX = 0f;
            float lineMaxY = 0f;

            foreach (CodePointEntry entry in codePointLine.Entries)
            {
                if (Glyphs.TryGetValue(entry.CodePoint, out FormeGlyph glyph))
                {
                    if (hasPreviousGlyph)
                    {
                        cursorX += GetPairAdvanceAdjustment(previousCodePoint, entry.CodePoint, scale);
                    }

                    float advance = glyph.AdvanceWidth * scale + options.CharacterSpacing;
                    FormeTextBounds glyphLogicalBounds = new FormeTextBounds(
                        cursorX,
                        cursorY - scaledMetrics.BaselineToTop,
                        cursorX + advance,
                        cursorY + scaledMetrics.BaselineToBottom);
                    FormeTextBounds visualBounds = ComputeVisualBounds(in glyph, cursorX, cursorY, scale);
                    placements.Add(new GlyphPlacement(this, entry.Index, entry.Utf16Length, entry.CodePoint, lineIndex, 0, cursorX, cursorY, glyphLogicalBounds, visualBounds, advance));

                    if (visualBounds.Width > 0f && visualBounds.Height > 0f)
                    {
                        if (!lineHasVisibleBounds)
                        {
                            lineMinX = visualBounds.X;
                            lineMinY = visualBounds.Y;
                            lineMaxX = visualBounds.X2;
                            lineMaxY = visualBounds.Y2;
                            lineHasVisibleBounds = true;
                        }
                        else
                        {
                            if (visualBounds.X < lineMinX)
                            {
                                lineMinX = visualBounds.X;
                            }
                            if (visualBounds.Y < lineMinY)
                            {
                                lineMinY = visualBounds.Y;
                            }
                            if (visualBounds.X2 > lineMaxX)
                            {
                                lineMaxX = visualBounds.X2;
                            }
                            if (visualBounds.Y2 > lineMaxY)
                            {
                                lineMaxY = visualBounds.Y2;
                            }
                        }

                        if (!hasVisibleBounds)
                        {
                            visualMinX = visualBounds.X;
                            visualMinY = visualBounds.Y;
                            visualMaxX = visualBounds.X2;
                            visualMaxY = visualBounds.Y2;
                            hasVisibleBounds = true;
                        }
                        else
                        {
                            if (visualBounds.X < visualMinX)
                            {
                                visualMinX = visualBounds.X;
                            }
                            if (visualBounds.Y < visualMinY)
                            {
                                visualMinY = visualBounds.Y;
                            }
                            if (visualBounds.X2 > visualMaxX)
                            {
                                visualMaxX = visualBounds.X2;
                            }
                            if (visualBounds.Y2 > visualMaxY)
                            {
                                visualMaxY = visualBounds.Y2;
                            }
                        }
                    }

                    cursorX += advance;
                    previousCodePoint = entry.CodePoint;
                    hasPreviousGlyph = true;
                }
            }

            FormeTextBounds lineLogicalBounds = new FormeTextBounds(
                lineOriginX,
                cursorY - scaledMetrics.BaselineToTop,
                lineOriginX + lineWidth,
                cursorY + scaledMetrics.BaselineToBottom);
            FormeTextBounds lineVisualBounds = lineHasVisibleBounds
                ? new FormeTextBounds(lineMinX, lineMinY, lineMaxX, lineMaxY)
                : FormeTextBounds.Empty;

            lines.Add(new TextLayoutLine(
                codePointLine.TextStart,
                codePointLine.TextLength,
                cursorY,
                lineHeight,
                scaledMetrics.Ascent,
                scaledMetrics.Descent,
                lineWidth,
                lineLogicalBounds,
                lineVisualBounds,
                glyphStart,
                placements.Count - glyphStart,
                0,
                1));

            cursorY += lineHeight;
        }

        FormeTextBounds logicalBounds = new FormeTextBounds(
            0f,
            -scaledMetrics.BaselineToTop,
            maxLineWidth,
            (codePointLines.Count - 1) * lineHeight + scaledMetrics.BaselineToBottom);
        FormeTextBounds visualBoundsResult = hasVisibleBounds
            ? new FormeTextBounds(visualMinX, visualMinY, visualMaxX, visualMaxY)
            : FormeTextBounds.Empty;
        TextFormat runFormat = new TextFormat(this, sizePixels);
        List<TextLayoutRun> runs =
        [
            new TextLayoutRun(runFormat, this, 0, text.Length, 0, placements.Count, logicalBounds, visualBoundsResult, 0, lines.Count)
        ];

        TextLayoutResult result = new TextLayoutResult(text.ToString(), logicalBounds, visualBoundsResult, lines, runs, placements);
        return ApplyGeometrySnap(result, options.GeometrySnap);
    }

    /// <summary>
    /// Returns the layout-computed position and metrics for each glyph in the given text.
    /// </summary>
    /// <param name="text">The text to lay out.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <returns>
    /// One <see cref="GlyphPlacement"/> per code point found in <see cref="Glyphs"/>,
    /// with positions relative to a draw origin at (0, 0).
    /// </returns>
    public IReadOnlyList<GlyphPlacement> GetGlyphs(ReadOnlySpan<char> text, float sizePixels)
    {
        TextLayoutOptions options = default;
        return GetGlyphs(text, sizePixels, in options);
    }

    /// <summary>
    /// Returns the layout-computed position and metrics for each glyph in the given text,
    /// applying the provided layout options.
    /// </summary>
    /// <param name="text">The text to lay out.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    /// <param name="options">Layout options controlling wrapping, spacing, alignment, and ellipsis.</param>
    /// <returns>
    /// One <see cref="GlyphPlacement"/> per code point found in <see cref="Glyphs"/>,
    /// with positions relative to a draw origin at (0, 0).
    /// </returns>
    public IReadOnlyList<GlyphPlacement> GetGlyphs(ReadOnlySpan<char> text, float sizePixels, in TextLayoutOptions options)
    {
        return LayoutText(text, sizePixels, in options).Glyphs;
    }

    private TextFormat ValidateLayoutJob(TextLayoutJob job)
    {
        if (job.Sections.Count == 0)
        {
            throw new ArgumentException("Non-empty text layout jobs require at least one section.", nameof(job));
        }

        TextFormat baseFormat = job.Sections[0].Format;
        ValidateSectionFormat(job.Sections[0], nameof(job));

        for (int i = 1; i < job.Sections.Count; i++)
        {
            ValidateSectionFormat(job.Sections[i], nameof(job));
        }

        return baseFormat;
    }

    private void ValidateSectionFormat(TextSection section, string paramName)
    {
        if (!section.Format.IsValid)
        {
            throw new ArgumentException("Text layout sections require a valid TextFormat.", paramName);
        }
    }

    private static TextLayoutResult ApplySectionsToLayout(TextLayoutResult baseResult, IReadOnlyList<TextSection> sections)
    {
        List<GlyphPlacement> glyphs = new(baseResult.Glyphs.Count);
        List<TextLayoutRun> runs = new(sections.Count);
        int glyphCursor = 0;

        for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
        {
            TextSection section = sections[sectionIndex];
            List<GlyphPlacement> sectionGlyphs = new();

            while (glyphCursor < baseResult.Glyphs.Count)
            {
                GlyphPlacement glyph = baseResult.Glyphs[glyphCursor];
                if (glyph.Index >= section.TextEnd)
                {
                    break;
                }

                if (glyph.TextEnd > section.TextStart)
                {
                    sectionGlyphs.Add(new GlyphPlacement(
                        glyph.Font,
                        glyph.Index,
                        glyph.TextLength,
                        glyph.CodePoint,
                        glyph.LineIndex,
                        0,
                        glyph.BaselineX,
                        glyph.BaselineY,
                        glyph.LogicalBounds,
                        glyph.VisualBounds,
                        glyph.AdvanceWidth));
                }

                glyphCursor++;
            }

            if (!baseResult.TryGetCaretFromTextIndex(section.TextStart, out TextCaret startCaret))
            {
                throw new InvalidOperationException("Failed to resolve the start caret for a text section.");
            }

            if (!baseResult.TryGetCaretFromTextIndex(section.TextEnd, out TextCaret endCaret))
            {
                throw new InvalidOperationException("Failed to resolve the end caret for a text section.");
            }

            if (sectionGlyphs.Count == 0)
            {
                FormeTextBounds emptyLogicalBounds = BuildSectionLogicalBounds(baseResult, glyphs, glyphs.Count, 0, startCaret, endCaret);
                runs.Add(new TextLayoutRun(
                    section.Format,
                    section.Format.PrimaryFont!,
                    section.TextStart,
                    section.TextLength,
                    glyphs.Count,
                    0,
                    emptyLogicalBounds,
                    FormeTextBounds.Empty,
                    startCaret.LineIndex,
                    endCaret.LineIndex - startCaret.LineIndex + 1));
                continue;
            }

            int runTextStart = section.TextStart;
            int sectionGlyphIndex = 0;
            while (sectionGlyphIndex < sectionGlyphs.Count)
            {
                FormeFont runFont = sectionGlyphs[sectionGlyphIndex].Font;
                int runIndex = runs.Count;
                int runGlyphStart = glyphs.Count;
                bool hasVisualBounds = false;
                float visualMinX = 0f;
                float visualMinY = 0f;
                float visualMaxX = 0f;
                float visualMaxY = 0f;

                while (sectionGlyphIndex < sectionGlyphs.Count && ReferenceEquals(sectionGlyphs[sectionGlyphIndex].Font, runFont))
                {
                    GlyphPlacement glyph = sectionGlyphs[sectionGlyphIndex];
                    glyphs.Add(new GlyphPlacement(
                        glyph.Font,
                        glyph.Index,
                        glyph.TextLength,
                        glyph.CodePoint,
                        glyph.LineIndex,
                        runIndex,
                        glyph.BaselineX,
                        glyph.BaselineY,
                        glyph.LogicalBounds,
                        glyph.VisualBounds,
                        glyph.AdvanceWidth));

                    if (glyph.VisualBounds.Width > 0f && glyph.VisualBounds.Height > 0f)
                    {
                        if (!hasVisualBounds)
                        {
                            visualMinX = glyph.VisualBounds.X;
                            visualMinY = glyph.VisualBounds.Y;
                            visualMaxX = glyph.VisualBounds.X2;
                            visualMaxY = glyph.VisualBounds.Y2;
                            hasVisualBounds = true;
                        }
                        else
                        {
                            if (glyph.VisualBounds.X < visualMinX)
                            {
                                visualMinX = glyph.VisualBounds.X;
                            }

                            if (glyph.VisualBounds.Y < visualMinY)
                            {
                                visualMinY = glyph.VisualBounds.Y;
                            }

                            if (glyph.VisualBounds.X2 > visualMaxX)
                            {
                                visualMaxX = glyph.VisualBounds.X2;
                            }

                            if (glyph.VisualBounds.Y2 > visualMaxY)
                            {
                                visualMaxY = glyph.VisualBounds.Y2;
                            }
                        }
                    }

                    sectionGlyphIndex++;
                }

                int runTextEnd = sectionGlyphIndex < sectionGlyphs.Count
                    ? sectionGlyphs[sectionGlyphIndex].Index
                    : section.TextEnd;
                if (!baseResult.TryGetCaretFromTextIndex(runTextStart, out TextCaret runStartCaret))
                {
                    throw new InvalidOperationException("Failed to resolve the start caret for a text run.");
                }

                if (!baseResult.TryGetCaretFromTextIndex(runTextEnd, out TextCaret runEndCaret))
                {
                    throw new InvalidOperationException("Failed to resolve the end caret for a text run.");
                }

                FormeTextBounds logicalBounds = BuildSectionLogicalBounds(baseResult, glyphs, runGlyphStart, glyphs.Count - runGlyphStart, runStartCaret, runEndCaret);
                FormeTextBounds visualBounds = hasVisualBounds
                    ? new FormeTextBounds(visualMinX, visualMinY, visualMaxX, visualMaxY)
                    : FormeTextBounds.Empty;

                runs.Add(new TextLayoutRun(
                    section.Format,
                    runFont,
                    runTextStart,
                    runTextEnd - runTextStart,
                    runGlyphStart,
                    glyphs.Count - runGlyphStart,
                    logicalBounds,
                    visualBounds,
                    runStartCaret.LineIndex,
                    runEndCaret.LineIndex - runStartCaret.LineIndex + 1));

                runTextStart = runTextEnd;
            }
        }

        List<TextLayoutLine> lines = new(baseResult.Lines.Count);
        for (int lineIndex = 0; lineIndex < baseResult.Lines.Count; lineIndex++)
        {
            TextLayoutLine line = baseResult.Lines[lineIndex];
            int runStart = 0;
            int runCount = 0;
            bool hasRun = false;

            for (int runIndex = 0; runIndex < runs.Count; runIndex++)
            {
                TextLayoutRun run = runs[runIndex];
                if (lineIndex >= run.LineStart && lineIndex < run.LineEnd)
                {
                    if (!hasRun)
                    {
                        runStart = runIndex;
                        hasRun = true;
                    }

                    runCount++;
                }
            }

            lines.Add(new TextLayoutLine(
                line.TextStart,
                line.TextLength,
                line.BaselineY,
                line.LineHeight,
                line.Ascent,
                line.Descent,
                line.Width,
                line.LogicalBounds,
                line.VisualBounds,
                line.GlyphStart,
                line.GlyphCount,
                runStart,
                runCount));
        }

        return new TextLayoutResult(baseResult.Text, baseResult.LogicalBounds, baseResult.VisualBounds, lines, runs, glyphs);
    }

    private static FormeTextBounds BuildSectionLogicalBounds(TextLayoutResult baseResult, List<GlyphPlacement> glyphs, int glyphStart, int glyphCount, TextCaret startCaret, TextCaret endCaret)
    {
        bool hasBounds = false;
        float minX = 0f;
        float minY = 0f;
        float maxX = 0f;
        float maxY = 0f;

        for (int lineIndex = startCaret.LineIndex; lineIndex <= endCaret.LineIndex; lineIndex++)
        {
            TextLayoutLine line = baseResult.Lines[lineIndex];
            float sectionMinX;
            float sectionMaxX;

            if (startCaret.LineIndex == endCaret.LineIndex)
            {
                sectionMinX = startCaret.X;
                sectionMaxX = endCaret.X;
            }
            else if (lineIndex == startCaret.LineIndex)
            {
                sectionMinX = startCaret.X;
                sectionMaxX = line.LogicalBounds.X2;
            }
            else if (lineIndex == endCaret.LineIndex)
            {
                sectionMinX = line.LogicalBounds.X;
                sectionMaxX = endCaret.X;
            }
            else
            {
                sectionMinX = line.LogicalBounds.X;
                sectionMaxX = line.LogicalBounds.X2;
            }

            bool hasLineGlyphBounds = false;
            float sectionMinY = 0f;
            float sectionMaxY = 0f;

            for (int glyphIndex = glyphStart; glyphIndex < glyphStart + glyphCount; glyphIndex++)
            {
                GlyphPlacement glyph = glyphs[glyphIndex];
                if (glyph.LineIndex != lineIndex)
                {
                    continue;
                }

                if (!hasLineGlyphBounds)
                {
                    sectionMinY = glyph.LogicalBounds.Y;
                    sectionMaxY = glyph.LogicalBounds.Y2;
                    hasLineGlyphBounds = true;
                }
                else
                {
                    if (glyph.LogicalBounds.Y < sectionMinY)
                    {
                        sectionMinY = glyph.LogicalBounds.Y;
                    }

                    if (glyph.LogicalBounds.Y2 > sectionMaxY)
                    {
                        sectionMaxY = glyph.LogicalBounds.Y2;
                    }
                }
            }

            if (!hasLineGlyphBounds)
            {
                sectionMinY = line.LogicalBounds.Y;
                sectionMaxY = line.LogicalBounds.Y2;
            }

            if (!hasBounds)
            {
                minX = sectionMinX;
                minY = sectionMinY;
                maxX = sectionMaxX;
                maxY = sectionMaxY;
                hasBounds = true;
            }
            else
            {
                if (sectionMinX < minX)
                {
                    minX = sectionMinX;
                }

                if (sectionMinY < minY)
                {
                    minY = sectionMinY;
                }

                if (sectionMaxX > maxX)
                {
                    maxX = sectionMaxX;
                }

                if (sectionMaxY > maxY)
                {
                    maxY = sectionMaxY;
                }
            }
        }

        return hasBounds
            ? new FormeTextBounds(minX, minY, maxX, maxY)
            : FormeTextBounds.Empty;
    }

    private TextLayoutResult LayoutTextCore(TextLayoutJob job, TextFormat baseFormat)
    {
        SectionLayoutInfo[] sectionInfos = BuildSectionLayoutInfos(job);
        List<JobLineLayoutInfo> codePointLines = BuildLines(job, sectionInfos);
        if (codePointLines.Count == 0)
        {
            return TextLayoutResult.Empty;
        }

        List<GlyphPlacement> placements = new();
        List<TextLayoutLine> lines = new(codePointLines.Count);

        float maxLineWidth = 0f;
        bool hasVisibleBounds = false;
        float visualMinX = 0f;
        float visualMinY = 0f;
        float visualMaxX = 0f;
        float visualMaxY = 0f;
        float cursorY = 0f;

        for (int lineIndex = 0; lineIndex < codePointLines.Count; lineIndex++)
        {
            JobLineLayoutInfo codePointLine = codePointLines[lineIndex];
            float lineWidth = MeasureLineWidth(codePointLine.Entries, sectionInfos);
            if (lineWidth > maxLineWidth)
            {
                maxLineWidth = lineWidth;
            }

            float lineOriginX = job.LayoutOptions.Alignment switch
            {
                TextHorizontalAlignment.Center => -lineWidth * 0.5f,
                TextHorizontalAlignment.Right => -lineWidth,
                _ => 0f
            };

            int glyphStart = placements.Count;
            float cursorX = lineOriginX;
            JobCodePointEntry previousEntry = default;
            bool hasPreviousGlyph = false;
            bool lineHasVisibleBounds = false;
            float lineMinX = 0f;
            float lineMinY = 0f;
            float lineMaxX = 0f;
            float lineMaxY = 0f;
            float lineAscent = 0f;
            float lineDescent = 0f;
            float lineBottom = 0f;
            float lineAdvance = 0f;

            for (int i = 0; i < codePointLine.Entries.Count; i++)
            {
                JobCodePointEntry entry = codePointLine.Entries[i];
                SectionLayoutInfo sectionInfo = sectionInfos[entry.SectionIndex];

                if (!TryGetSupportingFont(entry, sectionInfos, out FormeFont? glyphFont))
                {
                    continue;
                }

                FormeFont resolvedGlyphFont = glyphFont!;
                FormeGlyph glyph = resolvedGlyphFont.Glyphs[entry.CodePoint];
                float glyphScale = GetScale(resolvedGlyphFont, sectionInfo.Format.SizePixels);
                ScaledFontMetrics glyphMetrics = resolvedGlyphFont.GetScaledMetrics(sectionInfo.Format.SizePixels);

                if (hasPreviousGlyph)
                {
                    cursorX += GetJobPairAdvanceAdjustment(previousEntry, entry, sectionInfos);
                }

                float advance = glyph.AdvanceWidth * glyphScale + sectionInfo.CharacterSpacing;
                float glyphBaselineY = cursorY + sectionInfo.BaselineShift;
                FormeTextBounds glyphLogicalBounds = new FormeTextBounds(
                    cursorX,
                    glyphBaselineY - glyphMetrics.BaselineToTop,
                    cursorX + advance,
                    glyphBaselineY + glyphMetrics.BaselineToBottom);
                FormeTextBounds visualBounds = ComputeVisualBounds(in glyph, cursorX, glyphBaselineY, glyphScale);
                placements.Add(new GlyphPlacement(
                    resolvedGlyphFont,
                    entry.Index,
                    entry.Utf16Length,
                    entry.CodePoint,
                    lineIndex,
                    0,
                    cursorX,
                    glyphBaselineY,
                    glyphLogicalBounds,
                    visualBounds,
                    advance));

                float sectionAscent = glyphMetrics.Ascent - sectionInfo.BaselineShift;
                if (sectionAscent > lineAscent)
                {
                    lineAscent = sectionAscent;
                }

                float sectionDescent = glyphMetrics.Descent - sectionInfo.BaselineShift;
                if (sectionDescent < lineDescent)
                {
                    lineDescent = sectionDescent;
                }

                float sectionBottom = glyphMetrics.BaselineToBottom + sectionInfo.BaselineShift;
                if (sectionBottom > lineBottom)
                {
                    lineBottom = sectionBottom;
                }

                if (sectionInfo.LineAdvance > lineAdvance)
                {
                    lineAdvance = sectionInfo.LineAdvance;
                }

                if (visualBounds.Width > 0f && visualBounds.Height > 0f)
                {
                    if (!lineHasVisibleBounds)
                    {
                        lineMinX = visualBounds.X;
                        lineMinY = visualBounds.Y;
                        lineMaxX = visualBounds.X2;
                        lineMaxY = visualBounds.Y2;
                        lineHasVisibleBounds = true;
                    }
                    else
                    {
                        if (visualBounds.X < lineMinX)
                        {
                            lineMinX = visualBounds.X;
                        }

                        if (visualBounds.Y < lineMinY)
                        {
                            lineMinY = visualBounds.Y;
                        }

                        if (visualBounds.X2 > lineMaxX)
                        {
                            lineMaxX = visualBounds.X2;
                        }

                        if (visualBounds.Y2 > lineMaxY)
                        {
                            lineMaxY = visualBounds.Y2;
                        }
                    }

                    if (!hasVisibleBounds)
                    {
                        visualMinX = visualBounds.X;
                        visualMinY = visualBounds.Y;
                        visualMaxX = visualBounds.X2;
                        visualMaxY = visualBounds.Y2;
                        hasVisibleBounds = true;
                    }
                    else
                    {
                        if (visualBounds.X < visualMinX)
                        {
                            visualMinX = visualBounds.X;
                        }

                        if (visualBounds.Y < visualMinY)
                        {
                            visualMinY = visualBounds.Y;
                        }

                        if (visualBounds.X2 > visualMaxX)
                        {
                            visualMaxX = visualBounds.X2;
                        }

                        if (visualBounds.Y2 > visualMaxY)
                        {
                            visualMaxY = visualBounds.Y2;
                        }
                    }
                }

                cursorX += advance;
                previousEntry = entry;
                hasPreviousGlyph = true;
            }

            if (codePointLine.Entries.Count == 0)
            {
                SectionLayoutInfo emptyLineInfo = sectionInfos[GetSectionIndexForTextPosition(codePointLine.TextStart, job.Sections, job.Text.Length)];
                lineAscent = emptyLineInfo.Metrics.Ascent - emptyLineInfo.BaselineShift;
                lineDescent = emptyLineInfo.Metrics.Descent - emptyLineInfo.BaselineShift;
                lineBottom = emptyLineInfo.Metrics.BaselineToBottom + emptyLineInfo.BaselineShift;
                lineAdvance = emptyLineInfo.LineAdvance;
            }

            FormeTextBounds lineLogicalBounds = new FormeTextBounds(
                lineOriginX,
                cursorY - lineAscent,
                lineOriginX + lineWidth,
                cursorY + lineBottom);
            FormeTextBounds lineVisualBounds = lineHasVisibleBounds
                ? new FormeTextBounds(lineMinX, lineMinY, lineMaxX, lineMaxY)
                : FormeTextBounds.Empty;

            lines.Add(new TextLayoutLine(
                codePointLine.TextStart,
                codePointLine.TextLength,
                cursorY,
                lineAdvance,
                lineAscent,
                lineDescent,
                lineWidth,
                lineLogicalBounds,
                lineVisualBounds,
                glyphStart,
                placements.Count - glyphStart,
                0,
                1));

            cursorY += lineAdvance;
        }

        FormeTextBounds logicalBounds = BuildLogicalBounds(lines, maxLineWidth);
        FormeTextBounds visualBoundsResult = hasVisibleBounds
            ? new FormeTextBounds(visualMinX, visualMinY, visualMaxX, visualMaxY)
            : FormeTextBounds.Empty;
        List<TextLayoutRun> runs =
        [
            new TextLayoutRun(baseFormat, this, 0, job.Text.Length, 0, placements.Count, logicalBounds, visualBoundsResult, 0, lines.Count)
        ];

        return new TextLayoutResult(job.Text, logicalBounds, visualBoundsResult, lines, runs, placements);
    }

    private static TextLayoutResult ApplyGeometrySnap(TextLayoutResult result, TextGeometrySnap geometrySnap)
    {
        if (geometrySnap == TextGeometrySnap.None)
        {
            return result;
        }

        List<TextLayoutLine> lines = new(result.Lines.Count);
        for (int i = 0; i < result.Lines.Count; i++)
        {
            TextLayoutLine line = result.Lines[i];
            lines.Add(new TextLayoutLine(
                line.TextStart,
                line.TextLength,
                Snap(line.BaselineY),
                Snap(line.LineHeight),
                Snap(line.Ascent),
                Snap(line.Descent),
                Snap(line.Width),
                Snap(line.LogicalBounds),
                Snap(line.VisualBounds),
                line.GlyphStart,
                line.GlyphCount,
                line.RunStart,
                line.RunCount));
        }

        List<TextLayoutRun> runs = new(result.Runs.Count);
        for (int i = 0; i < result.Runs.Count; i++)
        {
            TextLayoutRun run = result.Runs[i];
            runs.Add(new TextLayoutRun(
                run.Format,
                run.Font,
                run.TextStart,
                run.TextLength,
                run.GlyphStart,
                run.GlyphCount,
                Snap(run.LogicalBounds),
                Snap(run.VisualBounds),
                run.LineStart,
                run.LineCount));
        }

        List<GlyphPlacement> glyphs = new(result.Glyphs.Count);
        for (int i = 0; i < result.Glyphs.Count; i++)
        {
            GlyphPlacement glyph = result.Glyphs[i];
            glyphs.Add(new GlyphPlacement(
                glyph.Font,
                glyph.Index,
                glyph.TextLength,
                glyph.CodePoint,
                glyph.LineIndex,
                glyph.RunIndex,
                Snap(glyph.BaselineX),
                Snap(glyph.BaselineY),
                Snap(glyph.LogicalBounds),
                Snap(glyph.VisualBounds),
                Snap(glyph.AdvanceWidth)));
        }

        return new TextLayoutResult(
            result.Text,
            Snap(result.LogicalBounds),
            Snap(result.VisualBounds),
            lines,
            runs,
            glyphs);
    }

    private static float Snap(float value)
    {
        return MathF.Round(value);
    }

    private static FormeTextBounds Snap(FormeTextBounds bounds)
    {
        return new FormeTextBounds(
            Snap(bounds.X),
            Snap(bounds.Y),
            Snap(bounds.X2),
            Snap(bounds.Y2));
    }

    private static FormeTextBounds BuildLogicalBounds(List<TextLayoutLine> lines, float maxLineWidth)
    {
        if (lines.Count == 0)
        {
            return FormeTextBounds.Empty;
        }

        float minY = lines[0].LogicalBounds.Y;
        float maxY = lines[0].LogicalBounds.Y2;
        for (int i = 1; i < lines.Count; i++)
        {
            TextLayoutLine line = lines[i];
            if (line.LogicalBounds.Y < minY)
            {
                minY = line.LogicalBounds.Y;
            }

            if (line.LogicalBounds.Y2 > maxY)
            {
                maxY = line.LogicalBounds.Y2;
            }
        }

        return new FormeTextBounds(0f, minY, maxLineWidth, maxY);
    }

    private List<LineLayoutInfo> BuildLines(ReadOnlySpan<char> text, float scale, in TextLayoutOptions options)
    {
        List<LineLayoutInfo> result = new();

        if (options.MaxWidth.HasValue && options.EllipsisMode != EllipsisMode.None)
        {
            BuildEllipsisLine(text, scale, in options, result);
            return result;
        }

        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int newlineAt = text[lineStart..].IndexOf('\n');
            int segEnd = newlineAt < 0 ? text.Length : lineStart + newlineAt;
            ReadOnlySpan<char> segment = text[lineStart..segEnd];

            if (options.MaxWidth.HasValue)
            {
                WrapSegment(segment, lineStart, scale, in options, result);
            }
            else
            {
                result.Add(CreateLineLayoutInfo(DecodeSegment(segment, lineStart), lineStart, segment.Length));
            }

            if (newlineAt < 0)
            {
                break;
            }

            lineStart = lineStart + newlineAt + 1;
        }

        return result;
    }

    private List<JobLineLayoutInfo> BuildLines(TextLayoutJob job, SectionLayoutInfo[] sectionInfos)
    {
        List<JobLineLayoutInfo> result = new();
        ReadOnlySpan<char> text = job.Text.AsSpan();

        if (job.LayoutOptions.MaxWidth.HasValue && job.LayoutOptions.EllipsisMode != EllipsisMode.None)
        {
            BuildEllipsisLine(text, job, sectionInfos, result);
            return result;
        }

        int lineStart = 0;
        while (lineStart <= text.Length)
        {
            int newlineAt = text[lineStart..].IndexOf('\n');
            int segEnd = newlineAt < 0 ? text.Length : lineStart + newlineAt;
            ReadOnlySpan<char> segment = text[lineStart..segEnd];

            if (job.LayoutOptions.MaxWidth.HasValue)
            {
                WrapSegment(segment, lineStart, job, sectionInfos, result);
            }
            else
            {
                result.Add(CreateLineLayoutInfo(DecodeSegment(segment, lineStart, job.Sections), lineStart, segment.Length));
            }

            if (newlineAt < 0)
            {
                break;
            }

            lineStart = lineStart + newlineAt + 1;
        }

        return result;
    }

    private void WrapSegment(
        ReadOnlySpan<char> segment,
        int segmentOffset,
        float scale,
        in TextLayoutOptions options,
        List<LineLayoutInfo> output)
    {
        float maxWidth = options.MaxWidth!.Value;

        List<CodePointEntry> chars = DecodeSegment(segment, segmentOffset);
        if (chars.Count == 0)
        {
            output.Add(new LineLayoutInfo(new List<CodePointEntry>(), segmentOffset, 0));
            return;
        }

        int lineStart = 0;
        while (lineStart < chars.Count)
        {
            float lineWidth = 0f;
            int lineEnd = lineStart;
            int lastBreakAt = -1;
            int previousCodePoint = 0;
            bool hasPreviousGlyph = false;

            while (lineEnd < chars.Count)
            {
                CodePointEntry entry = chars[lineEnd];
                float advance = MeasureIncrement(entry.CodePoint, previousCodePoint, hasPreviousGlyph, scale, options.CharacterSpacing);

                // Always take at least one character per line to avoid an infinite loop on
                // single characters that exceed maxWidth.
                if (lineEnd > lineStart && lineWidth + advance > maxWidth)
                {
                    break;
                }

                if (entry.CodePoint == ' ')
                {
                    lastBreakAt = lineEnd;
                }

                lineWidth += advance;
                if (Glyphs.ContainsKey(entry.CodePoint))
                {
                    previousCodePoint = entry.CodePoint;
                    hasPreviousGlyph = true;
                }
                lineEnd++;
            }

            int actualEnd;
            int nextStart;

            if (lineEnd >= chars.Count)
            {
                actualEnd = chars.Count;
                nextStart = chars.Count;
            }
            else if (lastBreakAt >= lineStart)
            {
                // Break at the space; the space is not included on either line.
                actualEnd = lastBreakAt;
                nextStart = lastBreakAt + 1;
            }
            else
            {
                // No break point in range; forced break.
                actualEnd = lineEnd;
                nextStart = lineEnd;
            }

            List<CodePointEntry> line = new(actualEnd - lineStart);
            for (int j = lineStart; j < actualEnd; j++)
            {
                line.Add(chars[j]);
            }

            int textStart = line.Count > 0 ? line[0].Index : segmentOffset;
            int textLength = 0;
            if (line.Count > 0)
            {
                CodePointEntry lastEntry = line[line.Count - 1];
                textLength = (lastEntry.Index + lastEntry.Utf16Length) - textStart;
            }

            output.Add(new LineLayoutInfo(line, textStart, textLength));

            lineStart = nextStart;
        }
    }

    private void WrapSegment(ReadOnlySpan<char> segment, int segmentOffset, TextLayoutJob job, SectionLayoutInfo[] sectionInfos, List<JobLineLayoutInfo> output)
    {
        float maxWidth = job.LayoutOptions.MaxWidth!.Value;

        List<JobCodePointEntry> chars = DecodeSegment(segment, segmentOffset, job.Sections);
        if (chars.Count == 0)
        {
            output.Add(new JobLineLayoutInfo(new List<JobCodePointEntry>(), segmentOffset, 0));
            return;
        }

        int lineStart = 0;
        while (lineStart < chars.Count)
        {
            float lineWidth = 0f;
            int lineEnd = lineStart;
            int lastBreakAt = -1;
            JobCodePointEntry previousEntry = default;
            bool hasPreviousGlyph = false;

            while (lineEnd < chars.Count)
            {
                JobCodePointEntry entry = chars[lineEnd];
                float advance = MeasureIncrement(entry, previousEntry, hasPreviousGlyph, sectionInfos);

                if (lineEnd > lineStart && lineWidth + advance > maxWidth)
                {
                    break;
                }

                if (entry.CodePoint == ' ')
                {
                    lastBreakAt = lineEnd;
                }

                lineWidth += advance;
                if (TryGetSupportingFont(entry, sectionInfos, out _))
                {
                    previousEntry = entry;
                    hasPreviousGlyph = true;
                }
                lineEnd++;
            }

            int actualEnd;
            int nextStart;

            if (lineEnd >= chars.Count)
            {
                actualEnd = chars.Count;
                nextStart = chars.Count;
            }
            else if (lastBreakAt >= lineStart)
            {
                actualEnd = lastBreakAt;
                nextStart = lastBreakAt + 1;
            }
            else
            {
                actualEnd = lineEnd;
                nextStart = lineEnd;
            }

            List<JobCodePointEntry> line = new(actualEnd - lineStart);
            for (int j = lineStart; j < actualEnd; j++)
            {
                line.Add(chars[j]);
            }

            int textStart = line.Count > 0 ? line[0].Index : segmentOffset;
            int textLength = 0;
            if (line.Count > 0)
            {
                JobCodePointEntry lastEntry = line[line.Count - 1];
                textLength = (lastEntry.Index + lastEntry.Utf16Length) - textStart;
            }

            output.Add(new JobLineLayoutInfo(line, textStart, textLength));

            lineStart = nextStart;
        }
    }

    private void BuildEllipsisLine(
        ReadOnlySpan<char> text,
        float scale,
        in TextLayoutOptions options,
        List<LineLayoutInfo> output)
    {
        float maxWidth = options.MaxWidth!.Value;
        string ellipsisStr = options.EllipsisString ?? "...";

        List<CodePointEntry> line = new();
        float cursorWidth = 0f;
        int truncationIndex = text.Length;
        int lastWordBoundary = 0;
        bool prevWasSpace = true;
        int previousCodePoint = 0;
        bool hasPreviousGlyph = false;

        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\n')
            {
                i++;
                continue;
            }

            Rune.DecodeFromUtf16(text[i..], out Rune rune, out int consumed);
            int cp = rune.Value;
            float advance = MeasureIncrement(cp, previousCodePoint, hasPreviousGlyph, scale, options.CharacterSpacing);

            if (cursorWidth + advance > maxWidth)
            {
                truncationIndex = i;
                break;
            }

            // Record where the last complete word ended, for Word ellipsis mode.
            if (cp == ' ' && !prevWasSpace)
            {
                lastWordBoundary = line.Count;
            }
            prevWasSpace = (cp == ' ');

            line.Add(new CodePointEntry(i, cp, consumed));
            cursorWidth += advance;
            if (Glyphs.ContainsKey(cp))
            {
                previousCodePoint = cp;
                hasPreviousGlyph = true;
            }
            i += consumed;
        }

        bool truncated = i < text.Length;

        if (truncated)
        {
            if (options.EllipsisMode == EllipsisMode.Word && lastWordBoundary > 0)
            {
                while (line.Count > lastWordBoundary)
                {
                    line.RemoveAt(line.Count - 1);
                }

                // Strip trailing spaces left before the word boundary.
                while (line.Count > 0 && line[line.Count - 1].CodePoint == ' ')
                {
                    line.RemoveAt(line.Count - 1);
                }
            }

            List<CodePointEntry> ellipsisEntries = DecodeSegment(ellipsisStr, truncationIndex);
            for (int ei = 0; ei < ellipsisEntries.Count; ei++)
            {
                line.Add(ellipsisEntries[ei]);
            }

            int ellipsisCount = ellipsisEntries.Count;
            while (line.Count > 0 && MeasureLineWidth(line, scale, options.CharacterSpacing) > maxWidth)
            {
                int removableIndex = line.Count - ellipsisCount - 1;
                if (removableIndex >= 0)
                {
                    line.RemoveAt(removableIndex);
                    continue;
                }

                line.RemoveAt(0);
                ellipsisCount--;
            }
        }

        int textStart = line.Count > 0 ? line[0].Index : 0;
        int textLength = truncationIndex > textStart ? truncationIndex - textStart : 0;
        output.Add(new LineLayoutInfo(line, textStart, textLength));
    }

    private void BuildEllipsisLine(ReadOnlySpan<char> text, TextLayoutJob job, SectionLayoutInfo[] sectionInfos, List<JobLineLayoutInfo> output)
    {
        float maxWidth = job.LayoutOptions.MaxWidth!.Value;
        string ellipsisStr = job.LayoutOptions.EllipsisString ?? "...";

        List<JobCodePointEntry> line = new();
        float cursorWidth = 0f;
        int truncationIndex = text.Length;
        int lastWordBoundary = 0;
        bool prevWasSpace = true;
        JobCodePointEntry previousEntry = default;
        bool hasPreviousGlyph = false;

        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\n')
            {
                i++;
                continue;
            }

            int sectionIndex = GetSectionIndexForTextPosition(i, job.Sections, text.Length);
            Rune.DecodeFromUtf16(text[i..], out Rune rune, out int consumed);
            JobCodePointEntry entry = new JobCodePointEntry(i, rune.Value, consumed, sectionIndex);
            float advance = MeasureIncrement(entry, previousEntry, hasPreviousGlyph, sectionInfos);

            if (cursorWidth + advance > maxWidth)
            {
                truncationIndex = i;
                break;
            }

            if (entry.CodePoint == ' ' && !prevWasSpace)
            {
                lastWordBoundary = line.Count;
            }
            prevWasSpace = entry.CodePoint == ' ';

            line.Add(entry);
            cursorWidth += advance;
            if (TryGetSupportingFont(entry, sectionInfos, out _))
            {
                previousEntry = entry;
                hasPreviousGlyph = true;
            }
            i += consumed;
        }

        bool truncated = i < text.Length;
        if (truncated)
        {
            if (job.LayoutOptions.EllipsisMode == EllipsisMode.Word && lastWordBoundary > 0)
            {
                while (line.Count > lastWordBoundary)
                {
                    line.RemoveAt(line.Count - 1);
                }

                while (line.Count > 0 && line[line.Count - 1].CodePoint == ' ')
                {
                    line.RemoveAt(line.Count - 1);
                }
            }

            int ellipsisSectionIndex = line.Count > 0
                ? line[line.Count - 1].SectionIndex
                : GetSectionIndexForTextPosition(truncationIndex, job.Sections, text.Length);
            List<JobCodePointEntry> ellipsisEntries = DecodeSegment(ellipsisStr.AsSpan(), truncationIndex, ellipsisSectionIndex);
            for (int ei = 0; ei < ellipsisEntries.Count; ei++)
            {
                line.Add(ellipsisEntries[ei]);
            }

            int ellipsisCount = ellipsisEntries.Count;
            while (line.Count > 0 && MeasureLineWidth(line, sectionInfos) > maxWidth)
            {
                int removableIndex = line.Count - ellipsisCount - 1;
                if (removableIndex >= 0)
                {
                    line.RemoveAt(removableIndex);
                    continue;
                }

                line.RemoveAt(0);
                ellipsisCount--;
            }
        }

        int textStart = line.Count > 0 ? line[0].Index : 0;
        int textLength = truncationIndex > textStart ? truncationIndex - textStart : 0;
        output.Add(new JobLineLayoutInfo(line, textStart, textLength));
    }

    private float MeasureLineWidth(List<CodePointEntry> line, float scale, float charSpacing)
    {
        float width = 0f;
        int previousCodePoint = 0;
        bool hasPreviousGlyph = false;

        foreach (CodePointEntry entry in line)
        {
            width += MeasureIncrement(entry.CodePoint, previousCodePoint, hasPreviousGlyph, scale, charSpacing);
            if (Glyphs.ContainsKey(entry.CodePoint))
            {
                previousCodePoint = entry.CodePoint;
                hasPreviousGlyph = true;
            }
        }
        return width;
    }

    private float MeasureLineWidth(List<JobCodePointEntry> line, SectionLayoutInfo[] sectionInfos)
    {
        float width = 0f;
        JobCodePointEntry previousEntry = default;
        bool hasPreviousGlyph = false;

        foreach (JobCodePointEntry entry in line)
        {
            width += MeasureIncrement(entry, previousEntry, hasPreviousGlyph, sectionInfos);
            if (TryGetSupportingFont(entry, sectionInfos, out _))
            {
                previousEntry = entry;
                hasPreviousGlyph = true;
            }
        }

        return width;
    }

    private static List<CodePointEntry> DecodeSegment(ReadOnlySpan<char> segment, int offset)
    {
        List<CodePointEntry> list = new(segment.Length);
        int i = 0;
        while (i < segment.Length)
        {
            Rune.DecodeFromUtf16(segment[i..], out Rune rune, out int consumed);
            list.Add(new CodePointEntry(offset + i, rune.Value, consumed));
            i += consumed;
        }
        return list;
    }

    private static List<JobCodePointEntry> DecodeSegment(ReadOnlySpan<char> segment, int offset, IReadOnlyList<TextSection> sections)
    {
        List<JobCodePointEntry> list = new(segment.Length);
        int i = 0;
        int sectionIndex = GetSectionIndexForTextPosition(offset, sections, int.MaxValue);

        while (i < segment.Length)
        {
            int absoluteIndex = offset + i;
            while (sectionIndex + 1 < sections.Count && absoluteIndex >= sections[sectionIndex].TextEnd)
            {
                sectionIndex++;
            }

            Rune.DecodeFromUtf16(segment[i..], out Rune rune, out int consumed);
            list.Add(new JobCodePointEntry(offset + i, rune.Value, consumed, sectionIndex));
            i += consumed;
        }

        return list;
    }

    private static List<JobCodePointEntry> DecodeSegment(ReadOnlySpan<char> segment, int offset, int sectionIndex)
    {
        List<JobCodePointEntry> list = new(segment.Length);
        int i = 0;
        while (i < segment.Length)
        {
            Rune.DecodeFromUtf16(segment[i..], out Rune rune, out int consumed);
            list.Add(new JobCodePointEntry(offset + i, rune.Value, consumed, sectionIndex));
            i += consumed;
        }

        return list;
    }

    private static LineLayoutInfo CreateLineLayoutInfo(List<CodePointEntry> entries, int textStart, int textLength)
    {
        if (entries.Count == 0)
        {
            return new LineLayoutInfo(entries, textStart, textLength);
        }

        CodePointEntry lastEntry = entries[entries.Count - 1];
        int actualStart = entries[0].Index;
        int actualLength = (lastEntry.Index + lastEntry.Utf16Length) - actualStart;
        return new LineLayoutInfo(entries, actualStart, actualLength);
    }

    private static JobLineLayoutInfo CreateLineLayoutInfo(List<JobCodePointEntry> entries, int textStart, int textLength)
    {
        if (entries.Count == 0)
        {
            return new JobLineLayoutInfo(entries, textStart, textLength);
        }

        JobCodePointEntry lastEntry = entries[entries.Count - 1];
        int actualStart = entries[0].Index;
        int actualLength = (lastEntry.Index + lastEntry.Utf16Length) - actualStart;
        return new JobLineLayoutInfo(entries, actualStart, actualLength);
    }

    private static FormeTextBounds ComputeVisualBounds(in FormeGlyph glyph, float baselineX, float baselineY, float scale)
    {
        if (glyph.BandInfo.Count == 0)
        {
            return new FormeTextBounds(baselineX, baselineY, baselineX, baselineY);
        }

        return new FormeTextBounds(
            x: baselineX + glyph.BoundingBox.X1 * scale,
            y: baselineY - glyph.BoundingBox.Y2 * scale,
            x2: baselineX + glyph.BoundingBox.X2 * scale,
            y2: baselineY - glyph.BoundingBox.Y1 * scale);
    }

    private void ValidateMissingGlyphPolicy(ReadOnlySpan<char> text, TextMissingGlyphPolicy policy)
    {
        if (policy != TextMissingGlyphPolicy.Throw)
        {
            return;
        }

        if (TryFindMissingCodePoint(text, out int textIndex, out int codePoint))
        {
            throw new InvalidOperationException(
                $"The current FormeFont does not contain a glyph for U+{codePoint:X4} at UTF-16 index {textIndex}.");
        }
    }

    private void ValidateMissingGlyphPolicy(TextLayoutJob job, TextMissingGlyphPolicy policy)
    {
        if (policy != TextMissingGlyphPolicy.Throw)
        {
            return;
        }

        ReadOnlySpan<char> text = job.Text.AsSpan();
        int textIndex = 0;
        while (textIndex < text.Length)
        {
            OperationStatus status = Rune.DecodeFromUtf16(text[textIndex..], out Rune rune, out int charsConsumed);
            if (status != OperationStatus.Done)
            {
                break;
            }

            if (rune.Value != '\n')
            {
                int sectionIndex = GetSectionIndexForTextPosition(textIndex, job.Sections, text.Length);
                if (!job.Sections[sectionIndex].Format.SupportsCodePoint(rune.Value))
                {
                    throw new InvalidOperationException(
                        $"The current text format does not contain a glyph for U+{rune.Value:X4} at UTF-16 index {textIndex}.");
                }
            }

            textIndex += charsConsumed;
        }
    }

    private static float GetScale(FormeFont font, float sizePixels)
    {
        return sizePixels / Math.Max(1, font.Metrics.UnitsPerEm);
    }

    private static bool TryGetSupportingFont(JobCodePointEntry entry, SectionLayoutInfo[] sectionInfos, out FormeFont? font)
    {
        return sectionInfos[entry.SectionIndex].Format.TryGetSupportingFont(entry.CodePoint, out font);
    }

    internal static ulong MakePairAdjustmentKey(int previousCodePoint, int currentCodePoint)
    {
        return ((ulong)(uint)previousCodePoint << 32) | (uint)currentCodePoint;
    }

    private float MeasureIncrement(int currentCodePoint, int previousCodePoint, bool hasPreviousGlyph, float scale, float charSpacing)
    {
        if (Glyphs.TryGetValue(currentCodePoint, out FormeGlyph glyph))
        {
            float width = glyph.AdvanceWidth * scale + charSpacing;
            if (hasPreviousGlyph)
            {
                width += GetPairAdvanceAdjustment(previousCodePoint, currentCodePoint, scale);
            }

            return width;
        }

        return 0f;
    }

    private float MeasureIncrement(JobCodePointEntry currentEntry, JobCodePointEntry previousEntry, bool hasPreviousGlyph, SectionLayoutInfo[] sectionInfos)
    {
        if (TryGetSupportingFont(currentEntry, sectionInfos, out FormeFont? glyphFont))
        {
            SectionLayoutInfo currentInfo = sectionInfos[currentEntry.SectionIndex];
            FormeFont resolvedGlyphFont = glyphFont!;
            FormeGlyph glyph = resolvedGlyphFont.Glyphs[currentEntry.CodePoint];
            float width = glyph.AdvanceWidth * GetScale(resolvedGlyphFont, currentInfo.Format.SizePixels) + currentInfo.CharacterSpacing;
            if (hasPreviousGlyph)
            {
                width += GetJobPairAdvanceAdjustment(previousEntry, currentEntry, sectionInfos);
            }

            return width;
        }

        return 0f;
    }

    private float GetPairAdvanceAdjustment(int previousCodePoint, int currentCodePoint, float scale)
    {
        return TryGetPairAdvanceAdjustment(previousCodePoint, currentCodePoint, out int adjustment)
            ? adjustment * scale
            : 0f;
    }

    private float GetJobPairAdvanceAdjustment(JobCodePointEntry previousEntry, JobCodePointEntry currentEntry, SectionLayoutInfo[] sectionInfos)
    {
        SectionLayoutInfo previousInfo = sectionInfos[previousEntry.SectionIndex];
        SectionLayoutInfo currentInfo = sectionInfos[currentEntry.SectionIndex];
        if (previousInfo.Format.SizePixels != currentInfo.Format.SizePixels)
        {
            return 0f;
        }

        if (!TryGetSupportingFont(previousEntry, sectionInfos, out FormeFont? previousFont)
            || !TryGetSupportingFont(currentEntry, sectionInfos, out FormeFont? currentFont)
            || !ReferenceEquals(previousFont, currentFont))
        {
            return 0f;
        }

        FormeFont resolvedPreviousFont = previousFont!;
        return resolvedPreviousFont.TryGetPairAdvanceAdjustment(previousEntry.CodePoint, currentEntry.CodePoint, out int adjustment)
            ? adjustment * GetScale(resolvedPreviousFont, currentInfo.Format.SizePixels)
            : 0f;
    }

    private SectionLayoutInfo[] BuildSectionLayoutInfos(TextLayoutJob job)
    {
        SectionLayoutInfo[] result = new SectionLayoutInfo[job.Sections.Count];
        for (int i = 0; i < job.Sections.Count; i++)
        {
            TextFormat format = job.Sections[i].Format;
            FormeFont primaryFont = format.PrimaryFont!;
            ScaledFontMetrics metrics = primaryFont.GetScaledMetrics(format.SizePixels);
            float scale = GetScale(primaryFont, format.SizePixels);
            float lineHeight = format.LineHeightPixels ?? metrics.LineHeight;
            result[i] = new SectionLayoutInfo(
                format,
                metrics,
                scale,
                lineHeight + job.LayoutOptions.LineSpacing,
                format.CharacterSpacing + job.LayoutOptions.CharacterSpacing,
                format.BaselineShift);
        }

        return result;
    }

    private static int GetSectionIndexForTextPosition(int textIndex, IReadOnlyList<TextSection> sections, int textLength)
    {
        if (sections.Count == 0)
        {
            return 0;
        }

        int resolvedIndex = textIndex;
        if (resolvedIndex >= textLength)
        {
            resolvedIndex = Math.Max(0, textLength - 1);
        }

        for (int i = 0; i < sections.Count; i++)
        {
            if (sections[i].ContainsTextIndex(resolvedIndex))
            {
                return i;
            }
        }

        return sections.Count - 1;
    }

    private readonly struct CodePointEntry
    {
        internal int Index { get; }
        internal int CodePoint { get; }
        internal int Utf16Length { get; }

        internal CodePointEntry(int index, int codePoint, int utf16Length)
        {
            Index = index;
            CodePoint = codePoint;
            Utf16Length = utf16Length;
        }
    }

    private readonly struct LineLayoutInfo
    {
        internal List<CodePointEntry> Entries { get; }
        internal int TextStart { get; }
        internal int TextLength { get; }

        internal LineLayoutInfo(List<CodePointEntry> entries, int textStart, int textLength)
        {
            Entries = entries;
            TextStart = textStart;
            TextLength = textLength;
        }
    }

    private readonly struct JobCodePointEntry
    {
        internal int Index { get; }
        internal int CodePoint { get; }
        internal int Utf16Length { get; }
        internal int SectionIndex { get; }

        internal JobCodePointEntry(int index, int codePoint, int utf16Length, int sectionIndex)
        {
            Index = index;
            CodePoint = codePoint;
            Utf16Length = utf16Length;
            SectionIndex = sectionIndex;
        }
    }

    private readonly struct JobLineLayoutInfo
    {
        internal List<JobCodePointEntry> Entries { get; }
        internal int TextStart { get; }
        internal int TextLength { get; }

        internal JobLineLayoutInfo(List<JobCodePointEntry> entries, int textStart, int textLength)
        {
            Entries = entries;
            TextStart = textStart;
            TextLength = textLength;
        }
    }

    private readonly struct SectionLayoutInfo
    {
        internal TextFormat Format { get; }
        internal ScaledFontMetrics Metrics { get; }
        internal float Scale { get; }
        internal float LineAdvance { get; }
        internal float CharacterSpacing { get; }
        internal float BaselineShift { get; }

        internal SectionLayoutInfo(
            TextFormat format,
            ScaledFontMetrics metrics,
            float scale,
            float lineAdvance,
            float characterSpacing,
            float baselineShift)
        {
            Format = format;
            Metrics = metrics;
            Scale = scale;
            LineAdvance = lineAdvance;
            CharacterSpacing = characterSpacing;
            BaselineShift = baselineShift;
        }
    }

}
