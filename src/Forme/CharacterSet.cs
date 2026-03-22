// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Forme;

/// <summary>
/// Specifies the set of Unicode code points to process when loading a font from a TTF file.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="CharacterSet"/> is immutable and contains a sorted, deduplicated list of
/// Unicode code points. Use the static factory methods to create instances from common
/// presets, ranges, or arbitrary strings.
/// </para>
/// <para>
/// Code points not present in the font are silently skipped during processing.
/// </para>
/// </remarks>
public sealed class CharacterSet : IEnumerable<int>
{
    // TODO: Add other sets
    /// <summary>
    /// Gets a <see cref="CharacterSet"/> covering printable ASCII: code points 32 (space) through 126 (tilde).
    /// </summary>
    public static CharacterSet Ascii { get; } = Range(32, 126);

    /// <summary>
    /// Gets a <see cref="CharacterSet"/> covering the Unicode Basic Latin block: code points 0 through 127.
    /// </summary>
    public static CharacterSet BasicLatin { get; } = Range(0, 127);

    private readonly int[] _codepoints;

    private CharacterSet(int[] codepoints)
    {
        _codepoints = codepoints;
    }

    /// <summary>
    /// Gets the number of code points in this set.
    /// </summary>
    public int Count => _codepoints.Length;

    /// <summary>
    /// Gets the sorted code points in this set as a read-only span.
    /// </summary>
    public ReadOnlySpan<int> Codepoints => _codepoints;

    /// <summary>
    /// Creates a <see cref="CharacterSet"/> containing all code points in the inclusive range
    /// [<paramref name="start"/>, <paramref name="end"/>].
    /// </summary>
    /// <param name="start">The first code point in the range. Must be non-negative and not greater than <paramref name="end"/>.</param>
    /// <param name="end">The last code point in the range, inclusive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start"/> is negative or <paramref name="start"/> is greater than <paramref name="end"/>.
    /// </exception>
    public static CharacterSet Range(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start);

        int[] codepoints = new int[end - start + 1];
        for (int i = 0; i < codepoints.Length; i++)
        {
            codepoints[i] = start + i;
        }

        return new CharacterSet(codepoints);
    }

    /// <summary>
    /// Creates a <see cref="CharacterSet"/> containing the unique code points found in
    /// <paramref name="chars"/>, sorted in ascending order.
    /// </summary>
    /// <param name="chars">
    /// The characters to extract code points from. Surrogate pairs are decoded to their
    /// corresponding supplementary code points. Must not be empty.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chars"/> is empty.</exception>
    public static CharacterSet FromString(ReadOnlySpan<char> chars)
    {
        if (chars.IsEmpty)
        {
            throw new ArgumentException("chars must not be empty.", nameof(chars));
        }

        SortedSet<int> set = [];
        int i = 0;

        while (i < chars.Length)
        {
            Rune.DecodeFromUtf16(chars[i..], out Rune rune, out int charsConsumed);
            set.Add(rune.Value);
            i += charsConsumed;
        }

        int[] codepoints = new int[set.Count];
        set.CopyTo(codepoints);

        return new CharacterSet(codepoints);
    }

    /// <summary>
    /// Creates a <see cref="CharacterSet"/> that is the union of all provided sets,
    /// sorted and deduplicated.
    /// </summary>
    /// <param name="sets">One or more <see cref="CharacterSet"/> instances to combine.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sets"/> is null.</exception>
    public static CharacterSet Combine(params CharacterSet[] sets)
    {
        ArgumentNullException.ThrowIfNull(sets);

        SortedSet<int> combined = [];

        foreach (CharacterSet cs in sets)
        {
            foreach (int cp in cs._codepoints)
            {
                combined.Add(cp);
            }
        }

        int[] codepoints = new int[combined.Count];
        combined.CopyTo(codepoints);

        return new CharacterSet(codepoints);
    }

    /// <inheritdoc/>
    public IEnumerator<int> GetEnumerator()
    {
        return ((IEnumerable<int>)_codepoints).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _codepoints.GetEnumerator();
    }
}
