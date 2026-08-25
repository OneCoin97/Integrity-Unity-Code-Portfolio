using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnitComponents;
using UnityEngine;

public sealed class PartyManager
{
    private static PartyManager instance;

    public static PartyManager GetInst => instance ??= new PartyManager();
    
    public event Action<Unit> unitDieEvent;
    private event Action<IReadOnlyList<Brave>, IReadOnlyList<Enemy>> partyDataChanged;
    
    private readonly List<Brave> braves = new List<Brave>();
    private readonly List<Enemy> enemies = new List<Enemy>();
    public IReadOnlyList<Brave> braveParty => braves;
    public IReadOnlyList<Enemy> enemyParty => enemies;

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance = null;
    }

    private PartyManager()
    {
        GameManager.GetInst.addFunction(GMEventType.Retry, clearEnemiesForRetry, 0, -1);
        GameManager.GetInst.addFunction(GMEventType.Combat, lockBraveMovement, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Combat, unlockBraveMovement, GMEventPhase.After);
        GameManager.GetInst.addFunction(GMEventType.Adventure, prepareAdventureMovement, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, resetAdventureBraves, GMEventPhase.After);
    }

    #endregion

    #region External API

    public void subscribePartyData(IPartyDataListener listener)
    {
        if (listener == null) return;
        partyDataChanged += listener.updatePartyData;
        listener.updatePartyData(braveParty, enemyParty);
    }

    public void unsubscribePartyData(IPartyDataListener listener)
    {
        if (listener != null) partyDataChanged -= listener.updatePartyData;
    }

    public void addBrave(Brave brave)
    {
        braves.Add(brave);
        publishPartyData();
    }

    public void addEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
        publishPartyData();
    }

    public void deathUnit(Unit unit)
    {
        if (unit is Brave brave)
        {
            braves.Remove(brave);
        }
        else if (unit is Enemy enemy)
        {
            enemies.Remove(enemy);
        }

        publishPartyData();

        unitDieEvent?.Invoke(unit);
    }

    public Unit getUnit(string unitName)
    {
        foreach (Brave brave in braves)
        {
            if (brave.unitIdentity.name.Equals(unitName))
                return brave;
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy.unitIdentity.name.Equals(unitName))
                return enemy;
        }

        return null;
    }

    public List<Unit> getUnits(List<string> names)
    {
        List<Unit> result = new List<Unit>();
        foreach (string unitName in names)
        {
            Unit unit = getUnit(unitName);
            if (unit != null)
            {
                result.Add(unit);
            }
        }

        return result;
    }

    public List<Unit> getBraves()
    {
        return new List<Unit>(braves);
    }

    public List<Unit> getEnemies()
    {
        return new List<Unit>(enemies);
    }

    public List<Enemy> getEnemyParty()
    {
        return new List<Enemy>(enemies);
    }

    public IEnumerator destroyAllUnits()
    {
        foreach (Brave brave in braves)
        {
            if (brave != null) { UnityEngine.Object.Destroy(brave.gameObject); yield return null; }
        }
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null) { UnityEngine.Object.Destroy(enemy.gameObject); yield return null; }
        }
        braves.Clear();
        enemies.Clear();
        publishPartyData();
    }

    public async Awaitable destroyAllUnitsAsync(CancellationToken cancellationToken)
    {
        foreach (Brave brave in braves)
        {
            if (brave == null)
                continue;

            UnityEngine.Object.Destroy(brave.gameObject);
            await Awaitable.NextFrameAsync(cancellationToken);
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            UnityEngine.Object.Destroy(enemy.gameObject);
            await Awaitable.NextFrameAsync(cancellationToken);
        }

        braves.Clear();
        enemies.Clear();
        publishPartyData();
    }

    #endregion

    #region Internal Logic

    private void clearEnemiesForRetry()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null) UnityEngine.Object.Destroy(enemy.gameObject);
        }
        enemies.Clear();
        publishPartyData();
    }

    private void lockBraveMovement()
    {
        foreach (Brave brave in braves) brave.unitRuntimeData.moveLock = true;
    }

    private void unlockBraveMovement()
    {
        foreach (Brave brave in braves) brave.unitRuntimeData.moveLock = false;
    }

    private void prepareAdventureMovement()
    {
        lockBraveMovement();
        GameManager.GetInst.addFunction(AdventureTurn.Move, unlockBraveMovement, true, 0, 2, true);
    }

    private void resetAdventureBraves()
    {
        foreach (Brave brave in braves)
        {
            if (brave.tryGetUnitComponent(out AnimationManager animationManager)) animationManager.fall = false;
            brave.unitUtility.rigidbody.linearVelocity = Vector3.zero;
        }
    }
    
    private void publishPartyData()
    {
        partyDataChanged?.Invoke(braveParty, enemyParty);
    }

    #endregion
}
