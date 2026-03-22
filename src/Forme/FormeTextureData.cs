// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Forme;

/// <summary>
/// A flat-float texture dataset produced by font processing, consisting of raw float data
/// and its dimensions in texels.
/// </summary>
/// <remarks>
/// <see cref="FormeFont"/> produces two instances: one for the curve texture (RGBA32F, 4
/// floats per texel) and one for the band texture (RG32F, 2 floats per texel). Both textures
/// are always 4096 texels wide.
/// </remarks>
public readonly struct FormeTextureData
{
    private readonly float[] _data;

    /// <summary>
    /// Gets the width of the texture in texels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the texture in texels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the raw float data for this texture.
    /// </summary>
    public ReadOnlyMemory<float> Data => _data;

    /// <summary>
    /// Initializes a new <see cref="FormeTextureData"/> wrapping the given array and dimensions.
    /// </summary>
    public FormeTextureData(float[] data, int width, int height)
    {
        _data = data;
        Width = width;
        Height = height;
    }
}
