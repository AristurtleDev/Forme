// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Forme.Internal;

internal readonly struct GlyphBandRange
{
    internal int HeaderStart { get; }
    internal int CurveStart { get; }
    internal int HeaderCount { get; }

    internal GlyphBandRange(int headerStart, int curveStart, int headerCount)
    {
        HeaderStart = headerStart;
        CurveStart = curveStart;
        HeaderCount = headerCount;
    }
}
