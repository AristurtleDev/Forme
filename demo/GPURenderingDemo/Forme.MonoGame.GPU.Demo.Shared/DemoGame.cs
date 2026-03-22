// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Forme;
using Forme.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Forme.MonoGame.GPU.Demo.DemoUI;

namespace Forme.MonoGame.GPU.Demo;

public class DemoGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    private FormeFont _font;
    private FormeFontDevice _gpuFont;
    private FormeFont _emojiFont;
    private FormeFontDevice _emojiGpuFont;
    private FormeRenderer _renderer;

    private FormeTextBounds _measureBounds;
    private FormeTextBounds _glyphsBounds;
    private IReadOnlyList<GlyphPlacement> _glyphPlacements;
    private Vector2 _measurePos;
    private Vector2 _glyphsPos;

    public DemoGame()
    {
        _graphics = new GraphicsDeviceManager(this);

        // FORME shaders require HiDef graphics profile
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font = Content.Load<FormeFont>("NotoSans-Regular");
        _gpuFont = new FormeFontDevice(GraphicsDevice, _font);
        _emojiFont = Content.Load<FormeFont>("NotoEmoji-Regular");
        _emojiGpuFont = new FormeFontDevice(GraphicsDevice, _emojiFont);
        _renderer = new FormeRenderer(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _measureBounds = _font.MeasureString(MEASURE_TEXT, MEASURE_FONT_SIZE);
        _glyphsBounds = _font.MeasureString(GLYPHS_TEXT, GLYPHS_FONT_SIZE);
        _glyphPlacements = _font.GetGlyphs(GLYPHS_TEXT, GLYPHS_FONT_SIZE);
        _measurePos = new Vector2(RIGHT_COLUMN_X, MEASURE_TEXT_Y);
        _glyphsPos = new Vector2(RIGHT_COLUMN_X, GLYPHS_TEXT_Y);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(22, 24, 34));

        // ---- SpriteBatch pass: panels, cards, overlays ----
        _spriteBatch.Begin();

        FillRect(_spriteBatch, _pixel, new Rectangle(0, 0, 1280, 95), new Color(18, 20, 30));
        FillRect(_spriteBatch, _pixel, new Rectangle(0, 101, 636, 612), new Color(26, 29, 40));
        FillRect(_spriteBatch, _pixel, new Rectangle(644, 101, 636, 612), new Color(26, 29, 40));

        DrawCard(_spriteBatch, _pixel, new Rectangle(LEFT_CARD_X, (int)WORD_WRAP_TOP, LEFT_CARD_WIDTH, 90));
        DrawCard(_spriteBatch, _pixel, new Rectangle(LEFT_CARD_X, (int)ELLIPSIS_CHAR_TOP, LEFT_CARD_WIDTH, 68));
        DrawCard(_spriteBatch, _pixel, new Rectangle(LEFT_CARD_X, (int)ELLIPSIS_WORD_TOP, LEFT_CARD_WIDTH, 68));
        DrawCard(_spriteBatch, _pixel, new Rectangle(LEFT_CARD_X, (int)CHAR_SPACING_TOP, LEFT_CARD_WIDTH, 94));
        DrawCard(_spriteBatch, _pixel, new Rectangle(LEFT_CARD_X, (int)LINE_SPACING_TOP, LEFT_CARD_WIDTH, 142));

        DrawCard(_spriteBatch, _pixel, new Rectangle(RIGHT_CARD_X, (int)ALIGN_TOP, RIGHT_CARD_WIDTH, 146));
        DrawCard(_spriteBatch, _pixel, new Rectangle(RIGHT_CARD_X, (int)MEASURE_TOP, RIGHT_CARD_WIDTH, 76));
        DrawCard(_spriteBatch, _pixel, new Rectangle(RIGHT_CARD_X, (int)GLYPHS_TOP, RIGHT_CARD_WIDTH, 78));
        DrawCard(_spriteBatch, _pixel, new Rectangle(RIGHT_CARD_X, (int)EMOJI_TOP, RIGHT_CARD_WIDTH, 84));

        // MeasureString bounding box
        DrawBorder(_spriteBatch, _pixel,
            new Rectangle(
                (int)(_measurePos.X + _measureBounds.X) - 2,
                (int)(_measurePos.Y + _measureBounds.Y) - 2,
                (int)_measureBounds.Width + 4,
                (int)_measureBounds.Height + 4),
            Color.White * 0.4f);

        // GetGlyphs baseline rule and VisualBounds rectangles
        FillRect(_spriteBatch, _pixel, new Rectangle((int)_glyphsPos.X, (int)_glyphsPos.Y, (int)_glyphsBounds.Width, 1), Color.White * 0.25f);
        foreach (GlyphPlacement placement in _glyphPlacements)
        {
            FormeTextBounds vb = placement.VisualBounds;
            if (vb.Width > 0 && vb.Height > 0)
            {
                DrawBorder(_spriteBatch, _pixel,
                    new Rectangle(
                        (int)(_glyphsPos.X + vb.X),
                        (int)(_glyphsPos.Y + vb.Y),
                        (int)vb.Width,
                        (int)vb.Height),
                    Color.White * 0.5f);
            }
        }

        _spriteBatch.End();

        // ---- FormeRenderer pass: all text ----
        _renderer.Begin();

        // hero
        _renderer.DrawString(_gpuFont, "Forme.MonoGame", new Vector2(LEFT_COLUMN_X, 62f), 56f, Color.Green);
        _renderer.DrawString(_gpuFont, "GPU text rendering via the Slug Algorithm  |  layout features demo", new Vector2(LEFT_COLUMN_X, 85f), 14f, Color.SlateGray);

        // column group headers
        _renderer.DrawString(_gpuFont, "FLOW & WRAPPING", new Vector2(LEFT_COLUMN_X, 116f), HEADER_FONT_SIZE, Color.White);
        _renderer.DrawString(_gpuFont, "Common paragraph layout behaviors", new Vector2(LEFT_COLUMN_X, 130f), LABEL_FONT_SIZE, Color.SlateGray);
        _renderer.DrawString(_gpuFont, "ALIGNMENT & METRICS", new Vector2(RIGHT_COLUMN_X, 116f), HEADER_FONT_SIZE, Color.White);
        _renderer.DrawString(_gpuFont, "Placement, measuring, glyph bounds, and emoji", new Vector2(RIGHT_COLUMN_X, 130f), LABEL_FONT_SIZE, Color.SlateGray);

        // L1: WORD WRAP
        _renderer.DrawString(_gpuFont, "WORD WRAP", new Vector2(LEFT_COLUMN_X, WORD_WRAP_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "max width 580px", new Vector2(LEFT_CARD_INNER_RIGHT, WORD_WRAP_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, SAMPLE, new Vector2(LEFT_COLUMN_X, WORD_WRAP_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { MaxWidth = COLUMN_WIDTH });

        // L2: ELLIPSIS - CHARACTER
        _renderer.DrawString(_gpuFont, "ELLIPSIS - CHARACTER", new Vector2(LEFT_COLUMN_X, ELLIPSIS_CHAR_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "max width 580px", new Vector2(LEFT_CARD_INNER_RIGHT, ELLIPSIS_CHAR_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, SAMPLE, new Vector2(LEFT_COLUMN_X, ELLIPSIS_CHAR_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { MaxWidth = COLUMN_WIDTH, EllipsisMode = EllipsisMode.Character });

        // L3: ELLIPSIS - WORD
        _renderer.DrawString(_gpuFont, "ELLIPSIS - WORD", new Vector2(LEFT_COLUMN_X, ELLIPSIS_WORD_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "max width 580px", new Vector2(LEFT_CARD_INNER_RIGHT, ELLIPSIS_WORD_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, SAMPLE, new Vector2(LEFT_COLUMN_X, ELLIPSIS_WORD_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { MaxWidth = COLUMN_WIDTH, EllipsisMode = EllipsisMode.Word });

        // L4: CHARACTER SPACING
        _renderer.DrawString(_gpuFont, "CHARACTER SPACING", new Vector2(LEFT_COLUMN_X, CHAR_SPACING_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "normal vs. +3px", new Vector2(LEFT_CARD_INNER_RIGHT, CHAR_SPACING_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, SAMPLE_SHORT, new Vector2(LEFT_COLUMN_X, CHAR_SPACING_TEXT1_Y), BODY_FONT_SIZE, Color.White);
        _renderer.DrawString(_gpuFont, SAMPLE_SHORT, new Vector2(LEFT_COLUMN_X, CHAR_SPACING_TEXT2_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { CharacterSpacing = 3f });

        // L5: LINE SPACING
        _renderer.DrawString(_gpuFont, "LINE SPACING", new Vector2(LEFT_COLUMN_X, LINE_SPACING_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "normal vs. +8px", new Vector2(LEFT_CARD_INNER_RIGHT, LINE_SPACING_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, "Normal", new Vector2(LEFT_COLUMN_X, LINE_SPACING_SUB_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray);
        _renderer.DrawString(_gpuFont, LINE_SPACING_SAMPLE, new Vector2(LEFT_COLUMN_X, LINE_SPACING_TEXT_Y), BODY_FONT_SIZE, Color.White, new TextLayoutOptions());
        _renderer.DrawString(_gpuFont, "+8px", new Vector2(LINE_SPACING_SECOND_COLUMN_X, LINE_SPACING_SUB_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray);
        _renderer.DrawString(_gpuFont, LINE_SPACING_SAMPLE, new Vector2(LINE_SPACING_SECOND_COLUMN_X, LINE_SPACING_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { LineSpacing = 8f });

        // R1: ALIGNMENT
        _renderer.DrawString(_gpuFont, "ALIGNMENT", new Vector2(RIGHT_COLUMN_X, ALIGN_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "left - center - right", new Vector2(RIGHT_CARD_INNER_RIGHT, ALIGN_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        // Left aligned
        _renderer.DrawString(_gpuFont, ALIGN_SAMPLE, new Vector2(RIGHT_COLUMN_X, ALIGN_LEFT_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Left });

        // Center aligned
        _renderer.DrawString(_gpuFont, ALIGN_SAMPLE, new Vector2(ALIGN_CENTER_X, ALIGN_CENTER_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Center });

        // Right aligned
        _renderer.DrawString(_gpuFont, ALIGN_SAMPLE, new Vector2(ALIGN_RIGHT_X, ALIGN_RIGHT_TEXT_Y), BODY_FONT_SIZE, Color.White,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });

        // R2: MEASURE STRING
        _renderer.DrawString(_gpuFont, "MEASURE STRING", new Vector2(RIGHT_COLUMN_X, MEASURE_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "visual bounds preview", new Vector2(RIGHT_CARD_INNER_RIGHT, MEASURE_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, MEASURE_TEXT, _measurePos, MEASURE_FONT_SIZE, Color.White);

        // R3: GET GLYPHS
        _renderer.DrawString(_gpuFont, "GET GLYPHS", new Vector2(RIGHT_COLUMN_X, GLYPHS_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "glyph visual bounds", new Vector2(RIGHT_CARD_INNER_RIGHT, GLYPHS_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_gpuFont, GLYPHS_TEXT, _glyphsPos, GLYPHS_FONT_SIZE, Color.White);

        // R4: EMOJI FONT
        _renderer.DrawString(_gpuFont, "EMOJI FONT", new Vector2(RIGHT_COLUMN_X, EMOJI_LABEL_Y), LABEL_FONT_SIZE, Color.LightSteelBlue);
        _renderer.DrawString(_gpuFont, "mixed unicode support", new Vector2(RIGHT_CARD_INNER_RIGHT, EMOJI_LABEL_Y), LABEL_FONT_SIZE, Color.SlateGray,
            new TextLayoutOptions { Alignment = TextHorizontalAlignment.Right });
        _renderer.DrawString(_emojiGpuFont, "😀😺🐲🐍👀✨🎉❤", new Vector2(RIGHT_COLUMN_X, EMOJI_TEXT_Y), EMOJI_FONT_SIZE, Color.White);

        _renderer.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _renderer?.Dispose();
        _gpuFont?.Dispose();
        _emojiGpuFont?.Dispose();
        _pixel?.Dispose();
    }
}
