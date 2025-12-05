using System.Linq;
using System.Text;
using System.Text.Json;
using LegacyDataExtractor.Parsing;

var app = new ExtractionApp();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
return app.Run(args);

internal sealed class ExtractionApp
{
    private readonly MapFileParser _mapParser = new();
    private readonly ItemFileParser _itemParser = new();
    private readonly SpellFileParser _spellParser = new();
    private readonly CommerceFileParser _commerceParser = new();
    private readonly MonsterFileParser _monsterParser = new();
    private readonly AttackMappingParser _attackMappingParser = new();
    private readonly AnimationMappingParser _animationMappingParser = new();
    private readonly GraphicFileParser _graphicParser = new();

    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var commandArgs = args.Skip(1).ToArray();

        return command switch
        {
            "map" => RunMap(commandArgs),
            "items" => RunItems(commandArgs),
            "spells" => RunSpells(commandArgs),
            "commerce" => RunCommerce(commandArgs),
            "monsters" => RunMonsters(commandArgs),
            "attackmap" => RunAttackMap(commandArgs),
            "animmap" => RunAnimationMap(commandArgs),
            "graphics" => RunGraphics(commandArgs),
            "-h" => PrintHelpAndReturnSuccess(),
            "--help" => PrintHelpAndReturnSuccess(),
            _ => PrintUnknownCommand(command)
        };
    }

    private static int PrintHelpAndReturnSuccess()
    {
        PrintUsage();
        return 0;
    }

    private static int PrintUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintUsage();
        return 1;
    }

    private int RunMap(string[] args)
    {
        var (inputPath, outputPath) = ParseMapArgs(args);

        try
        {
            var map = _mapParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(map, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Map exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse map '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunItems(string[] args)
    {
        var (inputPath, outputPath) = ParseItemArgs(args);
        try
        {
            var data = _itemParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Items exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse item file '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunSpells(string[] args)
    {
        var (inputPath, outputPath) = ParseSpellArgs(args);
        try
        {
            var data = _spellParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Spells exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse spell file '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunCommerce(string[] args)
    {
        var (inputPath, outputPath) = ParseCommerceArgs(args);
        try
        {
            var data = _commerceParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Commerce tables exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse commerce file '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunMonsters(string[] args)
    {
        var (inputPath, outputPath) = ParseMonsterArgs(args);
        try
        {
            var data = _monsterParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Monsters exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse monster file '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunAttackMap(string[] args)
    {
        var (inputPath, outputPath) = ParseAttackMapArgs(args);
        try
        {
            var data = _attackMappingParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Attack mapping exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse attack mapping '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunAnimationMap(string[] args)
    {
        var (inputPath, outputPath) = ParseAnimationMapArgs(args);
        try
        {
            var data = _animationMappingParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Animation mapping exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse animation mapping '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private int RunGraphics(string[] args)
    {
        var (inputPath, outputPath) = ParseGraphicArgs(args);
        try
        {
            var data = _graphicParser.Parse(inputPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            if (!string.IsNullOrEmpty(outputPath))
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath!, json);
                Console.WriteLine($"Graphic descriptors exported to {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse oc.b '{inputPath}': {ex.Message}");
            return 1;
        }
    }

    private static (string inputPath, string? outputPath) ParseMapArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }

                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }

                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "0.mpv");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseItemArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }

                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }

                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "obj.b");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseSpellArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }

                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }

                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "cjr.b");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseCommerceArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }

                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }

                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "comercio.b");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseMonsterArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }
                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }
                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "std.mon");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseAttackMapArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }
                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }
                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "mp_ataq.b");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseAnimationMapArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }
                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }
                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "mp_anim.b");
        return (inputPath, outputPath);
    }

    private static (string inputPath, string? outputPath) ParseGraphicArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "-o":
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --out option.");
                    }
                    outputPath = args[++i];
                    break;
                default:
                    if (current.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown option '{current}'.");
                    }
                    inputPath = current;
                    break;
            }
        }

        inputPath ??= Path.Combine("Original Pascal", "Laa", "bin", "oc.b");
        return (inputPath, outputPath);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("LegacyDataExtractor");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- map [path/to/map.mpv] [--out export.json]");
        Console.WriteLine("  dotnet run -- items [path/to/obj.b] [--out items.json]");
        Console.WriteLine("  dotnet run -- spells [path/to/cjr.b] [--out spells.json]");
        Console.WriteLine("  dotnet run -- commerce [path/to/comercio.b] [--out commerce.json]");
        Console.WriteLine("  dotnet run -- monsters [path/to/std.mon] [--out monsters.json]");
        Console.WriteLine("  dotnet run -- attackmap [path/to/mp_ataq.b] [--out attack_map.json]");
        Console.WriteLine("  dotnet run -- animmap [path/to/mp_anim.b] [--out anim_map.json]");
        Console.WriteLine("  dotnet run -- graphics [path/to/oc.b] [--out graphics.json]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  map        Parses a legacy .mpv map file and prints or exports JSON.");
        Console.WriteLine("  items      Reads obj.b (item descriptors) and prints or exports JSON.");
        Console.WriteLine("  spells     Reads cjr.b (spell descriptors) and prints or exports JSON.");
        Console.WriteLine("  commerce   Reads comercio.b (shop inventories) and prints or exports JSON.");
        Console.WriteLine("  monsters   Reads std.mon (monster definitions) and prints or exports JSON.");
        Console.WriteLine("  attackmap  Reads mp_ataq.b (attack mappings) and prints or exports JSON.");
        Console.WriteLine("  animmap    Reads mp_anim.b (animation mappings) and prints or exports JSON.");
        Console.WriteLine("  graphics   Reads oc.b (static graphic descriptors) and prints or exports JSON.");
        Console.WriteLine();
        Console.WriteLine("If no path is provided, defaults are used inside 'Original Pascal/Laa/bin/'.");
    }
}
