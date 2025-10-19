using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a node value in KuzuDB, including ID, label, and properties.
    /// </summary>
    public class KuzuNode : KuzuValue, IHasProperties
    {
        private InternalId? _id;
        private string? _label;

        private ulong? _propertyCount;

        /// <summary>
        /// Gets the internal ID of the node.
        /// </summary>
        public InternalId Id
        {
            get
            {
                if (!_id.HasValue)
                {
                    _id = FetchId();
                }
                return _id.Value;
            }
        }

        /// <summary>
        /// Gets the label of the node.
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
        /// Gets the properties of the node as a <see cref="PropertyDictionary"/>.
        /// </summary>
        public PropertyDictionary Properties => new(this);

        /// <summary>
        /// Gets the number of properties on the node.
        /// </summary>
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
            var st = NativeMethods.kuzu_node_val_get_property_name_at(ref Handle.NativeStruct, index, out var ptr);
            KuzuGuard.CheckSuccess(st, $"Failed to get node property name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        public KuzuValue GetPropertyValueAt(ulong index)
        {
            ThrowIfDisposed();
            ValidatePropertyIndex(index);
            var st = NativeMethods.kuzu_node_val_get_property_value_at(ref Handle.NativeStruct, index, out var h);
            KuzuGuard.CheckSuccess(st, $"Failed to get node property value at index {index}");
            return FromNativeStruct(h);
        }

        public override string ToString()
        {
            if (Handle.IsInvalid) return "Node(Disposed)";
            var st = NativeMethods.kuzu_node_val_to_string(ref Handle.NativeStruct, out var ptr);
            KuzuGuard.CheckSuccess(st, "Failed to convert node to string");
            return NativeUtil.PtrToStringAndDestroy(ptr, NativeMethods.kuzu_destroy_string);
        }

        private InternalId FetchId()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_id_val(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get node id value");
            //KuzuGuard.AssertBorrowed(h.IsOwnedByCpp);
            using var id = (KuzuInternalId)FromNativeStruct(h);
            return id.Value;
        }

        private string FetchLabel()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_label_val(ref Handle.NativeStruct, out var h);
            KuzuGuard.CheckSuccess(st, "Failed to get node label value");
            //KuzuGuard.AssertBorrowed(h.IsOwnedByCpp);
            using var label = (KuzuString)FromNativeStruct(h);
            return label.Value;
        }
        private ulong FetchPropertyCount()
        {
            ThrowIfDisposed();
            var st = NativeMethods.kuzu_node_val_get_property_size(ref Handle.NativeStruct, out var sz);
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