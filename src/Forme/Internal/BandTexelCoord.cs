// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme.Internal;

internal readonly struct BandTexelCoord
{
    internal ushort X { get; }
    internal ushort Y { get; }

    internal BandTexelCoord(ushort x, ushort y)
    {
        X = x;
        Y = y;
    }
}
