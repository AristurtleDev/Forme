// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Vertical metrics for a font, expressed in font design units.
/// </summary>
/// <remarks>
/// All values are in the font's native unit system. To convert to pixels, multiply by
/// <c>sizePixels / UnitsPerEm</c>.
/// </remarks>
public readonly struct FontMetrics
{
    /// <summary>
    /// Gets the number of design units per em square. Typically 1000 (PostScript) or 2048 (TrueType).
    /// </summary>
    public int UnitsPerEm { get; }

    /// <summary>
    /// Gets the distance from the baseline to the top of the em square, in font units.
    /// Positive values extend upward.
    /// </summary>
    public int Ascent { get; }

    /// <summary>
    /// Gets the distance from the baseline to the bottom of the em square, in font units.
    /// Typically negative (extends below the baseline).
    /// </summary>
    public int Descent { get; }

    /// <summary>
    /// Gets the recommended additional line spacing beyond ascent and descent, in font units.
    /// </summary>
    public int LineGap { get; }

    /// <summary>
    /// Initializes a new <see cref="FontMetrics"/> with the given values.
    /// </summary>
    public FontMetrics(int unitsPerEm, int ascent, int descent, int lineGap)
    {
        UnitsPerEm = unitsPerEm;
        Ascent = ascent;
        Descent = descent;
        LineGap = lineGap;
    }
}
