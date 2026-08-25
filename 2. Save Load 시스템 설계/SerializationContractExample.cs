using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortfolioSaveLoad.SerializationExtensionExample
{
    /*
     * 현재 구조에서 직렬화 작업을 백그라운드 스레드로 옮길 때
     * 변경할 지점만 요약한 포트폴리오용 예시입니다.
     *
     * 현재 프로젝트는 규모와 유지보수 비용을 고려해 JsonUtility를 유지합니다.
     * 아래 방식은 직렬화가 실제 병목이 되었을 때 적용할 수 있는 확장 방향입니다.
     */

    public interface ISaveSerializable
    {
        string serialize();
        void deserialize(string serializedData);
    }

    /// <summary>
    /// 현재 SaverForData에서 JsonUtility를 제거할 때의 핵심 변경 예시입니다.
    /// </summary>
    public sealed class SaverForData<T> where T : ISaveSerializable, new()
    {
        public T data { get; private set; }

        public SaverForData(T data)
        {
            if (ReferenceEquals(data, null))
                throw new ArgumentNullException(nameof(data));

            this.data = data;
        }

        public string serialize()
        {
            // JsonUtility.ToJson(...) 대신 데이터 구현에 직렬화를 위임한다.
            return data.serialize();
        }

        public void deserialize(string serializedData)
        {
            // JsonUtility.FromJson<T>(...) 대신 데이터 구현에 역직렬화를 위임한다.
            T loadedData = new T();
            loadedData.deserialize(serializedData);
            data = loadedData;
        }
    }

    /// <summary>
    /// ISaveSerializable 적용 후 SerializeStage에서 변경할 워커 시작 부분입니다.
    /// </summary>
    public static class SerializeStageThreadingExample
    {
        public static Task startOnBackgroundThread(
            Func<CancellationToken, Task> serializeProcessQueue,
            CancellationToken cancellationToken)
        {
            if (serializeProcessQueue == null)
                throw new ArgumentNullException(nameof(serializeProcessQueue));

            cancellationToken.ThrowIfCancellationRequested();

            // 현재 SerializeStage:
            // workerTask = serializeProcessQueue(cancellationToken);

            // 백그라운드 직렬화로 전환할 때:
            return Task.Run(
                () => serializeProcessQueue(cancellationToken),
                cancellationToken);
        }
    }

    /// <summary>
    /// Unity API를 사용하지 않는 직렬화 계약 구현 예시입니다.
    /// 실제 백그라운드 직렬화 시에는 직렬화 중 데이터가 변경되지 않도록
    /// 불변 스냅샷 또는 별도의 동기화가 필요합니다.
    /// </summary>
    public sealed class PlayerProgressData : ISaveSerializable
    {
        public int stage;
        public float health;

        public string serialize()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true);

            writer.Write(stage);
            writer.Write(health);
            writer.Flush();

            return Convert.ToBase64String(stream.ToArray());
        }

        public void deserialize(string serializedData)
        {
            if (serializedData == null)
                throw new ArgumentNullException(nameof(serializedData));

            byte[] bytes = Convert.FromBase64String(serializedData);
            using MemoryStream stream = new MemoryStream(bytes, false);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false);

            stage = reader.ReadInt32();
            health = reader.ReadSingle();
        }
    }
}
