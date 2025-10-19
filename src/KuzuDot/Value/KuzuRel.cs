using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a relationship value in KuzuDB, including source/destination IDs, label, and properties.
    /// </summary>
    public sealed class KuzuRel : KuzuValue, IHasProperties
    {
        private InternalId? _dstId;

        private InternalId? _id;

        private string? _label;

        private ulong? _propertyCount;

        private InternalId? _srcId;

        /// <summary>
        /// Gets the destination node ID of the relationship.
        /// </summary>
        public InternalId DstId
        {
            get
            {
                if (!_dstId.HasValue)
                    _dstId = FetchDstId();

                return _dstId.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Gets the internal ID of the relationship.
        /// </summary>
        public InternalId Id
        {
            get
            {
                if (!_id.HasValue)
                {
                    ThrowIfDisposed();
                    var st = NativeMethods.kuzu_rel_val_get_id_val(ref Handle.NativeStruct, out var h);
                    KuzuGuard.CheckSuccess(st, "Failed to get rel id value");
                    using var val = ((KuzuInternalId)FromNativeStruct(h));
                    _id = val.Value;
                }
                return _id.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Gets the label of the relationship.
        /// </summary>
        public string Label
        {
            get
            {
                _label ??= FetchLabel();
                return _label;
            }
        }

        /// <summary>
        /// Gets the properties of the relationship as a <see cref="PropertyDictionary"/>.
        /// </summary>
        public PropertyDictionary Properties => new(this);

        /// <summary>
        /// Gets the number of properties on the relationship.
        /// </summary>
        public ulong PropertyCount
        {
            get
            {
                if (!_propertyCount.HasValue)
                    _propertyCount = FetchPropertyCount();

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
                    var st = NativeMethods.kuzu_rel_val_get_src_id_val(ref Handle.NativeStruct, out var h);
                    KuzuGuard.CheckSuccess(st, "Failed to get rel src id value");
                    using var val = (KuzuInternalId)FromNativeStruct(h);
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
            var st = NativeMethods.kuzu_rel_val_get_property_name_at(ref Handle.NativeStruct, index, out var ptr);
            KuzuGuard.CheckSuccess(st, $"Failed to get rel property name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        public KuzuValue GetPropertyValueAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_rel_val_get_property_value_at(ref Handle.NativeStruct, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get rel property value at index {index}");
            return FromNativeStruct(h);
        }

        public override string ToString()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_to_string(ref Handle.NativeStruct, out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to convert rel to string");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        private InternalId FetchDstId()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_dst_id_val(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get rel dst id value");
            using var val = ((KuzuInternalId)FromNativeStruct(h));
            return val.Value;
        }
        private string FetchLabel()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_label_val(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get rel label value");
            using var val = ((KuzuString)FromNativeStruct(h));
            return val.Value;
        }
        private ulong FetchPropertyCount()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_rel_val_get_property_size(ref Handle.NativeStruct, out var sz);
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