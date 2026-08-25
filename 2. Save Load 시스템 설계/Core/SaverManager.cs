using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class SaverManager
{
    public static SaverManager GetInst
    {
        get
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new SaverManager();
                    }
                }
            }

            return instance;
        }
    }

    private static SaverManager instance;
    private static readonly object instanceLock = new object();

    private readonly SaveDataPool saveDataPool = new SaveDataPool();
    private readonly SerializeStage serializeStage;
    private readonly WriteStage writeStage;

    private List<Saver> savers = new List<Saver>();
    private HashSet<string> saverPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    public float saveCycle = 0.5f;

    private string name = "Save0";
    private string permPath = "perm";
    private string tempPath = "temp";
    private string path;

    private bool isSort; // 로드/세이브 순서 정렬 여부
    private volatile bool saveRequestLock;
    
    
    private SaverManager()
    {
        serializeStage = new SerializeStage(this);
        writeStage = new WriteStage(this);
        setSaveName(name);
    }

    public bool addSaveData(Saver saver)
    {
        if (saver == null)
            throw new ArgumentNullException(nameof(saver));

        if (saveRequestLock)
            return false;

        serializeStage.addSaveData(saver);
        return true;
    }

    public string addSaver(Saver saver, string path, bool isPerm)
    {
        if (saver == null)
            throw new ArgumentNullException(nameof(saver));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("저장 경로는 비어 있을 수 없습니다.", nameof(path));

        string fullPath = Path.Combine(getPath(isPerm), $"{path}.txt");

        if (!saverPaths.Add(fullPath))
            throw new InvalidOperationException($"이미 등록된 저장 경로입니다. 경로: {fullPath}");

        savers.Add(saver);
        isSort = false;
        return fullPath;
    }

    public void removeSaver(Saver saver)
    {
        if (saver == null)
            return;

        int removedCount = savers.RemoveAll(registeredSaver => ReferenceEquals(registeredSaver, saver));
        if (removedCount > 0)
            saverPaths.Remove(saver.fullPath);
    }

    public void setSaveName(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            throw new ArgumentException("세이브 슬롯 이름은 비어 있을 수 없습니다.", nameof(saveName));

        name = saveName;
        path = Path.Combine(Application.persistentDataPath, saveName);
    }

    public void setSavePaths(string permPath, string tempPath)
    {
        if (string.IsNullOrWhiteSpace(permPath))
            throw new ArgumentException("영구 저장 경로는 비어 있을 수 없습니다.", nameof(permPath));

        if (string.IsNullOrWhiteSpace(tempPath))
            throw new ArgumentException("임시 저장 경로는 비어 있을 수 없습니다.", nameof(tempPath));

        this.permPath = permPath;
        this.tempPath = tempPath;
    }

    public string getPath(bool isPerm)
    {
        if (isPerm)
        {
            return Path.Combine(path, permPath);
        }

        return Path.Combine(path, tempPath);
    }

    #region Utility
    public async Awaitable loadAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!isSort)
            sortSaversByLoadOrder(savers);

        List<Saver> localSavers = new List<Saver>(savers);

        for (int i = 0; i < localSavers.Count; i++)
        {
            Saver saver = localSavers[i];
            await saver.loadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public async Awaitable saveAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!isSort)
            sortSaversByLoadOrder(savers);

        List<Saver> localSavers = new List<Saver>(savers);
        beginSaveBatch();
        try
        {
            for (int i = 0; i < localSavers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Saver saver = localSavers[i];
                try
                {
                    saver.save();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
        finally
        {
            endSaveBatch();
        }

        while (serializeStage.isProcessing || !saveDataPool.isRunningSaveDataZero)
            await Awaitable.NextFrameAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
    }
    
    public void offSaveLock()
    {
        saveRequestLock = false;
    }
    #endregion

    #region etc
    
    private void sortSaversByLoadOrder(List<Saver> savers)
    {
        savers.Sort();
        isSort = true;
    }

    private void beginSaveBatch()
    {
        saveRequestLock = false;
    }

    private void endSaveBatch()
    {
        saveRequestLock = true;
    }

    private void addWritingData(SaveData saveData)
    {
        writeStage.addWritingData(saveData);
    }

    private void observeTask(Task task)
    {
        if (task == null)
            return;

        task.ContinueWith(observeTaskFaulted, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void observeTaskFaulted(Task task)
    {
        if (task?.Exception != null)
            Debug.LogException(task.Exception);
    }

    #endregion
}
