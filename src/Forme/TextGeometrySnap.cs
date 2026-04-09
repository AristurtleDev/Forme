// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme;

/// <summary>
/// Controls how text geometry is rounded in layout results.
/// </summary>
public enum TextGeometrySnap
{
    /// <summary>
    /// Preserve the raw floating-point geometry produced by layout.
    /// </summary>
    None = 0,

    /// <summary>
    /// Round geometry values to whole-pixel positions.
    /// </summary>
    Pixel = 1
}
