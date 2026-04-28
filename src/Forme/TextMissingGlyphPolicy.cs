// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Controls how text layout handles code points that are not available in the active
/// <see cref="FormeFont"/>.
/// </summary>
public enum TextMissingGlyphPolicy
{
    /// <summary>
    /// Substitute a visible fallback glyph, preferring U+FFFD, and otherwise continue layout
    /// without throwing.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Throw an exception when a missing glyph is encountered.
    /// </summary>
    Throw = 1
}
