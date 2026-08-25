using System;
using System.Threading;
using UnityEngine;

[Serializable]
public class SaveLoadExampleData
{
    public int stage;
    public Vector3 position;
}

public class SaveLoadUsageExample : MonoBehaviour
{
    private readonly SaverForData<SaveLoadExampleData> saverForData =
        new SaverForData<SaveLoadExampleData>(new SaveLoadExampleData());
    private bool initialized;

    public SaveLoadExampleData currentData { get; private set; }

    private void Start()
    {
        saverForData.initializeSaver("Player", false);
        saverForData.setOrder(0, 10);
        saverForData.setDelegate(SaverHookType.AfterLoad, ApplyLoadedData);
        currentData = saverForData.data;
        initialized = true;
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            saverForData.removeSaver();
        }
    }

    public void Save(int stage, Vector3 position)
    {
        saverForData.data.stage = stage;
        saverForData.data.position = position;
        saverForData.save();
    }

    public Awaitable LoadAsync(CancellationToken cancellationToken = default)
    {
        return saverForData.loadAsync(cancellationToken);
    }

    public async Awaitable SaveAllAndWait(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaverManager.GetInst.saveAll(cancellationToken);
        }
        finally
        {
            SaverManager.GetInst.offSaveLock();
        }
    }

    private void ApplyLoadedData()
    {
        currentData = saverForData.data;
    }
}
