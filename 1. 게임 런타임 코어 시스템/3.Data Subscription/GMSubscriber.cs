using System;
using System.Collections.Generic;
using UnityEngine;


public abstract class GMSubscriber : MonoBehaviour, IGameDataListener
{
    public IReadOnlyList<Brave> Braveparty { get; private set;}
    public IReadOnlyList<Enemy> Enemyparty { get; private set;}
    public Unit selectedUnit { get; private set;}
    
    public Unit beforeUnit  {get; private set;}
    public GameModeType gameModeType { get; private set; }
    public CombatTurn combatTurn { get; private set;}
    public int turnCounter { get; private set;}
    public int combatCount { get; private set;}
    public GameManager gameManager { get; private set;}
    public GameManagerExpData gameManagerExpData { get; private set; }

    protected virtual void Awake()
    {
        gameManager = GameManager.GetInst;
        CombatTurnManager.GetInst.subscribeCombatTurnData(this);
        PartyManager.GetInst.subscribePartyData(this);
        UnitSelectionManager.GetInst.subscribeUnitSelectionData(this);
        GameDataManager.GetInst.subscribeGameModeData(this);
        GameDataManager.GetInst.subscribeGameProgressData(this);
    }

    protected virtual void Start()
    {
        doSubscribe();
    }

    protected virtual void OnDestroy()
    {
        CombatTurnManager.GetInst.unsubscribeCombatTurnData(this);
        PartyManager.GetInst.unsubscribePartyData(this);
        UnitSelectionManager.GetInst.unsubscribeUnitSelectionData(this);
        GameDataManager.GetInst.unsubscribeGameModeData(this);
        GameDataManager.GetInst.unsubscribeGameProgressData(this);
        doUnsubscribe();
    }

    public void updateGameProgressData(GameManagerExpData data)
    {
        gameManagerExpData = data;
    }

    protected Vector3 getSelectedUnitPos()
    {
        if (selectedUnit != null)
        {
            return selectedUnit.transform.position;
        }

        return Vector3.zero;
    }

    protected bool tryGetSelectedUnitPos(out Vector3 pos)
    {
        if (selectedUnit != null)
        {
            pos = selectedUnit.transform.position;
            return true;
        }
        pos = Vector3.zero;
        return false;
    }

    public void updateGameModeData(GameModeType type)
    {
        this.gameModeType = type;
    }


    public async void updatePartyData(IReadOnlyList<Brave> Braveparty,IReadOnlyList<Enemy> Enemyparty)
    {
        this.Braveparty = Braveparty;
        this.Enemyparty = Enemyparty;
        try
        {
            await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async void updateUnitSelectionData(Unit beforeUnit, Unit selectedUnit)
    {
        this.beforeUnit = beforeUnit;
        this.selectedUnit = selectedUnit;
        try
        {
            await Awaitable.NextFrameAsync(destroyCancellationToken);
            onSelectedUnitChanged();
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async void updateCombatTurnData(int turnCounter, CombatTurn combatTurn, int combatCount)
    {
        this.turnCounter = turnCounter;
        this.combatTurn = combatTurn;
        this.combatCount = combatCount;
        try
        {
            await Awaitable.NextFrameAsync(destroyCancellationToken);
            onTurnChanged();
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected abstract void onSelectedUnitChanged();
    protected abstract void onTurnChanged();
    protected abstract void doSubscribe();
    protected abstract void doUnsubscribe();

    protected Unit getUnit(string name, bool isBrave)
    {
        if (isBrave)
            return getBrave(name);
        else
            return getEnemy(name);
    }
    
    protected Unit getUnit(string name)
    {
        Unit unit = getBrave(name);
        return unit ?? getEnemy(name);
    }
    
    
    
    protected Brave getBrave(string name)
    {
        foreach (var brave in Braveparty)
        {
            if (brave.unitIdentity.name.Equals(name))
            {
                return brave;
            }
        }
        return null;
    }
    protected Enemy getEnemy(string name)
    {
        foreach (var brave in Enemyparty)
        {
            if (brave.unitIdentity.name.Equals(name))
            {
                return brave;
            }
        }
        return null;
    }

    protected Vector3 getBraveAPosition()
    {
        Vector3 result = Vector3.zero;
        foreach (var VARIABLE in Braveparty)
        {
            if(VARIABLE != null)
                result += VARIABLE.transform.position;
        }

        return result / Braveparty.Count;
    }
}
