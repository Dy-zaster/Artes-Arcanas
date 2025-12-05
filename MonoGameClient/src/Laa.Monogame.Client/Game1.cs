using System;
using System.Linq;
using Laa.Content.Core.Maps;
using Laa.Monogame.Client.Content;
using Laa.Monogame.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client;

public class Game1 : Game
{
    private const int TileSize = 32;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private ContentContext? _content;
    private MapDocument? _activeMap;
    private bool _loggedContent;
    private TerrainRenderer? _terrainRenderer;
    private MapOverlayRenderer? _overlayRenderer;
    private TilePalette? _tilePalette;
    private Camera2D? _camera;
    private CameraController? _cameraController;
    private OverlayLayers _overlayLayers = OverlayLayers.None;
    private KeyboardState _previousKeyboard;

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

        _camera = new Camera2D(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _cameraController = new CameraController(_camera);
        ConfigureCameraBounds();
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _terrainRenderer = new TerrainRenderer(GraphicsDevice, TileSize);
        _terrainRenderer.LoadContent();
        _overlayRenderer = new MapOverlayRenderer(GraphicsDevice, TileSize);
        _overlayRenderer.LoadContent();
        _tilePalette = new TilePalette();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        var mouse = Mouse.GetState();
        HandleOverlayInput(keyboard);
        _cameraController?.Update(gameTime, keyboard, mouse);

        if (!_loggedContent)
        {
            _loggedContent = true;
            LogContentSummary();
        }

        base.Update(gameTime);

        _previousKeyboard = keyboard;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GetBackgroundColor());

        if (_spriteBatch is null || _terrainRenderer is null || _camera is null || _tilePalette is null)
        {
            base.Draw(gameTime);
            return;
        }

        _terrainRenderer.Draw(_spriteBatch, _activeMap, _camera, _tilePalette);
        _overlayRenderer?.Draw(_spriteBatch, _activeMap, _camera, _overlayLayers);

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.ClientSizeChanged -= OnClientSizeChanged;
            _spriteBatch?.Dispose();
            _terrainRenderer?.Dispose();
            _overlayRenderer?.Dispose();
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
            ConfigureCameraBounds();
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

    private void ConfigureCameraBounds()
    {
        if (_activeMap is null || _camera is null)
        {
            return;
        }

        var width = _activeMap.Terrain.FirstOrDefault()?.Count ?? 0;
        var height = _activeMap.Terrain.Count;
        if (width == 0 || height == 0)
        {
            return;
        }

        var pixelWidth = width * TileSize;
        var pixelHeight = height * TileSize;
        _camera.SetWorldSize(pixelWidth, pixelHeight);
        _camera.CenterOn(new Vector2(pixelWidth / 2f, pixelHeight / 2f));
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_camera is null)
        {
            return;
        }

        _camera.ResizeViewport(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        ConfigureCameraBounds();
    }

    private void HandleOverlayInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(keyboardState, Keys.Tab))
        {
            CycleOverlayMode();
        }

        if (IsKeyPressed(keyboardState, Keys.D0))
        {
            SetOverlay(OverlayLayers.None);
        }
        else if (IsKeyPressed(keyboardState, Keys.D1))
        {
            SetOverlay(OverlayLayers.Sensors);
        }
        else if (IsKeyPressed(keyboardState, Keys.D2))
        {
            SetOverlay(OverlayLayers.Nests);
        }
        else if (IsKeyPressed(keyboardState, Keys.D3))
        {
            SetOverlay(OverlayLayers.Merchants);
        }
        else if (IsKeyPressed(keyboardState, Keys.D4))
        {
            SetOverlay(OverlayLayers.All);
        }
    }

    private void CycleOverlayMode()
    {
        var next = _overlayLayers switch
        {
            OverlayLayers.None => OverlayLayers.Sensors,
            OverlayLayers.Sensors => OverlayLayers.Nests,
            OverlayLayers.Nests => OverlayLayers.Merchants,
            OverlayLayers.Merchants => OverlayLayers.All,
            _ => OverlayLayers.None
        };

        SetOverlay(next);
    }

    private void SetOverlay(OverlayLayers layers)
    {
        if (_overlayLayers == layers)
        {
            return;
        }

        _overlayLayers = layers;
        Console.WriteLine($"Overlay mode: {_overlayLayers}");
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }
}
