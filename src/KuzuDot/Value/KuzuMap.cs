using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KuzuDot.Value
{

    public class KuzuMap : KuzuValue, IEnumerable<KeyValuePair<KuzuValue, KuzuValue>>
    {
        public ulong Count
        {
            get
            {
                ThrowIfDisposed();
                var st = NativeMethods.kuzu_value_get_map_size(Handle, out var sz);
                KuzuGuard.CheckSuccess(st, "Failed to get map size");
                return sz;
            }
        }

        internal KuzuMap(NativeKuzuValue n) : base(n)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable", Justification = "Caller must dispose")]
        public IEnumerator<KeyValuePair<KuzuValue, KuzuValue>> GetEnumerator()
        {
            for (ulong i = 0; i < Count; i++)
            {
                using var key = GetKey(i);
                using var val = GetValueAt(i);
                yield return new KeyValuePair<KuzuValue, KuzuValue>(key.Clone(), val.Clone());
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public KuzuValue GetKey(ulong index)
        {
            ValidateIndex(index);
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_map_key(Handle, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get map key at index {index}");
            return FromNative(h);
        }

        public KuzuValue GetValueAt(ulong index)
        {
            ValidateIndex(index);
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_map_value(Handle, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get map value at index {index}");
            return FromNative(h);
        }

        private void ValidateIndex(ulong index)
        {
            var cnt = Count;
            if (index >= cnt) throw new ArgumentOutOfRangeException($"Map entry index {index} is out of range (count={cnt})");
        }
    }
}