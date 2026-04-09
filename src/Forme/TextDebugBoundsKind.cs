// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Identifies which text-layout element a debug-bounds entry represents.
/// </summary>
public enum TextDebugBoundsKind
{
    /// <summary>
    /// The entry represents a laid-out text row.
    /// </summary>
    Row,

    /// <summary>
    /// The entry represents a laid-out glyph.
    /// </summary>
    Glyph
}
