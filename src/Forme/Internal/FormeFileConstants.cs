// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme.Internal;

internal static class FormeFileConstants
{
    internal static readonly byte[] Magic = [(byte)'F', (byte)'O', (byte)'R', (byte)'M', (byte)'E', 0, 0, 0];
    internal const ushort Version = 2;
    internal const ushort Flags = 0;
}
