// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Forme.MonoGame.GPU.Demo;

internal static class DemoUI
{
    // sample strings used across demo sections
    public const string SAMPLE = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore.";
    public const string SAMPLE_SHORT = "Lorem ipsum dolor sit amet, consectetur adipiscing.";
    public const string ALIGN_SAMPLE = "Lorem ipsum dolor sit amet.";
    public const string MEASURE_TEXT = "Lorem ipsum";
    public const string GLYPHS_TEXT = "lorem ipsum";
    public const string LINE_SPACING_SAMPLE = "Lorem ipsum\nDolor sit amet\nConsectetur";

    // font sizes in pixels
    public const float LABEL_FONT_SIZE = 13f;
    public const float HEADER_FONT_SIZE = 15f;
    public const float BODY_FONT_SIZE = 18f;
    public const float MEASURE_FONT_SIZE = 24f;
    public const float GLYPHS_FONT_SIZE = 28f;
    public const float EMOJI_FONT_SIZE = 32f;

    // column X anchors and widths
    public const float LEFT_COLUMN_X = 20f;
    public const float RIGHT_COLUMN_X = 660f;
    public const float COLUMN_WIDTH = 580f;
    public const int CARD_PADDING = 12;
    public const int CARD_GAP = 8;
    public const int LEFT_CARD_X = 8;
    public const int LEFT_CARD_WIDTH = 620;
    public const int RIGHT_CARD_X = 652;
    public const int RIGHT_CARD_WIDTH = 620;
    public const float LEFT_CARD_INNER_RIGHT = 616f;
    public const float RIGHT_CARD_INNER_RIGHT = 1260f;

    public const float FIRST_CARD_TOP = 143f;

    // Word Wrap card
    public const float WORD_WRAP_TOP = FIRST_CARD_TOP;
    public const float WORD_WRAP_LABEL_Y = WORD_WRAP_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float WORD_WRAP_TEXT_Y = WORD_WRAP_LABEL_Y + 22f;

    // Ellipsis Character card
    public const float ELLIPSIS_CHAR_TOP = WORD_WRAP_TOP + 90 + CARD_GAP;
    public const float ELLIPSIS_CHAR_LABEL_Y = ELLIPSIS_CHAR_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float ELLIPSIS_CHAR_TEXT_Y = ELLIPSIS_CHAR_LABEL_Y + 22f;

    // Ellipsis Word card
    public const float ELLIPSIS_WORD_TOP = ELLIPSIS_CHAR_TOP + 68 + CARD_GAP;
    public const float ELLIPSIS_WORD_LABEL_Y = ELLIPSIS_WORD_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float ELLIPSIS_WORD_TEXT_Y = ELLIPSIS_WORD_LABEL_Y + 22f;

    // Character Spacing card
    public const float CHAR_SPACING_TOP = ELLIPSIS_WORD_TOP + 68 + CARD_GAP;
    public const float CHAR_SPACING_LABEL_Y = CHAR_SPACING_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float CHAR_SPACING_TEXT1_Y = CHAR_SPACING_LABEL_Y + 22f;
    public const float CHAR_SPACING_TEXT2_Y = CHAR_SPACING_TEXT1_Y + 26f;

    // Line Spacing card
    public const float LINE_SPACING_TOP = CHAR_SPACING_TOP + 94 + CARD_GAP;
    public const float LINE_SPACING_LABEL_Y = LINE_SPACING_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float LINE_SPACING_SUB_LABEL_Y = LINE_SPACING_LABEL_Y + 14f;
    public const float LINE_SPACING_TEXT_Y = LINE_SPACING_SUB_LABEL_Y + 22f;
    public const float LINE_SPACING_SECOND_COLUMN_X = LEFT_COLUMN_X + 300f;

    // Alignment card
    public const float ALIGN_TOP = FIRST_CARD_TOP;
    public const float ALIGN_LABEL_Y = ALIGN_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float ALIGN_LEFT_SUB_LABEL_Y = ALIGN_LABEL_Y + 14f;
    public const float ALIGN_LEFT_TEXT_Y = ALIGN_LEFT_SUB_LABEL_Y + 22f;
    public const float ALIGN_CENTER_SUB_LABEL_Y = ALIGN_LEFT_TEXT_Y + 10f;
    public const float ALIGN_CENTER_TEXT_Y = ALIGN_CENTER_SUB_LABEL_Y + 22f;
    public const float ALIGN_RIGHT_SUB_LABEL_Y = ALIGN_CENTER_TEXT_Y + 10f;
    public const float ALIGN_RIGHT_TEXT_Y = ALIGN_RIGHT_SUB_LABEL_Y + 22f;
    public const float ALIGN_CENTER_X = RIGHT_COLUMN_X + COLUMN_WIDTH * 0.5f;
    public const float ALIGN_RIGHT_X = RIGHT_COLUMN_X + COLUMN_WIDTH;

    // Measure String card
    public const float MEASURE_TOP = ALIGN_TOP + 146 + CARD_GAP;
    public const float MEASURE_LABEL_Y = MEASURE_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float MEASURE_TEXT_Y = MEASURE_LABEL_Y + 28f;

    // Get Glyphs card
    public const float GLYPHS_TOP = MEASURE_TOP + 76 + CARD_GAP;
    public const float GLYPHS_LABEL_Y = GLYPHS_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float GLYPHS_TEXT_Y = GLYPHS_LABEL_Y + 30f;

    // Emoji Font card
    public const float EMOJI_TOP = GLYPHS_TOP + 78 + CARD_GAP;
    public const float EMOJI_LABEL_Y = EMOJI_TOP + CARD_PADDING + LABEL_FONT_SIZE;
    public const float EMOJI_TEXT_Y = EMOJI_LABEL_Y + 34f;

    private static readonly Color s_cardBackground = new Color(30, 33, 45);
    private static readonly Color s_cardBorder = Color.White * 0.08f;

    internal static void FillRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    internal static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }

    internal static void DrawCard(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
    {
        FillRect(spriteBatch, pixel, rect, s_cardBackground);
        DrawBorder(spriteBatch, pixel, rect, s_cardBorder);
    }
}
