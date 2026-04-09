// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Describes a text selection range using anchor and focus UTF-16 indices.
/// </summary>
public readonly struct TextSelectionRange
{
    /// <summary>
    /// Gets the zero-based UTF-16 anchor index of the selection.
    /// </summary>
    public int AnchorTextIndex { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 focus index of the selection.
    /// </summary>
    public int FocusTextIndex { get; }

    /// <summary>
    /// Gets the inclusive start of the selection range, independent of anchor/focus order.
    /// </summary>
    public int Start => Math.Min(AnchorTextIndex, FocusTextIndex);

    /// <summary>
    /// Gets the exclusive end of the selection range, independent of anchor/focus order.
    /// </summary>
    public int End => Math.Max(AnchorTextIndex, FocusTextIndex);

    /// <summary>
    /// Gets whether this selection range is empty.
    /// </summary>
    public bool IsEmpty => AnchorTextIndex == FocusTextIndex;

    /// <summary>
    /// Initializes a new <see cref="TextSelectionRange"/> with the given anchor and focus
    /// indices.
    /// </summary>
    /// <param name="anchorTextIndex">The zero-based UTF-16 anchor index.</param>
    /// <param name="focusTextIndex">The zero-based UTF-16 focus index.</param>
    public TextSelectionRange(int anchorTextIndex, int focusTextIndex)
    {
        AnchorTextIndex = anchorTextIndex;
        FocusTextIndex = focusTextIndex;
    }

    /// <summary>
    /// Returns a new selection range that preserves this range's anchor and replaces the focus.
    /// </summary>
    /// <param name="focusTextIndex">The zero-based UTF-16 focus index to use.</param>
    /// <returns>The updated selection range.</returns>
    public TextSelectionRange WithFocus(int focusTextIndex)
    {
        return new TextSelectionRange(AnchorTextIndex, focusTextIndex);
    }

    /// <summary>
    /// Returns an empty selection range collapsed to this range's anchor.
    /// </summary>
    /// <returns>The collapsed selection range.</returns>
    public TextSelectionRange CollapseToAnchor()
    {
        return new TextSelectionRange(AnchorTextIndex, AnchorTextIndex);
    }

    /// <summary>
    /// Returns an empty selection range collapsed to this range's focus.
    /// </summary>
    /// <returns>The collapsed selection range.</returns>
    public TextSelectionRange CollapseToFocus()
    {
        return new TextSelectionRange(FocusTextIndex, FocusTextIndex);
    }
}
