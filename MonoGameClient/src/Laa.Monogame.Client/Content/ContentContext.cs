using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Laa.Content.Core.Commerce;
using Laa.Content.Core.Graphics;
using Laa.Content.Core.Items;
using Laa.Content.Core.Mappings;
using Laa.Content.Core.Monsters;
using Laa.Content.Core.Maps;
using Laa.Content.Core.Repositories;
using Laa.Content.Core.Spells;
using Laa.Content.Json.Repositories;

namespace Laa.Monogame.Client.Content;

public sealed class ContentContext
{
    public IMapRepository Maps { get; }
    public IItemRepository Items { get; }
    public ISpellRepository Spells { get; }
    public ICommerceRepository Commerce { get; }
    public IMonsterRepository Monsters { get; }
    public IAttackMappingRepository AttackMappings { get; }
    public IAnimationMappingRepository AnimationMappings { get; }
    public IGraphicRepository Graphics { get; }
    public IAnimationRepository Animations { get; }

    private ContentContext(
        IMapRepository maps,
        IItemRepository items,
        ISpellRepository spells,
        ICommerceRepository commerce,
        IMonsterRepository monsters,
        IAttackMappingRepository attackMappings,
        IAnimationMappingRepository animationMappings,
        IGraphicRepository graphics,
        IAnimationRepository animations)
    {
        Maps = maps;
        Items = items;
        Spells = spells;
        Commerce = commerce;
        Monsters = monsters;
        AttackMappings = attackMappings;
        AnimationMappings = animationMappings;
        Graphics = graphics;
        Animations = animations;
    }

    public static ContentContext Create(string? rootPath = null)
    {
        var dataRoot = ResolveContentRoot(rootPath);
        var options = JsonSerializerOptionsFactory();
        var mapPath = Path.Combine(dataRoot, "maps");
        if (!Directory.Exists(mapPath))
        {
            mapPath = dataRoot;
        }

        string ResolveFile(string fileName)
        {
            var candidate = Path.Combine(dataRoot, fileName);
            if (!File.Exists(candidate))
            {
                throw new FileNotFoundException($"Required content file '{fileName}' was not found inside '{dataRoot}'.", candidate);
            }
            return candidate;
        }

        return new ContentContext(
            new JsonMapRepository(mapPath, options),
            new JsonItemRepository(ResolveFile("items.json"), options),
            new JsonSpellRepository(ResolveFile("spells.json"), options),
            new JsonCommerceRepository(ResolveFile("commerce.json"), options),
            new JsonMonsterRepository(ResolveFile("monsters.json"), options),
            new JsonAttackMappingRepository(ResolveFile("attack_map.json"), options),
            new JsonAnimationMappingRepository(ResolveFile("anim_map.json"), options),
            new JsonGraphicRepository(ResolveFile("graphics.json"), options),
            new JsonAnimationRepository(ResolveFile("animations.json"), options));
    }

    private static string ResolveContentRoot(string? overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>
        {
            Path.Combine(baseDir, "content", "data"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "Docs", "Formats", "Samples"),
            Path.Combine(baseDir, "..", "..", "Docs", "Formats", "Samples")
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath))
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the exported JSON data. Set the base path explicitly or copy the files under 'content/data'.");
    }

    private static JsonSerializerOptions JsonSerializerOptionsFactory()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        return options;
    }
}
