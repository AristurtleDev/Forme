// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Forme.MonoGame.Content.Pipeline;

/// <summary>
/// Processes raw TTF bytes into a <see cref="SpriteFontContent"/> by CPU-rasterizing glyphs
/// at a fixed pixel size using font metrics sourced directly from the font file.
/// </summary>
/// <remarks>
/// <para>
/// The resulting XNB is loaded at runtime as a standard MonoGame <c>SpriteFont</c> via
/// <c>Content.Load&lt;SpriteFont&gt;</c> and works with <c>SpriteBatch.DrawString</c>
/// without any Forme-specific runtime dependencies.
/// </para>
/// <para>
/// Glyph metrics are read directly from the font file rather than from a platform font
/// engine, which avoids the rounding discrepancies that can affect MonoGame's built-in
/// SpriteFont generation on some platforms.
/// </para>
/// </remarks>
[ContentProcessor(DisplayName = "Forme Sprite Font Processor")]
public sealed class FormeSpriteAtlasProcessor : ContentProcessor<byte[], SpriteFontContent>
{
    /// <summary>
    /// Gets or sets the target em-square height in pixels.
    /// </summary>
    [DefaultValue(32)]
    public int SizePixels { get; set; } = 32;

    /// <summary>
    /// Gets or sets the character set to rasterize.
    /// Accepts a comma-separated list of tokens, where each token is one of:
    /// <c>"ASCII"</c>, <c>"BasicLatin"</c>, a decimal codepoint range like <c>"1024-1279"</c>,
    /// or a literal string of characters.
    /// Example: <c>"ASCII,1024-1279"</c> for ASCII plus a Cyrillic block.
    /// </summary>
    [DefaultValue("ASCII")]
    public string CharacterSet { get; set; } = "ASCII";

    /// <summary>
    /// Processes raw TTF bytes into a <see cref="SpriteFontContent"/>.
    /// </summary>
    /// <exception cref="InvalidContentException">
    /// Thrown when <see cref="SizePixels"/> is zero or negative, or when
    /// <see cref="CharacterSet"/> cannot be parsed.
    /// </exception>
    public override SpriteFontContent Process(byte[] input, ContentProcessorContext context)
    {
        if (SizePixels <= 0)
        {
            throw new InvalidContentException($"SizePixels must be greater than zero, but was {SizePixels}.");
        }

        CharacterSet charset = ParseCharacterSet(CharacterSet);
        FormeSpriteAtlas atlas = FormeSpriteAtlas.Bake(input, SizePixels, charset);

        // Build the premultiplied RGBA atlas texture. SpriteBatch uses BlendState.AlphaBlend
        // which expects premultiplied source pixels, so R=G=B=A=alpha. SpriteBatch then
        // multiplies by the draw color at runtime to produce the final tinted glyph.
        PixelBitmapContent<Color> face = new PixelBitmapContent<Color>(atlas.Width, atlas.Height);
        byte[] rgbaData = new byte[atlas.Width * atlas.Height * 4];

        for (int i = 0; i < atlas.Pixels.Length; i++)
        {
            byte a = atlas.Pixels[i];
            rgbaData[i * 4 + 0] = a;
            rgbaData[i * 4 + 1] = a;
            rgbaData[i * 4 + 2] = a;
            rgbaData[i * 4 + 3] = a;
        }

        face.SetPixelData(rgbaData);

        SpriteFontContent output = new SpriteFontContent();
        output.Texture.Faces[0].Add(face);
        output.VerticalLineSpacing = atlas.LineSpacing;

        char? defaultChar = null;

        foreach (FormeSpriteAtlasGlyph glyph in atlas.Glyphs)
        {
            // SpriteFontContent uses char (BMP only); skip supplementary plane codepoints.
            if (glyph.CodePoint > char.MaxValue)
            {
                continue;
            }

            char ch = (char)glyph.CodePoint;
            output.CharacterMap.Add(ch);

            output.Glyphs.Add(new Rectangle(
                glyph.AtlasRegion.X,
                glyph.AtlasRegion.Y,
                glyph.AtlasRegion.Width,
                glyph.AtlasRegion.Height));

            // croppingY places the glyph top relative to the top of the text line.
            int croppingY = atlas.AscentPixels + glyph.BitmapOriginY;
            output.Cropping.Add(new Rectangle(0, croppingY, glyph.AtlasRegion.Width, atlas.LineSpacing));

            output.Kerning.Add(new Vector3(glyph.Kerning.Left, glyph.Kerning.Width, glyph.Kerning.Right));

            if (ch == '?' && defaultChar == null)
            {
                defaultChar = '?';
            }
        }

        if (defaultChar == null && output.CharacterMap.Count > 0)
        {
            defaultChar = output.CharacterMap[0];
        }

        output.DefaultCharacter = defaultChar;

        return output;
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
                    throw new InvalidContentException(
                        $"CharacterSet range \"{token}\" in \"{fullValue}\" is invalid: {ex.Message}", ex);
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
