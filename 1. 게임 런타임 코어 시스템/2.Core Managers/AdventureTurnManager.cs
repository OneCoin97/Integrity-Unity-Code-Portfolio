using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class AdventureTurnManager
{
    private static AdventureTurnManager instance;

    public static AdventureTurnManager GetInst => instance ??= new AdventureTurnManager();

    public AdventureTurn currentTurn { get; private set; } = AdventureTurn.Move;

    private AdventureTurnData adventureData = new AdventureTurnData();
    private SaverForData<AdventureTurnData> saverForData;
    private Dictionary<AdventureTurn, GameMode<AdventureTurn>> gameModes;
    private CancellationTokenSource adventureModeCancellation;

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance?.stopAdventureMode();
        instance = null;
    }

    private AdventureTurnManager()
    {
        instance = this;
        GameManager.GetInst.addFunction(GMEventType.Retry, cancellationToken => GameManager.GetInst.invoke(GMEventType.Adventure, cancellationToken), 5, 5);
        GameManager.GetInst.addFunction(GMEventType.Adventure, prepareAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, enterAdventureMode, GMEventPhase.After);
        GameManager.GetInst.addFunction(GMEventType.Combat, stopAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Title, stopAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.NextStage, stopAdventureMode, -1000, 0);
        GameManager.GetInst.addFunction(GMEventType.StartNewGame, resetAdventureData);

        saverForData = new SaverForData<AdventureTurnData>(adventureData);
        saverForData.initializeSaver("AdventureTurn", false);
        saverForData.setOrder(4, 41);
        saverForData.setDelegate(SaverHookType.AfterLoad, afterLoad);

        gameModes = new Dictionary<AdventureTurn, GameMode<AdventureTurn>>
        {
            { AdventureTurn.Move, new AdventureMode.Move(changeAdventureMode) },
            { AdventureTurn.Skill, new AdventureMode.Skill(changeAdventureMode) },
            { AdventureTurn.Load, new AdventureMode.Load(changeAdventureMode) }
        };

        foreach (KeyValuePair<AdventureTurn, GameMode<AdventureTurn>> mode in gameModes)
        {
            mode.Value.connectEvent(GameManager.GetInst.getAdventureModeEvent(mode.Key));
        }
    }

    #endregion

    #region External API

    private void changeAdventureMode(AdventureTurn adventureTurn)
    {
        currentTurn = adventureTurn;
        adventureData.adventureTurn = currentTurn;
        saverForData.save();
    }

    #endregion

    #region Internal Logic

    private void afterLoad()
    {
        adventureData = saverForData.data;
        currentTurn = adventureData.adventureTurn;
    }

    private void prepareAdventureMode()
    {
        stopAdventureMode();
        changeAdventureMode(AdventureTurn.Load);
    }

    private void enterAdventureMode()
    {
        adventureModeCancellation = new CancellationTokenSource();
        _ = playAdventureMode(adventureModeCancellation.Token);
    }

    private void stopAdventureMode()
    {
        if (adventureModeCancellation == null) return;
        adventureModeCancellation.Cancel();
        adventureModeCancellation.Dispose();
        adventureModeCancellation = null;
    }

    private async Awaitable playAdventureMode(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!gameModes.TryGetValue(currentTurn, out GameMode<AdventureTurn> activeMode))
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                    continue;
                }

                await activeMode.update(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void resetAdventureData()
    {
        stopAdventureMode();
        currentTurn = AdventureTurn.Move;
        adventureData.adventureTurn = currentTurn;
        saverForData.save();
    }

    #endregion
}

#region Data Models

[Serializable]
public sealed class AdventureTurnData
{
    public AdventureTurn adventureTurn = AdventureTurn.Move;
}

#endregion
