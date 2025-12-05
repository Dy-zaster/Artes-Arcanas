using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Laa.Content.Core.Commerce;
using Laa.Content.Core.Graphics;
using Laa.Content.Core.Items;
using Laa.Content.Core.Maps;
using Laa.Content.Core.Monsters;
using Laa.Content.Core.Spells;
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
    private GraphicDocument? _graphicsCatalog;
    private bool _loggedContent;
    private TerrainRenderer? _terrainRenderer;
    private StaticGraphicRenderer? _staticGraphicRenderer;
    private MapOverlayRenderer? _overlayRenderer;
    private DebugTextRenderer? _debugTextRenderer;
    private TilePalette? _tilePalette;
    private Camera2D? _camera;
    private CameraController? _cameraController;
    private OverlayLayers _overlayLayers = OverlayLayers.None;
    private KeyboardState _previousKeyboard;
    private IReadOnlyList<StaticGraphic> _sortedStaticGraphics = Array.Empty<StaticGraphic>();
    private Texture2D? _hudBackgroundTexture;
    private bool _showHud = true;
    private string[] _mapIds = Array.Empty<string>();
    private int _currentMapIndex;
    private ItemDocument? _itemDocument;
    private SpellDocument? _spellDocument;
    private MonsterDocument? _monsterDocument;
    private CommerceDocument? _commerceDocument;
    private InfoPanel _infoPanel = InfoPanel.None;

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
        _graphicsCatalog = _content.Graphics.GetGraphics();
        _itemDocument = _content.Items.GetItems();
        _spellDocument = _content.Spells.GetSpells();
        _monsterDocument = _content.Monsters.GetMonsters();
        _commerceDocument = _content.Commerce.GetCommerce();
        InitializeMapList();
        LoadMapByIndex(0);

        base.Initialize();

        _camera = new Camera2D(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _cameraController = new CameraController(_camera);
        ConfigureCameraBounds();
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        var textureRoots = ResolveGraphicRoots();
        _terrainRenderer = new TerrainRenderer(GraphicsDevice, TileSize, textureRoots);
        _terrainRenderer.LoadContent();
        if (_graphicsCatalog is not null)
        {
            var textureProvider = new GraphicTextureProvider(GraphicsDevice, _graphicsCatalog, textureRoots);
            _staticGraphicRenderer = new StaticGraphicRenderer(GraphicsDevice, textureProvider, TileSize);
        }

        _overlayRenderer = new MapOverlayRenderer(GraphicsDevice, TileSize);
        _overlayRenderer.LoadContent();
        _tilePalette = new TilePalette();
        _debugTextRenderer = new DebugTextRenderer(GraphicsDevice);
        _hudBackgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
        _hudBackgroundTexture.SetData(new[] { new Color(0f, 0f, 0f, 0.65f) });
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
        HandleMapInput(keyboard);
        HandleHudInput(keyboard);
        HandleInfoPanelInput(keyboard);
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

        _terrainRenderer.Draw(_spriteBatch, _activeMap, _camera, _tilePalette, gameTime);
        _staticGraphicRenderer?.Draw(_spriteBatch, _sortedStaticGraphics, _camera);
        _overlayRenderer?.Draw(_spriteBatch, _activeMap, _camera, _overlayLayers);
        DrawHud();
        DrawInfoPanel();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.ClientSizeChanged -= OnClientSizeChanged;
            _spriteBatch?.Dispose();
            _terrainRenderer?.Dispose();
            _staticGraphicRenderer?.Dispose();
            _overlayRenderer?.Dispose();
            _debugTextRenderer?.Dispose();
            _hudBackgroundTexture?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeMapList()
    {
        if (_content is null)
        {
            _mapIds = Array.Empty<string>();
            return;
        }

        var maps = _content.Maps.ListMaps();
        _mapIds = maps.Count > 0 ? maps.ToArray() : new[] { "map_0" };
        _currentMapIndex = 0;
    }

    private void LoadMapByIndex(int index)
    {
        if (_content is null || _mapIds.Length == 0)
        {
            return;
        }

        var count = _mapIds.Length;
        if (count == 0)
        {
            return;
        }

        _currentMapIndex = (index % count + count) % count;
        var mapId = _mapIds[_currentMapIndex];
        try
        {
            _activeMap = _content.Maps.GetMap(mapId);
            PrepareStaticGraphics();
            ConfigureCameraBounds();
            Console.WriteLine($"Loaded map: {mapId} (index {_currentMapIndex + 1}/{count})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load map '{mapId}': {ex.Message}");
            _activeMap = null;
            _sortedStaticGraphics = Array.Empty<StaticGraphic>();
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

    private IReadOnlyList<string> ResolveGraphicRoots()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "content", "graphics"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "Original Pascal", "Laa", "grf"),
            Path.Combine(baseDir, "..", "..", "..", "Original Pascal", "Laa", "grf"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "Original Pascal", "Laa", "grf")
        };

        var roots = candidates
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roots.Length == 0)
        {
            Console.Error.WriteLine("Warning: no graphic directories were found. Place BMP/PNG exports under 'content/graphics' or keep the legacy 'Original Pascal/Laa/grf' folder nearby.");
        }

        return roots;
    }

    private void PrepareStaticGraphics()
    {
        if (_activeMap?.Graphics is null || _activeMap.Graphics.Count == 0)
        {
            _sortedStaticGraphics = Array.Empty<StaticGraphic>();
            return;
        }

        _sortedStaticGraphics = _activeMap.Graphics
            .OrderBy(g => g.SubLayer)
            .ThenBy(g => g.Y)
            .ThenBy(g => g.X)
            .ToArray();
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

    private void HandleInfoPanelInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(keyboardState, Keys.I))
        {
            ToggleInfoPanel(InfoPanel.Items);
        }
        else if (IsKeyPressed(keyboardState, Keys.S))
        {
            ToggleInfoPanel(InfoPanel.Spells);
        }
        else if (IsKeyPressed(keyboardState, Keys.M))
        {
            ToggleInfoPanel(InfoPanel.Monsters);
        }
        else if (IsKeyPressed(keyboardState, Keys.C))
        {
            ToggleInfoPanel(InfoPanel.Commerce);
        }
        else if (IsKeyPressed(keyboardState, Keys.F2))
        {
            ToggleInfoPanel(InfoPanel.World);
        }
    }

    private void HandleMapInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(keyboardState, Keys.OemOpenBrackets) || IsKeyPressed(keyboardState, Keys.PageDown))
        {
            LoadMapByIndex(_currentMapIndex - 1);
        }
        else if (IsKeyPressed(keyboardState, Keys.OemCloseBrackets) || IsKeyPressed(keyboardState, Keys.PageUp))
        {
            LoadMapByIndex(_currentMapIndex + 1);
        }
    }

    private void HandleHudInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(keyboardState, Keys.F1))
        {
            _showHud = !_showHud;
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

    private void ToggleInfoPanel(InfoPanel panel)
    {
        _infoPanel = _infoPanel == panel ? InfoPanel.None : panel;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void DrawHud()
    {
        if (!_showHud || _spriteBatch is null || _debugTextRenderer is null || _hudBackgroundTexture is null)
        {
            return;
        }

        var hudText = BuildHudText();
        var textSize = _debugTextRenderer.MeasureString(hudText, scale: 2f);
        var padding = new Vector2(12f, 12f);
        var backgroundRect = new Rectangle(
            8,
            8,
            (int)Math.Ceiling(textSize.X + padding.X * 2),
            (int)Math.Ceiling(textSize.Y + padding.Y * 2));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
        _spriteBatch.Draw(_hudBackgroundTexture, backgroundRect, Color.White);
        _debugTextRenderer.DrawString(
            _spriteBatch,
            hudText,
            new Vector2(backgroundRect.Left + padding.X, backgroundRect.Top + padding.Y),
            Color.White,
            scale: 2f);
        _spriteBatch.End();
    }

    private string BuildHudText()
    {
        if (_activeMap is null)
        {
            return "NO MAP LOADED";
        }

        var width = _activeMap.Terrain.FirstOrDefault()?.Count ?? 0;
        var height = _activeMap.Terrain.Count;
        var mapName = (_activeMap.Header.Name ?? _mapIds.ElementAtOrDefault(_currentMapIndex) ?? "N/A").ToUpperInvariant();
        var overlay = _overlayLayers.ToString().ToUpperInvariant();
        var sensorCount = _activeMap.Sensors.Count;
        var nestCount = _activeMap.Nests.Count;
        var merchantCount = _activeMap.Merchants.Count;
        var staticCount = _sortedStaticGraphics.Count;
        var currentMapLabel = _mapIds.Length > 0
            ? $"{_currentMapIndex + 1}/{_mapIds.Length}"
            : "0/0";

        var builder = new StringBuilder();
        builder.AppendLine($"MAP {mapName} ({currentMapLabel})");
        builder.AppendLine($"SIZE {width}X{height}  STATIC {staticCount}");
        builder.AppendLine($"SENSORS {sensorCount}  NESTS {nestCount}  MERCHANTS {merchantCount}");
        builder.AppendLine($"OVERLAY {overlay}");
        builder.Append("CONTROLS TAB CYCLE 0 NONE 1 SEN 2 NES 3 MER 4 ALL  +/- ZOOM  [] MAP  F1 HUD");
        return builder.ToString().ToUpperInvariant();
    }

    private void DrawInfoPanel()
    {
        if (_infoPanel == InfoPanel.None || _debugTextRenderer is null || _hudBackgroundTexture is null || _spriteBatch is null)
        {
            return;
        }

        var text = BuildInfoPanelText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var scale = 2f;
        var size = _debugTextRenderer.MeasureString(text, scale);
        var padding = new Vector2(12f, 12f);
        var viewport = GraphicsDevice.Viewport;
        var width = (int)Math.Ceiling(size.X + padding.X * 2);
        var height = (int)Math.Ceiling(size.Y + padding.Y * 2);
        var backgroundRect = new Rectangle(
            viewport.Width - width - 8,
            viewport.Height - height - 8,
            width,
            height);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
        _spriteBatch.Draw(_hudBackgroundTexture, backgroundRect, Color.White);
        _debugTextRenderer.DrawString(
            _spriteBatch,
            text,
            new Vector2(backgroundRect.Left + padding.X, backgroundRect.Top + padding.Y),
            Color.LightGreen,
            scale);
        _spriteBatch.End();
    }

    private string BuildInfoPanelText()
    {
        return _infoPanel switch
        {
            InfoPanel.Items => BuildItemsPanel(),
            InfoPanel.Spells => BuildSpellsPanel(),
            InfoPanel.Monsters => BuildMonstersPanel(),
            InfoPanel.Commerce => BuildCommercePanel(),
            InfoPanel.World => BuildWorldPanel(),
            _ => string.Empty
        };
    }

    private string BuildItemsPanel()
    {
        if (_itemDocument is null)
        {
            return "ITEM DATA UNAVAILABLE";
        }

        var builder = new StringBuilder();
        builder.AppendLine("ITEMS (F1 HUD / I TOGGLE)");
        var count = Math.Min(10, _itemDocument.Names.Count);
        for (var i = 0; i < count; i++)
        {
            var name = _itemDocument.Names[i] ?? "UNKNOWN";
            var cost = _itemDocument.Items[i].Cost;
            builder.AppendLine($"[{i:D3}] {name} ${cost}");
        }

        if (_itemDocument.Names.Count > count)
        {
            builder.AppendLine($"... +{_itemDocument.Names.Count - count} MORE");
        }

        return builder.ToString().ToUpperInvariant();
    }

    private string BuildSpellsPanel()
    {
        if (_spellDocument is null)
        {
            return "SPELL DATA UNAVAILABLE";
        }

        var builder = new StringBuilder();
        builder.AppendLine("SPELLS (S TOGGLE)");
        var count = Math.Min(10, _spellDocument.Names.Count);
        for (var i = 0; i < count; i++)
        {
            var desc = _spellDocument.Spells[i];
            var name = _spellDocument.Names[i] ?? "UNKNOWN";
            builder.AppendLine($"[{i:D2}] {name} L{desc.RequiredPlayerLevel} M{desc.RequiredMana} SCH{desc.School}");
        }

        if (_spellDocument.Names.Count > count)
        {
            builder.AppendLine($"... +{_spellDocument.Names.Count - count} MORE");
        }

        return builder.ToString().ToUpperInvariant();
    }

    private string BuildMonstersPanel()
    {
        if (_monsterDocument is null)
        {
            return "MONSTER DATA UNAVAILABLE";
        }

        var builder = new StringBuilder();
        builder.AppendLine("MONSTERS (M TOGGLE)");
        foreach (var monster in _monsterDocument.Monsters.Take(10))
        {
            builder.AppendLine($"{monster.Name} LV{monster.Level} HP{monster.AverageHp}");
        }

        if (_monsterDocument.Monsters.Count > 10)
        {
            builder.AppendLine($"... +{_monsterDocument.Monsters.Count - 10} MORE");
        }

        return builder.ToString().ToUpperInvariant();
    }

    private string BuildCommercePanel()
    {
        if (_commerceDocument is null)
        {
            return "COMMERCE DATA UNAVAILABLE";
        }

        var builder = new StringBuilder();
        builder.AppendLine("COMMERCE (C TOGGLE)");
        builder.AppendLine($"TOTAL SHOPS {_commerceDocument.Inventories.Count}");
        var previewCount = Math.Min(5, _commerceDocument.Inventories.Count);

        for (var i = 0; i < previewCount; i++)
        {
            var slots = _commerceDocument.Inventories[i].Items;
            var names = slots.Take(4)
                .Select(slot => ResolveItemName(slot.Id))
                .ToArray();
            var line = names.Length > 0 ? string.Join(", ", names) : "EMPTY";
            builder.AppendLine($"[{i:D2}] {line}");
        }

        if (_commerceDocument.Inventories.Count > previewCount)
        {
            builder.AppendLine($"... +{_commerceDocument.Inventories.Count - previewCount} MORE");
        }

        return builder.ToString().ToUpperInvariant();
    }

    private string BuildWorldPanel()
    {
        if (_activeMap is null)
        {
            return "MAP DATA UNAVAILABLE";
        }

        var builder = new StringBuilder();
        builder.AppendLine("WORLD DATA (F2 TOGGLE)");
        builder.AppendLine($"SENSORS {_activeMap.Sensors.Count}  NESTS {_activeMap.Nests.Count}  MERCHANTS {_activeMap.Merchants.Count}");

        var sensorPreview = _activeMap.Sensors.Take(5).ToList();
        if (sensorPreview.Count > 0)
        {
            builder.AppendLine("SENSORS:");
            foreach (var sensor in sensorPreview)
            {
                var text = string.IsNullOrWhiteSpace(sensor.Text) ? "-" : sensor.Text.Split('\\').FirstOrDefault() ?? "-";
                builder.AppendLine($"T{sensor.Type:D2} ({sensor.X},{sensor.Y}) {text}");
            }
        }

        var nestPreview = _activeMap.Nests.Take(5).ToList();
        if (nestPreview.Count > 0)
        {
            builder.AppendLine("NESTS:");
            foreach (var nest in nestPreview)
            {
                builder.AppendLine($"TYPE {nest.Type:D2} ({nest.X},{nest.Y}) QTY {nest.Quantity}");
            }
        }

        var merchantPreview = _activeMap.Merchants.Take(3).ToList();
        if (merchantPreview.Count > 0)
        {
            builder.AppendLine("MERCHANTS:");
            foreach (var merchant in merchantPreview)
            {
                var text = string.IsNullOrWhiteSpace(merchant.Text) ? "-" : merchant.Text;
                builder.AppendLine($"TYPE {merchant.Type:D2} ({merchant.X},{merchant.Y}) {text}");
            }
        }

        return builder.ToString().ToUpperInvariant();
    }

    private string ResolveItemName(int itemId)
    {
        if (_itemDocument is null)
        {
            return $"ITEM #{itemId:D3}";
        }

        if (itemId >= 0 && itemId < _itemDocument.Names.Count)
        {
            var name = _itemDocument.Names[itemId];
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return $"ITEM #{itemId:D3}";
    }
}
