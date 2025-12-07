using System.Collections.Concurrent;
using ArtesArcanas.Server.Core.Legacy;

namespace ArtesArcanas.Server.Core.World;

public sealed class LegacyPlayerContext
{
    private readonly ConcurrentQueue<LegacyPlayerAction> _pendingActions = new();

    public LegacyPlayerContext(ushort code, string login, string avatarName, LegacyUserState state, LegacyPlayerSnapshot snapshot)
    {
        Code = code;
        Login = login;
        AvatarName = avatarName;
        UserState = state;
        Snapshot = snapshot;
        LastActivityUtc = DateTime.UtcNow;
    }

    public ushort Code { get; }
    public string Login { get; }
    public string AvatarName { get; }
    public LegacyUserState UserState { get; }
    public LegacyPlayerSnapshot Snapshot { get; private set; }
    public DateTime LastActivityUtc { get; private set; }
    public LegacyPlayerActionType? LastActionType { get; private set; }
    public ushort? LastActionTarget { get; private set; }
    public DateTime LastActionUtc { get; private set; }
    public bool KeepAliveWarningSent { get; private set; }
    private bool _inspectingBag;
    private byte _bagMap;
    private byte _bagX;
    private byte _bagY;

    public void UpdateSnapshot(LegacyPlayerSnapshot snapshot)
    {
        Snapshot = snapshot;
        LastActivityUtc = DateTime.UtcNow;
        KeepAliveWarningSent = false;
    }

    public void EnqueueAction(LegacyPlayerAction action)
    {
        _pendingActions.Enqueue(action);
        LastActivityUtc = DateTime.UtcNow;
        KeepAliveWarningSent = false;
    }

    public bool TryDequeueAction(out LegacyPlayerAction action) =>
        _pendingActions.TryDequeue(out action);

    public void ClearActions()
    {
        while (_pendingActions.TryDequeue(out _))
        {
        }
    }

    public LegacyPlayerAction[] SnapshotPendingActions() => _pendingActions.ToArray();

    public bool RecordAction(LegacyPlayerAction action)
    {
        var changed = LastActionType != action.Type || LastActionTarget != action.PrimaryWord;
        LastActionType = action.Type;
        LastActionTarget = action.PrimaryWord;
        LastActionUtc = DateTime.UtcNow;
        LastActivityUtc = LastActionUtc;
        KeepAliveWarningSent = false;
        return changed;
    }

    public void MarkKeepAliveWarningSent() => KeepAliveWarningSent = true;

    public void BeginInspectingBag(byte mapId, byte x, byte y)
    {
        _inspectingBag = true;
        _bagMap = mapId;
        _bagX = x;
        _bagY = y;
    }

    public void EndInspectingBag() => _inspectingBag = false;

    public bool IsInspectingBag => _inspectingBag;

    public bool IsInspectingTile(byte mapId, byte x, byte y) =>
        _inspectingBag && _bagMap == mapId && _bagX == x && _bagY == y;

    public bool TryGetInspectedBag(out byte mapId, out byte x, out byte y)
    {
        mapId = _bagMap;
        x = _bagX;
        y = _bagY;
        return _inspectingBag;
    }
}
