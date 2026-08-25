using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class SaverManager
{
    public class WriteStage
    {
        private readonly SaverManager owner;
        private readonly SaveDataPool saveDataPool;

        private readonly Dictionary<string, SaveData> writeDatas = new Dictionary<string, SaveData>();
        private readonly object writeLock = new object();
        private readonly object startLock = new object();

        private readonly SemaphoreSlim signal = new SemaphoreSlim(0, int.MaxValue);
        private Task workerTask;
        private CancellationTokenSource workerCts;

        public WriteStage(SaverManager owner)
        {
            this.owner = owner;
            saveDataPool = owner.saveDataPool;
        }

        public void stopProcess()
        {
            lock (startLock)
            {
                if (workerCts != null)
                {
                    workerCts.Cancel();
                    workerCts.Dispose();
                    workerCts = null;
                }

                workerTask = null;
                while (signal.Wait(0)) { }
            }
        }

        public List<SaveData> drainWriteDatas()
        {
            lock (writeLock)
            {
                List<SaveData> list = new List<SaveData>(writeDatas.Values);
                writeDatas.Clear();
                return list;
            }
        }

        #region Process

        public void addWritingData(SaveData saveData)
        {
            SaveData oldSaveData = null;

            lock (writeLock)
            {
                if (writeDatas.TryGetValue(saveData.fullPath, out SaveData exist))
                {
                    if (exist != saveData)
                    {
                        oldSaveData = exist;
                    }
                }

                writeDatas[saveData.fullPath] = saveData;

                // 워커가 없으면 시작(딱 1번만)
                ensureWorkerStarted();
            }

            saveDataPool.returnSaveData(oldSaveData);

            // 할 일 생겼다는 신호
            signal.Release();
        }

        private void ensureWorkerStarted()
        {
            lock (startLock)
            {
                if (workerTask != null && !workerTask.IsCompleted)
                    return;

                if (workerCts != null)
                    workerCts.Dispose();

                workerCts = new CancellationTokenSource();
                workerTask = Task.Run(delegate { return writeWorkerLoop(workerCts.Token); }, workerCts.Token);
                owner.observeTask(workerTask);
            }
        }

        private async Task writeWorkerLoop(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    // 신호 올 때까지 대기 (작업 없으면 여기서 “멈춰”있음)
                    await signal.WaitAsync(token);

                    int delayMs = (int)(owner.saveCycle * 1000f);
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, token);
                    }

                    while (signal.Wait(0))
                    {
                    }

                    List<SaveData> batch = drainWriteDatas();
                    if (batch.Count > 0)
                        await saveBatchAsync(batch);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private async Task saveBatchAsync(List<SaveData> batch)
        {
            List<Exception> failures = null;

            for (int i = 0; i < batch.Count; i++)
            {
                SaveData item = batch[i];
                try
                {
                    await item.save();
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
                finally
                {
                    saveDataPool.returnSaveData(item);
                }
            }

            if (failures == null)
                return;

            Debug.LogException(new AggregateException("파일 저장 배치에서 오류가 발생했습니다.", failures));
        }

        #endregion

    }
}
