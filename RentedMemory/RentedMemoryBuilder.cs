namespace System.Buffers;

public static partial class Extensions
{
    #region IndexOf

    public static int IndexOf(this RentedMemoryBuilder<char> RentedMemoryBuilder, ReadOnlySpan<char> Value, StringComparison StringComparison = StringComparison.OrdinalIgnoreCase)
        => MemoryExtensions.IndexOf(RentedMemoryBuilder.WrittenSpan, Value, StringComparison);

    public static int IndexOf<T>(this RentedMemoryBuilder<T> RentedMemoryBuilder, ReadOnlySpan<T> Value) where T : struct, IEquatable<T>
       => RentedMemoryBuilder.WrittenSpan.IndexOf(Value);

    public static int IndexOf<T>(this RentedMemoryBuilder<T> RentedMemoryBuilder, T Value) where T : struct, IEquatable<T>
       => RentedMemoryBuilder.WrittenSpan.IndexOf(Value);

    public static int IndexOf<T>(this RentedMemoryBuilder<T> RentedMemoryBuilder, T Value, int StartIndex) where T : struct, IEquatable<T>
    {
        int Index = RentedMemoryBuilder.WrittenSpan[StartIndex..].IndexOf(Value);
        return Index == -1 ? Index : Index + StartIndex;
    }

    #endregion

    #region Contains

    public static bool Contains(this RentedMemoryBuilder<char> RentedMemoryBuilder, ReadOnlySpan<char> Value, StringComparison StringComparison = StringComparison.OrdinalIgnoreCase)
      => MemoryExtensions.Contains(RentedMemoryBuilder.WrittenSpan, Value, StringComparison);

    public static bool Contains<T>(this RentedMemoryBuilder<T> RentedMemoryBuilder, T Value) where T : struct, IEquatable<T> 
        => RentedMemoryBuilder.WrittenSpan.Contains(Value);

    public static bool Contains<T>(this RentedMemoryBuilder<T> RentedMemoryBuilder, ReadOnlySpan<T> Value) where T : struct, IEquatable<T> 
        => RentedMemoryBuilder.WrittenSpan.IndexOf(Value) != -1;

    #endregion

}

public sealed class RentedMemoryBuilder<T> where T : struct, IEquatable<T>
{
    private RentedMemory<T> RentedArray;
    private int WrittenCount;

    public static RentedMemoryBuilder<T> Rent(long MinimumSize = default) => Rent(RentedMemory<T>.Rent(MinimumSize));
    public static RentedMemoryBuilder<T> Rent(int MinimumSize = default) => Rent(RentedMemory<T>.Rent(MinimumSize));

    public static RentedMemoryBuilder<T> Rent(RentedMemory<T> RentedArray = default)
    {
        RentedMemoryBuilder<T>? RentedMemoryBuilder = RentedObjects<RentedMemoryBuilder<T>>.RentFromSharedPool();

        if (RentedMemoryBuilder is null)
            return new RentedMemoryBuilder<T>(RentedArray);
        else
            RentedMemoryBuilder.RentedArray = RentedArray;

        return RentedMemoryBuilder;
    }


    public Memory<T> WrittenMemory => RentedArray.Memory[..WrittenCount];
    public Span<T> WrittenSpan => RentedArray.Span[..WrittenCount];

    public Memory<T> RemainingMemory => RentedArray.Memory[WrittenCount..];
    public Span<T> RemainingSpan => RentedArray.Span[WrittenCount..];

    private RentedMemoryBuilder(RentedMemory<T> RentedArray) => this.RentedArray = RentedArray;

    public void AdvanceRemaining(int WrittenCount) => this.WrittenCount += WrittenCount;

    public void Reset() => WrittenCount = 0;
  

    public void Return()
    {
        if (RentedArray.Length > 0)
        {
            RentedArray.Return();
            RentedArray = default;
        }

        Reset();

        RentedObjects<RentedMemoryBuilder<T>>.ReturnToSharedPool(this);
    }

    public void Return(out RentedMemory<T> RentedArray)
    {
        RentedArray = this.RentedArray;
        this.RentedArray = default;

        Return();
    }

    public void EnsureSize(long MinimumSize) => EnsureSize((int)MinimumSize);
    public void EnsureSize(int MinimumSize)
    {
        if (RentedArray.Length - WrittenCount > MinimumSize)
            return;

        RentedMemory<T> NewArray = RentedMemory<T>.Rent(MinimumSize + WrittenCount);

        if (WrittenCount > 0)
        {
            WrittenSpan.TryCopyTo(RentedArray.Span);
            RentedArray.Return();
        }

        RentedArray = NewArray;
    }

    public void Prepend(ReadOnlySpan<T> New) => Insert(0, New);
    public void Append(ReadOnlySpan<T> New) => Insert(WrittenCount, New);

    public void Remove(ReadOnlySpan<T> Old) => Replace(Old, ReadOnlySpan<T>.Empty);

    public void Insert(int Position, ReadOnlySpan<T> New)
    {
        int NewLength = New.Length;

        EnsureSize(NewLength);
        AdvanceRemaining(NewLength);
        Span<T> WrittenSpan = this.WrittenSpan;

        for (int Start = WrittenSpan.Length - 1, End = Position + NewLength - 1; Start > End; Start--)
            WrittenSpan[Start] = WrittenSpan[Start - NewLength];

        New.CopyTo(WrittenSpan[Position..]);
    }

    public void Replace(ReadOnlySpan<T> Old, ReadOnlySpan<T> New)
    {
        int OldIndex, Diff = New.Length - Old.Length;
        Span<T> WrittenSpan = this.WrittenSpan;

        if (Diff == 0)
        {
            while ((OldIndex = WrittenSpan.IndexOf(Old)) != -1)
                New.TryCopyTo(WrittenSpan[OldIndex..]);
        }
        else if (Diff > 0)
        {
            while ((OldIndex = WrittenSpan.IndexOf(Old)) != -1)
            {
                EnsureSize(Diff);
                AdvanceRemaining(Diff);
                WrittenSpan = this.WrittenSpan;

                for (int Start = WrittenSpan.Length - 1, End = OldIndex + New.Length - 1; Start > End; Start--)
                    WrittenSpan[Start] = WrittenSpan[Start - Diff];

                New.TryCopyTo(WrittenSpan[OldIndex..]);
            }
        }
        else
        {
            while ((OldIndex = WrittenSpan.IndexOf(Old)) != -1)
            {
                for (int Start = OldIndex + Old.Length, End = WrittenSpan.Length; Start < End; Start++)
                    WrittenSpan[Start + Diff] = WrittenSpan[Start];

                New.TryCopyTo(WrittenSpan[OldIndex..]);
                AdvanceRemaining(Diff);

                WrittenSpan = this.WrittenSpan;
            }
        }
    }
}