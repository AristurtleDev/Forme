// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Specifies how text is truncated when it exceeds <see cref="TextLayoutOptions.MaxWidth"/>.
/// </summary>
/// <remarks>
/// Ellipsis truncation is only applied when <see cref="TextLayoutOptions.MaxWidth"/> is set.
/// When truncation is active the text is treated as a single line; word wrapping is not performed.
/// </remarks>
public enum EllipsisMode
{
    /// <summary>
    /// Text is not truncated. When <see cref="TextLayoutOptions.MaxWidth"/> is set, long lines
    /// are word-wrapped instead.
    /// </summary>
    None,

    /// <summary>
    /// Text is truncated at the last character that fits within the maximum width, and the
    /// ellipsis string is appended.
    /// </summary>
    Character,

    /// <summary>
    /// Text is truncated at the last word boundary that fits within the maximum width, and the
    /// ellipsis string is appended.
    /// </summary>
    Word
}
