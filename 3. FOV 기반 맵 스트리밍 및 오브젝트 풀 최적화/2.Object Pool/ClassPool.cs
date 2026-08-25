using System.Collections.Generic;

public interface IClassPoolEntry
{
    void onRent();
    void onReturn();
}

public sealed class ClassPool<T> where T : class, IClassPoolEntry, new()
{
    private readonly Stack<T> pool = new();

    public T Get()
    {
        T entry = pool.Count > 0 ? pool.Pop() : new T();
        entry.onRent();
        return entry;
    }

    public void Release(T entry)
    {
        if (entry == null)
            return;

        entry.onReturn();
        pool.Push(entry);
    }
}
