using System.Collections.Generic;

#region Data Listener Contracts

public interface IGameProgressDataListener
{
    void updateGameProgressData(GameManagerExpData data);
}

public interface IGameModeDataListener
{
    void updateGameModeData(GameModeType gameMode);
}

public interface IPartyDataListener
{
    void updatePartyData(IReadOnlyList<Brave> braveParty, IReadOnlyList<Enemy> enemyParty);
}

public interface IUnitSelectionListener
{
    void updateUnitSelectionData(Unit beforeUnit, Unit selectedUnit);
}

public interface ICombatTurnDataListener
{
    void updateCombatTurnData(int turnCounter, CombatTurn combatTurn, int combatCount);
}

#endregion

#region Composite Listener Contract

public interface IGameDataListener :
    IGameProgressDataListener,
    IGameModeDataListener,
    IPartyDataListener,
    IUnitSelectionListener,
    ICombatTurnDataListener
{
}

#endregion
