namespace System.Buffers;


public sealed class RentedObjects<T> where T : class
{
    private static RentedObjects<T>? SharedPool;
    private readonly T?[] PooledItems;

    public static RentedObjects<T> Shared => SharedPool ??= new RentedObjects<T>(MaxPooledItemsSize: 64);

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
}