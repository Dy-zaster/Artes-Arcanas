using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Laa.Content.Core.Animations;
using Laa.Content.Core.Commerce;
using Laa.Content.Core.Graphics;
using Laa.Content.Core.Items;
using Laa.Content.Core.Mappings;
using Laa.Content.Core.Maps;
using Laa.Content.Core.Monsters;
using Laa.Content.Core.Spells;
using Laa.Monogame.Client.Content;
using Laa.Monogame.Client.Networking;
using Laa.Monogame.Client.Rendering;
using Laa.Monogame.Client.UI;
using Laa.Monogame.Client.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Laa.Monogame.Client;

public class Game1 : Game
{
    private const int TileWidth = 24;
    private const int TileHeight = 16;
    private const int AvatarArmorCount = 32;
    private const int AvatarClassCount = 8;
    private const int AvatarRaceCount = 8;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private ContentContext? _content;
    private MapDocument? _activeMap;
    private GraphicDocument? _graphicsCatalog;
    private AnimationDocument? _animationCatalog;
    private AnimationMappingDocument? _animationMapping;
    private bool _loggedContent;
    private TerrainRenderer? _terrainRenderer;
    private StaticGraphicRenderer? _staticGraphicRenderer;
    private AnimationTextureProvider? _animationTextureProvider;
    private AnimationPreviewPlayer? _animationPreview;
    private MonsterRenderer? _monsterRenderer;
    private MonsterSimulation? _monsterSimulation;
    private MapOverlayRenderer? _overlayRenderer;
    private DebugTextRenderer? _debugTextRenderer;
    private TilePalette? _tilePalette;
    private Camera2D? _camera;
    private CameraController? _cameraController;
    private OverlayLayers _overlayLayers = OverlayLayers.None;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private IReadOnlyList<StaticGraphic> _sortedStaticGraphics = Array.Empty<StaticGraphic>();
    private readonly WorldState _worldState = new();
    private INetworkClient? _networkClient;
    private readonly ConcurrentQueue<NetworkMessage> _networkQueue = new();
    private readonly ServerCommandDecoder _serverCommandDecoder = new();
    private bool _networkFeedActive;
    private bool _loggedDirectionPlaceholder;
    private UiManager? _uiManager;
    private UiPanel? _uiRoadmapWindow;
    private UiPanel? _uiInventoryWindow;
    private UiItemGridWidget? _uiInventoryWidget;
    private UiPanel? _uiSpellWindow;
    private UiSpellListWidget? _uiSpellWidget;
    private UiPanel? _uiMerchantWindow;
    private UiItemGridWidget? _uiMerchantWidget;
    private UiLabel? _uiMerchantInfoLabel;
    private int _merchantPreviewIndex;
    private Texture2D? _hudBackgroundTexture;
    private bool _showHud = true;
    private bool _showAnimationPreview;
    private bool _avatarPreviewMode;
    private int _previewArmorIndex;
    private int _previewClassIndex;
    private int _previewRaceIndex;
    private bool _previewIsMale = true;
    private byte _previewAnimationId;
    private string[] _mapIds = Array.Empty<string>();
    private int _currentMapIndex;
    private ItemDocument? _itemDocument;
    private SpellDocument? _spellDocument;
    private MonsterDocument? _monsterDocument;
    private CommerceDocument? _commerceDocument;
    private InfoPanel _infoPanel = InfoPanel.None;
    private Vector2 _animationPreviewAnchor = Vector2.Zero;

    private static readonly string[] PlayerClassNames =
    {
        "Guerrero",
        "Clerigo",
        "Mago",
        "Bribon",
        "Montaraz",
        "Paladin",
        "Bardo",
        "Guerrero-Mago"
    };

