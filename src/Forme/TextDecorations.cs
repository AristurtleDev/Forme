// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Describes which text decorations apply to a layout run.
/// </summary>
[Flags]
public enum TextDecorations
{
    /// <summary>
    /// No decorations are applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// The run should be underlined.
    /// </summary>
    Underline = 1 << 0,

    /// <summary>
    /// The run should be drawn with a strikethrough.
    /// </summary>
    Strikethrough = 1 << 1,

    /// <summary>
    /// The run should be drawn with a background highlight.
    /// </summary>
    Background = 1 << 2
}
