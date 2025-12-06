using System.Collections.Generic;

namespace Laa.Monogame.Client.World;

public sealed class WorldState
{
    private readonly List<MonsterEntity> _monsters = new();
    private readonly Dictionary<int, MonsterEntity> _monsterLookup = new();

    public IReadOnlyList<MonsterEntity> Monsters => _monsters;

    public void SetMonsters(IEnumerable<MonsterEntity> monsters)
    {
        _monsters.Clear();
        _monsterLookup.Clear();
        if (monsters is null)
        {
            return;
        }

        foreach (var monster in monsters)
        {
            if (monster is null)
            {
                continue;
            }

            _monsters.Add(monster);
            _monsterLookup[monster.Id] = monster;
        }
    }

    public MonsterEntity? FindMonster(int id)
    {
        return _monsterLookup.TryGetValue(id, out var monster) ? monster : null;
    }
}
