// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
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
                    placements.Add(new GlyphPlacement(entry.Index, entry.Utf16Length, entry.CodePoint, lineIndex, 0, cursorX, cursorY, glyphLogicalBounds, visualBounds, advance));

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
            new TextLayoutRun(runFormat, 0, text.Length, 0, placements.Count, logicalBounds, visualBoundsResult, 0, lines.Count)
        ];

        return new TextLayoutResult(logicalBounds, visualBoundsResult, lines, runs, placements);
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

    private float GetPairAdvanceAdjustment(int previousCodePoint, int currentCodePoint, float scale)
    {
        return TryGetPairAdvanceAdjustment(previousCodePoint, currentCodePoint, out int adjustment)
            ? adjustment * scale
            : 0f;
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

}
