// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Represents a single font variation-axis coordinate.
/// </summary>
public readonly struct FontVariationCoordinate
{
    /// <summary>
    /// Gets the variation-axis tag.
    /// </summary>
    public FontVariationAxisTag Tag { get; }

    /// <summary>
    /// Gets the coordinate value for the axis.
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// Initializes a new <see cref="FontVariationCoordinate"/> from a validated axis tag.
    /// </summary>
    /// <param name="tag">The variation-axis tag.</param>
    /// <param name="value">The coordinate value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty.</exception>
    public FontVariationCoordinate(FontVariationAxisTag tag, float value)
    {
        if (tag.IsEmpty)
        {
            throw new ArgumentException("Variation coordinates require a non-empty axis tag.", nameof(tag));
        }

        Tag = tag;
        Value = value;
    }

    /// <summary>
    /// Initializes a new <see cref="FontVariationCoordinate"/> from a one- to four-character axis tag.
    /// </summary>
    /// <param name="tag">The variation-axis tag.</param>
    /// <param name="value">The coordinate value.</param>
    public FontVariationCoordinate(string tag, float value)
        : this(new FontVariationAxisTag(tag), value)
    {
    }
}
