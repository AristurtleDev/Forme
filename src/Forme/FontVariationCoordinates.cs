// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Forme;

/// <summary>
/// Represents an ordered list of font variation-axis coordinates.
/// </summary>
/// <remarks>
/// If more than one coordinate for the same axis is present, the last coordinate takes precedence.
/// </remarks>
public readonly struct FontVariationCoordinates
{
    private readonly FontVariationCoordinate[]? _coordinates;

    /// <summary>
    /// Gets an empty set of variation coordinates.
    /// </summary>
    public static FontVariationCoordinates Empty
    {
        get { return default; }
    }

    /// <summary>
    /// Gets the number of coordinates in the set.
    /// </summary>
    public int Count
    {
        get { return _coordinates?.Length ?? 0; }
    }

    /// <summary>
    /// Gets the coordinate at the given zero-based index.
    /// </summary>
    /// <param name="index">The coordinate index.</param>
    /// <returns>The coordinate at <paramref name="index"/>.</returns>
    public FontVariationCoordinate this[int index]
    {
        get
        {
            FontVariationCoordinate[] coordinates = _coordinates ?? [];
            return coordinates[index];
        }
    }

    /// <summary>
    /// Initializes a new <see cref="FontVariationCoordinates"/> from the given coordinates.
    /// </summary>
    /// <param name="coordinates">The variation coordinates to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="coordinates"/> is null.</exception>
    public FontVariationCoordinates(IEnumerable<FontVariationCoordinate> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);

        List<FontVariationCoordinate> items = new List<FontVariationCoordinate>();
        foreach (FontVariationCoordinate coordinate in coordinates)
        {
            items.Add(coordinate);
        }

        _coordinates = items.Count == 0 ? null : items.ToArray();
    }

    /// <summary>
    /// Returns the coordinates as a read-only span.
    /// </summary>
    /// <returns>The coordinates as a span.</returns>
    public ReadOnlySpan<FontVariationCoordinate> AsSpan()
    {
        return _coordinates;
    }

    /// <summary>
    /// Returns a new set with the given coordinate appended.
    /// </summary>
    /// <param name="coordinate">The coordinate to append.</param>
    /// <returns>A new coordinate set containing the appended coordinate.</returns>
    public FontVariationCoordinates Add(FontVariationCoordinate coordinate)
    {
        int count = Count;
        FontVariationCoordinate[] expanded = new FontVariationCoordinate[count + 1];
        ReadOnlySpan<FontVariationCoordinate> existing = AsSpan();
        for (int i = 0; i < existing.Length; i++)
        {
            expanded[i] = existing[i];
        }

        expanded[count] = coordinate;
        return new FontVariationCoordinates(expanded);
    }

    /// <summary>
    /// Returns a new set with the given coordinate appended.
    /// </summary>
    /// <param name="tag">The variation-axis tag.</param>
    /// <param name="value">The coordinate value.</param>
    /// <returns>A new coordinate set containing the appended coordinate.</returns>
    public FontVariationCoordinates Add(FontVariationAxisTag tag, float value)
    {
        return Add(new FontVariationCoordinate(tag, value));
    }

    /// <summary>
    /// Returns a new set with the given coordinate appended.
    /// </summary>
    /// <param name="tag">The variation-axis tag.</param>
    /// <param name="value">The coordinate value.</param>
    /// <returns>A new coordinate set containing the appended coordinate.</returns>
    public FontVariationCoordinates Add(string tag, float value)
    {
        return Add(new FontVariationCoordinate(tag, value));
    }

    /// <summary>
    /// Returns a new set with the coordinate at the given index removed.
    /// </summary>
    /// <param name="index">The zero-based coordinate index to remove.</param>
    /// <returns>A new coordinate set without the removed coordinate.</returns>
    public FontVariationCoordinates RemoveAt(int index)
    {
        FontVariationCoordinate[] coordinates = _coordinates ?? [];
        if ((uint)index >= (uint)coordinates.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (coordinates.Length == 1)
        {
            return Empty;
        }

        FontVariationCoordinate[] reduced = new FontVariationCoordinate[coordinates.Length - 1];
        int targetIndex = 0;
        for (int sourceIndex = 0; sourceIndex < coordinates.Length; sourceIndex++)
        {
            if (sourceIndex == index)
            {
                continue;
            }

            reduced[targetIndex] = coordinates[sourceIndex];
            targetIndex++;
        }

        return new FontVariationCoordinates(reduced);
    }

    /// <summary>
    /// Returns an empty coordinate set.
    /// </summary>
    /// <returns><see cref="Empty"/>.</returns>
    public FontVariationCoordinates Clear()
    {
        return Empty;
    }
}
