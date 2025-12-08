using Microsoft.Xna.Framework;

namespace Laa.Monogame.Client.World;

public sealed class MonsterSimulation
{
    private const float MoveRadiusPixels = 6f;
    private const float MoveSpeed = 0.75f;
    private const double MinStateDuration = 2.0;
    private const double MaxStateDuration = 5.0;

    private readonly Dictionary<int, MonsterSimState> _states = new();
    private readonly Random _random = new(0xA17B23);

    public void SetMonsters(IEnumerable<MonsterEntity> entities)
    {
        _states.Clear();
        foreach (var entity in entities)
        {
            _states[entity.Id] = new MonsterSimState(entity);
        }
    }

    public void Update(GameTime gameTime)
    {
        if (gameTime is null || _states.Count == 0)
        {
            return;
        }

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var now = gameTime.TotalGameTime.TotalSeconds;
        foreach (var state in _states.Values)
        {
            state.Update(elapsed, now, _random);
        }
    }

    private sealed class MonsterSimState
    {
        private readonly MonsterEntity _entity;
        private double _nextTransition;
        private float _phase;
        private float _direction;

        public MonsterSimState(MonsterEntity entity)
        {
            _entity = entity;
            _direction = entity.FacingSeed * 0.25f;
            ScheduleNextTransition(0.0, new Random(entity.FacingSeed));
        }

        public void Update(float elapsedSeconds, double now, Random random)
        {
            if (now >= _nextTransition)
            {
                ToggleState(random);
                ScheduleNextTransition(now, random);
            }

            if (_entity.Action == MonsterAction.Moving)
            {
                _phase += elapsedSeconds * MoveSpeed;
                var offset = new Vector2(
                    (float)Math.Cos(_phase + _direction),
                    (float)Math.Sin(_phase + _direction)) * MoveRadiusPixels;
                _entity.SetPosition(_entity.BaseAnchor + offset);
            }
            else
            {
                _entity.ResetPosition();
            }
        }

        private void ToggleState(Random random)
        {
            if (_entity.Action == MonsterAction.Moving)
            {
                _entity.SetAction(MonsterAction.Idle);
            }
            else if (_entity.Action == MonsterAction.Idle)
            {
                var roll = random.NextDouble();
                if (roll > 0.7)
                {
                    _entity.SetAction(MonsterAction.Attack);
                }
                else
                {
                    _entity.SetAction(MonsterAction.Moving);
                }
            }
            else
            {
                _entity.SetAction(MonsterAction.Idle);
            }
        }

        private void ScheduleNextTransition(double now, Random random)
        {
            var duration = MinStateDuration + random.NextDouble() * (MaxStateDuration - MinStateDuration);
            _nextTransition = now + duration;
        }
    }
}
