using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    public class KuzuNode : KuzuValue, IHasProperties
    {
        private InternalId? _id;
        private string? _label;

        private ulong? _propertyCount;

        public InternalId Id
        {
            get
            {
                if (!_id.HasValue)
                {
                    _id = FetchId(Handle);
                }
                return _id.Value;
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
                {
                    _propertyCount = FetchPropertyCount();
                }
                return _propertyCount.Value;
            }
        }

        internal KuzuNode(NativeKuzuValue n) : base(n)
        {
        }

        public string GetPropertyNameAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_node_val_get_property_name_at(Handle, index, out var ptr);
            KuzuGuard.CheckSuccess(st, $"Failed to get node property name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        public KuzuValue GetPropertyValueAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_node_val_get_property_value_at(Handle, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get node property value at index {index}");
            return FromNative(h);
        }

        public override string ToString()
        {
            if (Handle.IsInvalid) return "Node(Disposed)";
            var st = NativeMethods.kuzu_node_val_to_string(Handle, out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to convert node to string");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        private InternalId FetchId(SafeHandle handle)
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_id_val(handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get node id value");
            //KuzuGuard.AssertBorrowed(h.IsOwnedByCpp);
            using var id = (KuzuInternalId)FromNative(h);
            return id.Value;
        }

        private string FetchLabel(SafeHandle handle)
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_label_val(handle, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get node label value");
            //KuzuGuard.AssertBorrowed(h.IsOwnedByCpp);
            using var label = (KuzuString)FromNative(h);
            return label.Value;
        }
        private ulong FetchPropertyCount()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_property_size(Handle, out var sz);
            KuzuGuard.CheckSuccess(st, "Failed to get node property size");
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