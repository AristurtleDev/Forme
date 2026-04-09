// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Forme;

/// <summary>
/// The reusable output of laying out a text string with a <see cref="FormeFont"/>.
/// </summary>
public sealed class TextLayoutResult
{
    private List<TextBackgroundRect>? _backgroundRects;
    private List<TextDecorationLine>? _underlineLines;
    private List<TextDecorationLine>? _strikethroughLines;
    private List<TextDebugBounds>? _rowDebugBounds;
    private List<TextDebugBounds>? _glyphDebugBounds;

    /// <summary>
    /// Gets an empty layout result with no lines, no glyphs, and empty bounds.
    /// </summary>
    public static TextLayoutResult Empty { get; } = new TextLayoutResult(string.Empty, FormeTextBounds.Empty, FormeTextBounds.Empty, [], [], []);

    /// <summary>
    /// Gets the original source text this layout result was produced from.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the overall logical bounds of the laid-out text in pixels.
    /// </summary>
    public FormeTextBounds LogicalBounds { get; }

    /// <summary>
    /// Gets the overall visual bounds of the laid-out text in pixels.
    /// </summary>
    public FormeTextBounds VisualBounds { get; }

    /// <summary>
    /// Gets the laid-out lines in display order.
    /// </summary>
    public IReadOnlyList<TextLayoutLine> Lines { get; }

    /// <summary>
    /// Gets the laid-out style runs in display order.
    /// </summary>
    public IReadOnlyList<TextLayoutRun> Runs { get; }

    /// <summary>
    /// Gets the laid-out glyph placements in display order.
    /// </summary>
    public IReadOnlyList<GlyphPlacement> Glyphs { get; }

    /// <summary>
    /// Gets suggested row-level background rectangles for runs decorated with
    /// <see cref="TextDecorations.Background"/>.
    /// </summary>
    public IReadOnlyList<TextBackgroundRect> BackgroundRects
    {
        get
        {
            _backgroundRects ??= BuildBackgroundRects();
            return _backgroundRects;
        }
    }

    /// <summary>
    /// Gets suggested underline segments for runs decorated with
    /// <see cref="TextDecorations.Underline"/>.
    /// </summary>
    public IReadOnlyList<TextDecorationLine> UnderlineLines
    {
        get
        {
            _underlineLines ??= BuildDecorationLines(TextDecorations.Underline);
            return _underlineLines;
        }
    }

    /// <summary>
    /// Gets suggested strikethrough segments for runs decorated with
    /// <see cref="TextDecorations.Strikethrough"/>.
    /// </summary>
    public IReadOnlyList<TextDecorationLine> StrikethroughLines
    {
        get
        {
            _strikethroughLines ??= BuildDecorationLines(TextDecorations.Strikethrough);
            return _strikethroughLines;
        }
    }

    /// <summary>
    /// Gets row-level debug bounds derived directly from the laid-out line data.
    /// </summary>
    public IReadOnlyList<TextDebugBounds> RowDebugBounds
    {
        get
        {
            _rowDebugBounds ??= BuildRowDebugBounds();
            return _rowDebugBounds;
        }
    }

    /// <summary>
    /// Gets glyph-level debug bounds derived directly from the laid-out glyph data.
    /// </summary>
    public IReadOnlyList<TextDebugBounds> GlyphDebugBounds
    {
        get
        {
            _glyphDebugBounds ??= BuildGlyphDebugBounds();
            return _glyphDebugBounds;
        }
    }

    internal TextLayoutResult(
        string text,
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        IReadOnlyList<TextLayoutLine> lines,
        IReadOnlyList<TextLayoutRun> runs,
        IReadOnlyList<GlyphPlacement> glyphs)
    {
        Text = text;
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        Lines = lines;
        Runs = runs;
        Glyphs = glyphs;
    }