    private static readonly string[] PlayerRaceNames =
    {
        "Humano",
        "Elfo",
        "Enano",
        "Gnomo",
        "Semielfo",
        "Orco",
        "Drow",
        "Desconocido"
    };

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
        _animationCatalog = _content.Animations.GetAnimations();
        _animationMapping = _content.AnimationMappings.GetMappings();
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
        _terrainRenderer = new TerrainRenderer(GraphicsDevice, TileWidth, TileHeight, textureRoots);
        _terrainRenderer.LoadContent();
        if (_animationCatalog is not null)
        {
            _animationTextureProvider = new AnimationTextureProvider(GraphicsDevice, _animationCatalog, textureRoots);
            _animationPreview = new AnimationPreviewPlayer(
                _animationTextureProvider,
                _animationCatalog.Animations.Select(a => a.Key));
            _monsterRenderer = new MonsterRenderer(_animationTextureProvider);
            _monsterSimulation = new MonsterSimulation();
            RebuildMonsterEntities();
        }
        if (_graphicsCatalog is not null)
        {
            var textureProvider = new GraphicTextureProvider(GraphicsDevice, _graphicsCatalog, textureRoots);
            _staticGraphicRenderer = new StaticGraphicRenderer(GraphicsDevice, textureProvider, TileWidth, TileHeight);
        }

