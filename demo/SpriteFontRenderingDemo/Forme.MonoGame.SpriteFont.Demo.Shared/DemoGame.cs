// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using Forme;
using Forme.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Forme.MonoGame.Demo;

public class DemoGame : Game
{
    // Index 4 (32px) is loaded via the content pipeline; all others are created at runtime.
    private static readonly int[] s_fontSizes = { 10, 14, 18, 24, 32, 48, 64 };
    private const int PIPELINE_FONT_INDEX = 4;
    private const string SAMPLE = "The quick brown fox.";

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private SpriteFont[] _fonts;
    private SpriteFont _heroFont;
    private SpriteFont _labelFont;

    public DemoGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        byte[] ttfData;
        using (Stream stream = TitleContainer.OpenStream("Content/NotoSans-Regular.ttf"))
        {
            ttfData = new byte[stream.Length];
            stream.Read(ttfData, 0, ttfData.Length);
        }

        _heroFont = FormeSpriteFont.Create(GraphicsDevice, ttfData, 56, CharacterSet.Ascii);
        _labelFont = FormeSpriteFont.Create(GraphicsDevice, ttfData, 13, CharacterSet.Ascii);

        _fonts = new SpriteFont[s_fontSizes.Length];
        for (int i = 0; i < s_fontSizes.Length; i++)
        {
            if (i == PIPELINE_FONT_INDEX)
            {
                _fonts[i] = Content.Load<SpriteFont>("NotoSans-Regular");
            }
            else
            {
                _fonts[i] = FormeSpriteFont.Create(GraphicsDevice, ttfData, s_fontSizes[i], CharacterSet.Ascii);
            }
        }
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

        _spriteBatch.Begin();

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, 1280, _heroFont.LineSpacing + _labelFont.LineSpacing + 20), new Color(18, 20, 30));

        _spriteBatch.DrawString(_heroFont, "Forme.MonoGame", new Vector2(20f, 22f), Color.Green);
        float y = _heroFont.LineSpacing + 10;
        _spriteBatch.DrawString(_labelFont, "SpriteFont rendering via CPU rasterization  |  SpriteBatch compatible", new Vector2(20f, y), Color.SlateGray);

        y += _labelFont.LineSpacing + 10;

        for (int i = 0; i < s_fontSizes.Length; i++)
        {
            SpriteFont font = _fonts[i];
            int size = s_fontSizes[i];

            string source = i == PIPELINE_FONT_INDEX ? "pipeline" : "runtime";
            Color labelColor = i == PIPELINE_FONT_INDEX ? Color.LightSteelBlue : Color.SlateGray;

            _spriteBatch.DrawString(_labelFont, $"{size}px ({source})", new Vector2(20f, y), labelColor);
            _spriteBatch.DrawString(font, SAMPLE, new Vector2(200f, y), Color.White);

            y += font.LineSpacing + 8f;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _spriteBatch?.Dispose();
        _pixel?.Dispose();
        _heroFont?.Texture.Dispose();
        _labelFont?.Texture.Dispose();

        if (_fonts != null)
        {
            for (int i = 0; i < _fonts.Length; i++)
            {
                // The pipeline font's lifetime is managed by ContentManager; only dispose runtime fonts.
                if (i != PIPELINE_FONT_INDEX)
                {
                    _fonts[i]?.Texture.Dispose();
                }
            }
        }
    }
}
