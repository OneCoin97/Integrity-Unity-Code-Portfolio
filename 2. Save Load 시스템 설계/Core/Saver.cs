using System;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// SaverManager에 등록되어 저장·로드 순서와 파일 경로를 관리받는 저장 객체입니다.
/// </summary>
/// <remarks>
/// SaverManager는 등록된 Saver를 계속 참조합니다.
/// 소유 객체가 파괴되거나 씬 전환으로 수명이 끝날 수 있다면
/// 반드시 소유자의 OnDestroy()에서 removeSaver()를 호출해야 합니다.
/// </remarks>
public abstract class Saver : IComparable<Saver>
{
    public string fullPath { get; private set; }

    private int priority;
    private int sequence;

    // ✅ Saver 자체 정렬 기준
    public int CompareTo(Saver other)
    {
        if (ReferenceEquals(other, null))
            return -1; // this가 먼저(=null은 뒤로)

        if (ReferenceEquals(this, other))
            return 0;

        int priorityComparison = priority.CompareTo(other.priority);
        if (priorityComparison != 0)
            return priorityComparison;

        int sequenceComparison = sequence.CompareTo(other.sequence);
        if (sequenceComparison != 0)
            return sequenceComparison;

        // 동점이면 fullPath로만 결정(최소한의 결정적 정렬)
        string thisFullPath = fullPath;
        string otherFullPath = other.fullPath;

        return string.Compare(thisFullPath, otherFullPath, StringComparison.Ordinal);
    }

    
    public virtual void save()
    {
        addSaverData();
    }
    public abstract void load();
    public abstract void resetData();
    public abstract string serialize();

    public abstract Awaitable loadAsync(CancellationToken cancellationToken = default);

    public void deleteData()
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else
            {
                Debug.LogWarning($"삭제할 저장 파일이 없습니다: {fullPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 파일 삭제 중 오류가 발생했습니다. 경로: {fullPath}\n{e}");
        }
    }
    
    public void initializeSaver(string path, bool isPerm)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("저장 경로는 비어 있을 수 없습니다.", nameof(path));
        }

        fullPath = SaverManager.GetInst.addSaver(this, path, isPerm);
    }
    
    public void setOrder(int priority, int sequence = 10)
    {
        this.priority = priority;
        this.sequence = sequence;
    }

    /// <summary>
    /// SaverManager의 등록 목록과 경로 중복 검사 목록에서 이 Saver를 제거합니다.
    /// </summary>
    public virtual void removeSaver()
    {
        SaverManager.GetInst.removeSaver(this);
    }
    
    protected bool addSaverData()
    {
        return SaverManager.GetInst.addSaveData(this);
    }

    protected bool loadSerializedData(out string result)
    {
        result = null;

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"파일을 찾을 수 없습니다. 경로: {fullPath}");
            return false;
        }

        try
        {
            result = File.ReadAllText(fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 로드 중 예외가 발생했습니다. 경로: {fullPath}\n{e}");
            return false;
        }
    }

}
