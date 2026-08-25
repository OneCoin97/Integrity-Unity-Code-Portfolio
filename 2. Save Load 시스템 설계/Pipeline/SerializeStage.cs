using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class SaverManager
{
    public class SerializeStage
    {
        private SaverManager owner;
        private SaveDataPool saveDataPool;

        private HashSet<Saver> serializeWaitSavers = new();
        private Queue<Saver> serializeQueue = new();
        private int serializeProcessingCount;

        private readonly object serializeLock = new object();
        private readonly object startLock = new object();

        private SemaphoreSlim signal = new SemaphoreSlim(0, int.MaxValue);

        private CancellationTokenSource serializeCts;
        private Task workerTask;
        

        public SerializeStage(SaverManager owner)
        {
            this.owner = owner;
            saveDataPool = owner.saveDataPool;
        }
        
        public bool isProcessing
        {
            get
            {
                lock (serializeLock)
                {
                    return serializeQueue.Count > 0 || serializeWaitSavers.Count > 0 ||
                           serializeProcessingCount > 0;
                }
            }
        }

        public void stopProcess()
        {
            lock (startLock)
            {
                if (serializeCts != null)
                {
                    serializeCts.Cancel();
                    serializeCts.Dispose();
                    serializeCts = null;
                }

                workerTask = null;

                // 쌓인 신호(표) 정리
                while (signal.Wait(0)) { }
            }
        }
        

        #region Process

        public void addSaveData(Saver saver)
        {
            lock (serializeLock)
            {
                if (!serializeWaitSavers.Add(saver))
                    return;

                serializeQueue.Enqueue(saver);
            }

            startSerializeProcessIfNeeded();
            signal.Release();
        }

        private void startSerializeProcessIfNeeded()
        {
            lock (startLock)
            {
                if (workerTask != null && !workerTask.IsCompleted)
                {
                    return;
                }

                if (serializeCts != null)
                {
                    serializeCts.Dispose();
                }

                serializeCts = new CancellationTokenSource();

                // JsonUtility는 메인 스레드 전제라 Task.Run으로 워커를 띄우지 않음
                workerTask = serializeProcessQueue(serializeCts.Token);
                owner.observeTask(workerTask);
            }
        }

        private async Task serializeProcessQueue(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    await signal.WaitAsync(token);
                    while (signal.Wait(0)) { }
                    while (true)
                    {
                        Saver saver;

                        lock (serializeLock)
                        {
                            if (!serializeQueue.TryDequeue(out saver))
                            {
                                break;
                            }

                            serializeWaitSavers.Remove(saver);
                            serializeProcessingCount++;
                        }

                        try
                        {
                            serializeSaveData(saver);
                        }
                        finally
                        {
                            lock (serializeLock)
                            {
                                serializeProcessingCount--;
                            }
                        }

                        await Task.Yield();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void serializeSaveData(Saver saver)
        {
            SaveData saveData = null;

            try
            {
                saveData = saveDataPool.getNewSaveData(saver.fullPath, saver.serialize());
                owner.addWritingData(saveData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving data: {ex.Message}");
                saveDataPool.returnSaveData(saveData);
            }
        }

        #endregion
    }
}