    /// <summary>
    /// Tries to find the line containing the given UTF-16 text index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to look up.</param>
    /// <param name="lineIndex">
    /// When this method returns <see langword="true"/>, contains the matching line index within
    /// <see cref="Lines"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the index falls within a line's source-text range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetLineIndexFromTextIndex(int textIndex, out int lineIndex)
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            TextLayoutLine line = Lines[i];
            if (line.ContainsTextIndex(textIndex))
            {
                lineIndex = i;
                return true;
            }
        }

        lineIndex = -1;
        return false;
    }

    /// <summary>
    /// Tries to find the run containing the given UTF-16 text index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to look up.</param>
    /// <param name="runIndex">
    /// When this method returns <see langword="true"/>, contains the matching run index within
    /// <see cref="Runs"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the index falls within a run's source-text range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetRunIndexFromTextIndex(int textIndex, out int runIndex)
    {
        for (int i = 0; i < Runs.Count; i++)
        {
            TextLayoutRun run = Runs[i];
            if (run.ContainsTextIndex(textIndex))
            {
                runIndex = i;
                return true;
            }
        }

        runIndex = -1;
        return false;
    }

    /// <summary>
    /// Tries to find the glyph containing the given UTF-16 text index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to look up.</param>
    /// <param name="glyphIndex">
    /// When this method returns <see langword="true"/>, contains the matching glyph index within
    /// <see cref="Glyphs"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the index falls within a glyph's source-text range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphIndexFromTextIndex(int textIndex, out int glyphIndex)
    {
        for (int i = 0; i < Glyphs.Count; i++)
        {
            GlyphPlacement glyph = Glyphs[i];
            if (glyph.ContainsTextIndex(textIndex))
            {
                glyphIndex = i;
                return true;
            }
        }

        glyphIndex = -1;
        return false;
    }

    /// <summary>
    /// Tries to resolve a caret position for the given UTF-16 text index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 insertion index to look up.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="textIndex"/> falls within the laid-out source
    /// text, including line ends and the overall end of the text; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetCaretFromTextIndex(int textIndex, out TextCaret caret)
    {
        if (textIndex < 0 || textIndex > GetTextLength())
        {
            caret = default;
            return false;
        }

        if (!TryGetCaretLine(textIndex, out int lineIndex))
        {
            caret = default;
            return false;
        }

        TextLayoutLine line = Lines[lineIndex];
        float x = GetCaretXForLine(line, textIndex);
        caret = new TextCaret(textIndex, lineIndex, x, line.BaselineY, line.LogicalBounds.Y, line.LogicalBounds.Y2);
        return true;
    }

    /// <summary>
    /// Tries to find the glyph whose logical bounds contain the given point.
    /// </summary>
    /// <param name="x">The X position, relative to the layout origin.</param>
    /// <param name="y">The Y position, relative to the layout origin.</param>
    /// <param name="glyphIndex">
    /// When this method returns <see langword="true"/>, contains the matching glyph index within
    /// <see cref="Glyphs"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the point falls within a glyph's logical bounds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphIndexFromPoint(float x, float y, out int glyphIndex)
    {
        for (int i = 0; i < Glyphs.Count; i++)
        {
            GlyphPlacement glyph = Glyphs[i];
            if (glyph.LogicalBounds.Contains(x, y))
            {
                glyphIndex = i;
                return true;
            }
        }

        glyphIndex = -1;
        return false;
    }

    /// <summary>
    /// Returns the nearest glyph index for the given point.
    /// </summary>
    /// <param name="x">The X position, relative to the layout origin.</param>
    /// <param name="y">The Y position, relative to the layout origin.</param>
    /// <returns>
    /// The nearest glyph index, or <c>-1</c> when the layout contains no glyphs.
    /// </returns>
    public int GetNearestGlyphIndexFromPoint(float x, float y)
    {
        if (Glyphs.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDistance = Glyphs[0].LogicalBounds.DistanceSquaredTo(x, y);

        for (int i = 1; i < Glyphs.Count; i++)
        {
            float distance = Glyphs[i].LogicalBounds.DistanceSquaredTo(x, y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Tries to find the line whose logical bounds contain the given Y position.
    /// </summary>
    /// <param name="y">The Y position, relative to the layout origin.</param>
    /// <param name="lineIndex">
    /// When this method returns <see langword="true"/>, contains the matching line index within
    /// <see cref="Lines"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the position falls within a line's logical bounds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetLineIndexFromY(float y, out int lineIndex)
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            TextLayoutLine line = Lines[i];
            if (y >= line.LogicalBounds.Y && y < line.LogicalBounds.Y2)
            {
                lineIndex = i;
                return true;
            }
        }

        lineIndex = -1;
        return false;
    }

    /// <summary>
    /// Returns the nearest line index for the given Y position.
    /// </summary>
    /// <param name="y">The Y position, relative to the layout origin.</param>
    /// <returns>
    /// The nearest line index, or <c>-1</c> when the layout contains no lines.
    /// </returns>
    public int GetNearestLineIndexFromY(float y)
    {
        if (Lines.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDistance = DistanceToLineY(Lines[0], y);

        for (int i = 1; i < Lines.Count; i++)
        {
            float distance = DistanceToLineY(Lines[i], y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Tries to find the line owning the given glyph index.
    /// </summary>
    /// <param name="glyphIndex">The zero-based glyph index to look up.</param>
    /// <param name="lineIndex">
    /// When this method returns <see langword="true"/>, contains the matching line index within
    /// <see cref="Lines"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="glyphIndex"/> is within range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetLineIndexFromGlyphIndex(int glyphIndex, out int lineIndex)
    {
        if ((uint)glyphIndex < (uint)Glyphs.Count)
        {
            lineIndex = Glyphs[glyphIndex].LineIndex;
            return true;
        }

        lineIndex = -1;
        return false;
    }

    /// <summary>
    /// Tries to find the run owning the given glyph index.
    /// </summary>
    /// <param name="glyphIndex">The zero-based glyph index to look up.</param>
    /// <param name="runIndex">
    /// When this method returns <see langword="true"/>, contains the matching run index within
    /// <see cref="Runs"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="glyphIndex"/> is within range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetRunIndexFromGlyphIndex(int glyphIndex, out int runIndex)
    {
        if ((uint)glyphIndex < (uint)Glyphs.Count)
        {
            runIndex = Glyphs[glyphIndex].RunIndex;
            return true;
        }

        runIndex = -1;
        return false;
    }

    /// <summary>
    /// Tries to resolve the caret at the start of the given line.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index to query.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="lineIndex"/> is within range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetLineStartCaret(int lineIndex, out TextCaret caret)
    {
        if ((uint)lineIndex >= (uint)Lines.Count)
        {
            caret = default;
            return false;
        }

        return TryGetCaretFromTextIndex(Lines[lineIndex].TextStart, out caret);
    }

    /// <summary>
    /// Tries to resolve the caret at the end of the given line.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index to query.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="lineIndex"/> is within range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetLineEndCaret(int lineIndex, out TextCaret caret)
    {
        if ((uint)lineIndex >= (uint)Lines.Count)
        {
            caret = default;
            return false;
        }

        return TryGetCaretFromTextIndex(Lines[lineIndex].TextEnd, out caret);
    }

    /// <summary>
    /// Tries to resolve the nearest caret on the given line for the requested X position.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index to query.</param>
    /// <param name="x">The preferred X position, relative to the layout origin.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="lineIndex"/> is within range; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetCaretFromLineX(int lineIndex, float x, out TextCaret caret)
    {
        if ((uint)lineIndex >= (uint)Lines.Count)
        {
            caret = default;
            return false;
        }

        TextLayoutLine line = Lines[lineIndex];
        if (line.GlyphCount == 0)
        {
            caret = new TextCaret(line.TextStart, lineIndex, line.LogicalBounds.X, line.BaselineY, line.LogicalBounds.Y, line.LogicalBounds.Y2);
            return true;
        }

        if (x <= line.LogicalBounds.X)
        {
            return TryGetCaretFromTextIndex(line.TextStart, out caret);
        }

        if (x >= line.LogicalBounds.X2)
        {
            return TryGetCaretFromTextIndex(line.TextEnd, out caret);
        }

        for (int glyphIndex = line.GlyphStart; glyphIndex < line.GlyphEnd; glyphIndex++)
        {
            GlyphPlacement glyph = Glyphs[glyphIndex];
            float midpoint = glyph.BaselineX + glyph.AdvanceWidth * 0.5f;
            if (x < midpoint)
            {
                return TryGetCaretFromTextIndex(glyph.Index, out caret);
            }

            if (x <= glyph.LogicalBounds.X2)
            {
                return TryGetCaretFromTextIndex(glyph.TextEnd, out caret);
            }
        }

        return TryGetCaretFromTextIndex(line.TextEnd, out caret);
    }

    /// <summary>
    /// Tries to resolve a caret on a vertically adjacent line using the current caret X position
    /// as the preferred horizontal location.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 insertion index to move from.</param>
    /// <param name="lineDelta">The signed line offset to apply.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the source index and target line are valid; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetAdjacentLineCaret(int textIndex, int lineDelta, out TextCaret caret)
    {
        if (!TryGetCaretFromTextIndex(textIndex, out TextCaret currentCaret))
        {
            caret = default;
            return false;
        }

        return TryGetAdjacentLineCaret(textIndex, lineDelta, currentCaret.X, out caret);
    }

    /// <summary>
    /// Tries to resolve a caret on a vertically adjacent line using the supplied preferred X
    /// position.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 insertion index to move from.</param>
    /// <param name="lineDelta">The signed line offset to apply.</param>
    /// <param name="preferredX">The preferred X position, relative to the layout origin.</param>
    /// <param name="caret">
    /// When this method returns <see langword="true"/>, contains the resolved caret geometry.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the source index and target line are valid; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetAdjacentLineCaret(int textIndex, int lineDelta, float preferredX, out TextCaret caret)
    {
        if (!TryGetCaretFromTextIndex(textIndex, out TextCaret currentCaret))
        {
            caret = default;
            return false;
        }

        int targetLineIndex = currentCaret.LineIndex + lineDelta;
        if ((uint)targetLineIndex >= (uint)Lines.Count)
        {
            caret = default;
            return false;
        }

        return TryGetCaretFromLineX(targetLineIndex, preferredX, out caret);
    }

    /// <summary>
    /// Returns the previous word boundary at or before the given UTF-16 index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to search from.</param>
    /// <returns>The resolved previous word boundary.</returns>
    public int GetPreviousWordBoundary(int textIndex)
    {
        ValidateTextIndex(textIndex);

        if (textIndex == 0 || Text.Length == 0)
        {
            return 0;
        }

        int position = textIndex;
        if (!TryGetRuneBefore(position, out Rune current, out int currentStart))
        {
            return 0;
        }

        if (IsWordRune(current))
        {
            return FindWordStart(currentStart);
        }

        position = currentStart;
        while (position > 0)
        {
            if (!TryGetRuneBefore(position, out Rune previous, out int previousStart))
            {
                return 0;
            }

            if (IsWordRune(previous))
            {
                return FindWordStart(previousStart);
            }

            position = previousStart;
        }

        return 0;
    }

    /// <summary>
    /// Returns the next word boundary at or after the given UTF-16 index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to search from.</param>
    /// <returns>The resolved next word boundary.</returns>
    public int GetNextWordBoundary(int textIndex)
    {
        ValidateTextIndex(textIndex);

        if (textIndex >= Text.Length || Text.Length == 0)
        {
            return Text.Length;
        }

        int position = textIndex;
        if (!TryGetRuneAt(position, out Rune current, out int currentLength))
        {
            return Text.Length;
        }

        if (IsWordRune(current))
        {
            position += currentLength;
            while (position < Text.Length)
            {
                if (!TryGetRuneAt(position, out Rune next, out int nextLength))
                {
                    break;
                }

                if (next.Value == '.')
                {
                    return position;
                }

                if (!IsWordRune(next))
                {
                    return position;
                }

                position += nextLength;
            }

            return Text.Length;
        }

        position += currentLength;
        while (position < Text.Length)
        {
            if (!TryGetRuneAt(position, out Rune next, out int nextLength))
            {
                break;
            }

            if (IsWordRune(next))
            {
                position += nextLength;
                while (position < Text.Length)
                {
                    if (!TryGetRuneAt(position, out Rune wordNext, out int wordNextLength))
                    {
                        break;
                    }

                    if (wordNext.Value == '.')
                    {
                        return position;
                    }

                    if (!IsWordRune(wordNext))
                    {
                        return position;
                    }

                    position += wordNextLength;
                }

                return Text.Length;
            }

            if (next.Value == '.')
            {
                return position;
            }

            position += nextLength;
        }

        return Text.Length;
    }

    /// <summary>
    /// Tries to resolve the word-like range nearest the given UTF-16 index.
    /// </summary>
    /// <param name="textIndex">The zero-based UTF-16 index to query.</param>
    /// <param name="wordStart">
    /// When this method returns <see langword="true"/>, contains the resolved word start.
    /// </param>
    /// <param name="wordEnd">
    /// When this method returns <see langword="true"/>, contains the resolved word end.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a non-empty range could be resolved; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetWordRange(int textIndex, out int wordStart, out int wordEnd)
    {
        ValidateTextIndex(textIndex);

        wordStart = textIndex;
        wordEnd = textIndex;
        if (Text.Length == 0)
        {
            return false;
        }

        bool hasBefore = TryGetRuneBefore(textIndex, out Rune before, out int _);
        bool hasAfter = TryGetRuneAt(textIndex, out Rune after, out int _);

        if (textIndex == 0)
        {
            wordEnd = GetNextWordBoundary(textIndex);
            return wordEnd > wordStart;
        }

        if (hasBefore && hasAfter)
        {
            if (IsWordRune(before))
            {
                wordStart = GetPreviousWordBoundary(textIndex);
                wordEnd = GetNextWordBoundary(wordStart);
                return wordEnd > wordStart;
            }

            if (IsWordRune(after))
            {
                wordStart = textIndex;
                wordEnd = GetNextWordBoundary(textIndex);
                return wordEnd > wordStart;
            }

            wordStart = GetPreviousWordBoundary(textIndex);
            wordEnd = GetNextWordBoundary(textIndex);
            return wordEnd > wordStart;
        }

        if (hasBefore)
        {
            wordStart = GetPreviousWordBoundary(textIndex);
            wordEnd = textIndex;
            return wordEnd > wordStart;
        }

        if (hasAfter)
        {
            wordEnd = GetNextWordBoundary(textIndex);
            return wordEnd > wordStart;
        }

        return false;
    }

    /// <summary>
    /// Returns suggested selection rectangles for the given UTF-16 range.
    /// </summary>
    /// <param name="textStart">The zero-based UTF-16 start index of the selection.</param>
    /// <param name="textEnd">The zero-based UTF-16 end index of the selection.</param>
    /// <returns>
    /// One rectangle per touched line, in display order. Empty selections return no rectangles.
    /// </returns>
    public IReadOnlyList<TextSelectionRect> GetSelectionRects(int textStart, int textEnd)
    {
        int resolvedStart = textStart;
        int resolvedEnd = textEnd;
        if (resolvedStart > resolvedEnd)
        {
            resolvedStart = textEnd;
            resolvedEnd = textStart;
        }

        int textLength = GetTextLength();
        if (resolvedStart < 0 || resolvedStart > textLength)
        {
            throw new ArgumentOutOfRangeException(nameof(textStart));
        }

        if (resolvedEnd < 0 || resolvedEnd > textLength)
        {
            throw new ArgumentOutOfRangeException(nameof(textEnd));
        }

        if (resolvedStart == resolvedEnd || Lines.Count == 0)
        {
            return [];
        }

        if (!TryGetCaretFromTextIndex(resolvedStart, out TextCaret startCaret))
        {
            throw new InvalidOperationException("Failed to resolve the start caret for the selection.");
        }

        if (!TryGetCaretFromTextIndex(resolvedEnd, out TextCaret endCaret))
        {
            throw new InvalidOperationException("Failed to resolve the end caret for the selection.");
        }

        List<TextSelectionRect> rects = new(endCaret.LineIndex - startCaret.LineIndex + 1);
        for (int lineIndex = startCaret.LineIndex; lineIndex <= endCaret.LineIndex; lineIndex++)
        {
            TextLayoutLine line = Lines[lineIndex];
            int lineSelectionStart = lineIndex == startCaret.LineIndex ? resolvedStart : line.TextStart;
            int lineSelectionEnd = lineIndex == endCaret.LineIndex ? resolvedEnd : line.TextEnd;
            if (lineSelectionEnd <= lineSelectionStart)
            {
                continue;
            }

            float minX = lineIndex == startCaret.LineIndex ? startCaret.X : line.LogicalBounds.X;
            float maxX = lineIndex == endCaret.LineIndex ? endCaret.X : line.LogicalBounds.X2;
            if (maxX <= minX)
            {
                continue;
            }

            rects.Add(new TextSelectionRect(
                lineSelectionStart,
                lineSelectionEnd - lineSelectionStart,
                lineIndex,
                new FormeTextBounds(
                    minX,
                    line.LogicalBounds.Y,
                    maxX,
                    line.LogicalBounds.Y2)));
        }

        return rects;
    }

    private int GetTextLength()
    {
        if (Runs.Count > 0)
        {
            return Runs[Runs.Count - 1].TextEnd;
        }

        if (Lines.Count > 0)
        {
            return Lines[Lines.Count - 1].TextEnd;
        }

        return 0;
    }

    private void ValidateTextIndex(int textIndex)
    {
        if (textIndex < 0 || textIndex > Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(textIndex));
        }
    }

    private int FindWordStart(int textIndex)
    {
        int position = textIndex;
        while (position > 0)
        {
            if (!TryGetRuneBefore(position, out Rune previous, out int previousStart) || !IsWordRune(previous))
            {
                break;
            }

            position = previousStart;
        }

        return position;
    }

    private bool TryGetRuneAt(int textIndex, out Rune rune, out int runeLength)
    {
        if ((uint)textIndex >= (uint)Text.Length)
        {
            rune = default;
            runeLength = 0;
            return false;
        }

        OperationStatus status = Rune.DecodeFromUtf16(Text.AsSpan(textIndex), out rune, out int charsConsumed);
        if (status != OperationStatus.Done)
        {
            rune = default;
            runeLength = 0;
            return false;
        }

        runeLength = charsConsumed;
        return true;
    }

    private bool TryGetRuneBefore(int textIndex, out Rune rune, out int runeStart)
    {
        if (textIndex <= 0 || textIndex > Text.Length)
        {
            rune = default;
            runeStart = 0;
            return false;
        }

        OperationStatus status = Rune.DecodeLastFromUtf16(Text.AsSpan(0, textIndex), out rune, out int charsConsumed);
        if (status != OperationStatus.Done)
        {
            rune = default;
            runeStart = 0;
            return false;
        }

        runeStart = textIndex - charsConsumed;
        return true;
    }

    private static bool IsWordRune(Rune rune)
    {
        return Rune.IsLetterOrDigit(rune) || rune.Value == '_';
    }

    private bool TryGetCaretLine(int textIndex, out int lineIndex)
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            TextLayoutLine line = Lines[i];
            if (textIndex >= line.TextStart && textIndex <= line.TextEnd)
            {
                lineIndex = i;
                return true;
            }
        }

        lineIndex = -1;
        return false;
    }

    private float GetCaretXForLine(TextLayoutLine line, int textIndex)
    {
        if (line.GlyphCount == 0)
        {
            return line.LogicalBounds.X;
        }

        for (int i = line.GlyphStart; i < line.GlyphEnd; i++)
        {
            GlyphPlacement glyph = Glyphs[i];
            if (textIndex <= glyph.Index)
            {
                return glyph.BaselineX;
            }

            if (textIndex < glyph.TextEnd)
            {
                return GetCaretXWithinGlyph(glyph, textIndex);
            }

            if (textIndex == glyph.TextEnd)
            {
                return glyph.LogicalBounds.X2;
            }
        }

        return line.LogicalBounds.X2;
    }

    private static float GetCaretXWithinGlyph(GlyphPlacement glyph, int textIndex)
    {
        int distanceToLeadingEdge = textIndex - glyph.Index;
        int distanceToTrailingEdge = glyph.TextEnd - textIndex;

        return distanceToLeadingEdge < distanceToTrailingEdge
            ? glyph.BaselineX
            : glyph.LogicalBounds.X2;
    }

    private static float DistanceToLineY(TextLayoutLine line, float y)
    {
        if (y < line.LogicalBounds.Y)
        {
            return line.LogicalBounds.Y - y;
        }

        if (y >= line.LogicalBounds.Y2)
        {
            return y - line.LogicalBounds.Y2;
        }

        return 0f;
    }

    private List<TextBackgroundRect> BuildBackgroundRects()
    {
        if (Runs.Count == 0 || Lines.Count == 0)
        {
            return [];
        }

        List<TextBackgroundRect> result = new();
        for (int runIndex = 0; runIndex < Runs.Count; runIndex++)
        {
            TextLayoutRun run = Runs[runIndex];
            if ((run.Decorations & TextDecorations.Background) == 0
                || run.Format.BackgroundColor.A == 0
                || run.TextLength == 0)
            {
                continue;
            }

            for (int lineIndex = run.LineStart; lineIndex < run.LineEnd; lineIndex++)
            {
                TextLayoutLine line = Lines[lineIndex];
                int segmentStart = Math.Max(run.TextStart, line.TextStart);
                int segmentEnd = Math.Min(run.TextEnd, line.TextEnd);
                if (segmentEnd <= segmentStart)
                {
                    continue;
                }

                FormeTextBounds bounds = new FormeTextBounds(
                    GetCaretXForLine(line, segmentStart),
                    line.LogicalBounds.Y,
                    GetCaretXForLine(line, segmentEnd),
                    line.LogicalBounds.Y2);
                if (bounds.Width <= 0f || bounds.Height <= 0f)
                {
                    continue;
                }

                AddOrMergeBackgroundRect(
                    result,
                    new TextBackgroundRect(
                        run.Format.BackgroundColor,
                        segmentStart,
                        segmentEnd - segmentStart,
                        lineIndex,
                        bounds));
            }
        }

        return result;
    }

    private static void AddOrMergeBackgroundRect(List<TextBackgroundRect> rects, TextBackgroundRect rect)
    {
        if (rects.Count == 0)
        {
            rects.Add(rect);
            return;
        }

        TextBackgroundRect previous = rects[rects.Count - 1];
        if (previous.LineIndex != rect.LineIndex
            || previous.Color != rect.Color
            || previous.Bounds.Y != rect.Bounds.Y
            || previous.Bounds.Y2 != rect.Bounds.Y2
            || rect.Bounds.X > previous.Bounds.X2)
        {
            rects.Add(rect);
            return;
        }

        rects[rects.Count - 1] = new TextBackgroundRect(
            previous.Color,
            previous.TextStart,
            rect.TextEnd - previous.TextStart,
            previous.LineIndex,
            new FormeTextBounds(
                previous.Bounds.X,
                previous.Bounds.Y,
                Math.Max(previous.Bounds.X2, rect.Bounds.X2),
                previous.Bounds.Y2));
    }

    private List<TextDecorationLine> BuildDecorationLines(TextDecorations decoration)
    {
        if (Runs.Count == 0 || Glyphs.Count == 0)
        {
            return [];
        }

        List<TextDecorationLine> result = new();
        for (int runIndex = 0; runIndex < Runs.Count; runIndex++)
        {
            TextLayoutRun run = Runs[runIndex];
            if ((run.Decorations & decoration) == 0 || run.GlyphCount == 0)
            {
                continue;
            }

            for (int glyphIndex = run.GlyphStart; glyphIndex < run.GlyphEnd; glyphIndex++)
            {
                GlyphPlacement glyph = Glyphs[glyphIndex];
                float y = GetDecorationY(glyph, decoration);
                TextDecorationLine line = new TextDecorationLine(
                    decoration,
                    run.Format.Color,
                    glyph.Index,
                    glyph.TextLength,
                    glyph.LineIndex,
                    glyph.BaselineX,
                    glyph.LogicalBounds.X2,
                    y,
                    1f);
                AddOrMergeDecorationLine(result, line);
            }
        }

        return result;
    }

    private static float GetDecorationY(GlyphPlacement glyph, TextDecorations decoration)
    {
        return decoration switch
        {
            TextDecorations.Underline => glyph.LogicalBounds.Y2,
            TextDecorations.Strikethrough => glyph.LogicalBounds.Y + glyph.LogicalBounds.Height * 0.5f,
            _ => glyph.LogicalBounds.Y2
        };
    }

    private static void AddOrMergeDecorationLine(List<TextDecorationLine> lines, TextDecorationLine line)
    {
        if (line.Width <= 0f)
        {
            return;
        }

        if (lines.Count == 0)
        {
            lines.Add(line);
            return;
        }

        TextDecorationLine previous = lines[lines.Count - 1];
        if (previous.Decoration != line.Decoration
            || previous.LineIndex != line.LineIndex
            || previous.Color != line.Color
            || previous.Y != line.Y
            || previous.Thickness != line.Thickness
            || line.X > previous.X2)
        {
            lines.Add(line);
            return;
        }

        lines[lines.Count - 1] = new TextDecorationLine(
            previous.Decoration,
            previous.Color,
            previous.TextStart,
            line.TextEnd - previous.TextStart,
            previous.LineIndex,
            previous.X,
            Math.Max(previous.X2, line.X2),
            previous.Y,
            previous.Thickness);
    }

    private List<TextDebugBounds> BuildRowDebugBounds()
    {
        List<TextDebugBounds> result = new(Lines.Count);
        for (int i = 0; i < Lines.Count; i++)
        {
            TextLayoutLine line = Lines[i];
            result.Add(new TextDebugBounds(
                TextDebugBoundsKind.Row,
                i,
                line.TextStart,
                line.TextLength,
                line.LogicalBounds,
                line.VisualBounds));
        }

        return result;
    }

    private List<TextDebugBounds> BuildGlyphDebugBounds()
    {
        List<TextDebugBounds> result = new(Glyphs.Count);
        for (int i = 0; i < Glyphs.Count; i++)
        {
            GlyphPlacement glyph = Glyphs[i];
            result.Add(new TextDebugBounds(
                TextDebugBoundsKind.Glyph,
                i,
                glyph.Index,
                glyph.TextLength,
                glyph.LogicalBounds,
                glyph.VisualBounds));
        }

        return result;
    }
}
