// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Forme;

/// <summary>
/// The reusable output of laying out a text string with a <see cref="FormeFont"/>.
/// </summary>
public sealed class TextLayoutResult
{
    /// <summary>
    /// Gets an empty layout result with no lines, no glyphs, and empty bounds.
    /// </summary>
    public static TextLayoutResult Empty { get; } = new TextLayoutResult(FormeTextBounds.Empty, FormeTextBounds.Empty, [], [], []);

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

    internal TextLayoutResult(
        FormeTextBounds logicalBounds,
        FormeTextBounds visualBounds,
        IReadOnlyList<TextLayoutLine> lines,
        IReadOnlyList<TextLayoutRun> runs,
        IReadOnlyList<GlyphPlacement> glyphs)
    {
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
}
