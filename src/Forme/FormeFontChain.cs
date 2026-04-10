// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Forme;

/// <summary>
/// Represents an ordered primary-plus-fallback list of <see cref="FormeFont"/> instances.
/// </summary>
public sealed class FormeFontChain
{
    private readonly FormeFont[] _fonts;

    /// <summary>
    /// Gets the primary font in the chain.
    /// </summary>
    public FormeFont PrimaryFont { get; }

    /// <summary>
    /// Gets the total number of fonts in the chain, including the primary font.
    /// </summary>
    public int Count
    {
        get { return _fonts.Length; }
    }

    /// <summary>
    /// Gets the font at the given zero-based position in the chain.
    /// </summary>
    /// <param name="index">The zero-based chain index.</param>
    /// <returns>The font at the requested position.</returns>
    public FormeFont this[int index]
    {
        get { return _fonts[index]; }
    }

    /// <summary>
    /// Initializes a new <see cref="FormeFontChain"/> with the given primary font and optional
    /// fallback fonts in lookup order.
    /// </summary>
    /// <param name="primaryFont">The primary font to use first.</param>
    /// <param name="fallbackFonts">Optional fallback fonts to try after the primary font.</param>
    public FormeFontChain(FormeFont primaryFont, IReadOnlyList<FormeFont>? fallbackFonts = null)
    {
        ArgumentNullException.ThrowIfNull(primaryFont);

        int fallbackCount = fallbackFonts?.Count ?? 0;
        _fonts = new FormeFont[1 + fallbackCount];
        _fonts[0] = primaryFont;
        PrimaryFont = primaryFont;

        for (int i = 0; i < fallbackCount; i++)
        {
            FormeFont fallbackFont = fallbackFonts![i] ?? throw new ArgumentException("Fallback font entries must not be null.", nameof(fallbackFonts));
            for (int j = 0; j <= i; j++)
            {
                if (ReferenceEquals(_fonts[j], fallbackFont))
                {
                    throw new ArgumentException("Fallback font chains must not contain duplicate FormeFont instances.", nameof(fallbackFonts));
                }
            }

            _fonts[i + 1] = fallbackFont;
        }
    }

    /// <summary>
    /// Returns whether any font in this chain supports the given Unicode code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to query.</param>
    /// <returns>
    /// <see langword="true"/> when at least one font in the chain supports the code point;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool SupportsCodePoint(int codePoint)
    {
        return TryGetSupportingFont(codePoint, out _);
    }

    /// <summary>
    /// Tries to find the first font in this chain that supports the given Unicode code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to query.</param>
    /// <param name="font">
    /// When this method returns <see langword="true"/>, contains the first supporting font in
    /// chain order.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a supporting font is found; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool TryGetSupportingFont(int codePoint, out FormeFont? font)
    {
        for (int i = 0; i < _fonts.Length; i++)
        {
            FormeFont currentFont = _fonts[i];
            if (currentFont.SupportsCodePoint(codePoint))
            {
                font = currentFont;
                return true;
            }
        }

        font = null;
        return false;
    }
}
