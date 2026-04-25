// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Represents an OpenType variation-axis tag.
/// </summary>
/// <remarks>
/// Tags use one to four printable ASCII characters. Tags shorter than four characters are padded
/// with trailing spaces, matching OpenType tag semantics.
/// </remarks>
public readonly struct FontVariationAxisTag : IEquatable<FontVariationAxisTag>
{
    private readonly string? _value;

    /// <summary>
    /// Gets the four-character padded tag value.
    /// </summary>
    public string Value
    {
        get { return _value ?? string.Empty; }
    }

    internal bool IsEmpty
    {
        get { return string.IsNullOrEmpty(_value); }
    }

    /// <summary>
    /// Initializes a new <see cref="FontVariationAxisTag"/> from a one- to four-character OpenType tag.
    /// </summary>
    /// <param name="tag">The variation-axis tag.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="tag"/> is empty, longer than four characters, begins with a
    /// space, contains non-printable ASCII characters, or contains non-space characters after the
    /// first space.
    /// </exception>
    public FontVariationAxisTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (tag.Length == 0 || tag.Length > 4)
        {
            throw new ArgumentException("Variation-axis tags must contain between one and four characters.", nameof(tag));
        }

        int firstSpaceIndex = -1;
        for (int i = 0; i < tag.Length; i++)
        {
            char character = tag[i];
            if (character < 0x20 || character > 0x7E)
            {
                throw new ArgumentException("Variation-axis tags must contain only printable ASCII characters.", nameof(tag));
            }

            if (character == ' ')
            {
                if (i == 0)
                {
                    throw new ArgumentException("Variation-axis tags cannot begin with a space.", nameof(tag));
                }

                if (firstSpaceIndex < 0)
                {
                    firstSpaceIndex = i;
                }
            }
            else if (firstSpaceIndex >= 0)
            {
                throw new ArgumentException("Variation-axis tags cannot contain non-space characters after the first space.", nameof(tag));
            }
        }

        _value = tag.PadRight(4, ' ');
    }

    /// <summary>
    /// Initializes a new <see cref="FontVariationAxisTag"/> from individual tag characters.
    /// </summary>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    /// <param name="third">The optional third character. Defaults to a space.</param>
    /// <param name="fourth">The optional fourth character. Defaults to a space.</param>
    /// <returns>The constructed variation-axis tag.</returns>
    public static FontVariationAxisTag Create(char first, char second, char third = ' ', char fourth = ' ')
    {
        return new FontVariationAxisTag(new string(new[] { first, second, third, fourth }));
    }

    /// <inheritdoc />
    public bool Equals(FontVariationAxisTag other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FontVariationAxisTag other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _value ?? string.Empty;
    }

    /// <summary>
    /// Compares two variation-axis tags for equality.
    /// </summary>
    public static bool operator ==(FontVariationAxisTag left, FontVariationAxisTag right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two variation-axis tags for inequality.
    /// </summary>
    public static bool operator !=(FontVariationAxisTag left, FontVariationAxisTag right)
    {
        return !left.Equals(right);
    }
}
