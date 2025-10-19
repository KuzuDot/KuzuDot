using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using KuzuDot.Value;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot
{
    /// <summary>
    /// Represents a prepared statement for execution against a Kuzu database.
    /// </summary>
    public sealed partial class PreparedStatement : IDisposable
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, NamingStrategy), MemberBinding[]> _typeCache = new();

        private readonly Connection _connection;

        private readonly PreparedStatementSafeHandle _handle;

        /// <summary>
        /// Gets the error message if the prepared statement failed.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                ThrowIfDisposed();
                return GetErrorMessageSafe();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the prepared statement was created successfully.
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.kuzu_prepared_statement_is_success(ref _handle.NativeStruct);
            }
        }

        internal ref NativeKuzuPreparedStatement NativeStruct => ref _handle.NativeStruct;

        internal PreparedStatement(NativeKuzuPreparedStatement nativeHandle, Connection connection)
        {
            KuzuGuard.NotNull(connection, nameof(connection));
            _connection = connection;
            _handle = new PreparedStatementSafeHandle(nativeHandle);
        }

        private delegate KuzuState NativeBind<T>(ref NativeKuzuPreparedStatement handle, string paramName, T value);

        /// <summary>
        /// Binds parameters to the prepared statement using the default naming strategy (snake_case).
        /// </summary>
        /// <param name="parameters">The parameters object.</param>
        /// <returns>The current <see cref="PreparedStatement"/> instance.</returns>
        public PreparedStatement Bind(object parameters)
        {
            return Bind(parameters, NamingStrategy.SnakeCase);
        }

        /// <summary>
        /// Binds parameters to the prepared statement using the specified naming strategy.
        /// </summary>
        /// <param name="parameters">The parameters object.</param>
        /// <param name="strategy">The naming strategy to use for parameter names.</param>
        /// <returns>The current <see cref="PreparedStatement"/> instance.</returns>
        public PreparedStatement Bind(object parameters, NamingStrategy strategy)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNull(parameters, nameof(parameters));
            var type = parameters.GetType();
            var bindings = _typeCache.GetOrAdd((type, strategy), key =>
            {
                var list = new System.Collections.Generic.List<MemberBinding>();
                var props = key.Item1.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var p in props)
                {
                    if (!p.CanRead) continue;

                    var attr = (KuzuNameAttribute?)Attribute.GetCustomAttribute(p, typeof(KuzuNameAttribute));
                    var logical = attr?.Name ?? p.Name;
                    var normalized = NormalizeParam(logical, key.Item2);
                    if (normalized.Length > 0)
                        list.Add(new MemberBinding(normalized, true, p));
                }
                var fields = key.Item1.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var f in fields)
                {
                    var attr = (KuzuNameAttribute?)Attribute.GetCustomAttribute(f, typeof(KuzuNameAttribute));
                    var logical = attr?.Name ?? f.Name;
                    var normalized = NormalizeParam(logical, key.Item2);
                    if (normalized.Length > 0)
                        list.Add(new MemberBinding(normalized, false, f));
                }
                return [.. list];
            });

            static string NormalizeParam(string logicalName, NamingStrategy strategy)
            {
                if (string.IsNullOrWhiteSpace(logicalName)) return string.Empty;
                var trimmed = logicalName.Trim();
                
                var result = strategy switch
                {
                    NamingStrategy.Lowercase => ToLowercase(trimmed),
                    NamingStrategy.SnakeCase => ToSnakeCase(trimmed),
                    NamingStrategy.CamelCase => ToCamelCase(trimmed),
                    NamingStrategy.PascalCase => trimmed,
                    NamingStrategy.Exact => trimmed,
                    _ => ToSnakeCase(trimmed) // Default to snake_case
                };
                
                return result;
            }

            static string ToLowercase(string input)
            {
                var upper = NativeUtil.ToUpperInvariant(input);
                if (upper.Length == 0) return string.Empty;
                char[] lowerBuf = new char[upper.Length];
                for (int i = 0; i < upper.Length; i++)
                {
                    var ch = upper[i];
                    lowerBuf[i] = ch >= 'A' && ch <= 'Z' ? (char)(ch + 32) : ch;
                }
                return new string(lowerBuf);
            }

            static string ToSnakeCase(string input)
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                
                var result = new System.Text.StringBuilder();
                bool isFirst = true;
                
                foreach (char c in input)
                {
                    if (char.IsUpper(c))
                    {
                        if (!isFirst)
                            result.Append('_');
                        result.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        result.Append(c);
                    }
                    isFirst = false;
                }
                
                return result.ToString();
            }

            static string ToCamelCase(string input)
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                
                var result = new System.Text.StringBuilder();
                bool isFirst = true;
                
                foreach (char c in input)
                {
                    if (char.IsUpper(c))
                    {
                        if (isFirst)
                            result.Append(char.ToLowerInvariant(c));
                        else
                            result.Append(c);
                    }
                    else
                    {
                        result.Append(c);
                    }
                    isFirst = false;
                }
                
                return result.ToString();
            }


            foreach (var b in bindings)
            {
                object? val = b.IsProperty
                    ? ((System.Reflection.PropertyInfo)b.Member).GetValue(parameters)
                    : ((System.Reflection.FieldInfo)b.Member).GetValue(parameters);
                BindOne(b.Parameter, val ?? throw new InvalidOperationException($"Null value for parameter '{b.Parameter}'"));
            }
            return this;
        }

        public PreparedStatement Bind(string p, bool v) => BindBool(p, v);

        public PreparedStatement Bind(string p, sbyte v) => BindInt8(p, v);

        public PreparedStatement Bind(string p, short v) => BindInt16(p, v);

        public PreparedStatement Bind(string p, int v) => BindInt32(p, v);

        public PreparedStatement Bind(string p, long v) => BindInt64(p, v);

        public PreparedStatement Bind(string p, byte v) => BindUInt8(p, v);

        public PreparedStatement Bind(string p, ushort v) => BindUInt16(p, v);

        public PreparedStatement Bind(string p, uint v) => BindUInt32(p, v);

        public PreparedStatement Bind(string p, ulong v) => BindUInt64(p, v);

        public PreparedStatement Bind(string p, float v) => BindFloat(p, v);

        public PreparedStatement Bind(string p, double v) => BindDouble(p, v);

        public PreparedStatement Bind(string p, string? v) => BindString(p, v);

        public PreparedStatement Bind(string p, DateTime v) => BindTimestamp(p, v);

        public PreparedStatement Bind(string p, TimeSpan v) => BindInterval(p, v);

        public PreparedStatement Bind(string p, KuzuValue v) => BindValue(p, v);

        public PreparedStatement Bind(string p, object v)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(p, nameof(p));
            KuzuGuard.NotNull(v, nameof(v));
            
            // Prevent binding complex objects to single parameters
            var type = v.GetType();
            if (!type.IsPrimitive && type != typeof(string) && type != typeof(DateTime) && 
                type != typeof(TimeSpan) && type != typeof(Guid) && type != typeof(decimal) &&
                System.Nullable.GetUnderlyingType(type) == null)
            {
                throw new ArgumentException($"Cannot bind complex object of type '{type.Name}' to parameter '{p}'. " +
                    "Use Bind(object) to bind all properties of an object, or bind individual primitive values.");
            }
            
            return BindOne(p, v);
        }

        private PreparedStatement BindOne(string param, object value)
        {
            if (param.Length == 0) return this;
            if (value == null)
            {
                BindValue(param, KuzuValueFactory.CreateNull()); // creates a temporary wrapper; future optimization: reuse static NULL
                return this;
            }
            switch (value)
            {
                case bool b: BindBool(param, b); return this;
                case sbyte i8: BindInt8(param, i8); return this;
                case short i16: BindInt16(param, i16); return this;
                case int i32: BindInt32(param, i32); return this;
                case long i64: BindInt64(param, i64); return this;
                case byte u8: BindUInt8(param, u8); return this;
                case ushort u16: BindUInt16(param, u16); return this;
                case uint u32: BindUInt32(param, u32); return this;
                case ulong u64: BindUInt64(param, u64); return this;
                case float f: BindFloat(param, f); return this;
                case double d: BindDouble(param, d); return this;
                case decimal dec: BindString(param, dec.ToString(System.Globalization.CultureInfo.InvariantCulture)); return this;
                case string s: BindString(param, s); return this;
                case Guid guid: BindString(param, guid.ToString("D", System.Globalization.CultureInfo.InvariantCulture)); return this;
                case UUID uuid: BindString(param, uuid.ToString()); return this;
                //case DateOnly date: BindDate(param, date.ToDateTime(TimeOnly.MinValue)); return this;
                case DateTime dt: 
                    // Check if parameter name suggests it's a date (not timestamp)
#pragma warning disable CA2249 // Use 'string.Contains' instead of 'string.IndexOf' to improve readability
                    if (param.IndexOf("date", StringComparison.OrdinalIgnoreCase) >= 0 && 
                        param.IndexOf("timestamp", StringComparison.OrdinalIgnoreCase) < 0 && 
                        param.IndexOf("time", StringComparison.OrdinalIgnoreCase) < 0)
#pragma warning restore CA2249
                        BindDate(param, dt);
                    else
                        BindTimestamp(param, dt);
                    return this;
                case DateTimeOffset dto: BindTimestampWithTimeZone(param, dto); return this;
                case TimeSpan ts: BindInterval(param, ts); return this;
                case KuzuValue kv: BindValue(param, kv); return this;
            }
            var vt = value.GetType();
            var underlying = System.Nullable.GetUnderlyingType(vt);
            if (underlying != null)
            {
                if (underlying == typeof(Guid)) { BindString(param, ((Guid)value).ToString("D", System.Globalization.CultureInfo.InvariantCulture)); return this; }
                if (underlying == typeof(decimal)) { BindString(param, ((decimal)value).ToString(System.Globalization.CultureInfo.InvariantCulture)); return this; }
            }
            BindString(param, value.ToString());
            return this;
        }

        public PreparedStatement BindBool(string paramName, bool value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_bool); return this; }

        public PreparedStatement BindDate(string paramName, DateTime value)
        { Bind(paramName, DateTimeUtilities.DateTimeToKuzuDate(value), NativeMethods.kuzu_prepared_statement_bind_date); return this; }

        public PreparedStatement BindDouble(string paramName, double value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_double); return this; }

        public PreparedStatement BindFloat(string paramName, float value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_float); return this; }

        public PreparedStatement BindInt16(string paramName, short value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int16); return this; }

        public PreparedStatement BindInt32(string paramName, int value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int32); return this; }

        public PreparedStatement BindInt64(string paramName, long value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int64); return this; }

        public PreparedStatement BindInt8(string paramName, sbyte value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int8); return this; }

        public PreparedStatement BindInterval(string paramName, TimeSpan value)
        { Bind(paramName, DateTimeUtilities.TimeSpanToNativeInterval(value), NativeMethods.kuzu_prepared_statement_bind_interval); return this; }

        public PreparedStatement BindString(string paramName, string? value)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            var safe = value ?? string.Empty;
            var result = NativeMethods.kuzu_prepared_statement_bind_string(ref _handle.NativeStruct, paramName, safe);
            KuzuGuard.CheckSuccess(result, $"Failed to bind string parameter '{paramName}': {GetErrorMessageSafe()}");
            return this;
        }

        public PreparedStatement BindTimestamp(string paramName, DateTime value)
        { Bind(paramName, DateTimeUtilities.DateTimeToNativeTimestamp(value), NativeMethods.kuzu_prepared_statement_bind_timestamp); return this; }

        public PreparedStatement BindTimestampMicros(string paramName, long unixMicros)
        { Bind(paramName, new NativeKuzuTimestamp(unixMicros), NativeMethods.kuzu_prepared_statement_bind_timestamp); return this; }

        public PreparedStatement BindTimestampMilliseconds(string paramName, long unixMillis)
        { Bind(paramName, new NativeKuzuTimestampMs(unixMillis), NativeMethods.kuzu_prepared_statement_bind_timestamp_ms); return this; }

        public PreparedStatement BindTimestampNanoseconds(string paramName, long unixNanos)
        { Bind(paramName, new NativeKuzuTimestampNs(unixNanos), NativeMethods.kuzu_prepared_statement_bind_timestamp_ns); return this; }

        public PreparedStatement BindTimestampSeconds(string paramName, long unixSeconds)
        { Bind(paramName, new NativeKuzuTimestampSec(unixSeconds), NativeMethods.kuzu_prepared_statement_bind_timestamp_sec); return this; }

        public PreparedStatement BindTimestampWithTimeZone(string paramName, DateTimeOffset dto)
        { Bind(paramName, new NativeKuzuTimestampTz(DateTimeUtilities.DateTimeToUnixMicroseconds(dto.UtcDateTime)), NativeMethods.kuzu_prepared_statement_bind_timestamp_tz); return this; }

        public PreparedStatement BindUInt16(string paramName, ushort value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint16); return this; }

        public PreparedStatement BindUInt32(string paramName, uint value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint32); return this; }

        public PreparedStatement BindUInt64(string paramName, ulong value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint64); return this; }

        public PreparedStatement BindUInt8(string paramName, byte value)
        { Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint8); return this; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "<Pending>")]
        public PreparedStatement BindValue(string paramName, KuzuValue value)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            KuzuGuard.NotNull(value, nameof(value));
            var result = NativeMethods.kuzu_prepared_statement_bind_value(ref _handle.NativeStruct, paramName, value.NativePtr);
            value.Dispose();
            KuzuGuard.CheckSuccess(result, $"Failed to bind value parameter '{paramName}': {GetErrorMessageSafe()}");
            return this;
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        /// <summary>
        /// Executes the prepared statement and returns the query result.
        /// </summary>
        /// <returns>The <see cref="QueryResult"/> produced by executing this statement.</returns>
        public QueryResult Execute()
        {
            ThrowIfDisposed();
            return _connection.Execute(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_handle.IsInvalid) return "PreparedStatement(Disposed)";
            return IsSuccess ? "PreparedStatement(Success)" : "PreparedStatement(Failed: " + ErrorMessage + ")";
        }

        private void Bind<T>(string paramName, T value, NativeBind<T> binder)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            var result = binder(ref _handle.NativeStruct, paramName, value);
            KuzuGuard.CheckSuccess(result, $"Failed to bind parameter '{paramName}': {GetErrorMessageSafe()}");
        }

        private string GetErrorMessageSafe()
        {
            ThrowIfDisposed();
            var ptr = NativeMethods.kuzu_prepared_statement_get_error_message(ref _handle.NativeStruct);
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        private void ThrowIfDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(PreparedStatement));
        }

        private readonly struct MemberBinding
        {
            internal readonly bool IsProperty;
            internal readonly System.Reflection.MemberInfo Member;
            internal readonly string Parameter; // normalized final param name
            internal MemberBinding(string parameter, bool isProperty, System.Reflection.MemberInfo member)
            { Parameter = parameter; IsProperty = isProperty; Member = member; }
        }

        private sealed class PreparedStatementSafeHandle : KuzuDot.Utils.KuzuSafeHandle
        {
            internal NativeKuzuPreparedStatement NativeStruct;

            public override bool IsInvalid => NativeStruct.PreparedStatement == IntPtr.Zero;

            internal PreparedStatementSafeHandle(NativeKuzuPreparedStatement nativeStruct) : base("PreparedStatement")
            {
                NativeStruct = nativeStruct;
                Initialize(nativeStruct.PreparedStatement);
            }

            protected override void Release()
            {
                NativeMethods.kuzu_prepared_statement_destroy(ref NativeStruct);
                NativeStruct = default;
            }
        }
    }
}