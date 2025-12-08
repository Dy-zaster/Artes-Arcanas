using System;
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
using Laa.Protocol;
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
    private UiPanel? _uiLoginPanel;
    private UiTextInputWidget? _uiLoginUserInput;
    private UiTextInputWidget? _uiLoginPassInput;
    private UiTextInputWidget? _uiCreateNameInput;
    private UiLabel? _uiLoginStatusLabel;
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
    private PlayerEntity? _playerEntity;
    private Vector2 _playerVelocity = Vector2.Zero;
    private AnimationTexture? _playerAnimation;
    private int _playerDirection;
    private MonsterAction _playerAction = MonsterAction.Idle;
    private float _playerAnimTimer;
    private int _playerFrameIndex;
    private bool _loggedPlayerAnimInfo;
    private bool _playerMirror;
    private bool _isLoggedIn;

    private UiPanel? _uiCharacterPanel;
    private UiPanel? _uiCharacterCreatePanel;
    private UiSelectableListWidget? _uiCharacterList;
    private UiButtonWidget? _uiEnterCharacterButton;
    private UiLabel? _uiCharacterStatusLabel;
    private UiLabel? _uiCreateStatusLabel;
    private UiLabel? _uiCreateRaceLabel;
    private UiLabel? _uiCreateClassLabel;
    private UiSelectableListWidget? _uiCreatePerkList;
    private UiButtonWidget? _uiRollStatsButton;
    private UiLabel? _uiCreateStatsLabel;
    private readonly List<CharacterInfo> _accountCharacters = new();
    private int _selectedCharacterIndex = -1;
    private int _createRaceIndex;
    private int _createClassIndex;
    private readonly HashSet<int> _createSelectedPerks = new();
    private readonly Random _random = new();
    private ClientStage _stage = ClientStage.Login;
    private List<StatEntry> _creationStats = new();
    private TcpGameClient? _tcpClient;
    private string _accountUsername = string.Empty;
    private string _accountPassword = string.Empty;

    private enum ClientStage
    {
        Login,
        CharacterSelect,
        CharacterCreate,
        InGame
    }

    private sealed record CharacterInfo(
        string Name,
        byte Race,
        byte Class,
        uint Perks,
        byte StatStrength,
        byte StatConstitution,
        byte StatIntelligence,
        byte StatWisdom,
        byte StatDexterity,
        byte Evasion,
        ushort Level,
        uint Experience,
        byte MapId,
        byte PosX,
        byte PosY,
        ushort MaxMana,
        ushort Mana,
        ushort MaxHealth,
        ushort Health,
        uint Gold,
        uint Silver,
        byte FoodPercent,
        byte Honor,
        ushort Armor,
        ushort MagicResist);

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

    private static readonly string[] PerkNames =
    {
        "Alquimia",
        "Escribir magia",
        "Herbalismo",
        "Joyeria",
        "Herreria",
        "Carpinteria",
        "Sastreria",
        "Holgazaneria",
        "Mineria",
        "Elocuencia",
        "Regeneracion",
        "Ambidextria",
        "Apuñalar",
        "Ocultarse",
        "Ira Berserker",
        "Zoomorfismo"
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
        public string AvatarName { get; set; } = "Testin";
        public int Level { get; set; } = 1;
        public int ClassIndex { get; set; } = 6;
        public int RaceIndex { get; set; } = 0;
        public bool IsMale { get; set; } = true;
        public uint Experience { get; set; } = 0;
        public byte Honor { get; set; } = 1;
        public IReadOnlyList<StatEntry> CoreStats { get; set; } = Array.Empty<StatEntry>();
        public IReadOnlyList<string> SkillLines { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> CombatLines { get; set; } = Array.Empty<string>();
        public int Health { get; set; } = 9;
        public int MaxHealth { get; set; } = 9;
        public int Mana { get; set; } = 3;
        public int MaxMana { get; set; } = 3;
        public int FoodPercent { get; set; } = 48;
        public int Gold { get; set; } = 50;
        public int Silver { get; set; } = 1;
        public int Armor { get; set; }
        public int MagicResist { get; set; }
        public int Evasion { get; set; }

        public static PlayerState CreateSample()
        {
            return new PlayerState
            {
                AvatarName = "Testin",
                Level = 1,
                ClassIndex = 6,
                RaceIndex = 0,
                Experience = 0,
                Honor = 1,
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
                    "Armadura: 0",
                    "Res. Mágica: 0",
                    "Evasión: 0"
                },
                Health = 9,
                MaxHealth = 9,
                Mana = 3,
                MaxMana = 3,
                FoodPercent = 48,
                Gold = 50,
                Silver = 1,
                Armor = 0,
                MagicResist = 0,
                Evasion = 0
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
        BuildLoginPanel(viewport);
        BuildHudPanel(viewport);
        if (_uiHudPanel is not null)
        {
            _uiManager.AddWindow(_uiHudPanel);
        }
        BuildCharacterPanel(viewport);
        BuildCharacterCreatePanel(viewport);
        SetStage(ClientStage.Login);
        var roadmapBounds = new Rectangle(
            viewport.Width - 360,
            64,
            320,
            200);
        SetWorldUiVisibility(visible: false);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            if (_stage == ClientStage.Login)
            {
                Exit();
            }
            else if (_stage == ClientStage.CharacterSelect || _stage == ClientStage.CharacterCreate)
            {
                SetStage(ClientStage.Login);
            }
            else
            {
                Exit();
            }
            return;
        }

        var mouse = Mouse.GetState();
        _uiManager?.Update(gameTime, mouse, _previousMouse);
        if (_stage != ClientStage.InGame)
        {
            PumpNetwork();
            base.Update(gameTime);
            _previousKeyboard = keyboard;
            _previousMouse = mouse;
            return;
        }

        HandleOverlayInput(keyboard);
        HandleMapInput(keyboard);
        HandleHudInput(keyboard);
        HandleInfoPanelInput(keyboard);
        HandleAnimationPreviewInput(keyboard);
        HandleMonsterDebugInput(keyboard);
        HandleUiInput(keyboard);
        var hudCapturedScroll = HandleHudScroll(mouse, _previousMouse);
        _cameraController?.Update(gameTime, keyboard, mouse, !hudCapturedScroll);
        UpdatePlayerMovement(gameTime, keyboard);
        _animationPreview?.Update(gameTime);
        // Selection labels now updated via event handler.
        UpdateHudText();
        UpdateMonsterRenderer(gameTime);
        PumpNetwork();

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
        if (_spriteBatch is null)
        {
            base.Draw(gameTime);
            return;
        }

        if (!_isLoggedIn)
        {
            GraphicsDevice.Clear(Color.Black);
            _uiManager?.Draw(_spriteBatch);
            base.Draw(gameTime);
            return;
        }

        GraphicsDevice.Clear(GetBackgroundColor());

        if (_terrainRenderer is null || _camera is null || _tilePalette is null)
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
            Path.Combine(baseDir, "content", "graphics")
        };

        var roots = candidates
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roots.Length == 0)
        {
            Console.Error.WriteLine("Warning: no graphic directories were found. Place atlases/exports under 'content/graphics'.");
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
        // Monsters will be provided by the server; client no longer seeds fake spawns.
        _worldState.SetMonsters(Array.Empty<MonsterEntity>());
        _monsterRenderer?.SetMonsters(_worldState.Monsters);
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
        // Map cycling disabled while player-controlled movement is active.
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
    }

    private void LoadMapById(byte mapId)
    {
        if (_mapIds.Length == 0)
        {
            InitializeMapList();
        }

        var targetId = $"map_{mapId}";
        var index = Array.FindIndex(_mapIds, id => string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            LoadMapByIndex(0);
            return;
        }

        LoadMapByIndex(index);
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

        builder.AppendLine($"Experiencia: {_playerState.Experience}");
        builder.AppendLine($"Honor: {_playerState.Honor}");
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

    private void BuildLoginPanel(Viewport viewport)
    {
        var width = 440;
        var height = 240;
        var x = (viewport.Width - width) / 2;
        var y = (viewport.Height - height) / 2;
        _uiLoginPanel = new UiPanel("INICIO DE SESIÓN", new Rectangle(x, y, width, height))
        {
            Draggable = false,
            DragAnywhere = false
        };

        _uiLoginPanel.AddWidget(new UiLabel("Usuario", new Vector2(12f, 12f), Color.LightGreen));
        _uiLoginUserInput = new UiTextInputWidget(new Vector2(12f, 32f), new Point(180, 28), "nombre de cuenta");
        _uiLoginPanel.AddWidget(_uiLoginUserInput);

        _uiLoginPanel.AddWidget(new UiLabel("Clave", new Vector2(12f, 70f), Color.LightGreen));
        _uiLoginPassInput = new UiTextInputWidget(new Vector2(12f, 90f), new Point(180, 28), "contraseña", isPassword: true);
        _uiLoginPanel.AddWidget(_uiLoginPassInput);

        var loginButton = new UiButtonWidget("Ingresar", new Vector2(12f, 130f), new Point(180, 28), textColor: Color.LightYellow);
        loginButton.Clicked += OnLoginSubmit;
        _uiLoginPanel.AddWidget(loginButton);

        _uiLoginStatusLabel = new UiLabel(string.Empty, new Vector2(12f, 180f), Color.White);
        _uiLoginPanel.AddWidget(_uiLoginStatusLabel);

        _uiManager?.AddWindow(_uiLoginPanel);
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
        }
        _hudMessageLog.StickToBottom(_uiHudMessageLabels.Length);
        _uiHudPanel = hudPanel;
        AddHudMessage("Interfaz preparada.");
        UpdateHudPanelBounds(viewport);
        PopulateInventoryPreview();
        UpdateHudTabLabels();
        UpdateHudPortrait();
    }

    private void BuildCharacterPanel(Viewport viewport)
    {
        var bounds = new Rectangle(
            (viewport.Width - 640) / 2,
            (viewport.Height - 260) / 2,
            640,
            260);

        _uiCharacterPanel = new UiPanel("PERSONAJES", bounds)
        {
            Draggable = false
        };

        _uiCharacterPanel.AddWidget(new UiLabel("Personajes de la cuenta", new Vector2(12f, 12f), Color.LightGreen));
        _uiCharacterList = new UiSelectableListWidget(columns: 1, cellSize: new Vector2(280f, 26f), color: Color.White)
        {
            Offset = new Vector2(12f, 32f)
        };
        _uiCharacterList.SelectionChanged += indices =>
        {
            _selectedCharacterIndex = indices.Any() ? indices.First() : -1;
            UpdateCharacterStatus();
        };
        _uiCharacterList.ItemActivated += _ => EnterGameWithSelection();
        _uiCharacterPanel.AddWidget(_uiCharacterList);

        _uiCharacterStatusLabel = new UiLabel(string.Empty, new Vector2(12f, 200f), Color.White);
        _uiCharacterPanel.AddWidget(_uiCharacterStatusLabel);

        var createBtn = new UiButtonWidget("Crear personaje", new Vector2(340f, 32f), new Point(200, 28), textColor: Color.LightYellow);
        createBtn.Clicked += () => SetStage(ClientStage.CharacterCreate);
        _uiCharacterPanel.AddWidget(createBtn);

        _uiEnterCharacterButton = new UiButtonWidget("Ingresar", new Vector2(340f, 72f), new Point(200, 28), textColor: Color.LightYellow)
        {
            IsEnabled = false
        };
        _uiEnterCharacterButton.Clicked += EnterGameWithSelection;
        _uiCharacterPanel.AddWidget(_uiEnterCharacterButton);

        var backBtn = new UiButtonWidget("Volver", new Vector2(340f, 112f), new Point(200, 28));
        backBtn.Clicked += () => SetStage(ClientStage.Login);
        _uiCharacterPanel.AddWidget(backBtn);

        var exitBtn = new UiButtonWidget("Salir al escritorio", new Vector2(340f, 152f), new Point(200, 28), textColor: Color.OrangeRed);
        exitBtn.Clicked += Exit;
        _uiCharacterPanel.AddWidget(exitBtn);

        _uiManager?.AddWindow(_uiCharacterPanel);
        RefreshCharacterList();
        UpdateCharacterStatus();
        _uiCharacterPanel.Visible = false;
    }

    private void BuildCharacterCreatePanel(Viewport viewport)
    {
        var bounds = new Rectangle(
            (viewport.Width - 720) / 2,
            (viewport.Height - 320) / 2,
            720,
            320);

        _uiCharacterCreatePanel = new UiPanel("CREAR PERSONAJE", bounds)
        {
            Draggable = false
        };

        _uiCharacterCreatePanel.AddWidget(new UiLabel("Nombre", new Vector2(12f, 12f), Color.LightGreen));
        _uiCreateNameInput = new UiTextInputWidget(new Vector2(12f, 32f), new Point(240, 28), "nombre del personaje");
        _uiCharacterCreatePanel.AddWidget(_uiCreateNameInput);

        _uiCharacterCreatePanel.AddWidget(new UiLabel("Raza", new Vector2(12f, 70f), Color.LightGreen));
        _uiCreateRaceLabel = new UiLabel(string.Empty, new Vector2(80f, 70f), Color.White);
        _uiCharacterCreatePanel.AddWidget(_uiCreateRaceLabel);
        var racePrev = new UiButtonWidget("<", new Vector2(12f, 90f), new Point(32, 24));
        racePrev.Clicked += () => CycleRace(-1);
        _uiCharacterCreatePanel.AddWidget(racePrev);
        var raceNext = new UiButtonWidget(">", new Vector2(52f, 90f), new Point(32, 24));
        raceNext.Clicked += () => CycleRace(1);
        _uiCharacterCreatePanel.AddWidget(raceNext);

        _uiCharacterCreatePanel.AddWidget(new UiLabel("Clase", new Vector2(120f, 70f), Color.LightGreen));
        _uiCreateClassLabel = new UiLabel(string.Empty, new Vector2(180f, 70f), Color.White);
        _uiCharacterCreatePanel.AddWidget(_uiCreateClassLabel);
        var classPrev = new UiButtonWidget("<", new Vector2(120f, 90f), new Point(32, 24));
        classPrev.Clicked += () => CycleClass(-1);
        _uiCharacterCreatePanel.AddWidget(classPrev);
        var classNext = new UiButtonWidget(">", new Vector2(160f, 90f), new Point(32, 24));
        classNext.Clicked += () => CycleClass(1);
        _uiCharacterCreatePanel.AddWidget(classNext);

        _uiCharacterCreatePanel.AddWidget(new UiLabel("Pericias (máx 3)", new Vector2(320f, 12f), Color.LightGreen));
        _uiCreatePerkList = new UiSelectableListWidget(columns: 2, cellSize: new Vector2(180f, 20f), color: Color.White, multiSelect: true, maxSelection: 3)
        {
            Offset = new Vector2(320f, 32f)
        };
        _uiCreatePerkList.SelectionChanged += indices =>
        {
            _createSelectedPerks.Clear();
            foreach (var i in indices)
            {
                _createSelectedPerks.Add(i);
            }
            UpdateCreateStatus($"Pericias seleccionadas: {_createSelectedPerks.Count}/3", Color.White);
        };
        _uiCreatePerkList.SetItems(PerkNames);
        _uiCharacterCreatePanel.AddWidget(_uiCreatePerkList);

        _uiCharacterCreatePanel.AddWidget(new UiLabel("Estadísticas", new Vector2(12f, 130f), Color.LightGreen));
        _uiCreateStatsLabel = new UiLabel(string.Empty, new Vector2(12f, 150f), Color.White);
        _uiCharacterCreatePanel.AddWidget(_uiCreateStatsLabel);
        _uiRollStatsButton = new UiButtonWidget("Tirar dados", new Vector2(12f, 220f), new Point(120, 28), textColor: Color.LightYellow);
        _uiRollStatsButton.Clicked += RollCreationStats;
        _uiCharacterCreatePanel.AddWidget(_uiRollStatsButton);

        var createBtn = new UiButtonWidget("Crear personaje", new Vector2(320f, 240f), new Point(180, 28), textColor: Color.LightYellow);
        createBtn.Clicked += OnCreateCharacterConfirm;
        _uiCharacterCreatePanel.AddWidget(createBtn);

        var cancelBtn = new UiButtonWidget("Cancelar", new Vector2(520f, 240f), new Point(140, 28));
        cancelBtn.Clicked += () => SetStage(ClientStage.CharacterSelect);
        _uiCharacterCreatePanel.AddWidget(cancelBtn);

        _uiCreateStatusLabel = new UiLabel(string.Empty, new Vector2(12f, 260f), Color.White);
        _uiCharacterCreatePanel.AddWidget(_uiCreateStatusLabel);

        _uiManager?.AddWindow(_uiCharacterCreatePanel);
        _uiCharacterCreatePanel.Visible = false;

        ResetCreationForm();
    }

    private void OnLoginSubmit()
    {
        var username = _uiLoginUserInput?.Text.Trim() ?? string.Empty;
        var password = _uiLoginPassInput?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetLoginStatus("Ingresa usuario y clave.", Color.Yellow);
            return;
        }

        _accountUsername = username;
        _accountPassword = password;
        _ = Task.Run(async () =>
        {
            var ok = await EnsureConnectedAsync();
            if (!ok)
            {
                SetLoginStatus("No se pudo conectar al servidor.", Color.OrangeRed);
                return;
            }

            var payload = TcpGameClient.BuildLoginPayload(username, password);
            try
            {
                await _tcpClient!.SendAsync(MessageId.LoginRequest, payload);
                SetLoginStatus("Login enviado...", Color.LightGreen);
            }
            catch (Exception ex)
            {
                SetLoginStatus($"Error enviando login: {ex.Message}", Color.OrangeRed);
            }
        });
    }

    private void SetLoginStatus(string message, Color color)
    {
        if (_uiLoginStatusLabel is null)
        {
            return;
        }

        _uiLoginStatusLabel.Text = message;
        _uiLoginStatusLabel.Color = color;
    }

    private void SetWorldUiVisibility(bool visible)
    {
        _showHud = visible;
        if (_uiHudPanel is not null)
        {
            _uiHudPanel.Visible = visible;
        }
        if (_uiRoadmapWindow is not null)
        {
            _uiRoadmapWindow.Visible = visible;
        }
        // Floating debug panels removed; nothing else to toggle here.
    }

    private void SetStage(ClientStage stage)
    {
        _stage = stage;
        _isLoggedIn = stage == ClientStage.InGame;

        if (_uiLoginPanel is not null)
        {
            _uiLoginPanel.Visible = stage == ClientStage.Login;
        }

        if (_uiCharacterPanel is not null)
        {
            _uiCharacterPanel.Visible = stage == ClientStage.CharacterSelect;
        }

        if (_uiCharacterCreatePanel is not null)
        {
            _uiCharacterCreatePanel.Visible = stage == ClientStage.CharacterCreate;
        }

        var worldVisible = stage == ClientStage.InGame;
        SetWorldUiVisibility(worldVisible);

        if (stage == ClientStage.CharacterSelect)
        {
            RefreshCharacterList();
            UpdateCharacterStatus();
        }

        if (stage == ClientStage.CharacterCreate)
        {
            ResetCreationForm();
        }
    }

    private void RefreshCharacterList()
    {
        _accountCharacters.RemoveAll(c => string.IsNullOrWhiteSpace(c.Name));
        if (_accountCharacters.Count > 5)
        {
            _accountCharacters.RemoveRange(5, _accountCharacters.Count - 5);
        }

        var entries = new List<string>();
        for (var i = 0; i < _accountCharacters.Count; i++)
        {
            entries.Add($"{i + 1}. {_accountCharacters[i].Name}");
        }

        if (_uiCharacterList is not null)
        {
            _uiCharacterList.SetItems(entries);
        }

        if (_accountCharacters.Count == 0)
        {
            _selectedCharacterIndex = -1;
        }
        else
        {
            _selectedCharacterIndex = Math.Clamp(_selectedCharacterIndex, 0, _accountCharacters.Count - 1);
        }
    }

    private void UpdateCharacterStatus()
    {
        if (_uiCharacterStatusLabel is null)
        {
            return;
        }

        if (_accountCharacters.Count == 0)
        {
            _uiCharacterStatusLabel.Text = "No hay personajes. Crea uno nuevo.";
            _uiCharacterStatusLabel.Color = Color.Yellow;
            if (_uiEnterCharacterButton is not null)
            {
                _uiEnterCharacterButton.IsEnabled = false;
            }
            return;
        }

        if (_selectedCharacterIndex < 0 || _selectedCharacterIndex >= _accountCharacters.Count)
        {
            _uiCharacterStatusLabel.Text = "Selecciona un personaje para entrar.";
            _uiCharacterStatusLabel.Color = Color.White;
            if (_uiEnterCharacterButton is not null)
            {
                _uiEnterCharacterButton.IsEnabled = false;
            }
            return;
        }

        var name = _accountCharacters[_selectedCharacterIndex].Name;
        _uiCharacterStatusLabel.Text = $"Listo para entrar: {name}";
        _uiCharacterStatusLabel.Color = Color.LightGreen;
        if (_uiEnterCharacterButton is not null)
        {
            _uiEnterCharacterButton.IsEnabled = true;
        }
    }

    private void EnterGameWithSelection()
    {
        if (_selectedCharacterIndex < 0 || _selectedCharacterIndex >= _accountCharacters.Count)
        {
            UpdateCharacterStatus();
            return;
        }

        var name = _accountCharacters[_selectedCharacterIndex];
        AddHudMessage($"Entrando con {name.Name}...");
        _ = Task.Run(async () =>
        {
            var ok = await EnsureConnectedAsync();
            if (!ok)
            {
                UpdateCharacterStatus();
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(name.Name);
            await _tcpClient!.SendAsync(MessageId.EnterWorldRequest, bytes);
        });
    }

    private void ResetCreationForm()
    {
        _createRaceIndex = 0;
        _createClassIndex = 0;
        _createSelectedPerks.Clear();
        if (_uiCreateNameInput is not null)
        {
            _uiCreateNameInput.Text = string.Empty;
        }
        if (_uiCreatePerkList is not null)
        {
            _uiCreatePerkList.SetItems(PerkNames);
        }

        RollCreationStats();
        UpdateRaceClassLabels();
        UpdateCreateStatus("Configura tu personaje y presiona Crear.", Color.White);
    }

    private void CycleRace(int delta)
    {
        _createRaceIndex = WrapValue(_createRaceIndex, delta, PlayerRaceNames.Length);
        UpdateRaceClassLabels();
    }

    private void CycleClass(int delta)
    {
        _createClassIndex = WrapValue(_createClassIndex, delta, PlayerClassNames.Length);
        UpdateRaceClassLabels();
    }

    private void UpdateRaceClassLabels()
    {
        if (_uiCreateRaceLabel is not null)
        {
            _uiCreateRaceLabel.Text = PlayerRaceNames[_createRaceIndex];
        }

        if (_uiCreateClassLabel is not null)
        {
            _uiCreateClassLabel.Text = PlayerClassNames[_createClassIndex];
        }
    }

    private void RollCreationStats()
    {
        var stats = new List<StatEntry>();
        int total;
        do
        {
            stats.Clear();
            total = 0;
            foreach (var name in new[] { "Fuerza", "Constitución", "Inteligencia", "Sabiduría", "Destreza" })
            {
                var value = (_random.Next(0, 10) + 1) * 5;
                total += value;
                stats.Add(new StatEntry(name, $"{value}%"));
            }
        } while (total >= 100);

        _creationStats = stats;
        UpdateCreationStatsLabel();
    }

    private void UpdateCreationStatsLabel()
    {
        if (_uiCreateStatsLabel is null)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var stat in _creationStats)
        {
            builder.AppendLine($"{stat.Label}: {stat.Value}");
        }

        _uiCreateStatsLabel.Text = builder.ToString().TrimEnd();
    }

    private void OnCreateCharacterConfirm()
    {
        var name = _uiCreateNameInput?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateCreateStatus("El nombre es obligatorio.", Color.Yellow);
            return;
        }

        if (_createSelectedPerks.Count > 3)
        {
            UpdateCreateStatus("Máximo 3 pericias.", Color.OrangeRed);
            return;
        }

        var payload = new CharacterCreationPayload(
            Name: name,
            Race: (byte)_createRaceIndex,
            Class: (byte)_createClassIndex,
            PerkMask: BuildPerkMask(),
            Strength: ParseStatValue("Fuerza"),
            Constitution: ParseStatValue("Constitución"),
            Intelligence: ParseStatValue("Inteligencia"),
            Wisdom: ParseStatValue("Sabiduría"),
            Dexterity: ParseStatValue("Destreza"));

        _ = Task.Run(async () =>
        {
            var ok = await EnsureConnectedAsync();
            if (!ok)
            {
                UpdateCreateStatus("No conectado al servidor.", Color.OrangeRed);
                return;
            }

            var bytes = TcpGameClient.BuildCharacterCreatePayload(_accountUsername, payload);
            await _tcpClient!.SendAsync(MessageId.CharacterCreateRequest, bytes);
            UpdateCreateStatus("Creación enviada...", Color.LightGreen);
        });
    }

    private void UpdateCreateStatus(string message, Color color)
    {
        if (_uiCreateStatusLabel is null)
        {
            return;
        }

        _uiCreateStatusLabel.Text = message;
        _uiCreateStatusLabel.Color = color;
    }

    private uint BuildPerkMask()
    {
        uint mask = 0;
        foreach (var index in _createSelectedPerks)
        {
            if (index >= 0 && index < 32)
            {
                mask |= (uint)(1 << index);
            }
        }
        return mask;
    }

    private byte ParseStatValue(string name)
    {
        var stat = _creationStats.FirstOrDefault(s => s.Label == name);
        if (stat is null)
        {
            return 5;
        }

        if (stat.Value.EndsWith("%") && byte.TryParse(stat.Value.TrimEnd('%'), out var value))
        {
            return value;
        }

        return 5;
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (_tcpClient is null)
        {
            _tcpClient = new TcpGameClient();
        }

        if (_tcpClient is not null && _tcpClientConnected)
        {
            return true;
        }

        try
        {
            var ok = await _tcpClient.ConnectAsync("127.0.0.1", 7667, CancellationToken.None);
            _tcpClientConnected = ok;
            return ok;
        }
        catch
        {
            _tcpClientConnected = false;
            return false;
        }
    }

    private bool _tcpClientConnected;

    private void PumpNetwork()
    {
        if (_tcpClient is null || !_tcpClientConnected)
        {
            return;
        }

        try
        {
            var packets = _tcpClient.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
            foreach (var packet in packets)
            {
                HandlePacket(packet);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[NET] Error: {ex.Message}");
            _tcpClientConnected = false;
        }
    }

    private void HandlePacket(Packet packet)
    {
        switch (packet.MessageId)
        {
            case MessageId.LoginResponse:
                HandleLoginResponse(packet.Payload.Span);
                break;
            case MessageId.CharacterListResponse:
                HandleCharacterListResponse(packet.Payload.Span);
                break;
            case MessageId.CharacterCreateResponse:
                HandleCharacterCreateResponse(packet.Payload.Span);
                break;
            case MessageId.EnterWorldResponse:
                HandleEnterWorldResponse();
                break;
            case MessageId.ErrorResponse:
                HandleErrorResponse(packet.Payload.Span);
                break;
        }
    }

    private void HandleLoginResponse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
        {
            SetLoginStatus("Respuesta de login inválida.", Color.OrangeRed);
            return;
        }

        var result = payload[0];
        if (result != 0)
        {
            SetLoginStatus($"Login rechazado (código {result}).", Color.OrangeRed);
            return;
        }

        SetLoginStatus("Login OK.", Color.LightGreen);
        RequestCharacterList();
    }

    private void RequestCharacterList()
    {
        if (string.IsNullOrWhiteSpace(_accountUsername) || _tcpClient is null)
        {
            return;
        }

        var bytes = TcpGameClient.BuildCharacterListPayload(_accountUsername);
        _ = _tcpClient.SendAsync(MessageId.CharacterListRequest, bytes);
    }

    private void HandleCharacterListResponse(ReadOnlySpan<byte> payload)
    {
        _accountCharacters.Clear();
        if (payload.IsEmpty)
        {
            SetStage(ClientStage.CharacterSelect);
            return;
        }

        var reader = new SpanReader(payload);
        if (!reader.TryReadByte(out var count))
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (!reader.TryReadString(out var name))
            {
                break;
            }

            if (!reader.TryReadByte(out var race) ||
                !reader.TryReadByte(out var cls) ||
                !reader.TryReadUInt(out var perks) ||
                !reader.TryReadByte(out var str) ||
                !reader.TryReadByte(out var con) ||
                !reader.TryReadByte(out var intel) ||
                !reader.TryReadByte(out var wis) ||
                !reader.TryReadByte(out var dex) ||
                !reader.TryReadByte(out var evasion) ||
                !reader.TryReadUInt16(out var level) ||
                !reader.TryReadUInt(out var xp) ||
                !reader.TryReadByte(out var mapId) ||
                !reader.TryReadByte(out var posX) ||
                !reader.TryReadByte(out var posY) ||
                !reader.TryReadUInt16(out var maxMana) ||
                !reader.TryReadUInt16(out var mana) ||
                !reader.TryReadUInt16(out var maxHp) ||
                !reader.TryReadUInt16(out var hp) ||
                !reader.TryReadUInt(out var gold) ||
                !reader.TryReadUInt(out var silver) ||
                !reader.TryReadByte(out var food) ||
                !reader.TryReadByte(out var honor) ||
                !reader.TryReadUInt16(out var armor) ||
                !reader.TryReadUInt16(out var magicResist))
            {
                break;
            }

            var invCount = reader.TryReadByte(out var inv) ? inv : (byte)0;
            for (var invIdx = 0; invIdx < invCount; invIdx++)
            {
                reader.TryReadUInt(out _);
                reader.TryReadUInt16(out _);
                reader.TryReadByte(out _);
            }

            var spellCount = reader.TryReadByte(out var spells) ? spells : (byte)0;
            for (var s = 0; s < spellCount; s++)
            {
                reader.TryReadUInt16(out _);
            }

            _accountCharacters.Add(new CharacterInfo(
                name,
                race,
                cls,
                perks,
                str,
                con,
                intel,
                wis,
                dex,
                evasion,
                level,
                xp,
                mapId,
                posX,
                posY,
                maxMana,
                mana,
                maxHp,
                hp,
                gold,
                silver,
                food,
                honor,
                armor,
                magicResist));
        }

        _selectedCharacterIndex = _accountCharacters.Count > 0 ? 0 : -1;
        SetStage(ClientStage.CharacterSelect);
    }

    private void HandleCharacterCreateResponse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0 || payload[0] != 0)
        {
            UpdateCreateStatus("Creación rechazada.", Color.OrangeRed);
            return;
        }

        UpdateCreateStatus("Personaje creado.", Color.LightGreen);
        RequestCharacterList();
        SetStage(ClientStage.CharacterSelect);
    }

    private void HandleEnterWorldResponse()
    {
        if (_selectedCharacterIndex < 0 || _selectedCharacterIndex >= _accountCharacters.Count)
        {
            return;
        }

        var info = _accountCharacters[_selectedCharacterIndex];
        ApplyCharacterToWorld(info);
        SetStage(ClientStage.InGame);
    }

    private void UpdatePlayerMovement(GameTime gameTime, KeyboardState keyboard)
    {
        if (_playerEntity is null || _camera is null || gameTime is null)
        {
            return;
        }

        var move = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
        {
            move.X -= 1f;
        }
        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
        {
            move.X += 1f;
        }
        if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
        {
            move.Y -= 1f;
        }
        if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
        {
            move.Y += 1f;
        }

        if (move != Vector2.Zero)
        {
            move.Normalize();
            _playerDirection = VectorToDirection(move, out _playerMirror);
            _playerAction = MonsterAction.Moving;
        }
        else
        {
            _playerAction = MonsterAction.Idle;
        }

        const float speed = 90f; // pixels per second
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var displacement = move * speed * delta;
        var next = _playerEntity.Position + displacement;

        // Clamp to map bounds
        if (_activeMap is not null)
        {
            var width = _activeMap.Terrain.FirstOrDefault()?.Count ?? 0;
            var height = _activeMap.Terrain.Count;
            var maxX = width * TileWidth;
            var maxY = height * TileHeight;
            next.X = Math.Clamp(next.X, 0f, maxX);
            next.Y = Math.Clamp(next.Y, 0f, maxY);
        }

        _playerEntity.SetPosition(next);
        _camera.CenterOn(next);
        UpdatePlayerAnimation(gameTime);
    }

    private void UpdatePlayerAnimation(GameTime gameTime)
    {
        if (_playerAnimation is null)
        {
            return;
        }

        var directionSlice = _playerAnimation.Directions[Math.Clamp(_playerDirection, 0, _playerAnimation.Directions.Count - 1)];
        var sequence = _playerAction == MonsterAction.Moving
            ? MonsterSpriteSequences.Move
            : MonsterSpriteSequences.Idle;

        _playerAnimTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        const float frameDuration = 0.12f;
        while (_playerAnimTimer >= frameDuration)
        {
            _playerAnimTimer -= frameDuration;
            _playerFrameIndex = (_playerFrameIndex + 1) % sequence.Length;
        }
    }

    private static class MonsterSpriteSequences
    {
        public static readonly int[] Idle = { 4 };
        public static readonly int[] Move = { 0, 1, 2, 3 };
    }

    private AnimationFrameSlice GetPlayerFrame(AnimationDirectionSlice direction)
    {
        var sequence = _playerAction == MonsterAction.Moving
            ? MonsterSpriteSequences.Move
            : MonsterSpriteSequences.Idle;
        if (sequence.Length == 0)
        {
            return default;
        }

        var idx = Math.Abs(_playerFrameIndex) % sequence.Length;
        var frameIndex = sequence[idx];
        if (frameIndex < 0 || frameIndex >= direction.Frames.Count)
        {
            return default;
        }

        return direction.Frames[frameIndex];
    }


    private int VectorToDirection(Vector2 move, out bool mirror)
    {
        mirror = false;
        if (move == Vector2.Zero) return _playerDirection;
        var dirCount = _playerAnimation?.Directions.Count ?? 8;

        if (dirCount == 5)
        {
            // Legacy 5-direction set using MC_DirAnimacion mapping from Pascal:
            // 8-way input order: 0=N,1=S,2=W,3=E,4=NE,5=SW,6=NW,7=SE
            // maps to:           0,1,2,2,3,4,3,4
            int dir8;
            var ax = Math.Abs(move.X);
            var ay = Math.Abs(move.Y);
            if (ax < 0.01f && ay < 0.01f)
            {
                dir8 = 0;
            }
            else if (ay >= ax && ay > 0f && ax < 0.01f)
            {
                dir8 = move.Y > 0 ? 1 : 0; // S or N
            }
            else if (ax > ay && ay < 0.01f)
            {
                dir8 = move.X > 0 ? 3 : 2; // E or W
            }
            else
            {
                if (move.X > 0 && move.Y < 0) dir8 = 4;       // NE
                else if (move.X < 0 && move.Y > 0) dir8 = 5;  // SW
                else if (move.X < 0 && move.Y < 0) dir8 = 6;  // NW
                else dir8 = 7;                                // SE
            }

            int[] mcDirAnim = { 0, 1, 2, 2, 3, 4, 3, 4 };
            int[] mcMirror = { 0, 0, 0, 1, 1, 0, 0, 1 }; // MC_espejo from Pascal (true for E, NE, SE)
            mirror = mcMirror[Math.Clamp(dir8, 0, 7)] != 0;
            return mcDirAnim[Math.Clamp(dir8, 0, 7)];
        }

        if (dirCount == 4)
        {
            // Assume order: 0=N,1=S,2=W,3=E
            if (Math.Abs(move.X) > Math.Abs(move.Y))
            {
                return move.X > 0 ? 3 : 2; // E or W
            }

            return move.Y < 0 ? 0 : 1; // N or S (Y- up)
        }

        var angle = MathF.Atan2(-move.Y, move.X); // invert Y to match screen coords
        var octant = (int)MathF.Round(8 * angle / (2 * MathF.PI)) % 8;
        if (octant < 0) octant += 8;
        return MapDirectionIndex(dirCount, octant);
    }

    private static int MapDirectionIndex(int directionCount, int octant)
    {
        if (directionCount >= 8)
        {
            // Asset order from MC_avanceX/MC_avanceY (Demonios.pas):
            // 0=N (0,-1),1=S (0,1),2=W (-1,0),3=E (1,0),
            // 4=NE (1,-1),5=SW (-1,1),6=NW (-1,-1),7=SE (1,1)
            // Movement octants: 0=E,1=NE,2=N,3=NW,4=W,5=SW,6=S,7=SE
            int[] map = { 3, 4, 0, 6, 2, 5, 1, 7 };
            return map[Math.Clamp(octant, 0, 7)];
        }

        return Math.Clamp(octant, 0, Math.Max(0, directionCount - 1));
    }

    private void HandleErrorResponse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            return;
        }

        var code = payload[0];
        var message = payload.Length > 1 ? Encoding.UTF8.GetString(payload[1..]) : $"Error {code}";
        if (_stage == ClientStage.Login)
        {
            SetLoginStatus(message, Color.OrangeRed);
        }
        else
        {
            UpdateCreateStatus(message, Color.OrangeRed);
        }
    }

    private void ApplyCharacterToWorld(CharacterInfo info)
    {
        LoadMapById(info.MapId);
        var stats = new[]
        {
            new StatEntry("Fuerza", $"{info.StatStrength}%"),
            new StatEntry("Constitución", $"{info.StatConstitution}%"),
            new StatEntry("Inteligencia", $"{info.StatIntelligence}%"),
            new StatEntry("Sabiduría", $"{info.StatWisdom}%"),
            new StatEntry("Destreza", $"{info.StatDexterity}%")
        };

        _playerState.AvatarName = info.Name;
        _playerState.Level = info.Level;
        _playerState.ClassIndex = info.Class;
        _playerState.RaceIndex = info.Race;
        _playerState.Experience = info.Experience;
        _playerState.Honor = info.Honor;
        _playerState.CoreStats = stats;
        _playerState.SkillLines = BuildPerkLines(info.Perks);
        _playerState.CombatLines = new[]
        {
            $"Armadura: {info.Armor}",
            $"Res. Mágica: {info.MagicResist}",
            $"Evasión: {info.Evasion}"
        };
        _playerState.Health = info.Health;
        _playerState.MaxHealth = info.MaxHealth;
        _playerState.Mana = info.Mana;
        _playerState.MaxMana = info.MaxMana;
        _playerState.FoodPercent = info.FoodPercent;
        _playerState.Gold = (int)info.Gold;
        _playerState.Silver = (int)info.Silver;
        _playerState.Armor = info.Armor;
        _playerState.MagicResist = info.MagicResist;
        _playerState.Evasion = info.Evasion;

        var spawn = new Vector2(
            (info.PosX + 0.5f) * TileWidth,
            (info.PosY + 1f) * TileHeight);
        _playerEntity = new PlayerEntity(id: -1, position: spawn);
        _camera?.CenterOn(spawn);
        _welcomeMessage = $"Bienvenido, {info.Name}";
        BuildPlayerAnimation(info);
    }

    private void BuildPlayerAnimation(CharacterInfo info)
    {
        if (_animationMapping is null || _animationTextureProvider is null)
        {
            _playerAnimation = null;
            return;
        }

        var genderMask = 0; // 0 = male (bit 11 set would be female)
        var armorIndex = 0;
        var classIndex = info.Class & 0x7;
        var raceIndex = info.Race & 0x7;
        var index = (armorIndex & 0x1F) |
                    ((classIndex & 0x7) << 5) |
                    ((raceIndex & 0x7) << 8) |
                    genderMask;

        var ids = _animationMapping.AnimationIds;
        if (index < 0 || index >= ids.Count)
        {
            Console.Error.WriteLine($"Player animation index {index} out of range.");
            _playerAnimation = null;
            return;
        }

        var animationId = ids[index];
        var key = $"m{animationId}";
        if (!_animationTextureProvider.TryGetAnimation(key, out var animation))
        {
            Console.Error.WriteLine($"Player animation '{key}' not found.");
            _playerAnimation = null;
            return;
        }

        _playerAnimation = animation;
        _playerDirection = 0;
        _playerAction = MonsterAction.Idle;
        _playerFrameIndex = 0;
        _playerAnimTimer = 0f;
        if (!_loggedPlayerAnimInfo)
        {
            _loggedPlayerAnimInfo = true;
            var dirInfo = string.Join(", ",
                animation.Directions.Select((d, i) =>
                    $"dir {i}: frames {d.Frames.Count} nonEmpty {d.Frames.Count(f => f.Source != Rectangle.Empty)}"));
            Console.WriteLine($"Player animation {key} dirs={animation.Directions.Count} | {dirInfo}");
        }
    }

    private static IReadOnlyList<string> BuildPerkLines(uint perkMask)
    {
        var perks = new List<string>();
        for (var i = 0; i < PerkNames.Length; i++)
        {
            var bit = 1u << i;
            if ((perkMask & bit) != 0)
            {
                perks.Add($"o {PerkNames[i]}");
                if (perks.Count >= 3)
                {
                    break;
                }
            }
        }

        return perks;
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
        RefreshHudHintLabel();
    }

    private void RefreshHudHintLabel()
    {
        if (_uiHudHintLabel is null)
        {
            return;
        }

        _uiHudHintLabel.Text = string.Empty;
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
            _uiHudHealthLabel.Text = $"{_playerState.Health} / {_playerState.MaxHealth}";
        }

        var manaRatio = _playerState.MaxMana > 0
            ? Math.Clamp((float)_playerState.Mana / _playerState.MaxMana, 0f, 1f)
            : 0f;
        _hudManaFill = manaRatio;
        if (_uiHudManaLabel is not null)
        {
            _uiHudManaLabel.Text = $"{_playerState.Mana} / {_playerState.MaxMana}";
        }

        if (_uiHudFoodLabel is not null)
        {
            _uiHudFoodLabel.Text = $"{_playerState.FoodPercent}%";
        }

        if (_uiHudGoldLabel is not null)
        {
            _uiHudGoldLabel.Text = $"{_playerState.Gold}";
        }

        if (_uiHudSilverLabel is not null)
        {
            _uiHudSilverLabel.Text = $"{_playerState.Silver}";
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

        if (_playerEntity is not null && _animationTextureProvider is not null)
        {
            DrawPlayer(_spriteBatch, _camera, _playerEntity);
        }
    }

    private void DrawPlayer(SpriteBatch spriteBatch, Camera2D camera, PlayerEntity player)
    {
        if (_debugTextRenderer is null)
        {
            return;
        }

        if (_playerAnimation is null)
        {
            var size = new Point(20, 28);
            var position = player.Position - new Vector2(size.X / 2f, size.Y);
            var rect = new Rectangle((int)position.X, (int)position.Y, size.X, size.Y);

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                blendState: BlendState.NonPremultiplied,
                transformMatrix: camera.GetViewMatrix());

            spriteBatch.Draw(_debugTextRenderer.PixelTexture, rect, Color.CornflowerBlue);
            spriteBatch.End();
            return;
        }

        var directionSlice = _playerAnimation.Directions[Math.Clamp(_playerDirection, 0, _playerAnimation.Directions.Count - 1)];
        var frame = GetPlayerFrame(directionSlice);
        if (frame.Source == Rectangle.Empty)
        {
            return;
        }

        var position2 = AnimationRenderHelper.CalculateDrawPosition(player.Position, directionSlice, frame, _playerMirror);

        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.NonPremultiplied,
            transformMatrix: camera.GetViewMatrix());

        var effects = _playerMirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(_playerAnimation.Texture, position2, frame.Source, Color.White, 0f, Vector2.Zero, Vector2.One, effects, 0.7f);
        spriteBatch.End();
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
