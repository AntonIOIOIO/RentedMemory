using Microsoft.Extensions.ObjectPool;

namespace System.Buffers
{
    public sealed class RentedMemoryBuilder<T> where T : struct, IEquatable<T>
    {
        private sealed class RentedArrayPooledPolicy : PooledObjectPolicy<RentedMemoryBuilder<T>>
        {
            public override RentedMemoryBuilder<T> Create() => new();

            public override bool Return(RentedMemoryBuilder<T> _) => true;
        }

        private static readonly DefaultObjectPool<RentedMemoryBuilder<T>> Pool = new(new RentedArrayPooledPolicy(), 16);

        private RentedMemory<T> RentedArray;
        private int WrittenCount;

        public Memory<T> WrittenMemory => RentedArray.Memory[..WrittenCount];
        public Span<T> WrittenSpan => RentedArray.Span[..WrittenCount];

        public Memory<T> RemainingMemory => RentedArray.Memory[WrittenCount..];
        public Span<T> RemainingSpan => RentedArray.Span[WrittenCount..];

        private RentedMemoryBuilder()
        {
            RentedArray = default;
            WrittenCount = 0;
        }

        public void AdvanceRemaining(int WrittenCount) => this.WrittenCount += WrittenCount;

        public void Reset() => WrittenCount = 0;

        public static RentedMemoryBuilder<T> Rent(RentedMemory<T> RentedArray = default)
        {
            RentedMemoryBuilder<T> RentedBytes = Pool.Get();
            RentedBytes.RentedArray = RentedArray;
            return RentedBytes;
        }

        public static RentedMemoryBuilder<T> Rent(long MinimumSize = default) => Rent((int)MinimumSize);
        public static RentedMemoryBuilder<T> Rent(int MinimumSize = default)
        {
            RentedMemoryBuilder<T> RentedBytes = Pool.Get();

            if (MinimumSize > 0)
                RentedBytes.RentedArray = RentedMemory<T>.Rent(MinimumSize);

            return RentedBytes;
        }

        public void Return()
        {
            if (RentedArray.Length > 0)
            {
                RentedArray.Return();
                RentedArray = default;
            }

            Reset();
            Pool.Return(this);
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

        public int IndexOf(ReadOnlySpan<T> Span) => WrittenSpan.IndexOf(Span);
        public int IndexOf(T Value) => WrittenSpan.IndexOf(Value);

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

}