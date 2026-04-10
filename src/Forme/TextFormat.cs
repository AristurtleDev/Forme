// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Describes the format of a contiguous section of text.
/// </summary>
public readonly struct TextFormat
{
    /// <summary>
    /// Gets the font used for this section.
    /// </summary>
    public FormeFont? Font { get; init; }

    /// <summary>
    /// Gets the optional ordered fallback chain for this section.
    /// When present, <see cref="Font"/> remains the primary font for compatibility.
    /// </summary>
    public FormeFontChain? FontChain { get; init; }

    /// <summary>
    /// Gets the primary font used for this section, whether it comes from <see cref="Font"/> or
    /// the primary entry of <see cref="FontChain"/>.
    /// </summary>
    public FormeFont? PrimaryFont
    {
        get { return FontChain?.PrimaryFont ?? Font; }
    }

    /// <summary>
    /// Gets the em-square height in pixels used for this section.
    /// </summary>
    public float SizePixels { get; init; }

    /// <summary>
    /// Gets the foreground color for this section.
    /// </summary>
    public TextColor Color { get; init; }

    /// <summary>
    /// Gets the background color for this section.
    /// </summary>
    public TextColor BackgroundColor { get; init; }

    /// <summary>
    /// Gets the text decorations for this section.
    /// </summary>
    public TextDecorations Decorations { get; init; }

    /// <summary>
    /// Gets additional spacing in pixels added between characters in this section.
    /// </summary>
    public float CharacterSpacing { get; init; }

    /// <summary>
    /// Gets the optional explicit line height in pixels for this section.
    /// When <see langword="null"/>, the font's natural line height is used.
    /// </summary>
    public float? LineHeightPixels { get; init; }

    /// <summary>
    /// Gets the vertical baseline shift in pixels for this section.
    /// Positive values move text downward; negative values move it upward.
    /// </summary>
    public float BaselineShift { get; init; }

    /// <summary>
    /// Gets whether this format contains the minimum information needed to lay out text.
    /// </summary>
    public bool IsValid
    {
        get { return PrimaryFont != null && SizePixels > 0f; }
    }

    /// <summary>
    /// Initializes a new <see cref="TextFormat"/> with the given font and size.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    public TextFormat(FormeFont font, float sizePixels)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (sizePixels <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sizePixels));
        }

        Font = font;
        FontChain = null;
        SizePixels = sizePixels;
        Color = TextColor.White;
        BackgroundColor = TextColor.Transparent;
        Decorations = TextDecorations.None;
        CharacterSpacing = 0f;
        LineHeightPixels = null;
        BaselineShift = 0f;
    }

    /// <summary>
    /// Initializes a new <see cref="TextFormat"/> with the given font chain and size.
    /// </summary>
    /// <param name="fontChain">The ordered primary-plus-fallback font chain to use.</param>
    /// <param name="sizePixels">The em-square height in pixels.</param>
    public TextFormat(FormeFontChain fontChain, float sizePixels)
    {
        ArgumentNullException.ThrowIfNull(fontChain);

        if (sizePixels <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sizePixels));
        }

        Font = fontChain.PrimaryFont;
        FontChain = fontChain;
        SizePixels = sizePixels;
        Color = TextColor.White;
        BackgroundColor = TextColor.Transparent;
        Decorations = TextDecorations.None;
        CharacterSpacing = 0f;
        LineHeightPixels = null;
        BaselineShift = 0f;
    }
}
