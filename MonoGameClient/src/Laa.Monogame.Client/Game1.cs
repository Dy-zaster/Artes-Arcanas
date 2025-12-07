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
    private const float HudContentOffsetY = 16f;
    private const int PortraitTileSize = 40;
    private const string HudControlsHelpText = "[F1] UI  [F5] Animaciones  [PgUp/PgDn] Mapas  [F2] Panel Mundial";

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
    private UiPanel? _uiHudPanel;
    private UiPanel? _uiRoadmapWindow;
    private UiPanel? _uiInventoryWindow;
    private UiInventoryWidget? _uiInventoryWidget;
    private UiLabel? _uiInventorySelectionLabel;
    private UiLabel? _uiItemDetailLabel;
    private UiPanel? _uiSpellWindow;
    private UiSpellListWidget? _uiSpellWidget;
    private UiPanel? _uiMerchantWindow;
    private UiListWidget? _uiMerchantWidget;
    private UiLabel? _uiMerchantInfoLabel;
    private UiSpriteLibrary? _uiSpriteLibrary;
    private UiMinimapWidget? _uiMinimapWidget;
    private HudPaperDollWidget? _hudPaperDollWidget;
    private HudPortraitWidget? _hudPortraitWidget;
    private HudQuickActionWidget? _hudQuickActionWidget;
    private UiLabel? _uiMapNameLabel;
    private string _welcomeMessage =
        "Bienvenido al mundo de Artes Arcanas\nEl servidor permite usar varios avatares al mismo tiempo.";
    private Vector2 _welcomeMessagePosition = new(16f, 96f);
    private UiLabel? _uiHudHintLabel;
    private UiLabel? _uiHudStatsLabel;
    private UiLabel? _uiHudSkillLabel;
    private UiLabel? _uiHudCombatStatsLabel;
    private UiLabel? _uiHudItemDetailLabel;
    private UiLabel? _uiHudSpellDetailLabel;
    private UiLabel? _uiHudHealthLabel;
    private UiLabel? _uiHudManaLabel;
    private UiLabel? _uiHudFoodLabel;
    private UiLabel? _uiHudGoldLabel;
    private UiLabel? _uiHudSilverLabel;
    private UiLabel? _uiHudMenuLabel;
    private UiLabel? _uiHudInventoryTabLabel;
    private UiLabel? _uiHudSpellTabLabel;
    private UiLabel[]? _uiHudMessageLabels;
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
    private IReadOnlyList<string> _textureRoots = Array.Empty<string>();
    private Point _hudSpriteSize = Point.Zero;
    private readonly HudMessageLog _hudMessageLog = new();
    private float _hudHealthFill = 0.25f;
    private float _hudManaFill = 0.5f;
    private IReadOnlyList<int> _backpackPreviewItemIds = Array.Empty<int>();
    private int _hudPortraitIndex;
    private int _portraitCount = 1;
    private QuickActionIcon? _activeQuickItemIcon;
    private QuickActionIcon? _activeQuickSpellIcon;
    private int _activeQuickActionSlot = -1;
    private string _hudQuickAttackLabel = "Sin seleccionar";
    private string _hudQuickSpellLabel = "Ninguno";
    private readonly PlayerState _playerState = PlayerState.CreateSample();

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
        "Deva"
    };

    private static readonly string[] EquipmentSlotNames =
    {
        "CASCO",
        "ARMADURA",
        "ARMA",
        "ESCUDO",
        "ANILLO 1",
        "ANILLO 2",
        "BOTAS",
        "AMULETO"
    };

    private static readonly string[] WeaponTypeNames =
    {
        "Cortante",
        "Punzante",
        "Contundente",
        "Veneno",
        "Fuego",
        "Hielo",
        "Rayo",
        "Magia",
        "Municion",
        "N/A"
    };

    private static readonly string[] WeaponWeightNames =
    {
        "Ligera",
        "Normal",
        "Pesada",
        "No es arma"
    };

    private static readonly string[] WeaponRangeNames =
    {
        "Cuerpo a cuerpo",
        "A distancia",
        "Magica",
        "No es arma"
    };

    private static readonly string[] CraftDisciplineNames =
    {
        "No se construye",
        "Herrero",
        "Gran Herrero",
        "Alquimista",
        "Gran Alquimista",
        "Sastre",
        "Gran Sastre",
        "Carpintero Armero",
        "Herbalista",
        "Carpintero",
        "Gran Carpintero"
    };

    private static readonly string[] RepairTypeNames =
    {
        "No reparable",
        "Afilar",
        "Aceitar",
        "Martillar",
        "Coser"
    };

    private static readonly string[] AttackNames =
    {
        "ácido",
        "aguijón",
        "alabarda",
        "aliento",
        "arcabuz",
        "arco y flecha",
        "ballesta",
        "cola",
        "cuernos",
        "daga",
        "embestida",
        "espada",
        "fuego",
        "garra",
        "golpe",
        "hacha",
        "hechizo",
        "hielo",
        "lanza",
        "mandoble",
        "maza",
        "dardo venenoso",
        "mordida",
        "patada",
        "picotazo",
        "rayo",
        "tenazas",
        "flecha venenosa",
        "aliento frío",
        "hechizo mortal"
    };

    private sealed record StatEntry(string Label, string Value);

    private sealed class PlayerState
    {
        public string AvatarName { get; init; } = "Testin";
        public int Level { get; init; } = 1;
        public int ClassIndex { get; init; } = 6;
        public int RaceIndex { get; init; } = 0;
        public bool IsMale { get; init; } = true;
        public int ExperienceNeeded { get; set; } = 200;
        public string HonorTitle { get; init; } = "Plebeyo";
        public IReadOnlyList<StatEntry> CoreStats { get; init; } = Array.Empty<StatEntry>();
        public IReadOnlyList<string> SkillLines { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> CombatLines { get; init; } = Array.Empty<string>();
        public int Health { get; set; } = 9;
        public int MaxHealth { get; set; } = 9;
        public int Mana { get; set; } = 3;
        public int MaxMana { get; set; } = 3;
        public int FoodPercent { get; set; } = 48;
        public int Gold { get; set; } = 50;
        public int Silver { get; set; } = 1;

        public static PlayerState CreateSample()
        {
            return new PlayerState
            {
                AvatarName = "Testin",
                Level = 1,
                ClassIndex = 6,
                RaceIndex = 0,
                ExperienceNeeded = 200,
                HonorTitle = "Plebeyo",
                CoreStats = new[]
                {
                    new StatEntry("Fuerza", "40%"),
                    new StatEntry("Constitución", "35%"),
                    new StatEntry("Inteligencia", "45%"),
                    new StatEntry("Sabiduría", "15%"),
                    new StatEntry("Destreza", "55%")
                },
                SkillLines = new[]
                {
                    "o Herbalismo",
                    "o Elocuencia",
                    "o Apuñalar"
                },
                CombatLines = new[]
                {
                    "Daño: 100%",
                    "Evasión: 19% [+15%]",
                    "Armadura: 20% 20% 20%",
                    "HO% FO% RO% VO%"
                },
                Health = 9,
                MaxHealth = 9,
                Mana = 3,
                MaxMana = 3,
                FoodPercent = 48,
                Gold = 50,
                Silver = 1
            };
        }
    }

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
        _textureRoots = textureRoots;
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
        _uiMinimapWidget?.Dispose();
        _uiMinimapWidget = null;
        _uiSpriteLibrary?.Dispose();
        _uiSpriteLibrary = new UiSpriteLibrary(GraphicsDevice, _textureRoots);
        _uiManager = new UiManager(GraphicsDevice, _debugTextRenderer);
        var viewport = GraphicsDevice.Viewport;
        BuildHudPanel(viewport);
        if (_uiHudPanel is not null)
        {
            _uiManager.AddWindow(_uiHudPanel);
        }
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
            Visible = false,
            UseDefaultChrome = false,
            HeaderHeight = 0,
            ContentPadding = 0,
            DragAnywhere = true
        };
        if (_uiSpriteLibrary is not null)
        {
            _uiInventoryWindow.AddWidget(new UiSpriteWidget(_uiSpriteLibrary, "cjr")
            {
                FillContentBounds = true
            });
        }
        _uiInventoryWidget = new UiInventoryWidget
        {
            EquipmentColor = Color.Black,
            ItemColor = Color.Black,
            EquipmentOrigin = new Vector2(12f, 32f),
            BackpackOrigin = new Vector2(180f, 32f)
        };
        _uiInventoryWidget.SelectionChanged += OnInventorySelectionChanged;
        _uiInventoryWindow.AddWidget(new UiLabel(
            "EQUIPO / MOCHILA",
            new Vector2(12f, 8f),
            Color.Yellow));
        _uiInventoryWindow.AddWidget(_uiInventoryWidget);
        _uiInventorySelectionLabel = new UiLabel("SELECCION: NINGUNO", new Vector2(12f, inventoryBounds.Height - 42f), Color.Black);
        _uiInventoryWindow.AddWidget(_uiInventorySelectionLabel);
        _uiItemDetailLabel = new UiLabel("DETALLE: --", new Vector2(12f, inventoryBounds.Height - 22f), Color.Black);
        _uiInventoryWindow.AddWidget(_uiItemDetailLabel);
        _uiManager.AddWindow(_uiInventoryWindow);
        PopulateInventoryPreview();

        var spellBounds = new Rectangle(viewport.Width - 420, 300, 380, 260);
        _uiSpellWindow = new UiPanel("SPELLBOOK", spellBounds)
        {
            Draggable = true,
            Visible = false,
            UseDefaultChrome = false,
            HeaderHeight = 0,
            ContentPadding = 0,
            DragAnywhere = true
        };
        if (_uiSpriteLibrary is not null)
        {
            _uiSpellWindow.AddWidget(new UiSpriteWidget(_uiSpriteLibrary, "ros")
            {
                FillContentBounds = true
            });
        }
        _uiSpellWindow.AddWidget(new UiLabel(
            "AGRUPADO POR ESCUELA",
            new Vector2(16f, 8f),
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
            Visible = false,
            UseDefaultChrome = false,
            HeaderHeight = 0,
            ContentPadding = 0,
            DragAnywhere = true
        };
        if (_uiSpriteLibrary is not null)
        {
            _uiMerchantWindow.AddWidget(new UiSpriteWidget(_uiSpriteLibrary, "bmenu")
            {
                FillContentBounds = true
            });
        }
        _uiMerchantInfoLabel = new UiLabel(string.Empty, new Vector2(16f, 8f), Color.Yellow);
        _uiMerchantWindow.AddWidget(_uiMerchantInfoLabel);
        _uiMerchantWidget = new UiListWidget(columns: 1, cellSize: new Vector2(320f, 22f), color: Color.Black)
        {
            Offset = new Vector2(12f, 32f)
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
        var hudCapturedScroll = HandleHudScroll(mouse, _previousMouse);
        _cameraController?.Update(gameTime, keyboard, mouse, !hudCapturedScroll);
        _animationPreview?.Update(gameTime);
        if (!_networkFeedActive)
        {
            _monsterSimulation?.Update(gameTime);
        }
        _uiManager?.Update(gameTime, mouse, _previousMouse);
        // Selection labels now updated via event handler.
        UpdateHudText();
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
        DrawResourceBars();
        DrawWelcomeMessage();
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
            _uiMinimapWidget?.Dispose();
            _uiMinimapWidget = null;
            _uiManager?.Dispose();
            _uiSpriteLibrary?.Dispose();
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
            _uiMinimapWidget?.SetMap(_activeMap);
            AddHudMessage($"Mapa cargado: {_activeMap.Header.Name} ({mapId})");
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
        if (_camera is not null)
        {
            _camera.ResizeViewport(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            ConfigureCameraBounds();
        }

        UpdateHudPanelBounds(GraphicsDevice.Viewport);
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

        foreach (var error in _serverCommandDecoder.FlushErrors())
        {
            Console.Error.WriteLine($"[NET] {error}");
            AddHudMessage($"Red: {error}");
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
            case PlayerHealthCommand hp:
                ApplyPlayerHealth(hp.Value);
                break;
            case PlayerManaCommand mana:
                ApplyPlayerMana(mana.Value);
                break;
            case PlayerFoodCommand food:
                ApplyPlayerFood(food.Value);
                break;
            case PlayerMoneyCommand money:
                ApplyPlayerMoney(money.Amount);
                break;
            case PlayerExperienceCommand xp:
                ApplyPlayerExperience(xp.Value);
                break;
            case PlayerDamageFromMonsterCommand dmgMonster:
                ApplyPlayerDamageFromMonster(dmgMonster);
                break;
            case PlayerDamageFromObjectCommand dmgObject:
                ApplyPlayerDamageFromObject(dmgObject);
                break;
            case PlayerDamageFromSpellCommand dmgSpell:
                ApplyPlayerDamageFromSpell(dmgSpell);
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

    private void ApplyPlayerHealth(ushort value)
    {
        var hp = Math.Clamp((int)value, 0, 2000);
        _playerState.Health = hp;
        if (hp > _playerState.MaxHealth)
        {
            _playerState.MaxHealth = hp;
        }
    }

    private void ApplyPlayerMana(byte value)
    {
        var mana = Math.Clamp((int)value, 0, 200);
        _playerState.Mana = mana;
        if (mana > _playerState.MaxMana)
        {
            _playerState.MaxMana = mana;
        }
    }

    private void ApplyPlayerFood(byte value)
    {
        _playerState.FoodPercent = Math.Clamp((int)value, 0, 100);
    }

    private void ApplyPlayerMoney(uint rawValue)
    {
        var gold = (int)Math.Clamp(rawValue / 100, 0, int.MaxValue);
        var silver = (int)(rawValue % 100);
        _playerState.Gold = gold;
        _playerState.Silver = silver;
    }

    private void ApplyPlayerExperience(ushort value)
    {
        _playerState.ExperienceNeeded = value;
    }

    private void ApplyPlayerDamageFromMonster(PlayerDamageFromMonsterCommand command)
    {
        ApplyPlayerHealth(command.NewHealth);
        var monsterName = ResolveMonsterName(command.MonsterIndex);
        var attack = ResolveAttackName(command.AttackIndex);
        AddHudMessage($"{monsterName} te ataca con {attack}. Salud {_playerState.Health}/{_playerState.MaxHealth}.");
    }

    private void ApplyPlayerDamageFromObject(PlayerDamageFromObjectCommand command)
    {
        ApplyPlayerHealth(command.NewHealth);
        var itemName = ResolveItemName(command.ObjectId);
        AddHudMessage($"Recibes daño de {itemName} (avatar #{command.AttackerId}). Salud {_playerState.Health}/{_playerState.MaxHealth}.");
    }

    private void ApplyPlayerDamageFromSpell(PlayerDamageFromSpellCommand command)
    {
        ApplyPlayerHealth(command.NewHealth);
        var spellName = ResolveSpellName(command.SpellId);
        AddHudMessage($"El hechizo {spellName} te alcanza (avatar #{command.AttackerId}). Salud {_playerState.Health}/{_playerState.MaxHealth}.");
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
            UpdateHudVisibility();
        }
        else if (IsKeyPressed(keyboardState, Keys.Home))
        {
            ScrollHudMessages(-1);
        }
        else if (IsKeyPressed(keyboardState, Keys.End))
        {
            ScrollHudMessages(1);
        }
    }

    private bool HandleHudScroll(MouseState currentMouse, MouseState previousMouse)
    {
        var scrollDelta = currentMouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        if (scrollDelta == 0 || !_showHud || _uiHudPanel is null || _hudPaperDollWidget is null)
        {
            return false;
        }

        var mousePoint = new Point(currentMouse.X, currentMouse.Y);
        if (!_uiHudPanel.Bounds.Contains(mousePoint))
        {
            return false;
        }

        return _hudPaperDollWidget.HandleScroll(mousePoint, _uiHudPanel.Bounds, scrollDelta);
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

    private IReadOnlyList<KeyValuePair<string, string>> BuildEquipmentPreview()
    {
        var equipment = new List<KeyValuePair<string, string>>(EquipmentSlotNames.Length);
        if (_itemDocument is null || _itemDocument.Names.Count == 0)
        {
            foreach (var slot in EquipmentSlotNames)
            {
                equipment.Add(new KeyValuePair<string, string>(slot, "SIN DATOS"));
            }

            return equipment;
        }

        for (var i = 0; i < EquipmentSlotNames.Length; i++)
        {
            var slotName = EquipmentSlotNames[i];
            var name = _itemDocument.Names.ElementAtOrDefault(i);
            var resolved = string.IsNullOrWhiteSpace(name) ? $"ITEM #{i:D3}" : name!;
            equipment.Add(new KeyValuePair<string, string>(slotName, resolved));
        }

        return equipment;
    }

    private IReadOnlyList<string> BuildBackpackPreviewItems(out List<int> itemIds)
    {
        itemIds = new List<int>();
        if (_itemDocument is null || _itemDocument.Names.Count == 0)
        {
            itemIds.Add(-1);
            return new[] { "SIN ITEMS DISPONIBLES" };
        }

        var list = new List<string>();
        var start = EquipmentSlotNames.Length;
        for (var i = start; i < _itemDocument.Names.Count; i++)
        {
            var name = _itemDocument.Names[i];
            list.Add(string.IsNullOrWhiteSpace(name) ? $"ITEM #{i:D3}" : name);
            itemIds.Add(i);
        }

        if (list.Count == 0)
        {
            list.Add("MOCHILA VACIA");
            itemIds.Add(-1);
        }

        return list;
    }

    private string BuildCharacterSummary()
    {
        if (_playerState is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var className = PlayerClassNames[Math.Clamp(_playerState.ClassIndex, 0, PlayerClassNames.Length - 1)];
        builder.AppendLine($"{className}, nivel {_playerState.Level}");
        foreach (var stat in _playerState.CoreStats)
        {
            builder.AppendLine($"{stat.Label}: {stat.Value}");
        }

        builder.AppendLine($"Exp. necesaria: {_playerState.ExperienceNeeded}");
        builder.AppendLine($"Honor: {_playerState.HonorTitle}");
        return builder.ToString();
    }

    private IReadOnlyList<int> BuildEquipmentIconPreview()
    {
        var icons = new List<int>(EquipmentSlotNames.Length);
        if (_itemDocument is null || _itemDocument.Names.Count == 0)
        {
            for (var i = 0; i < EquipmentSlotNames.Length; i++)
            {
                icons.Add(-1);
            }

            return icons;
        }

        var count = Math.Min(EquipmentSlotNames.Length, _itemDocument.Names.Count);
        for (var i = 0; i < count; i++)
        {
            icons.Add(i);
        }

        while (icons.Count < EquipmentSlotNames.Length)
        {
            icons.Add(-1);
        }

        return icons;
    }

    private IReadOnlyList<int> BuildBackpackIconPreview()
    {
        var icons = new List<int>();
        if (_itemDocument is null || _itemDocument.Names.Count == 0)
        {
            for (var i = 0; i < HudPaperDollWidget.VisibleBackpackSlots; i++)
            {
                icons.Add(-1);
            }

            return icons;
        }

        var start = EquipmentSlotNames.Length;
        for (var i = start; i < _itemDocument.Names.Count; i++)
        {
            icons.Add(i);
        }

        return icons;
    }

    private IReadOnlyList<int> BuildSpellIconPreview()
    {
        var maxSlots = HudPaperDollWidget.VisibleBackpackSlots * 2;
        var icons = new List<int>(maxSlots);
        if (_spellDocument is null || _spellDocument.Spells.Count == 0)
        {
            for (var i = 0; i < maxSlots; i++)
            {
                icons.Add(i);
            }

            return icons;
        }

        for (var i = 0; i < _spellDocument.Spells.Count && icons.Count < maxSlots; i++)
        {
            icons.Add(i);
        }

        return icons;
    }

    private IReadOnlyList<QuickActionIcon> BuildQuickActionIcons(
        IReadOnlyList<int> equipmentIcons,
        IReadOnlyList<int> backpackIcons,
        IReadOnlyList<int> spellIcons)
    {
        var list = new List<QuickActionIcon>(3);
        var equipmentIcon = equipmentIcons.FirstOrDefault(id => id >= 0);
        if (equipmentIcon >= 0)
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Item, equipmentIcon));
        }
        else
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Item, 0));
        }

        var backpackIcon = backpackIcons.FirstOrDefault(id => id >= 0);
        if (backpackIcon >= 0)
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Item, backpackIcon));
        }
        else
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Item, 1));
        }

        var spellIcon = spellIcons.FirstOrDefault(id => id >= 0);
        if (spellIcon >= 0)
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Spell, spellIcon));
        }
        else
        {
            list.Add(new QuickActionIcon(QuickActionIconType.Spell, 0));
        }

        return list;
    }

    private void HandleQuickActionInvoked(int slot, QuickActionIcon icon)
    {
        SetQuickActionHighlight(slot);
        if (icon.Type == QuickActionIconType.Spell)
        {
            _activeQuickSpellIcon = icon;
            var spellName = ResolveSpellName(icon.Id);
            _hudQuickSpellLabel = spellName;
            UpdateSpellDetail(icon);
            AddHudMessage($"Conjuro rápido preparado: {spellName}.");
        }
        else
        {
            _activeQuickItemIcon = icon;
            var itemName = ResolveItemName(icon.Id);
            _hudQuickAttackLabel = itemName;
            UpdateHudItemDetail(icon.Id);
            AddHudMessage($"Ataque rápido configurado con {itemName}.");
        }

        RefreshHudHintLabel();
    }

    private void HandlePortraitClicked(int portraitIndex)
    {
        if (_portraitCount <= 0)
        {
            _portraitCount = 1;
        }

        _hudPortraitIndex = (portraitIndex + 1) % _portraitCount;
        UpdateHudPortrait();
        AddHudMessage($"Cambiaste el retrato al slot #{_hudPortraitIndex + 1:D2}.");
    }

    private void HandleSpellSlotActivated(int index, QuickActionIcon icon)
    {
        UpdateSpellDetail(icon);
        _activeQuickSpellIcon = icon;
        _hudQuickSpellLabel = ResolveSpellName(icon.Id);
        RefreshHudHintLabel();
        AddHudMessage($"Hechizo seleccionado #{icon.Id:D3} (slot {index + 1}).");
    }

    private void HandleHudInventorySlotActivated(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _backpackPreviewItemIds.Count)
        {
            UpdateHudItemDetail(null);
            return;
        }

        var itemId = _backpackPreviewItemIds[slotIndex];
        if (itemId < 0)
        {
            UpdateHudItemDetail(null);
            return;
        }

        UpdateHudItemDetail(itemId);
        var itemName = ResolveItemName(itemId);
        AddHudMessage($"Objeto seleccionado #{itemId:D3}: {itemName}.");
    }

    private void OnInventorySelectionChanged(InventorySelectionChangedEventArgs args)
    {
        if (_uiInventorySelectionLabel is not null)
        {
            var label = args.Kind == InventorySelectionKind.None ? "NINGUNO" : args.Label;
            _uiInventorySelectionLabel.Text = $"SELECCION: {label}";
        }

        UpdateItemDetailText(args);
    }

    private void UpdateItemDetailText(InventorySelectionChangedEventArgs args)
    {
        if (_uiItemDetailLabel is null)
        {
            return;
        }

        if (args.Kind != InventorySelectionKind.Backpack || _itemDocument is null)
        {
            _uiItemDetailLabel.Text = "DETALLE: --";
            return;
        }

        var id = args.Index >= 0 && args.Index < _backpackPreviewItemIds.Count
            ? _backpackPreviewItemIds[args.Index]
            : -1;
        if (id < 0 || id >= _itemDocument.Items.Count)
        {
            _uiItemDetailLabel.Text = $"DETALLE: {args.Label}";
            return;
        }

        var descriptor = _itemDocument.Items[id];
        var name = !string.IsNullOrWhiteSpace(_itemDocument.Names.ElementAtOrDefault(id))
            ? _itemDocument.Names[id]
            : $"ITEM #{id:D3}";
        var detail =
            $"DETALLE: {name}  MO {descriptor.Cost}  Nivel {descriptor.MinimumLevel}  Tipo {descriptor.WeaponType}";
        _uiItemDetailLabel.Text = detail;
    }
    private void UpdateHudItemDetail(int? itemId)
    {
        if (_uiHudItemDetailLabel is null)
        {
            return;
        }

        if (itemId is null || _itemDocument is null)
        {
            _uiHudItemDetailLabel.Text = "Item: --";
            return;
        }

        _uiHudItemDetailLabel.Text = BuildHudItemDetailText(itemId.Value);
    }

    private void UpdateSpellDetail(QuickActionIcon? icon)
    {
        if (_uiHudSpellDetailLabel is null)
        {
            return;
        }

        if (icon is null || icon.Value.Id < 0 || _spellDocument is null)
        {
            _uiHudSpellDetailLabel.Text = "Hechizo: Ninguno";
            return;
        }

        var id = icon.Value.Id;
        var spell = id >= 0 && id < _spellDocument.Spells.Count ? _spellDocument.Spells[id] : null;
        var name = id >= 0 && id < _spellDocument.Names.Count ? _spellDocument.Names[id] : $"Hechizo #{id:D3}";
        if (spell is null)
        {
            _uiHudSpellDetailLabel.Text = $"Hechizo: {name}";
            return;
        }

        var detail =
            $"{name}, Nivel: {spell.RequiredPlayerLevel}, Maná: {spell.RequiredMana}, I: {spell.RequiredIntelligence * 5}, S: {spell.RequiredWisdom * 5}";
        _uiHudSpellDetailLabel.Text = detail;
    }

    private string BuildHudItemDetailText(int itemId)
    {
        if (_itemDocument is null || itemId < 0 || itemId >= _itemDocument.Items.Count)
        {
            return "Item: --";
        }

        var descriptor = _itemDocument.Items[itemId];
        var name = ResolveItemName(itemId);
        var builder = new StringBuilder();
        builder.AppendLine($"Item: {name}");
        builder.AppendLine(
            $"MO {descriptor.Cost}  Nivel {descriptor.MinimumLevel}  Peso {ResolveWeaponWeightName(descriptor.WeaponWeight)}  Alcance {ResolveWeaponRangeName(descriptor.RangeType)}");

        if (IsAmmoLikeItem(itemId))
        {
            builder.AppendLine(BuildWeaponDetail(descriptor));
        }
        else if (IsWeaponItem(itemId))
        {
            builder.AppendLine(BuildWeaponDetail(descriptor));
        }
        else if (IsArmorItem(itemId))
        {
            builder.AppendLine(
                $"Evasión: {FormatPercent(descriptor.DefenseModifier)}  Punz: {FormatArmor(descriptor.Damage1Blunt)}  Cort: {FormatArmor(descriptor.Damage1Pierce)}  Cont: {FormatArmor(descriptor.Damage2Blunt)}  Magia: {FormatArmor(descriptor.Damage2Pierce)}");
        }
        else
        {
            builder.AppendLine($"Ataque/Evasión: {FormatPercent(descriptor.DefenseModifier)}");
        }

        var raceRestrictions = FormatRestrictionList(descriptor.ForbiddenRaces, PlayerRaceNames);
        if (!string.IsNullOrEmpty(raceRestrictions))
        {
            builder.AppendLine($"Razas prohibidas: {raceRestrictions}");
        }

        var classRestrictions = FormatRestrictionList(descriptor.ForbiddenClasses, PlayerClassNames);
        if (!string.IsNullOrEmpty(classRestrictions))
        {
            builder.AppendLine($"Clases prohibidas: {classRestrictions}");
        }

        var craft = ResolveCraftDisciplineName(descriptor.CraftDiscipline);
        if (!string.IsNullOrEmpty(craft))
        {
            builder.AppendLine($"Construye: {craft} (Nivel {descriptor.CrafterLevel})");
        }

        var repair = ResolveRepairTypeName(descriptor.RepairType);
        if (!string.IsNullOrEmpty(repair))
        {
            builder.AppendLine($"Reparación: {repair}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildWeaponDetail(ItemDescriptor descriptor)
    {
        var attack = FormatPercent(descriptor.DefenseModifier);
        var damageType = ResolveWeaponTypeName(descriptor.WeaponType);
        var pm = FormatDamageRange(descriptor.Damage1Blunt, descriptor.Damage1Pierce);
        var g = FormatDamageRange(descriptor.Damage2Blunt, descriptor.Damage2Pierce);
        return $"Ataque: {attack}  Daño {damageType}: {pm} PM, {g} G";
    }

    private static bool IsAmmoLikeItem(int itemId)
    {
        return (itemId >= 8 && itemId <= 15) || (itemId >= 48 && itemId <= 55);
    }

    private static bool IsWeaponItem(int itemId)
    {
        return itemId >= 16 && itemId <= 47;
    }

    private static bool IsArmorItem(int itemId)
    {
        return (itemId >= 56 && itemId <= 103) || (itemId >= 248 && itemId <= 253);
    }

    private static string FormatRestrictionList(byte mask, IReadOnlyList<string> names)
    {
        if (mask == 0)
        {
            return string.Empty;
        }

        var list = new List<string>();
        for (var i = 0; i < names.Count; i++)
        {
            var bit = 1 << i;
            if ((mask & bit) != 0)
            {
                var name = names[i];
                if (!string.IsNullOrWhiteSpace(name))
                {
                    list.Add(name);
                }
            }
        }

        return string.Join(", ", list);
    }

    private static string ResolveWeaponTypeName(byte weaponType)
    {
        return weaponType < WeaponTypeNames.Length
            ? WeaponTypeNames[weaponType]
            : $"Tipo #{weaponType}";
    }

    private static string ResolveWeaponWeightName(byte weight)
    {
        return weight < WeaponWeightNames.Length
            ? WeaponWeightNames[weight]
            : $"Peso #{weight}";
    }

    private static string ResolveWeaponRangeName(byte range)
    {
        return range < WeaponRangeNames.Length
            ? WeaponRangeNames[range]
            : $"Alcance #{range}";
    }

    private static string ResolveCraftDisciplineName(byte discipline)
    {
        if (discipline <= 0 || discipline >= CraftDisciplineNames.Length)
        {
            return string.Empty;
        }

        return CraftDisciplineNames[discipline];
    }

    private static string ResolveRepairTypeName(byte repairType)
    {
        return repairType < RepairTypeNames.Length
            ? RepairTypeNames[repairType]
            : string.Empty;
    }

    private static string FormatPercent(int value)
    {
        var sign = value > 0 ? "+" : string.Empty;
        return $"{sign}{value}%";
    }

    private static string FormatDamageRange(int baseValue, int bonus)
    {
        if (bonus <= 1)
        {
            if (baseValue == 0)
            {
                return "---";
            }

            return baseValue > 0 ? $"+{baseValue}" : baseValue.ToString();
        }

        var max = baseValue + bonus - 1;
        return $"{baseValue} a {max}";
    }

    private static string FormatArmor(int level)
    {
        if (level > 0)
        {
            return $"{100 - (400 / (level + 4))}%";
        }

        if (level < 0)
        {
            var penalty = (100 * -level) >> 2;
            return $"-{penalty}%";
        }

        return "0%";
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
        AddHudMessage($"Overlay activo: {_overlayLayers}");
    }

    private void ToggleInfoPanel(InfoPanel panel)
    {
        _infoPanel = _infoPanel == panel ? InfoPanel.None : panel;
        if (_infoPanel == InfoPanel.None)
        {
            AddHudMessage("Panel de información oculto.");
        }
        else
        {
            AddHudMessage($"Panel de información: {_infoPanel}");
        }
    }

    private void BuildHudPanel(Viewport viewport)
    {
        _uiHudPanel = null;
        _hudPaperDollWidget = null;
        _hudPortraitWidget = null;
        _hudQuickActionWidget = null;
        _hudSpriteSize = Point.Zero;
        if (_uiSpriteLibrary is null)
        {
            return;
        }

        if (!_uiSpriteLibrary.TryGetSprite("fondo", out var sprite))
        {
            Console.Error.WriteLine("HUD sprite 'fondo' not found in atlas.");
            return;
        }

        _hudSpriteSize = new Point(sprite.Source.Width, sprite.Source.Height);
        var hudPanel = new UiPanel(string.Empty, Rectangle.Empty)
        {
            Draggable = false,
            DragAnywhere = false,
            UseDefaultChrome = false,
            HeaderHeight = 0,
            ContentPadding = 0,
            Visible = _showHud
        };
        hudPanel.AddWidget(new UiSpriteWidget(_uiSpriteLibrary, "fondo"));
        _uiMinimapWidget = new UiMinimapWidget(GraphicsDevice, _textureRoots);
        _uiMinimapWidget.SetMap(_activeMap);
        _uiMinimapWidget.Offset = new Vector2(0f, HudContentOffsetY);
        hudPanel.AddWidget(_uiMinimapWidget);
        _uiMapNameLabel = new UiLabel(string.Empty, HudPoint(8f, -6f), Color.LightGreen);
        hudPanel.AddWidget(_uiMapNameLabel);
        _uiHudHintLabel = new UiLabel(string.Empty, HudPoint(482f, 70f), Color.White);
        hudPanel.AddWidget(_uiHudHintLabel);
        _uiHudStatsLabel = new UiLabel(string.Empty, HudPoint(140f, 32f), Color.White);
        hudPanel.AddWidget(_uiHudStatsLabel);
        _uiHudSkillLabel = new UiLabel(string.Empty, HudPoint(300f, 32f), Color.White);
        hudPanel.AddWidget(_uiHudSkillLabel);
        _uiHudCombatStatsLabel = new UiLabel(string.Empty, HudPoint(300f, 82f), Color.White);
        hudPanel.AddWidget(_uiHudCombatStatsLabel);
        _uiHudSpellDetailLabel = new UiLabel(string.Empty, HudPoint(640f, 68f), Color.White);
        hudPanel.AddWidget(_uiHudSpellDetailLabel);
        _uiHudHealthLabel = new UiLabel(string.Empty, HudPoint(522f, 86f), Color.White);
        hudPanel.AddWidget(_uiHudHealthLabel);
        _uiHudManaLabel = new UiLabel(string.Empty, HudPoint(522f, 100f), Color.White);
        hudPanel.AddWidget(_uiHudManaLabel);
        _uiHudFoodLabel = new UiLabel(string.Empty, HudPoint(522f, 114f), Color.White);
        hudPanel.AddWidget(_uiHudFoodLabel);
        _uiHudGoldLabel = new UiLabel(string.Empty, HudPoint(916f, 115f), Color.White);
        hudPanel.AddWidget(_uiHudGoldLabel);
        _uiHudSilverLabel = new UiLabel(string.Empty, HudPoint(916f, 102f), Color.White);
        hudPanel.AddWidget(_uiHudSilverLabel);
        _uiHudMenuLabel = new UiLabel("Menú", HudPoint(522f, 68f), Color.LightYellow);
        hudPanel.AddWidget(_uiHudMenuLabel);
        _uiHudItemDetailLabel = new UiLabel("Item: --", HudPoint(640f, 128f), Color.White);
        hudPanel.AddWidget(_uiHudItemDetailLabel);
        _uiHudInventoryTabLabel = new UiLabel("Inventario", HudPoint(1148f, 6f), Color.White);
        hudPanel.AddWidget(_uiHudInventoryTabLabel);
        _uiHudSpellTabLabel = new UiLabel("Hechizos", HudPoint(1212f, 6f), Color.Gray);
        hudPanel.AddWidget(_uiHudSpellTabLabel);
        _uiHudMessageLabels = new UiLabel[5];
        for (var i = 0; i < _uiHudMessageLabels.Length; i++)
        {
            var offset = HudPoint(432f, 54f - 13f * i);
            var label = new UiLabel(string.Empty, offset, Color.White);
            _uiHudMessageLabels[i] = label;
            hudPanel.AddWidget(label);
        }
        if (_uiSpriteLibrary is not null)
        {
            if (_uiSpriteLibrary.TryGetSprite("ros", out var portraitSprite))
            {
                var columns = Math.Max(1, portraitSprite.Source.Width / PortraitTileSize);
                var rows = Math.Max(1, portraitSprite.Source.Height / PortraitTileSize);
                _portraitCount = Math.Max(1, columns * rows);
                if (_portraitCount > 0)
                {
                    _hudPortraitIndex %= _portraitCount;
                }
            }
            _hudPortraitWidget = new HudPortraitWidget(_uiSpriteLibrary)
            {
                PortraitIndex = _hudPortraitIndex,
                Offset = HudPoint(432f, 86f)
            };
            _hudPortraitWidget.PortraitClicked += HandlePortraitClicked;
            hudPanel.AddWidget(_hudPortraitWidget);

            _hudPaperDollWidget = new HudPaperDollWidget(_uiSpriteLibrary);
            _hudPaperDollWidget.GridModeChanged += mode => UpdateHudTabLabels();
            _hudPaperDollWidget.SpellTabBlocked += () => AddHudMessage("Los guerreros y los bribones no lanzan conjuros.");
            _hudPaperDollWidget.SpellSlotActivated += HandleSpellSlotActivated;
            _hudPaperDollWidget.InventorySlotActivated += HandleHudInventorySlotActivated;
            _hudPaperDollWidget.AdditionalOffset = new Vector2(0f, HudContentOffsetY);
            hudPanel.AddWidget(_hudPaperDollWidget);

            _hudQuickActionWidget = new HudQuickActionWidget(_uiSpriteLibrary);
            _hudQuickActionWidget.Offset = HudPoint(640f, 94f);
            _hudQuickActionWidget.ActionInvoked += HandleQuickActionInvoked;
            hudPanel.AddWidget(_hudQuickActionWidget);
        }
        _hudMessageLog.StickToBottom(_uiHudMessageLabels.Length);
        _uiHudPanel = hudPanel;
        AddHudMessage("Interfaz preparada.");
        UpdateHudPanelBounds(viewport);
        PopulateInventoryPreview();
        UpdateHudTabLabels();
        UpdateHudPortrait();
    }

    private void UpdateHudPanelBounds(Viewport viewport)
    {
        if (_uiHudPanel is null)
        {
            UpdateWelcomeMessageAnchor(viewport);
            return;
        }

        var width = _hudSpriteSize.X > 0 ? _hudSpriteSize.X : viewport.Width;
        var height = _hudSpriteSize.Y > 0 ? _hudSpriteSize.Y : 144;
        var x = width >= viewport.Width ? 0 : (viewport.Width - width) / 2;
        var y = Math.Max(0, viewport.Height - height);
        var bounds = new Rectangle(x, y, Math.Min(width, viewport.Width), height);
        _uiHudPanel.SetBounds(bounds);
        UpdateWelcomeMessageAnchor(viewport);
    }

    private void UpdateWelcomeMessageAnchor(Viewport viewport)
    {
        var minimapTop = _uiHudPanel?.Bounds.Top + HudContentOffsetY ?? viewport.Height * 0.75f;
        var healthBottom = 32f;
        var y = MathHelper.Lerp(healthBottom, minimapTop, 0.5f);
        _welcomeMessagePosition = new Vector2(16f, y);
    }

    private void UpdateHudVisibility()
    {
        if (_uiHudPanel is not null)
        {
            _uiHudPanel.Visible = _showHud;
        }
    }

    private void UpdateHudTabLabels()
    {
        var activeMode = _hudPaperDollWidget?.GridMode ?? HudGridMode.Inventory;
        var hasSpells = _hudPaperDollWidget?.SpellTabAvailable ?? false;
        var activeColor = Color.LightYellow;
        var inactiveColor = new Color(140, 140, 140);
        var disabledColor = new Color(80, 80, 80);
        if (_uiHudInventoryTabLabel is not null)
        {
            _uiHudInventoryTabLabel.Color = activeMode == HudGridMode.Inventory ? activeColor : inactiveColor;
        }

        if (_uiHudSpellTabLabel is not null)
        {
            if (!hasSpells)
            {
                _uiHudSpellTabLabel.Color = disabledColor;
            }
            else
            {
                _uiHudSpellTabLabel.Color = activeMode == HudGridMode.Spells ? activeColor : inactiveColor;
            }
        }
    }

    private static Vector2 HudPoint(float x, float y)
    {
        return new Vector2(x, y + HudContentOffsetY);
    }

    private void UpdateHudPortrait()
    {
        if (_hudPortraitWidget is not null)
        {
            _hudPortraitWidget.PortraitIndex = _hudPortraitIndex;
        }
    }

    private void PopulateInventoryPreview()
    {
        var equipment = BuildEquipmentPreview();
        var backpack = BuildBackpackPreviewItems(out var backpackIds);
        _backpackPreviewItemIds = backpackIds;
        _uiInventoryWidget?.SetEquipment(equipment);
        _uiInventoryWidget?.SetBackpack(backpack);
        var equipmentIcons = BuildEquipmentIconPreview();
        var backpackIcons = BuildBackpackIconPreview();
        _hudPaperDollWidget?.SetEquipmentIcons(equipmentIcons);
        _hudPaperDollWidget?.SetBackpackIcons(backpackIcons);
        var spellIcons = BuildSpellIconPreview();
        _hudPaperDollWidget?.SetSpellIcons(spellIcons);
        var quickIcons = BuildQuickActionIcons(equipmentIcons, backpackIcons, spellIcons);
        _hudQuickActionWidget?.SetIcons(quickIcons);
        UpdateHudTabLabels();
        UpdateSpellDetail(null);
        UpdateHudItemDetail(null);
        OnInventorySelectionChanged(InventorySelectionChangedEventArgs.None);
        ResetQuickActionState();
    }

    private void ResetQuickActionState()
    {
        _activeQuickActionSlot = -1;
        _activeQuickItemIcon = null;
        _activeQuickSpellIcon = null;
        _hudQuickAttackLabel = "Sin seleccionar";
        _hudQuickSpellLabel = "Ninguno";
        if (_hudQuickActionWidget is not null)
        {
            _hudQuickActionWidget.ActiveIndex = -1;
        }
        RefreshHudHintLabel();
    }

    private void RefreshHudHintLabel()
    {
        if (_uiHudHintLabel is null)
        {
            return;
        }

        _uiHudHintLabel.Text =
            $"Ataque rápido: {_hudQuickAttackLabel}\nHechizo rápido: {_hudQuickSpellLabel}\n{HudControlsHelpText}";
    }

    private void SetQuickActionHighlight(int slot)
    {
        _activeQuickActionSlot = slot;
        if (_hudQuickActionWidget is not null)
        {
            _hudQuickActionWidget.ActiveIndex = slot;
        }
    }

    private void AddHudMessage(string message)
    {
        var visible = _uiHudMessageLabels?.Length ?? 5;
        _hudMessageLog.Add(message, visible);
    }

    private void ScrollHudMessages(int delta)
    {
        var visible = _uiHudMessageLabels?.Length ?? 5;
        _hudMessageLog.Scroll(delta, visible);
    }

    private void UpdateHudText()
    {
        var mapName = _activeMap?.Header.Name ?? "SIN MAPA";
        var mapWidth = _activeMap?.Terrain.FirstOrDefault()?.Count ?? 0;
        var mapHeight = _activeMap?.Terrain.Count ?? 0;
        var nestCount = _activeMap?.Nests.Count ?? 0;
        var merchantCount = _activeMap?.Merchants.Count ?? 0;
        var sensorCount = _activeMap?.Sensors.Count ?? 0;
        var monsterCount = _worldState.Monsters.Count;
        var networkMode = _networkFeedActive ? "Servidor" : "Simulación local";
        var overlayLabel = _overlayLayers == OverlayLayers.None ? "sin overlay" : _overlayLayers.ToString();
        var infoPanelLabel = _infoPanel == InfoPanel.None ? "ninguno" : _infoPanel.ToString();

        if (_activeMap is not null && _camera is not null)
        {
            var worldWidth = mapWidth * TileWidth;
            var worldHeight = mapHeight * TileHeight;
            if (worldWidth > 0 && worldHeight > 0)
            {
                var center = _camera.Position + new Vector2(
                    _camera.ViewportWidth / (2f * _camera.ZoomFactor),
                    _camera.ViewportHeight / (2f * _camera.ZoomFactor));
                var normalized = new Vector2(
                    MathHelper.Clamp(center.X / worldWidth, 0f, 1f),
                    MathHelper.Clamp(center.Y / worldHeight, 0f, 1f));
                _uiMinimapWidget?.SetHighlight(normalized);
            }
        }
        else
        {
            _uiMinimapWidget?.SetHighlight(null);
        }

        if (_uiMapNameLabel is not null)
        {
            _uiMapNameLabel.Text = mapName;
        }

        _welcomeMessage =
            "Bienvenido al mundo de Artes Arcanas\nEl servidor permite usar varios avatares al mismo tiempo.";

        RefreshHudHintLabel();

        if (_uiHudMenuLabel is not null)
        {
            _uiHudMenuLabel.Text = "Menú";
        }

        if (_uiHudStatsLabel is not null)
        {
            _uiHudStatsLabel.Text = BuildCharacterSummary();
        }

        if (_uiHudSkillLabel is not null)
        {
            var skills = _playerState.SkillLines.Count > 0
                ? string.Join('\n', _playerState.SkillLines)
                : string.Empty;
            _uiHudSkillLabel.Text = skills;
        }

        if (_uiHudCombatStatsLabel is not null)
        {
            _uiHudCombatStatsLabel.Text = _playerState.CombatLines.Count > 0
                ? string.Join('\n', _playerState.CombatLines)
                : string.Empty;
        }

        var healthRatio = _playerState.MaxHealth > 0
            ? Math.Clamp((float)_playerState.Health / _playerState.MaxHealth, 0f, 1f)
            : 0f;
        _hudHealthFill = healthRatio;
        if (_uiHudHealthLabel is not null)
        {
            _uiHudHealthLabel.Text = $"Salud {_playerState.Health} / {_playerState.MaxHealth}";
        }

        var manaRatio = _playerState.MaxMana > 0
            ? Math.Clamp((float)_playerState.Mana / _playerState.MaxMana, 0f, 1f)
            : 0f;
        _hudManaFill = manaRatio;
        if (_uiHudManaLabel is not null)
        {
            _uiHudManaLabel.Text = $"Mana {_playerState.Mana} / {_playerState.MaxMana}";
        }

        if (_uiHudFoodLabel is not null)
        {
            _uiHudFoodLabel.Text = $"Comida {_playerState.FoodPercent}%";
        }

        if (_uiHudGoldLabel is not null)
        {
            _uiHudGoldLabel.Text = $"MO {_playerState.Gold}";
        }

        if (_uiHudSilverLabel is not null)
        {
            _uiHudSilverLabel.Text = $"MP {_playerState.Silver}";
        }

        if (_uiHudMessageLabels is not null)
        {
            var lines = _hudMessageLog.GetVisibleLines(_uiHudMessageLabels.Length);
            for (var i = 0; i < _uiHudMessageLabels.Length; i++)
            {
                _uiHudMessageLabels[i].Text = i < lines.Count ? lines[i] : string.Empty;
            }
        }
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void DrawResourceBars()
    {
        if (!_showHud || _spriteBatch is null || _uiSpriteLibrary is null)
        {
            return;
        }

        if (!_uiSpriteLibrary.TryGetSprite("barra", out var sprite))
        {
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);

        DrawResourceBar(sprite, new Vector2(0f, 0f), _hudHealthFill, flip: false, Color.DarkRed);
        var rightX = GraphicsDevice.Viewport.Width - sprite.Source.Width;
        DrawResourceBar(sprite, new Vector2(rightX, 0f), _hudManaFill, flip: true, Color.CornflowerBlue);

        _spriteBatch.End();
    }

    private void DrawResourceBar(UiSprite sprite, Vector2 position, float ratio, bool flip, Color tint)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        var frameSource = new Rectangle(sprite.Source.X, sprite.Source.Y, 116, 32);
        var dest = new Rectangle(
            (int)Math.Round(position.X),
            (int)Math.Round(position.Y),
            frameSource.Width,
            frameSource.Height);
        var effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        _spriteBatch!.Draw(sprite.Texture, dest, frameSource, Color.White, 0f, Vector2.Zero, effects, 0f);

        var fillPixels = (int)Math.Round(96f * ratio);
        if (fillPixels <= 0)
        {
            return;
        }

        var sourceX = sprite.Source.X + 20;
        if (flip)
        {
            sourceX += 96 - fillPixels;
        }

        var fillSource = new Rectangle(sourceX, sprite.Source.Y + 32, fillPixels, 24);
        var fillDestX = flip ? dest.Right - 16 - fillPixels : dest.Left + 16;
        var fillDest = new Rectangle(fillDestX, dest.Top + 8, fillPixels, 24);
        _spriteBatch.Draw(sprite.Texture, fillDest, fillSource, tint * 0.85f);
    }

    private void DrawWelcomeMessage()
    {
        if (!_showHud || _spriteBatch is null || _debugTextRenderer is null)
        {
            return;
        }

        var text = _welcomeMessage;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
        _debugTextRenderer.DrawString(_spriteBatch, text, _welcomeMessagePosition, Color.LightYellow, scale: 2f);
        _spriteBatch.End();
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

    private string ResolveSpellName(int spellId)
    {
        if (_spellDocument is null)
        {
            return $"Hechizo #{spellId:D3}";
        }

        if (spellId >= 0 && spellId < _spellDocument.Names.Count)
        {
            var name = _spellDocument.Names[spellId];
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return $"Hechizo #{spellId:D3}";
    }

    private string ResolveMonsterName(int monsterIndex)
    {
        if (_monsterDocument is null)
        {
            return $"Monstruo #{monsterIndex:D3}";
        }

        if (monsterIndex >= 0 && monsterIndex < _monsterDocument.Monsters.Count)
        {
            var name = _monsterDocument.Monsters[monsterIndex].Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return $"Monstruo #{monsterIndex:D3}";
    }

    private static string ResolveAttackName(int attackIndex)
    {
        if (AttackNames.Length == 0)
        {
            return $"Ataque #{attackIndex}";
        }

        var index = Math.Abs(attackIndex) % AttackNames.Length;
        return AttackNames[index];
    }
}
