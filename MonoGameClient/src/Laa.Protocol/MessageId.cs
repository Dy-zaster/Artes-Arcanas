namespace Laa.Protocol;

public enum MessageId : byte
{
    LoginRequest = 1,
    LoginResponse = 2,
    CreateAccountRequest = 3,
    CreateAccountResponse = 4,
    LogoutRequest = 5,
    KeepAlive = 6,
    CharacterListRequest = 10,
    CharacterListResponse = 11,
    CharacterCreateRequest = 12,
    CharacterCreateResponse = 13,
    EnterWorldRequest = 14,
    EnterWorldResponse = 15,

    MapRequest = 20,
    MapChunk = 21,
    MapReady = 22,

    EntitySnapshot = 30,
    EntityUpdate = 31,
    EntityRemove = 32,

    ChatMessage = 40,
    SystemMessage = 41,

    InventorySnapshot = 50,
    InventoryUpdate = 51,
    MerchantOffer = 52,

    CombatEvent = 60,

    ErrorResponse = 200,
    Ping = 254,
    Pong = 255
}
