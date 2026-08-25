using System;
using System.Threading;
using UnityEngine;
 
public enum SaverHookType
{
    BeforeLoad,
    AfterLoad,
    BeforeSave,
    AfterSave
}

/// <summary>
///  initialize는 Start에서만 실행한다
/// </summary>
/// <typeparam name="T">저장할 data타입</typeparam>
public class SaverForData<T> : Saver where T : new()
{
    private CancellationTokenSource loadCancellation;
    private Action afterLoad;
    private Action afterSave;// 저장 요청이 쓰기 파이프라인에 등록된 직후 호출되며, 파일 쓰기 완료를 의미하지 않는다.
    private Action beforeLoad;
    private Action beforeSave;
    private Func<CancellationToken, Awaitable> afterLoadAwaitable;
    private Func<CancellationToken, Awaitable> beforeLoadAwaitable;

    public T data;
    public SaverForData(T data)
    {
        this.data = data;
        verification();
    }

    private void verification()
    {
        Type type = typeof(T);

        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            throw new InvalidOperationException($"{type.Name} 은 UnityEngine.Object 계열이므로 저장 불가");

        if (!type.IsSerializable)
            Debug.LogWarning($"{type.Name} 에 [Serializable] 속성이 없습니다 ─ 직렬화 실패 가능성");
    }

    /// <summary>
    /// Awaitable 로드 델리게이트를 설정한다. 같은 훅에 기존 값은 덮어쓴다(할당).
    /// </summary>
    public void setDelegateLoadAwaitable(bool isAfter, Func<CancellationToken, Awaitable> func)
    {
        if (isAfter)
        {
            afterLoadAwaitable = func;
        }
        else
        {
            beforeLoadAwaitable = func;
        }
    }

    /// <summary>
    /// void 델리게이트를 설정한다. 같은 훅에 기존 값은 덮어쓴다(할당).
    /// </summary>
    public void setDelegate(SaverHookType type, Action action)
    {
        switch (type)
        {
            case SaverHookType.BeforeLoad:
                beforeLoad = action;
                break;
            case SaverHookType.AfterLoad:
                afterLoad = action;
                break;
            case SaverHookType.BeforeSave:
                beforeSave = action;
                break;
            case SaverHookType.AfterSave:
                afterSave = action;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "지원하지 않는 SaverHookType");
        }
    }
    
    /// <summary>
    /// 모든 훅을 한 번에 해제한다(Awaitable, void 모두).
    /// </summary>
    public void clearAllDelegates()
    {
        beforeLoadAwaitable = null;
        afterLoadAwaitable = null;
        beforeLoad = null;
        afterLoad = null;
        beforeSave = null;
        afterSave = null;
    }

    public override void resetData()
    {
        data = new T();
    }

    public override string serialize()
    {
        return JsonUtility.ToJson(new DataCapsule<T>(data));
    }

    private bool tryDeserialize(out DataCapsule<T> result)
    {
        result = default;
        if (!loadSerializedData(out string serializedData))
            return false;

        try
        {
            result = JsonUtility.FromJson<DataCapsule<T>>(serializedData);
            if (ReferenceEquals(result, null))
            {
                Debug.LogWarning($"JSON 역직렬화에 실패했습니다. 경로: {fullPath}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 역직렬화 중 예외가 발생했습니다. 경로: {fullPath}\n{e}");
            return false;
        }
    }

    public override async Awaitable loadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (beforeLoadAwaitable != null)
            await beforeLoadAwaitable(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        beforeLoad?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();

        if (tryDeserialize(out DataCapsule<T> tData))
        {
            data = tData.data;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (afterLoadAwaitable != null)
            await afterLoadAwaitable(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        afterLoad?.Invoke();
    }

    public void loadImmediate()
    {
        if (beforeLoadAwaitable != null || afterLoadAwaitable != null)
        {
            throw new InvalidOperationException("Awaitable 로드 훅이 등록된 Saver는 loadAsync()를 사용해야 합니다.");
        }

        beforeLoad?.Invoke();
        if (tryDeserialize(out DataCapsule<T> tData))
        {
            data = tData.data;
        }
        afterLoad?.Invoke();
    }

  
    public override void load()
    {
        loadCancellation?.Cancel();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        loadCancellation = cancellation;
        _ = runLoadAsync(cancellation);
    }

    public override void removeSaver()
    {
        loadCancellation?.Cancel();
        base.removeSaver();
    }

    private async Awaitable runLoadAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await loadAsync(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (ReferenceEquals(loadCancellation, cancellation))
                loadCancellation = null;

            cancellation.Dispose();
        }
    }
    

    public override void save()
    {
        if (beforeSave != null)
            beforeSave();
        
        if (data != null)
        {
            if (addSaverData())
                afterSave?.Invoke();
        }
    }
    
}


[Serializable]
public class DataCapsule<T>
{
    public T data;

    public DataCapsule(T data)
    {
        this.data = data;
    }
}
