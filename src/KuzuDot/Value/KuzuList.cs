using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a list value in KuzuDB, providing access to elements and conversion utilities.
    /// </summary>
    public class KuzuList : KuzuValue, IEnumerable<KuzuValue>
    {
        private ulong? _count;

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public virtual ulong Count
        {
            get
            {
                if (!_count.HasValue)
                    _count = FetchCount();
                return _count.Value;
            }
        }

        protected virtual ulong FetchCount()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_list_size(ref Handle.NativeStruct, out var cnt);
            KuzuGuard.CheckSuccess(st, "Failed to get list size");
            return cnt;
        }

        internal KuzuList(NativeKuzuValue n) : base(n)
        {
        }

        internal KuzuList(IntPtr ptr) : base(ptr)
        {
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element.</param>
        /// <returns>The <see cref="KuzuValue"/> at the specified index.</returns>
        public KuzuValue this[ulong index] => GetElement(index);

        public IEnumerable<T> As<T>()
        {
            for (ulong i = 0; i < Count; i++)
            {
                using var v = GetElement(i);
                if (v is KuzuTypedValue<T> t)
                    yield return t.Value;
                else
                    throw new InvalidCastException($"Element at index {i} is of type {v.GetType().Name}, cannot cast to {typeof(T).Name}");
            }
        }

        public IEnumerable<T> As<T>(Func<KuzuValue, T> converter)
        {
            KuzuGuard.NotNull(converter, nameof(converter));
            for (ulong i = 0; i < Count; i++)
            {
                using var v = GetElement(i);
                yield return converter(v);
            }
        }

        public KuzuValue GetElement(ulong index)
        {
            ValidateIndex(index);
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_list_element(ref Handle.NativeStruct, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get list element at index {index}");
            return FromNativeStruct(h);
        }

        public IEnumerator<KuzuValue> GetEnumerator()
        {
            for (ulong i = 0; i < Count; i++)
                yield return GetElement(i);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void ValidateIndex(ulong index)
        {
            var cnt = Count;
            if (index >= cnt) throw new ArgumentOutOfRangeException($"List index {index} is out of range (count={cnt})");
        }
    }
}