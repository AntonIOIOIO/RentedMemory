namespace System.Buffers;


public readonly struct RentedMemory<T> where T : struct, IEquatable<T>
{
    private static readonly T[] Empty = Array.Empty<T>();
    private static readonly ArrayPool<T> SharedPool = ArrayPool<T>.Shared;

    private readonly T[] InternalArray = Empty;

    public readonly Span<T> Span => InternalArray;
    public readonly Memory<T> Memory => InternalArray;
    public readonly int Length => InternalArray.Length;


    private RentedMemory(int MinimumSize) => InternalArray = SharedPool.Rent(MinimumSize);

    public static RentedMemory<T> Rent(int MinimumSize) => new(MinimumSize);
    public static RentedMemory<T> Rent(long MinimumSize) => new((int)MinimumSize);

    public void Return() => SharedPool.Return(InternalArray, clearArray: false);
}