        _overlayRenderer = new MapOverlayRenderer(GraphicsDevice, TileWidth, TileHeight);
        _overlayRenderer.LoadContent();
        _tilePalette = new TilePalette();
        _debugTextRenderer = new DebugTextRenderer(GraphicsDevice);
        _hudBackgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
        _hudBackgroundTexture.SetData(new[] { new Color(0f, 0f, 0f, 0.65f) });
        InitializeUiSystem();
        InitializeNetworkClient();
    }

    private void InitializeUiSystem()
    {
        if (_debugTextRenderer is null)
        {
            return;
        }

        _uiManager?.Dispose();
        _uiManager = new UiManager(GraphicsDevice, _debugTextRenderer);
        var viewport = GraphicsDevice.Viewport;
        var roadmapBounds = new Rectangle(
            viewport.Width - 360,
            64,
            320,
            200);
        _uiRoadmapWindow = new UiPanel("UI ROADMAP", roadmapBounds)
        {
            Draggable = true
        };
        _uiRoadmapWindow.AddWidget(new UiLabel(
            "1. OVERLAY FRAMEWORK (ACTIVE)\n2. INVENTORY WINDOW\n3. SPELLBOOK & MERCHANT UI",
            new Vector2(12f, 12f),
            Color.White));
        _uiManager.AddWindow(_uiRoadmapWindow);

        var inventoryBounds = new Rectangle(40, 64, 360, 220);
        _uiInventoryWindow = new UiPanel("INVENTORY PREVIEW", inventoryBounds)
        {
            Draggable = true,
            Visible = false
        };
        _uiInventoryWindow.AddWidget(new UiLabel(
            "MOSTRANDO 12 OBJETOS DE EJEMPLO",
            new Vector2(12f, 12f),
            Color.Yellow));
        _uiInventoryWidget = new UiItemGridWidget(columns: 2, cellSize: new Vector2(160f, 22f), color: Color.White)
        {
            Offset = new Vector2(0f, 32f)
        };
        _uiInventoryWidget.SetItems(BuildInventoryPreviewItems());
        _uiInventoryWindow.AddWidget(_uiInventoryWidget);
        _uiManager.AddWindow(_uiInventoryWindow);

        var spellBounds = new Rectangle(viewport.Width - 420, 300, 380, 260);
        _uiSpellWindow = new UiPanel("SPELLBOOK", spellBounds)
        {
            Draggable = true,
            Visible = false
        };
        _uiSpellWindow.AddWidget(new UiLabel(
            "AGRUPADO POR ESCUELA",
            new Vector2(12f, 12f),
            Color.Yellow));
        _uiSpellWidget = new UiSpellListWidget
        {
            LineHeight = 18f
        };
        _uiSpellWidget.SetGroups(BuildSpellGroups());
        _uiSpellWindow.AddWidget(_uiSpellWidget);
        _uiManager.AddWindow(_uiSpellWindow);

        var merchantBounds = new Rectangle(420, 64, 360, 220);
        _uiMerchantWindow = new UiPanel("MERCHANT PREVIEW", merchantBounds)
        {
            Draggable = true,
            Visible = false
        };
        _uiMerchantInfoLabel = new UiLabel(string.Empty, new Vector2(12f, 12f), Color.Yellow);
        _uiMerchantWindow.AddWidget(_uiMerchantInfoLabel);
        _uiMerchantWidget = new UiItemGridWidget(columns: 1, cellSize: new Vector2(320f, 22f), color: Color.White)
        {
            Offset = new Vector2(0f, 32f)
        };
        RefreshMerchantPreview();
        _uiMerchantWindow.AddWidget(_uiMerchantWidget);
        _uiManager.AddWindow(_uiMerchantWindow);
    }

    private void InitializeNetworkClient()
    {
        _networkFeedActive = false;
        if (_networkClient is not null)
        {
            _networkClient.MessageReceived -= OnNetworkMessageReceived;
            _networkClient.Dispose();
            _networkClient = null;
        }

        try
        {
            var client = new MockNetworkClient();
            client.MessageReceived += OnNetworkMessageReceived;
            client.ConnectAsync("mock", 0).GetAwaiter().GetResult();
            _networkClient = client;
            _networkFeedActive = true;
            Console.WriteLine("Mock network client connected (scripted monster updates).");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize mock network client: {ex.Message}");
        }
    }

    private void OnNetworkMessageReceived(object? sender, NetworkEventArgs e)
    {
        if (e?.Message is null)
        {
            return;
        }

        _networkQueue.Enqueue(e.Message);
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
        HandleAnimationPreviewInput(keyboard);
        HandleMonsterDebugInput(keyboard);
        HandleUiInput(keyboard);
        ProcessNetworkEvents();
        _cameraController?.Update(gameTime, keyboard, mouse);
        _animationPreview?.Update(gameTime);
        if (!_networkFeedActive)
        {
            _monsterSimulation?.Update(gameTime);
        }
        _uiManager?.Update(gameTime, mouse, _previousMouse);
        UpdateMonsterRenderer(gameTime);

        if (!_loggedContent)
        {
            _loggedContent = true;
            LogContentSummary();
        }

        base.Update(gameTime);

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
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
        DrawMonsters();
        _staticGraphicRenderer?.Draw(_spriteBatch, _sortedStaticGraphics, _camera);
        DrawAnimationPreview();
        _overlayRenderer?.Draw(_spriteBatch, _activeMap, _camera, _overlayLayers);
        DrawHud();
        DrawInfoPanel();
        if (_spriteBatch is not null)
        {
            _uiManager?.Draw(_spriteBatch);
        }

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
            _animationTextureProvider?.Dispose();
            _overlayRenderer?.Dispose();
            _debugTextRenderer?.Dispose();
            _hudBackgroundTexture?.Dispose();
            _uiManager?.Dispose();
            if (_networkClient is not null)
            {
                _networkClient.MessageReceived -= OnNetworkMessageReceived;
                _networkClient.Dispose();
                _networkClient = null;
                _networkFeedActive = false;
            }
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
            UpdateAnimationPreviewAnchor();
            RebuildMonsterEntities();
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
        var animationCount = _content.Animations.GetAnimations().Animations.Count;
        Console.WriteLine($"Loaded Map: {mapName} | Items: {itemCount} | Spells: {spellCount} | Monsters: {monsterCount} | Animations: {animationCount}");
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

        var pixelWidth = width * TileWidth;
        var pixelHeight = height * TileHeight;
        _camera.SetWorldSize(pixelWidth, pixelHeight);
        _camera.CenterOn(new Vector2(pixelWidth / 2f, pixelHeight / 2f));
    }

    private void UpdateAnimationPreviewAnchor()
    {
        if (_activeMap is null)
        {
            _animationPreviewAnchor = Vector2.Zero;
            return;
        }

        var width = _activeMap.Terrain.FirstOrDefault()?.Count ?? 0;
        var height = _activeMap.Terrain.Count;
        if (width == 0 || height == 0)
        {
            _animationPreviewAnchor = Vector2.Zero;
            return;
        }

        _animationPreviewAnchor = new Vector2(width * TileWidth / 2f, height * TileHeight / 2f);
    }

    private static int WrapValue(int value, int delta, int modulo)
    {
        if (modulo <= 0)
        {
            return value;
        }

        var next = (value + delta) % modulo;
        return next < 0 ? next + modulo : next;
    }

    private void UpdateAvatarPreviewAnimation()
    {
        if (!_avatarPreviewMode || _animationPreview is null || _animationMapping is null)
        {
            return;
        }

        var ids = _animationMapping.AnimationIds;
        if (ids.Count == 0)
        {
            return;
        }

        var genderMask = _previewIsMale ? 0 : (1 << 11);
        var index = (_previewArmorIndex & 0x1F) |
                    ((_previewClassIndex & 0x7) << 5) |
                    ((_previewRaceIndex & 0x7) << 8) |
                    genderMask;

        if (index < 0 || index >= ids.Count)
        {
            Console.Error.WriteLine($"Avatar animation index {index} out of range.");
            return;
        }

        _previewAnimationId = ids[index];
        var key = $"m{_previewAnimationId}";
        if (!_animationPreview.TrySetAnimation(key))
        {
            Console.Error.WriteLine($"Avatar preview animation '{key}' not found in atlas.");
        }
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
            .OrderBy(g => ((g.Y << 9) | g.SubLayer))
            .ThenBy(g => g.X)
            .ToArray();
    }

    private void RebuildMonsterEntities()
    {
        var monsters = new List<MonsterEntity>();
        if (_activeMap is not null && _monsterDocument is not null)
        {
            var monsterLookup = _monsterDocument.Monsters
                .GroupBy(m => (int)m.TypeId)
                .ToDictionary(g => g.Key, g => g.First(), comparer: EqualityComparer<int>.Default);

            var identifier = 0;
            foreach (var nest in _activeMap.Nests)
            {
                if (!monsterLookup.TryGetValue(nest.Type, out var descriptor))
                {
                    continue;
                }

                var key = $"m{descriptor.TypeId}";
                var anchor = new Vector2(
                    (nest.X + 0.5f) * TileWidth,
                    (nest.Y + 1f) * TileHeight);
                monsters.Add(new MonsterEntity(identifier++, descriptor, anchor, key));
            }
        }

        _worldState.SetMonsters(monsters);
        _monsterRenderer?.SetMonsters(_worldState.Monsters);
        _monsterSimulation?.SetMonsters(_worldState.Monsters);
    }

    private void ProcessNetworkEvents()
    {
        if (_networkQueue.IsEmpty)
        {
            return;
        }

        while (_networkQueue.TryDequeue(out var message))
        {
            if (message.Type == NetworkMessageType.RawServerStream)
            {
                ProcessServerStream(message.Payload.Span);
                continue;
            }

            HandleNetworkMessage(message);
        }
    }

    private void ProcessServerStream(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return;
        }

        _serverCommandDecoder.Enqueue(payload);
        foreach (var command in _serverCommandDecoder.Decode())
        {
            ApplyServerCommand(command);
        }
    }

    private void HandleNetworkMessage(NetworkMessage message)
    {
        if (!TryDecodeMonsterPayload(message.Payload.Span, out var payload))
        {
            Console.Error.WriteLine($"[NET] Ignoring malformed payload for {message.Type}.");
            return;
        }

        switch (message.Type)
        {
            case NetworkMessageType.MonsterSpawn:
                HandleMonsterSpawn(payload);
                break;
            case NetworkMessageType.MonsterMove:
                HandleMonsterMove(payload);
                break;
            case NetworkMessageType.MonsterAttack:
                HandleMonsterAttack(payload);
                break;
            case NetworkMessageType.MonsterDeath:
                HandleMonsterDeath(payload);
                break;
            default:
                Console.WriteLine($"[NET] Unhandled message type {message.Type}.");
                break;
        }
    }

    private void ApplyServerCommand(IServerCommand command)
    {
        switch (command)
        {
            case SpritePositionCommand single:
                ApplySpritePosition(single.SpriteId, single.X, single.Y);
                break;
            case SpriteBatchPositionCommand batch:
                foreach (var entry in batch.Entries)
                {
                    ApplySpritePosition(entry.SpriteId, entry.X, entry.Y);
                }
                break;
            case SpriteActionCommand action:
                ApplySpriteAction(action.SpriteId, action.Action);
                break;
            case SpriteDirectionCommand direction:
                ApplySpriteDirection(direction.SpriteId, direction.Direction);
                break;
            case LocalPlayerPositionCommand local:
                Console.WriteLine($"[NET] Local player moved to ({local.X},{local.Y}) dir {local.Direction}.");
                break;
            default:
                Console.WriteLine($"[NET] Unhandled command {command.Type}.");
                break;
        }
    }

    private void ApplySpritePosition(int spriteId, byte tileX, byte tileY)
    {
        var position = TileToWorldPosition(tileX, tileY);
        var payload = new MonsterNetworkPayload(spriteId, position, MonsterAction.Moving, 0);
        HandleMonsterMove(payload);
    }

    private void ApplySpriteAction(int spriteId, byte actionCode)
    {
        var action = TranslateServerAction(actionCode);
        var monster = _worldState.FindMonster(spriteId);
        if (monster is null)
        {
            Console.Error.WriteLine($"[NET] Action for unknown sprite {spriteId}.");
            return;
        }

        monster.SetAction(action);
        if (action == MonsterAction.Dead)
        {
            monster.ResetPosition();
        }
    }

    private void ApplySpriteDirection(int spriteId, byte direction)
    {
        if (!_loggedDirectionPlaceholder)
        {
            Console.WriteLine("[NET] Sprite direction updates acknowledged (renderer not yet synced).");
            _loggedDirectionPlaceholder = true;
        }
    }

    private void HandleMonsterSpawn(in MonsterNetworkPayload payload)
    {
        var monster = ResolveMonster(payload.Id, NetworkMessageType.MonsterSpawn);
        if (monster is null)
        {
            return;
        }

        if (payload.Position != Vector2.Zero)
        {
            monster.SetPosition(payload.Position);
        }
        else
        {
            monster.ResetPosition();
        }

        monster.SetAction(payload.Action);
    }

    private void HandleMonsterMove(in MonsterNetworkPayload payload)
    {
        var monster = ResolveMonster(payload.Id, NetworkMessageType.MonsterMove);
        if (monster is null)
        {
            return;
        }

        if (payload.Position != Vector2.Zero)
        {
            monster.SetPosition(payload.Position);
        }

        monster.SetAction(payload.Action);
    }

    private void HandleMonsterAttack(in MonsterNetworkPayload payload)
    {
        var monster = ResolveMonster(payload.Id, NetworkMessageType.MonsterAttack);
        if (monster is null)
        {
            return;
        }

        monster.SetAction(MonsterAction.Attack);
    }

    private void HandleMonsterDeath(in MonsterNetworkPayload payload)
    {
        var monster = ResolveMonster(payload.Id, NetworkMessageType.MonsterDeath);
        if (monster is null)
        {
            return;
        }

        monster.SetAction(MonsterAction.Dead);
        monster.ResetPosition();
    }

    private MonsterEntity? ResolveMonster(int id, NetworkMessageType context)
    {
        var monster = _worldState.FindMonster(id);
        if (monster is null)
        {
            Console.Error.WriteLine($"[NET] Monster {id} not found for {context} message.");
        }

        return monster;
    }

    private static bool TryDecodeMonsterPayload(ReadOnlySpan<byte> payload, out MonsterNetworkPayload result)
    {
        result = default;
        if (payload.Length < 13)
        {
            return false;
        }

        try
        {
            var id = BitConverter.ToInt32(payload.Slice(0, 4));
            var posX = BitConverter.ToSingle(payload.Slice(4, 4));
            var posY = BitConverter.ToSingle(payload.Slice(8, 4));
            var action = (MonsterAction)payload[12];
            var seed = payload.Length >= 17 ? BitConverter.ToInt32(payload.Slice(13, 4)) : 0;
            result = new MonsterNetworkPayload(id, new Vector2(posX, posY), action, seed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly record struct MonsterNetworkPayload(int Id, Vector2 Position, MonsterAction Action, int Seed);

    private static Vector2 TileToWorldPosition(byte tileX, byte tileY)
    {
        return new Vector2(
            (tileX + 0.5f) * TileWidth,
            (tileY + 1f) * TileHeight);
    }

    private static MonsterAction TranslateServerAction(byte action)
    {
        return action switch
        {
            0 => MonsterAction.Idle,
            1 => MonsterAction.Moving,
            2 => MonsterAction.Attack,
            3 => MonsterAction.Dead,
            _ => MonsterAction.Idle
        };
    }

    private void UpdateMonsterRenderer(GameTime gameTime)
    {
        _monsterRenderer?.Update(gameTime);
    }

    private void CycleMonsterActions()
    {
        foreach (var monster in _worldState.Monsters)
        {
            var next = monster.Action switch
            {
                MonsterAction.Idle => MonsterAction.Moving,
                MonsterAction.Moving => MonsterAction.Attack,
                MonsterAction.Attack => MonsterAction.Dead,
                _ => MonsterAction.Idle
            };
            monster.SetAction(next);
        }
    }

    private void ToggleMonsterAttackMode()
    {
        foreach (var monster in _worldState.Monsters)
        {
            var next = monster.Action == MonsterAction.Attack ? MonsterAction.Idle : MonsterAction.Attack;
            monster.SetAction(next);
        }
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

    private bool HandleAvatarModeInput(KeyboardState keyboardState)
    {
        var updated = false;
        if (IsKeyPressed(keyboardState, Keys.U))
        {
            _previewArmorIndex = WrapValue(_previewArmorIndex, 1, AvatarArmorCount);
            updated = true;
        }
        else if (IsKeyPressed(keyboardState, Keys.J))
        {
            _previewArmorIndex = WrapValue(_previewArmorIndex, -1, AvatarArmorCount);
            updated = true;
        }

        if (IsKeyPressed(keyboardState, Keys.I))
        {
            _previewClassIndex = WrapValue(_previewClassIndex, 1, AvatarClassCount);
            updated = true;
        }
        else if (IsKeyPressed(keyboardState, Keys.K))
        {
            _previewClassIndex = WrapValue(_previewClassIndex, -1, AvatarClassCount);
            updated = true;
        }

        if (IsKeyPressed(keyboardState, Keys.O))
        {
            _previewRaceIndex = WrapValue(_previewRaceIndex, 1, AvatarRaceCount);
            updated = true;
        }
        else if (IsKeyPressed(keyboardState, Keys.L))
        {
            _previewRaceIndex = WrapValue(_previewRaceIndex, -1, AvatarRaceCount);
            updated = true;
        }

        if (IsKeyPressed(keyboardState, Keys.P))
        {
            _previewIsMale = !_previewIsMale;
            updated = true;
        }

        return updated;
    }

    private void HandleAnimationPreviewInput(KeyboardState keyboardState)
    {
        if (_animationPreview is null)
        {
            return;
        }

        if (IsKeyPressed(keyboardState, Keys.F5))
        {
            _showAnimationPreview = !_showAnimationPreview;
        }

        if (IsKeyPressed(keyboardState, Keys.F10))
        {
            _avatarPreviewMode = !_avatarPreviewMode;
            if (_avatarPreviewMode)
            {
                if (!_showAnimationPreview)
                {
                    _showAnimationPreview = true;
                }
                UpdateAvatarPreviewAnimation();
            }
        }

        if (!_showAnimationPreview)
        {
            return;
        }

        if (IsKeyPressed(keyboardState, Keys.F6))
        {
            _animationPreview.StepAnimation(1);
        }
        else if (IsKeyPressed(keyboardState, Keys.F7))
        {
            _animationPreview.StepAnimation(-1);
        }

        if (IsKeyPressed(keyboardState, Keys.F8))
        {
            _animationPreview.StepDirection(1);
        }

        if (IsKeyPressed(keyboardState, Keys.F9))
        {
            _animationPreview.ToggleMirror();
        }

        if (_avatarPreviewMode && HandleAvatarModeInput(keyboardState))
        {
            UpdateAvatarPreviewAnimation();
        }
    }

    private void HandleUiInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(keyboardState, Keys.F3) && _uiRoadmapWindow is not null)
        {
            _uiRoadmapWindow.Visible = !_uiRoadmapWindow.Visible;
        }

        if (IsKeyPressed(keyboardState, Keys.F4) && _uiInventoryWindow is not null)
        {
            _uiInventoryWindow.Visible = !_uiInventoryWindow.Visible;
        }

        if (IsKeyPressed(keyboardState, Keys.B) && _uiSpellWindow is not null)
        {
            _uiSpellWindow.Visible = !_uiSpellWindow.Visible;
        }

        if (IsKeyPressed(keyboardState, Keys.V) && _uiMerchantWindow is not null)
        {
            _uiMerchantWindow.Visible = !_uiMerchantWindow.Visible;
        }

        if (_commerceDocument is not null && _commerceDocument.Inventories.Count > 0)
        {
            if (IsKeyPressed(keyboardState, Keys.OemComma))
            {
                CycleMerchantPreview(-1);
            }
            else if (IsKeyPressed(keyboardState, Keys.OemPeriod))
            {
                CycleMerchantPreview(1);
            }
        }
    }

    private void HandleMonsterDebugInput(KeyboardState keyboardState)
    {
        if (_worldState.Monsters.Count == 0)
        {
            return;
        }

        if (IsKeyPressed(keyboardState, Keys.F11))
        {
            CycleMonsterActions();
        }
        else if (IsKeyPressed(keyboardState, Keys.F12))
        {
            ToggleMonsterAttackMode();
        }
    }

    private IReadOnlyList<string> BuildInventoryPreviewItems()
    {
        if (_itemDocument is null || _itemDocument.Names.Count == 0)
        {
            return new[] { "SIN DATOS DE ITEMS" };
        }

        var count = Math.Min(12, _itemDocument.Names.Count);
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var name = _itemDocument.Names[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                list.Add($"ITEM #{i:D3}");
            }
            else
            {
                list.Add(name);
            }
        }

        return list;
    }

    private IReadOnlyList<UiSpellGroup> BuildSpellGroups()
    {
        if (_spellDocument is null || _spellDocument.Spells.Count == 0)
        {
            return new[] { new UiSpellGroup("SIN HECHIZOS", Array.Empty<string>()) };
        }

        var groups = new Dictionary<byte, List<string>>();
        for (var i = 0; i < _spellDocument.Spells.Count; i++)
        {
            var descriptor = _spellDocument.Spells[i];
            var school = descriptor.School;
            var name = _spellDocument.Names.ElementAtOrDefault(i);
            var resolvedName = string.IsNullOrWhiteSpace(name) ? $"HECHIZO #{i:D3}" : name;
            var entry = $"{resolvedName}  MN {descriptor.RequiredMana:D2}  LV {descriptor.RequiredPlayerLevel:D2}";
            if (!groups.TryGetValue(school, out var list))
            {
                list = new List<string>();
                groups[school] = list;
            }
            list.Add(entry);
        }

        var ordered = new List<UiSpellGroup>();
        foreach (var (school, entries) in groups.OrderBy(kvp => kvp.Key))
        {
            ordered.Add(new UiSpellGroup(ResolveSpellSchoolName(school), entries));
        }

        return ordered;
    }

    private static string ResolveSpellSchoolName(byte school)
    {
        return school switch
        {
            0 => "GENERAL",
            1 => "OFENSIVO",
            2 => "DEFENSIVO",
            3 => "APOYO",
            4 => "INVOCACION",
            5 => "BENDICION",
            6 => "MALDICION",
            _ => $"ESCUELA #{school}"
        };
    }

    private void CycleMerchantPreview(int delta)
    {
        if (_commerceDocument is null || _commerceDocument.Inventories.Count == 0)
        {
            return;
        }

        _merchantPreviewIndex = WrapValue(_merchantPreviewIndex, delta, _commerceDocument.Inventories.Count);
        RefreshMerchantPreview();
    }

    private void RefreshMerchantPreview()
    {
        if (_uiMerchantInfoLabel is null || _uiMerchantWidget is null)
        {
            return;
        }

        if (_commerceDocument is null || _commerceDocument.Inventories.Count == 0)
        {
            _uiMerchantInfoLabel.Text = "SIN DATOS DE COMERCIO";
            _uiMerchantWidget.SetItems(Array.Empty<string>());
            return;
        }

        var clampedIndex = Math.Clamp(_merchantPreviewIndex, 0, _commerceDocument.Inventories.Count - 1);
        _merchantPreviewIndex = clampedIndex;
        var total = _commerceDocument.Inventories.Count;
        var title = $"MOSTRANDO INVENTARIO {clampedIndex + 1}/{total}";
        _uiMerchantInfoLabel.Text = title;
        _uiMerchantWidget.SetItems(BuildMerchantPreviewItems(clampedIndex));
    }

    private IReadOnlyList<string> BuildMerchantPreviewItems(int inventoryIndex)
    {
        if (_commerceDocument is null || _commerceDocument.Inventories.Count == 0)
        {
            return new[] { "SIN DATOS DE COMERCIO" };
        }

        var clamped = Math.Clamp(inventoryIndex, 0, _commerceDocument.Inventories.Count - 1);
        var inventory = _commerceDocument.Inventories[clamped];
        if (inventory.Items.Count == 0)
        {
            return new[] { "EL INVENTARIO ESTA VACIO" };
        }

        var items = new List<string>();
        foreach (var slot in inventory.Items)
        {
            if (items.Count >= 12)
            {
                break;
            }

            var name = ResolveItemName(slot.Id);
            var cost = ResolveItemCost(slot.Id);
            var modifier = slot.Modifier > 0 ? $" MOD {slot.Modifier}" : string.Empty;
            items.Add($"{name} ${cost}{modifier}");
        }

        return items;
    }

    private int ResolveItemCost(int itemId)
    {
        if (_itemDocument is null || itemId < 0 || itemId >= _itemDocument.Items.Count)
        {
            return 0;
        }

        return _itemDocument.Items[itemId].Cost;
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

        var animationLine = BuildAnimationPreviewLine();

        var builder = new StringBuilder();
        builder.AppendLine($"MAP {mapName} ({currentMapLabel})");
        builder.AppendLine($"SIZE {width}X{height}  STATIC {staticCount}");
        builder.AppendLine($"SENSORS {sensorCount}  NESTS {nestCount}  MERCHANTS {merchantCount}");
        builder.AppendLine($"OVERLAY {overlay}");
        if (!string.IsNullOrEmpty(animationLine))
        {
            builder.AppendLine(animationLine);
        }
        builder.Append("CONTROLS TAB CYCLE 0 NONE 1 SEN 2 NES 3 MER 4 ALL  +/- ZOOM  [] MAP  F1 HUD  F5 PREVIEW  F6/F7 ANIM  F8 DIR  F9 MIR  F10 AV MODE  J/U ARM  K/I CLASS  L/O RACE  P GEND  F11 MON CYCLE  F12 MON ATT  F3 UI PLAN  F4 INV  B SPELLS  V MERCHANT  ,/. MERCH +/-");
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

    private void DrawAnimationPreview()
    {
        if (!_showAnimationPreview || _animationPreview is null || _spriteBatch is null || _camera is null)
        {
            return;
        }

        _animationPreview.Draw(_spriteBatch, _camera, _animationPreviewAnchor);
    }

    private void DrawMonsters()
    {
        if (_monsterRenderer is null || _spriteBatch is null || _camera is null)
        {
            return;
        }

        _monsterRenderer.Draw(_spriteBatch, _camera);
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

    private string BuildAnimationPreviewLine()
    {
        if (_animationPreview is null)
        {
            return string.Empty;
        }

        if (!_showAnimationPreview || !_animationPreview.HasAnimation)
        {
            return "ANIM PREVIEW OFF (F5 TOGGLE)";
        }

        var key = _animationPreview.CurrentKey ?? "N/A";
        var direction = _animationPreview.DirectionIndex;
        var mirror = _animationPreview.Mirror ? "MIR" : "FWD";
        var kind = _animationPreview.CurrentKind?.ToString() ?? "UNK";
        var builder = new StringBuilder();
        builder.Append($"ANIM {key} DIR {direction} {mirror} {kind}");
        if (_avatarPreviewMode)
        {
            var className = PlayerClassNames[Math.Clamp(_previewClassIndex, 0, PlayerClassNames.Length - 1)];
            var raceName = PlayerRaceNames[Math.Clamp(_previewRaceIndex, 0, PlayerRaceNames.Length - 1)];
            var gender = _previewIsMale ? "M" : "F";
            builder.Append($" AV {className}/{raceName} ARM {_previewArmorIndex:D2} G {gender} ID {_previewAnimationId:D3}");
        }

        return builder.ToString();
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
