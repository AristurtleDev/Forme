// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Forme.Internal;

internal static class FormeFileReader
{
    internal static FormeFont Read(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        ValidateMagic(reader);

        ushort version = reader.ReadUInt16();

        if (version > FormeFileConstants.Version)
        {
            throw new NotSupportedException(
                $"This version of Forme does not support .forme file version {version}. " +
                $"Only version {FormeFileConstants.Version} is supported.");
        }

        reader.ReadUInt16(); // flags (reserved)

        ushort glyphCount = reader.ReadUInt16();
        ushort unitsPerEm = reader.ReadUInt16();
        int ascent = reader.ReadInt32();
        int descent = reader.ReadInt32();
        int lineGap = reader.ReadInt32();
        uint pairAdjustmentCount = version >= 2 ? reader.ReadUInt32() : 0;

        FontMetrics metrics = new FontMetrics(unitsPerEm, ascent, descent, lineGap);

        Dictionary<int, FormeGlyph> glyphs = new Dictionary<int, FormeGlyph>(glyphCount);

        for (int i = 0; i < glyphCount; i++)
        {
            int codePoint = (int)reader.ReadUInt32();
            int bBoxX1 = reader.ReadInt32();
            int bBoxY1 = reader.ReadInt32();
            int bBoxX2 = reader.ReadInt32();
            int bBoxY2 = reader.ReadInt32();
            int advanceWidth = reader.ReadInt32();
            int leftSideBearing = reader.ReadInt32();
            int bandCount = (int)reader.ReadUInt32();
            int bandDimX = (int)reader.ReadUInt32();
            int bandDimY = (int)reader.ReadUInt32();
            int bandsTexCoordX = reader.ReadUInt16();
            int bandsTexCoordY = reader.ReadUInt16();

            FormeGlyph glyph = new FormeGlyph
            (
                codePoint,
                new FormeBoundingBox(bBoxX1, bBoxY1, bBoxX2, bBoxY2),
                advanceWidth,
                leftSideBearing,
                new FormeBandInfo(bandCount, bandDimX, bandDimY, bandsTexCoordX, bandsTexCoordY)
            );

            glyphs[codePoint] = glyph;
        }

        Dictionary<ulong, int> pairAdjustments = new Dictionary<ulong, int>((int)pairAdjustmentCount);
        for (int i = 0; i < pairAdjustmentCount; i++)
        {
            ulong pairKey = reader.ReadUInt64();
            int adjustment = reader.ReadInt32();
            pairAdjustments[pairKey] = adjustment;
        }

        FormeTextureData curveTexture = ReadFloatTexture(reader);
        FormeTextureData bandTexture = ReadFloatTexture(reader);

        return new FormeFont(metrics, glyphs, pairAdjustments, curveTexture, bandTexture);
    }

    private static void ValidateMagic(BinaryReader reader)
    {
        byte[] magic = reader.ReadBytes(FormeFileConstants.Magic.Length);

        if (magic.Length != FormeFileConstants.Magic.Length)
        {
            throw new InvalidDataException("Stream is too short to be a valid .forme file.");
        }

        for (int i = 0; i < FormeFileConstants.Magic.Length; i++)
        {
            if (magic[i] != FormeFileConstants.Magic[i])
            {
                throw new InvalidDataException(
                    "Stream does not begin with the .forme file magic bytes. " +
                    "The file may be corrupt or not a .forme file.");
            }
        }
    }

    private static FormeTextureData ReadFloatTexture(BinaryReader reader)
    {
        int width = reader.ReadUInt16();
        int height = reader.ReadUInt16();
        uint byteCount = reader.ReadUInt32();

        int floatCount = (int)(byteCount / sizeof(float));
        float[] data = new float[floatCount];

        for (int i = 0; i < floatCount; i++)
        {
            data[i] = reader.ReadSingle();
        }

        return new FormeTextureData(data, width, height);
    }
}
