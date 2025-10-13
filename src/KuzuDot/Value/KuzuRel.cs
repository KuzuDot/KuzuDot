using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    public sealed class KuzuRel : KuzuValue, IHasProperties
    {
        private InternalId? _dstId;

        private InternalId? _id;

        private string? _label;

        private ulong? _propertyCount;

        private InternalId? _srcId;

        public InternalId DstId
        {
            get
            {
                if (!_dstId.HasValue)
                    _dstId = FetchDstId(Handle);

                return _dstId.GetValueOrDefault();
            }
        }

        public InternalId Id
        {
            get
            {
                if (!_id.HasValue)
                {
                    ThrowIfDisposed();
                    var st = NativeMethods.kuzu_rel_val_get_id_val(Handle, out var h);
                    KuzuGuard.CheckSuccess(st, "Failed to get rel id value");
                    using var val = ((KuzuInternalId)FromNative(h));
                    _id = val.Value;
                }
                return _id.GetValueOrDefault();
            }
        }

        public string Label
        {
            get
            {
                _label ??= FetchLabel(Handle);
                return _label;
            }
        }

        public PropertyDictionary Properties => new(this);

        public ulong PropertyCount
        {
            get
            {
                if (!_propertyCount.HasValue)
                    _propertyCount = FetchPropertyCount(Handle);

                return _propertyCount.Value;
            }
        }

        public InternalId SrcId
        {
            get
            {
                if (!_srcId.HasValue)
                {
                    ThrowIfDisposed();
                    var st = NativeMethods.kuzu_rel_val_get_src_id_val(Handle, out var h);
                    KuzuGuard.CheckSuccess(st, "Failed to get rel src id value");
                    using var val = (KuzuInternalId)FromNative(h);
                    _srcId = val.Value;
                }
                return _srcId.GetValueOrDefault();
            }
        }

        internal KuzuRel(NativeKuzuValue n) : base(n)
        {
        }
        public string GetPropertyNameAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_rel_val_get_property_name_at(Handle, index, out var ptr);
            KuzuGuard.CheckSuccess(st, $"Failed to get rel property name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        public KuzuValue GetPropertyValueAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_rel_val_get_property_value_at(Handle, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get rel property value at index {index}");
            return FromNative(h);
        }

        public override string ToString()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_to_string(Handle, out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to convert rel to string");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        private InternalId FetchDstId(SafeHandle handle)
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_dst_id_val(handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get rel dst id value");
            using var val = ((KuzuInternalId)FromNative(h));
            return val.Value;
        }
        private string FetchLabel(SafeHandle handle)
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_label_val(handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get rel label value");
            using var val = ((KuzuString)FromNative(h));
            return val.Value;
        }
        private ulong FetchPropertyCount(SafeHandle handle)
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_property_size(handle, out var sz);
            KuzuGuard.CheckSuccess(st, "Failed to get rel property size");
            return sz;
        }

        private void ValidatePropertyIndex(ulong index)
        {
            var count = PropertyCount;
            if (index >= count)
                throw new ArgumentOutOfRangeException($"Property index {index} is out of range (count={count})");
        }
    }
}