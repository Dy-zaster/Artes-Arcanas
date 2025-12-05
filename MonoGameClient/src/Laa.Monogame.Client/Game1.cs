using System;
using System.Linq;
using Laa.Content.Core.Maps;
using Laa.Monogame.Client.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _placeholderTexture;
    private double _elapsedSeconds;
    private ContentContext? _content;
    private MapDocument? _activeMap;
    private bool _loggedContent;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        _content = ContentContext.Create();
        LoadInitialMap();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _placeholderTexture = new Texture2D(GraphicsDevice, 1, 1);
        _placeholderTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        _elapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;

        if (!_loggedContent)
        {
            _loggedContent = true;
            LogContentSummary();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GetBackgroundColor());

        if (_spriteBatch is null || _placeholderTexture is null)
        {
            base.Draw(gameTime);
            return;
        }

        var pulse = (float)((Math.Sin(_elapsedSeconds) + 1.0) * 0.5);
        var color = new Color(pulse, pulse, 1f);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_placeholderTexture, new Rectangle(100, 100, 320, 80), color);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch?.Dispose();
            _placeholderTexture?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void LoadInitialMap()
    {
        if (_content is null)
        {
            return;
        }

        var firstMapId = _content.Maps.ListMaps().FirstOrDefault() ?? "map_0";
        try
        {
            _activeMap = _content.Maps.GetMap(firstMapId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load map '{firstMapId}': {ex.Message}");
            _activeMap = null;
        }
    }

    private void LogContentSummary()
    {
        if (_content is null)
        {
            Console.WriteLine("ContentContext not initialized.");
            return;
        }

        var mapName = _activeMap?.Header.Name ?? "N/A";
        var itemCount = _content.Items.GetItems().Items.Count;
        var spellCount = _content.Spells.GetSpells().Spells.Count;
        var monsterCount = _content.Monsters.GetMonsters().Monsters.Count;
        Console.WriteLine($"Loaded Map: {mapName} | Items: {itemCount} | Spells: {spellCount} | Monsters: {monsterCount}");
    }

    private Color GetBackgroundColor()
    {
        if (_activeMap?.Header is null)
        {
            return Color.Black;
        }

        var seed = _activeMap.Header.Flags;
        var r = (byte)(seed & 0xFF);
        var g = (byte)((seed >> 8) & 0xFF);
        var b = (byte)((seed >> 4) & 0xFF);
        return new Color(r, g, b);
    }
}
