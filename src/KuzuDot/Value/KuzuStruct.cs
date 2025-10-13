using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace KuzuDot.Value
{
    public class KuzuStruct : KuzuValue, IEnumerable<(string Name, KuzuValue Value)>
    {
        private ulong? _fieldCount;

        internal KuzuStruct(NativeKuzuValue n) : base(n)
        {
        }

        public ulong FieldCount
        {
            get
            {
                if (!_fieldCount.HasValue)
                    _fieldCount = FetchFieldCount();
                return _fieldCount.Value;
            }
        }

        private ulong FetchFieldCount()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_struct_num_fields(Handle, out var c);
            KuzuGuard.CheckSuccess(st, "Failed to get struct field count");
            return c;
        }

        public (string Name, KuzuValue Value) this[ulong index]
        {
            get
            {
                var name = GetFieldName(index);
                var val = GetFieldValue(index);
                return (name, val);
            }
        }

        public IEnumerator<(string Name, KuzuValue Value)> GetEnumerator()
        {
            for (ulong i = 0; i < FieldCount; i++)
            {
                var name = GetFieldName(i);
                var value = GetFieldValue(i);
                yield return (name, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string GetFieldName(ulong index)
        {
            ValidateIndex(index);
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_struct_field_name(Handle, index, out var ptr);
            KuzuGuard.CheckSuccess(st, $"Failed to get struct field name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        public KuzuValue GetFieldValue(ulong index)
        {
            ValidateIndex(index);
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_value_get_struct_field_value(Handle, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get struct field value at index {index}");
            return FromNative(h);
        }

        private void ValidateIndex(ulong index)
        {
            var cnt = FieldCount;
            if (index >= cnt) throw new ArgumentOutOfRangeException($"Struct field index {index} is out of range (count={cnt})");
        }
    }
}