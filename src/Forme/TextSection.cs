// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Describes a format-applied section of source text within a <see cref="TextLayoutJob"/>.
/// </summary>
public readonly struct TextSection
{
    /// <summary>
    /// Gets the zero-based UTF-16 start index of the section within the source text.
    /// </summary>
    public int TextStart { get; }

    /// <summary>
    /// Gets the UTF-16 length of the section within the source text.
    /// </summary>
    public int TextLength { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 end index of the section within the source text.
    /// </summary>
    public int TextEnd => TextStart + TextLength;

    /// <summary>
    /// Gets the format applied to this section.
    /// </summary>
    public TextFormat Format { get; }

    /// <summary>
    /// Initializes a new <see cref="TextSection"/> with the given values.
    /// </summary>
    /// <param name="textStart">The zero-based UTF-16 start index of the section.</param>
    /// <param name="textLength">The UTF-16 length of the section.</param>
    /// <param name="format">The format applied to the section.</param>
    public TextSection(int textStart, int textLength, TextFormat format)
    {
        if (textStart < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textStart));
        }

        if (textLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textLength));
        }

        if (!format.IsValid)
        {
            throw new ArgumentException("Text sections require a valid TextFormat.", nameof(format));
        }

        TextStart = textStart;
        TextLength = textLength;
        Format = format;
    }

    /// <summary>
    /// Returns whether the given UTF-16 text index falls within this section.
    /// </summary>
    public bool ContainsTextIndex(int textIndex)
    {
        return textIndex >= TextStart && textIndex < TextEnd;
    }
}
