using System;
using System.Threading;
using UnityEngine;

public sealed class GameDataManager
{
    private static GameDataManager instance;

    public static GameDataManager GetInst => instance ??= new GameDataManager();

    private event Action<GameManagerExpData> progressDataChanged;
    private event Action<GameModeType> gameModeChanged;

    private GameManagerExpData progressData = new GameManagerExpData();
    private GameManagerData stateData = new GameManagerData();
    private readonly SaverForData<GameManagerExpData> saverForData;
    private readonly SaverForData<GameManagerData> stateSaver;

    public GameManagerExpData data => new GameManagerExpData(progressData);
    public GameModeType currentMode => stateData.cModeType;

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance = null;
    }

    private GameDataManager()
    {
        instance = this;

        saverForData = new SaverForData<GameManagerExpData>(progressData);
        saverForData.initializeSaver("GameMangerExp", true);
        saverForData.setOrder(0, 0);
        saverForData.setDelegate(SaverHookType.AfterLoad, afterProgressLoad);

        stateSaver = new SaverForData<GameManagerData>(stateData);
        stateSaver.initializeSaver("GameManager", false);
        stateSaver.setOrder(5);
        stateSaver.setDelegateLoadAwaitable(true, afterStateLoad);

        GameManager.GetInst.addFunction(GMEventType.StartEnding, onStartEnding, 0, 0);
        GameManager.GetInst.addFunction(GMEventType.StopEnding, onStopEnding, 0, 0);
        GameManager.GetInst.addFunction(GMEventType.StartNewGame, startNewgame);
        GameManager.GetInst.addFunction(GMEventType.Combat, enterCombatMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, enterAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Title, enterTitleMode, GMEventPhase.Before);
        saverForData.loadImmediate();
    }

    #endregion

    #region External API

    public void subscribeGameProgressData(IGameProgressDataListener listener)
    {
        if (listener == null) return;
        progressDataChanged += listener.updateGameProgressData;
        listener.updateGameProgressData(new GameManagerExpData(progressData));
    }

    public void unsubscribeGameProgressData(IGameProgressDataListener listener)
    {
        if (listener != null) progressDataChanged -= listener.updateGameProgressData;
    }

    public void subscribeGameModeData(IGameModeDataListener listener)
    {
        if (listener == null) return;
        gameModeChanged += listener.updateGameModeData;
        listener.updateGameModeData(currentMode);
    }

    public void unsubscribeGameModeData(IGameModeDataListener listener)
    {
        if (listener != null) gameModeChanged -= listener.updateGameModeData;
    }

    public void advanceStage()
    {
        progressData.stage++;
        progressData.maxStage = Mathf.Max(progressData.maxStage, progressData.stage);
        saveAndPublish();
    }

    private void setGameMode(GameModeType mode)
    {
        stateData.cModeType = mode;
        gameModeChanged?.Invoke(mode);
    }

    public void saveState()
    {
        stateSaver.save();
    }

    #endregion

    #region Internal Logic

    private void startNewgame()
    {
        progressData.stage = 1;
        stateData.endStage = false;
        stateSaver.save();
        saveAndPublish();
    }

    private void onStartEnding()
    {
        stateData.endStage = true;
        stateSaver.save();

        if (progressData.viewEnding) return;

        progressData.viewEnding = true;
        saveAndPublish();
    }

    private void onStopEnding()
    {
        stateData.endStage = false;
        stateSaver.save();
    }

    private void afterProgressLoad()
    {
        progressData = saverForData.data;
        publish();
    }

    private async Awaitable afterStateLoad(CancellationToken cancellationToken)
    {
        stateData = stateSaver.data;
        gameModeChanged?.Invoke(currentMode);
        await resumeAfterLoad(stateData.cModeType, stateData.endStage, cancellationToken);
    }

    private async Awaitable resumeAfterLoad(GameModeType gameModeType, bool endStage, CancellationToken cancellationToken)
    {
        if (gameModeType == GameModeType.Combat)
        {
            await GameManager.GetInst.invoke(GMEventType.Combat, cancellationToken);
            return;
        }

        await GameManager.GetInst.invoke(GMEventType.Adventure, cancellationToken);
        if (endStage) await GameManager.GetInst.invoke(GMEventType.ResumeEnding, cancellationToken);
    }

    private void enterCombatMode()
    {
        setGameMode(GameModeType.Combat);
    }

    private void enterAdventureMode()
    {
        setGameMode(GameModeType.Adventure);
    }

    private void enterTitleMode()
    {
        setGameMode(GameModeType.Title);
    }

    private void saveAndPublish()
    {
        saverForData.save();
        publish();
    }

    private void publish()
    {
        progressDataChanged?.Invoke(new GameManagerExpData(progressData));
    }

    #endregion
}

#region Data Models

[Serializable]
public sealed class GameManagerExpData
{
    public int stage = 1;
    public int maxStage;
    public bool viewEnding;

    public GameManagerExpData()
    {
    }

    public GameManagerExpData(GameManagerExpData source)
    {
        stage = source.stage;
        maxStage = source.maxStage;
        viewEnding = source.viewEnding;
    }
}

[Serializable]
public sealed class GameManagerData
{
    public bool endStage;
    public GameModeType cModeType = GameModeType.None;
}

#endregion
