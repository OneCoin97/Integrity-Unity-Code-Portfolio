using System.Collections.Generic;

public class HashSetPool<T>
{
    private readonly Stack<HashSet<T>> pool = new Stack<HashSet<T>>();

    public HashSet<T> Get()
    {
        if (pool.Count > 0)
        {
            HashSet<T> hashSet = pool.Pop();
            hashSet.Clear();
            return hashSet;
        }

        return new HashSet<T>();
    }

    public HashSet<T> Get(HashSet<T> source)
    {
        HashSet<T> hashSet = Get();

        if (source != null)
        {
            hashSet.UnionWith(source);
        }

        return hashSet;
    }

    public HashSet<T> Get(IEnumerable<T> source)
    {
        HashSet<T> hashSet = Get();

        if (source != null)
        {
            hashSet.UnionWith(source);
        }

        return hashSet;
    }

    public void Release(HashSet<T> hashSet)
    {
        if (hashSet == null)
        {
            return;
        }

        hashSet.Clear();
        pool.Push(hashSet);
    }

}
