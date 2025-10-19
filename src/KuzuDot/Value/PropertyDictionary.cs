using KuzuDot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KuzuDot.Value
{
    /// <summary>
    /// Represents a read-only dictionary of properties for a Kuzu node or relationship.
    /// </summary>
    public class PropertyDictionary : IReadOnlyDictionary<string, KuzuValue>
    {
        private readonly IHasProperties _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDictionary"/> class.
        /// </summary>
        /// <param name="source">The source object that provides property access.</param>
        public PropertyDictionary(IHasProperties source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Gets the property value for the given key. The returned KuzuValue is owned by the underlying NodeValue and should not be disposed by the caller.
        /// </summary>
        public KuzuValue Get(string key)
        {
            var idx = TryGetPropertyIndex(key);
            if (idx is null)
                throw new KeyNotFoundException($"Property '{key}' not found.");
            return _source.GetPropertyValueAt(idx.Value);
        }

        /// <inheritdoc/>
        public KuzuValue this[string key] => Get(key);
        /// <inheritdoc/>
        public IEnumerable<string> Keys
        {
            get
            {
                var count = _source.PropertyCount;
                for (ulong i = 0; i < count; i++)
                    yield return _source.GetPropertyNameAt(i);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<KuzuValue> Values 
        {
            get
            {
                var count = _source.PropertyCount;
                for (ulong i = 0; i < count; i++)
                    yield return _source.GetPropertyValueAt(i);
            }
        }

        /// <inheritdoc/>
        public int Count => (int)_source.PropertyCount;

        /// <inheritdoc/>
        public bool ContainsKey(string key) => TryGetPropertyIndex(key) != null;

        /// <summary>
        /// Gets the property value for the given key and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <returns>The property value converted to <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidCastException">Thrown if the property cannot be cast to the specified type.</exception>
        public T Get<T>(string key)
        {
            using var val = Get(key);
            if (val is KuzuTypedValue<T> t)
                return t.Value;
            throw new InvalidCastException($"Property '{key}' is of type {val.GetType().Name}, cannot cast to {typeof(T).Name}");
        }

        public T GetOrDefault<T>(string key, T defaultValue = default) where T : struct
        {
            var val = GetOrNull(key);
            if (val == null)
                return defaultValue;
            using (val)
            {
                if (val is KuzuTypedValue<T> t)
                    return t.Value;
                throw new InvalidCastException($"Property '{key}' is of type {val.GetType().Name}, cannot cast to {typeof(T).Name}");
            }
        }

        public T? GetOrNull<T>(string key) where T : struct
        {
            var val = GetOrNull(key);
            if (val == null)
                return null;
            using (val)
            {
                if (val is KuzuTypedValue<T> t)
                    return t.Value;
                throw new InvalidCastException($"Property '{key}' is of type {val.GetType().Name}, cannot cast to {typeof(T).Name}");
            }
        }

        public bool TryGetValue(string key, out KuzuValue value)
        {
            var idx = TryGetPropertyIndex(key);
            if (idx != null)
            {
                value = _source.GetPropertyValueAt(idx.Value);
                return true;
            }
            value = null!;
            return false;
        }

        public Dictionary<string, KuzuValue> AsDictionary()
        {
            var dict = new Dictionary<string, KuzuValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in this)
                dict[kvp.Key] = kvp.Value;
            return dict;
        }



        public KuzuValue? GetOrNull(string key)
        {
            var idx = TryGetPropertyIndex(key);
            return idx != null ? _source.GetPropertyValueAt(idx.Value) : null;
        }

        public bool Has(string key) => ContainsKey(key);

        public IEnumerator<KeyValuePair<string, KuzuValue>> GetEnumerator()
        {
            var count = _source.PropertyCount;
            for (ulong i = 0; i < count; i++)
            {
                var name = _source.GetPropertyNameAt(i);
                var val = _source.GetPropertyValueAt(i);
                yield return new KeyValuePair<string, KuzuValue>(name, val);
            }
        }

        private ulong? TryGetPropertyIndex(string key)
        {
            KuzuGuard.NotNull(key, nameof(key));
            var count = _source.PropertyCount;
            for (ulong i = 0; i < count; i++)
            {
                var name = _source.GetPropertyNameAt(i);
                if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
