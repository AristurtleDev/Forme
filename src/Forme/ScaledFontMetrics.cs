// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Vertical font metrics scaled into pixel space for a specific text size.
/// </summary>
/// <remarks>
/// Coordinates follow the same convention as the rest of Forme's layout API: positive X extends
/// rightward, positive Y extends downward, and the baseline sits at Y = 0 for the first line.
/// <see cref="Ascent"/> remains positive and extends upward from the baseline, while
/// <see cref="Descent"/> remains negative and extends downward from the baseline.
/// </remarks>
public readonly struct ScaledFontMetrics
{
    /// <summary>
    /// Gets the ascent in pixels. This is the distance from the baseline to the top of the em box.
    /// </summary>
    public float Ascent { get; }

    /// <summary>
    /// Gets the descent in pixels. This value is typically negative.
    /// </summary>
    public float Descent { get; }

    /// <summary>
    /// Gets the line gap in pixels.
    /// </summary>
    public float LineGap { get; }

    /// <summary>
    /// Gets the natural baseline-to-baseline distance in pixels.
    /// </summary>
    public float LineHeight { get; }

    /// <summary>
    /// Gets the distance from the baseline to the top edge of the logical line box in pixels.
    /// </summary>
    public float BaselineToTop { get; }

    /// <summary>
    /// Gets the distance from the baseline to the bottom edge of the logical line box in pixels.
    /// </summary>
    public float BaselineToBottom { get; }

    /// <summary>
    /// Initializes a new <see cref="ScaledFontMetrics"/> with the given values.
    /// </summary>
    public ScaledFontMetrics(float ascent, float descent, float lineGap)
    {
        Ascent = ascent;
        Descent = descent;
        LineGap = lineGap;
        LineHeight = ascent - descent + lineGap;
        BaselineToTop = ascent;
        BaselineToBottom = -descent;
    }
}
