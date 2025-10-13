using KuzuDot.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KuzuDot.Value
{
    public class PropertyDictionary : IReadOnlyDictionary<string, KuzuValue>
    {
        private readonly IHasProperties _source;

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

        public KuzuValue this[string key] => Get(key);
        public IEnumerable<string> Keys
        {
            get
            {
                var count = _source.PropertyCount;
                for (ulong i = 0; i < count; i++)
                    yield return _source.GetPropertyNameAt(i);
            }
        }

        public IEnumerable<KuzuValue> Values 
        {
            get
            {
                var count = _source.PropertyCount;
                for (ulong i = 0; i < count; i++)
                    yield return _source.GetPropertyValueAt(i);
            }
        }

        public int Count => (int)_source.PropertyCount;

        public bool ContainsKey(string key) => TryGetPropertyIndex(key) != null;

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
