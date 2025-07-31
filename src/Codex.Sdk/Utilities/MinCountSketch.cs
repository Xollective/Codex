using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;

namespace Codex.Utilities
{
    public abstract class MinCountSketch<TKey, TInt>
        where TInt : struct, INumber<TInt>, IMinMaxValue<TInt>
    {
        protected readonly TInt[,] _values;
        public int Rows { get; }
        private ulong _columns;

        public int Columns => (int)_columns;

        public MinCountSketch(int rows, int columns)
        {
            Rows = rows;
            _columns = (ulong)columns;
            _values = new TInt[rows, columns];
        }

        protected Span<TInt> GetSpan()
        {
            return _values.AsSpan();
        }

        public void Clear()
        {
            GetSpan().Clear();
        }

        protected Span<byte> GetByteSpan()
        {
            return MemoryMarshal.AsBytes(_values.AsSpan());
        }

        public TInt Get(TKey key)
        {
            return ProcessValues(key, (ref TInt value, ref TInt state) => state = TInt.Min(state, value), initialState: TInt.MaxValue);
        }

        public void Add(TKey key)
        {
            ProcessValues(key, (ref TInt value, ref TInt state) => value++);
        }

        public void Add(MinCountSketch<TKey, TInt> other)
        {
            AddOrSubtract(other, add: true);
        }

        public void Subtract(MinCountSketch<TKey, TInt> other)
        {
            AddOrSubtract(other, add: false);
        }

        private void AddOrSubtract(MinCountSketch<TKey, TInt> other, bool add)
        {
            Contract.Check(Rows == other.Rows)?.Assert($"Number of rows not equal {Rows} != {other.Rows}");
            Contract.Check(_columns == other._columns)?.Assert($"Number of columns not equal {_columns} != {other._columns}");

            var otherSpan = other._values.AsSpan();
            AddOrSubtract(otherSpan, add);
        }

        protected void AddOrSubtract(Span<TInt> otherSpan, bool add)
        {
            var thisSpan = _values.AsSpan();

            for (int i = 0; i < thisSpan.Length; i++)
            {
                if (add)
                {
                    thisSpan[i] += otherSpan[i];
                }
                else
                {
                    thisSpan[i] -= otherSpan[i];
                }
            }
        }

        public void Remove(TKey key)
        {
            ProcessValues(key, (ref TInt value, ref TInt state) => value--);
        }

        protected abstract ulong GetHash(TKey key);

        private delegate void Process(ref TInt value, ref TInt state);

        private TInt ProcessValues(TKey key, Process process, TInt initialState = default)
        {
            var hash = GetHash(key);
            var state = initialState;
            unchecked
            {
                var (high, low) = ((uint)(hash >> 32), (uint)hash);
                for (uint row = 0; row < Rows; row++)
                {
                    int column = (int)((high + (low * row)) % _columns);
                    ref var value = ref _values[row, column];
                    process(ref value, ref state);
                }
            }

            return state;
        }
    }
}

public class MinCountSketch
{
    public MinCountSketch<TKey, TInt> Create<TKey, TInt>(
        int rows, 
        int columns, 
        Func<TKey, ulong> getHash)
        where TInt : struct, INumber<TInt>, IMinMaxValue<TInt>
    {
        return new FuncMinCountSketch<TKey, TInt>(rows, columns, getHash);
    }

    private class FuncMinCountSketch<TKey, TInt> : MinCountSketch<TKey, TInt>
        where TInt : struct, INumber<TInt>, IMinMaxValue<TInt>
    {
        private readonly Func<TKey, ulong> getHash;

        public FuncMinCountSketch(int rows, int columns, Func<TKey, ulong> getHash) 
            : base(rows, columns)
        {
            this.getHash = getHash;
        }

        protected override ulong GetHash(TKey key) => getHash(key);
    }
}