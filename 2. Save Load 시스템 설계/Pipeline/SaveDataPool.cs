using System.Collections.Generic;

public partial class SaverManager 
{
    private class SaveDataPool
    {
        private readonly object saveDataPoolLock = new object();
        private Queue<SaveData> saveDataPool = new();
        private int runningSaveData = 0;

        public bool isRunningSaveDataZero
        {
            get
            {
                lock (saveDataPoolLock)
                {
                    return runningSaveData == 0;
                }
            }
        }

        public SaveData getNewSaveData(string fullPath, string serializedData)
        {
            SaveData result;

            lock (saveDataPoolLock)
            {
                runningSaveData++;
                if (!saveDataPool.TryDequeue(out result))
                {
                    result = new SaveData();
                }
            }

            result.setData(fullPath, serializedData);
            return result;
        }

        public void returnSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.initialize();

            lock (saveDataPoolLock)
            {
                runningSaveData--;
                saveDataPool.Enqueue(saveData);
            }
        }
    }

}
