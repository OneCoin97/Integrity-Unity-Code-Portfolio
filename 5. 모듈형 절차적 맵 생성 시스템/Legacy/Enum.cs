using System;

public enum TriggerAreaType
{
    Null,
    Camera,
    Room,
    Bridge,
    CreateLocker,
    Activator,
    Dialogue,
    Recovery,
    Popup,
    BGM,
}

public enum CameraMode
{
    Nothing,
    Adventure_Move,
    Adventure_Move_A,
    Adventure_Move_B,
    Adventure_Move_C,
    Combat_Move,
    Combat_Skill,
    Combat_Enemy_A,
    Combat_Enemy_B,
    Combat_Enemy_C,
    Title_A,
    Combat_Enemy_CT,
    Ending,
}

[Flags]
public enum TriggerActivatorType
{
    Null        = 0,        // 아무 것도 없음
    Skill       = 1 << 0,   // 1
    BlockAction = 1 << 1,   // 2
    Stay        = 1 << 2,   // 4
    Interaction = 1 << 3,   // 8
}

[System.Flags]
public enum GameModeType
{
    None = 0, // 플래그 없음
    Adventure = 1 << 0, // 1
    Combat = 1 << 1, // 2
    Title = 1 << 2, // 4
}

public enum DialoguePlace
{
    Null,
    Start,
    Tutorial,
    RestRoom,
    SURoom,
    SkillUpgradeStart,
    SkillUpgradeEnd,
    Enemy,
    Ending
}

public enum SFXTriggerType
{
    Null,
    BGM,
    SubBGM,
    Stinger,
}

public enum RoomType
{
    Null,
    Combat,
    RestRoom,
    SURoom
}
    
[Flags]
public enum RoomCombatType
{
    Null    = 0,
    기본    = 1 << 0, // 1
    낙사  = 1 << 1, // 2
    어려움   = 1 << 2, // 4
    미니보스   = 1 << 3, // 8
    보스   = 1 << 4, // 16
    보스낙사   = 1 << 5, // 32
    Demo5   = 1 << 6, // 32
}