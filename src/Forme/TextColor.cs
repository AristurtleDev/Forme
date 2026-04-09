// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// Represents a simple 8-bit RGBA text color.
/// </summary>
public readonly struct TextColor : IEquatable<TextColor>
{
    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static TextColor Transparent { get; } = new TextColor(0, 0, 0, 0);

    /// <summary>
    /// Gets an opaque white color.
    /// </summary>
    public static TextColor White { get; } = new TextColor(255, 255, 255, 255);

    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Initializes a new <see cref="TextColor"/> with the given channel values.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel.</param>
    public TextColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <inheritdoc/>
    public bool Equals(TextColor other)
    {
        return R == other.R
            && G == other.G
            && B == other.B
            && A == other.A;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TextColor other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <summary>
    /// Returns whether two <see cref="TextColor"/> values are equal.
    /// </summary>
    public static bool operator ==(TextColor left, TextColor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns whether two <see cref="TextColor"/> values are not equal.
    /// </summary>
    public static bool operator !=(TextColor left, TextColor right)
    {
        return !left.Equals(right);
    }
}
