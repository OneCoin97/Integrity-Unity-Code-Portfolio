using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public partial class SaverManager
{
    public class SaveData
    {
        private string serializedData;
        public string fullPath { get; private set; }

        public void initialize()
        {
            serializedData = null;
            fullPath = null;
        }

        public void setData(string fullPath, string serializedData)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("저장할 파일 경로는 비어 있을 수 없습니다.", nameof(fullPath));

            this.fullPath = fullPath;
            this.serializedData = serializedData;
        }

        public async Task save()
        {
            await atomicSaveInternal();
        }

        private async Task atomicSaveInternal()
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new InvalidOperationException("저장할 파일 경로가 설정되지 않았습니다.");

            string directoryPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new InvalidOperationException($"저장할 디렉터리 경로를 확인할 수 없습니다. 경로: {fullPath}");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string finalPath = fullPath;

            if (serializedData == null)
                throw new InvalidOperationException("직렬화 데이터가 설정되지 않았습니다.");

            byte[] bytes = Encoding.UTF8.GetBytes(serializedData);

            // 같은 디렉터리에 temp 파일을 만들어야 "교체/이동"이 원자적으로 동작할 가능성이 큼
            string tempPath = finalPath + ".tmp_" + Guid.NewGuid().ToString("N");

            try
            {
                using (FileStream fs = new FileStream(
                           tempPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           1024 * 1024,
                           FileOptions.Asynchronous))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                    await fs.FlushAsync();
                }

                // 여기서부터 "최종 파일 교체" 단계
                replaceAtomicBestEffort(tempPath, finalPath);
            }
            catch (Exception exception)
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        $"저장과 임시 파일 정리에 모두 실패했습니다. 경로: {finalPath}",
                        exception,
                        cleanupException);
                }

                throw new IOException($"파일 저장에 실패했습니다. 경로: {finalPath}", exception);
            }
        }

        private void replaceAtomicBestEffort(string tempPath, string finalPath)
        {
            // 기존 파일이 없으면 그냥 move
            if (!File.Exists(finalPath))
            {
                File.Move(tempPath, finalPath);
                return;
            }

            // 1순위: File.Replace (지원되면 가장 깔끔하게 교체)
            // - 백업 파일이 필요해서 .bak을 잠깐 만들었다가 성공하면 삭제
            string backupPath = finalPath + ".bak";

            try
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                File.Replace(tempPath, finalPath, backupPath);
            }
            catch (Exception replaceException)
            {
                // 2순위 폴백: 기존 삭제 후 move
                // - 완전한 "원자적" 교체는 깨질 수 있지만, 최소한 temp -> final로 마무리
                try
                {
                    if (!File.Exists(tempPath))
                        throw new IOException("파일 교체 실패 후 임시 파일을 찾을 수 없습니다.");

                    if (File.Exists(finalPath))
                        File.Delete(finalPath);

                    File.Move(tempPath, finalPath);
                    return;
                }
                catch (Exception fallbackException)
                {
                    throw new AggregateException(
                        $"파일 교체와 fallback에 모두 실패했습니다. 경로: {finalPath}",
                        replaceException,
                        fallbackException);
                }
            }

            // File.Replace는 이미 성공했다. 백업 정리 실패가 fallback으로 이어지면 안 된다.
            try
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
            }
            catch (Exception cleanupException)
            {
                Debug.LogWarning($"저장 파일 교체는 성공했지만 백업 파일 정리에 실패했습니다. 경로: {backupPath}\n{cleanupException}");
            }
        }
    }
    
    
}
