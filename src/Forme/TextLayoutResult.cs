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
    public static TextLayoutResult Empty { get; } = new TextLayoutResult(FormeTextBounds.Empty, FormeTextBounds.Empty, [], []);

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
    /// Gets the laid-out glyph placements in display order.
    /// </summary>
    public IReadOnlyList<GlyphPlacement> Glyphs { get; }

    internal TextLayoutResult(FormeTextBounds logicalBounds, FormeTextBounds visualBounds, IReadOnlyList<TextLayoutLine> lines, IReadOnlyList<GlyphPlacement> glyphs)
    {
        LogicalBounds = logicalBounds;
        VisualBounds = visualBounds;
        Lines = lines;
        Glyphs = glyphs;
    }
}
