namespace System.Buffers;


public sealed class RentedObjects<T> where T : class
{
    private const int MaxSharedPooledItemsSize = 64;

    private static T?[]? SharedPool;
    private static T? SharedItem;

    private readonly T?[] PooledItems;

    public RentedObjects(int MaxPooledItemsSize) => PooledItems = new T[MaxPooledItemsSize];

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise returns null.
    /// </summary>
    /// <returns>T or null</returns>
    public T? Rent()
    {
        for (int i = 0; i < PooledItems.Length; i++)
        {
            if (PooledItems[i] is null)
                continue;

            T? Object = Interlocked.Exchange(ref PooledItems[i], null);

            if (Object is not null)
                return Object;
        }

        return null;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    public void Return(T Object)
    {
        for (int i = 0; i < PooledItems.Length; i++)
            if (PooledItems[i] is null && Interlocked.CompareExchange(ref PooledItems[i], Object, null) is null)
                return;
    }

    /// <summary>
    /// Gets an object from the shared pool if one is available, otherwise returns null.
    /// </summary>
    /// <returns>T or null</returns>
    public static T? RentFromSharedPool()
    {
        T? Object;

        if (SharedItem != null)
        {
            Object = Interlocked.Exchange(ref SharedItem, null);

            if (Object is not null)
                return Object;
        }

        T?[]? SharedPool = RentedObjects<T>.SharedPool;
        if (SharedPool != null)
        {
            for (int i = 0; i < SharedPool.Length; i++)
            {
                if (SharedPool[i] is null)
                    continue;

                Object = Interlocked.Exchange(ref SharedPool[i], null);

                if (Object is not null)
                    return Object;
            }

        }

        return null;
    }

  

    /// <summary>
    /// Return an object to the shared pool.
    /// </summary>
    public static void ReturnToSharedPool(T Object)
    {
        if (SharedItem is null && Interlocked.CompareExchange(ref SharedItem, Object, null) is null)
            return;

        T?[]? SharedPool = RentedObjects<T>.SharedPool;

        if (SharedPool is null)
        {
            Interlocked.CompareExchange(ref RentedObjects<T>.SharedPool, new T[MaxSharedPooledItemsSize], null);
            SharedPool = RentedObjects<T>.SharedPool;
        }

        for (int i = 0; i < SharedPool.Length; i++)
            if (SharedPool[i] is null && Interlocked.CompareExchange(ref SharedPool[i], Object, null) is null)
                return;
    }

    public static void DisposeSharedPool()
    {
        SharedItem = null;

        if(SharedPool is not null)
        {
            T?[]? SharedPool = RentedObjects<T>.SharedPool;

            for (int i = 0; i < SharedPool.Length; i++)
                SharedPool[i] = null;

            RentedObjects<T>.SharedPool = null;
        }
    }
}