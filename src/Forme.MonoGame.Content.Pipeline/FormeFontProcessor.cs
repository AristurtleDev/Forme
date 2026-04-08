// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Forme.MonoGame.Content.Pipeline;

/// <summary>
/// Processes raw font bytes (from a .ttf or .forme file) into <see cref="FormeFontContent"/>
/// for serialization by <see cref="FormeFontWriter"/>.
/// </summary>
/// <remarks>
/// When the input is a .ttf file, the processor calls <see cref="FormeFont.FromTtf"/> with
/// the configured <see cref="CharacterSet"/>. When the input is a .forme file (detected by magic
/// bytes), the processor calls <see cref="FormeFont.FromStream"/> and the
/// <see cref="CharacterSet"/> property is ignored.
/// </remarks>
[ContentProcessor(DisplayName = "Forme Font Processor")]
public sealed class FormeFontProcessor : ContentProcessor<byte[], FormeFontContent>
{
    private static readonly byte[] FormeMagic =
    {
        (byte)'F', (byte)'O', (byte)'R', (byte)'M', (byte)'E', 0, 0, 0
    };

    /// <summary>
    /// Gets or sets the character set to process when the input is a .ttf file.
    /// Accepts a comma-separated list of tokens, where each token is one of:
    /// <c>"ASCII"</c>, <c>"BasicLatin"</c>, a decimal codepoint range like <c>"1024-1279"</c>,
    /// or a literal string of characters.
    /// Example: <c>"ASCII,1024-1279,42560-42655"</c> for ASCII plus two Cyrillic blocks.
    /// Ignored when the input is a .forme file.
    /// </summary>
    [DefaultValue("ASCII")]
    public string CharacterSet { get; set; } = "ASCII";

    /// <summary>
    /// Processes raw font bytes into <see cref="FormeFontContent"/>.
    /// </summary>
    /// <exception cref="InvalidContentException">
    /// Thrown when <see cref="CharacterSet"/> cannot be parsed into a valid character set.
    /// </exception>
    public override FormeFontContent Process(byte[] input, ContentProcessorContext context)
    {
        FormeFont font;

        if (HasFormeMagic(input))
        {
            using MemoryStream stream = new MemoryStream(input);
            font = FormeFont.FromStream(stream);
        }
        else
        {
            CharacterSet charset = ParseCharacterSet(CharacterSet);
            font = FormeFont.FromTtf(input, charset);
        }

        FormeFontContent content = new FormeFontContent();
        content.Metrics = font.Metrics;
        content.Glyphs = new List<FormeGlyph>(font.Glyphs.Values);
        content.PairAdjustments = new Dictionary<ulong, int>(font.PairAdjustments);
        content.CurveTextureData = font.CurveTexture.Data.ToArray();
        content.CurveTextureWidth = font.CurveTexture.Width;
        content.CurveTextureHeight = font.CurveTexture.Height;
        content.BandTextureData = font.BandTexture.Data.ToArray();
        content.BandTextureWidth = font.BandTexture.Width;
        content.BandTextureHeight = font.BandTexture.Height;
        return content;
    }

    private static bool HasFormeMagic(byte[] data)
    {
        if (data.Length < FormeMagic.Length)
        {
            return false;
        }

        for (int i = 0; i < FormeMagic.Length; i++)
        {
            if (data[i] != FormeMagic[i])
            {
                return false;
            }
        }

        return true;
    }

    private static CharacterSet ParseCharacterSet(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidContentException(
                "CharacterSet must not be empty. Use \"ASCII\", \"BasicLatin\", " +
                "a range like \"32-126\", a literal string of characters, " +
                "or a comma-separated combination such as \"ASCII,1024-1279\".");
        }

        string[] parts = value.Split(',');

        if (parts.Length == 1)
        {
            return ParseSingleToken(parts[0].Trim(), value);
        }

        CharacterSet[] sets = new CharacterSet[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            sets[i] = ParseSingleToken(parts[i].Trim(), value);
        }

        return Forme.CharacterSet.Combine(sets);
    }

    private static CharacterSet ParseSingleToken(string token, string fullValue)
    {
        if (token == "ASCII")
        {
            return Forme.CharacterSet.Ascii;
        }

        if (token == "BasicLatin")
        {
            return Forme.CharacterSet.BasicLatin;
        }

        int dashIndex = token.IndexOf('-');
        if (dashIndex > 0 && dashIndex < token.Length - 1)
        {
            string startStr = token.Substring(0, dashIndex);
            string endStr = token.Substring(dashIndex + 1);

            if (int.TryParse(startStr, out int start) && int.TryParse(endStr, out int end))
            {
                try
                {
                    return Forme.CharacterSet.Range(start, end);
                }
                catch (Exception ex)
                {
                    throw new InvalidContentException($"CharacterSet range \"{token}\" in \"{fullValue}\" is invalid: {ex.Message}", ex);
                }
            }
        }

        try
        {
            return Forme.CharacterSet.FromString(token);
        }
        catch (Exception ex)
        {
            throw new InvalidContentException(
                $"CharacterSet token \"{token}\" in \"{fullValue}\" could not be parsed: {ex.Message}", ex);
        }
    }
}
