using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using KuzuDot.Value;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace KuzuDot
{
    public sealed class Connection : IDisposable
    {
        private readonly ConnectionSafeHandle _handle;

        /// <summary>
        /// Gets or sets the maximum number of threads to use for executing queries.
        /// </summary>
        public ulong MaxNumThreadsForExecution
        {
            get
            {
                var state = NativeMethods.kuzu_connection_get_max_num_thread_for_exec(ref _handle.NativeStruct, out var n);
                KuzuGuard.CheckSuccess(state, "Failed to get maximum number of threads for execution");
                return n;
            }
            set
            {
                NotDisposed();
                var state = NativeMethods.kuzu_connection_set_max_num_thread_for_exec(ref _handle.NativeStruct, value);
                KuzuGuard.CheckSuccess(state, "Failed to set maximum number of threads for execution");
            }
        }

        internal Connection(Database database)
        {
            KuzuGuard.NotNull(database, nameof(database));
            var state = NativeMethods.kuzu_connection_init(ref database.NativeStruct, out var nativeConn);
            KuzuGuard.CheckSuccess(state, "Failed to initialize connection");
            KuzuGuard.AssertNotZero(nativeConn.Connection, "Native connection pointer is null");
            _handle = new ConnectionSafeHandle(nativeConn);
        }

        /// <summary>
        /// Releases all resources used by the Connection.
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
        }

        /// <summary>
        /// Async wrapper for executing a prepared statement.
        /// </summary>
        public Task<QueryResult> ExecuteAsync(PreparedStatement preparedStatement, System.Threading.CancellationToken cancellationToken = default)
            => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Execute(preparedStatement);
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        InterruptSafe();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    throw;
                }
            }, cancellationToken);

        /// <summary>
        /// Executes a command that does not return a result set (DDL / INSERT / UPDATE / DELETE).
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>True if native layer reported success.</returns>
        /// <exception cref="KuzuException">Thrown if the query fails inside KuzuDB</exception>
        public bool ExecuteNonQuery(string query) => NonQuery(query);

        /// <summary>
        /// Executes a previously prepared (and optionally bound) statement returning a QueryResult.
        /// </summary>
        public QueryResult ExecutePrepared(PreparedStatement statement)
        {
            KuzuGuard.NotNull(statement, nameof(statement));
            return Execute(statement);
        }

        /// <summary>
        /// Executes a query expected to return a single scalar value (first column of first row).
        /// </summary>
        /// <typeparam name="T">Expected CLR type.</typeparam>
        /// <param name="query">Scalar returning query.</param>
        /// <returns>The value converted to <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result has no rows or columns.</exception>
        public T ExecuteScalar<T>(string query)
        {
            using var result = Query(query);
            if (!result.HasNext()) throw new InvalidOperationException("Scalar query returned no rows");
            using var row = result.GetNext();
            if (result.ColumnCount == 0) throw new InvalidOperationException("Scalar query returned zero columns");
            return row.GetValueAs<T>(0);
        }

        /// <summary>
        /// Interrupts the current query execution in this connection.
        /// </summary>
        public void Interrupt()
        {
            NotDisposed();
            NativeMethods.kuzu_connection_interrupt(ref _handle.NativeStruct);
        }

        /// <summary>
        /// Executes a command that does not return a result set (DDL / INSERT / UPDATE / DELETE).
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>True if native layer reported success.</returns>
        /// <exception cref="KuzuException">Thrown if the query fails inside KuzuDB</exception>
        public bool NonQuery(string query)
        {
            using var result = Query(query);
            return result.IsSuccess; // will have thrown already if not
        }

        /// <summary>
        /// Prepares a statement for execution.
        /// </summary>
        public PreparedStatement Prepare(string query)
        {
            KuzuGuard.NotNullOrEmpty(query, nameof(query));
            var haveInterceptors = KuzuInterceptorRegistry.HasGlobal;
            System.Diagnostics.Stopwatch? sw = null;
            if (haveInterceptors)
            {
                foreach (var i in KuzuInterceptorRegistry.Snapshot())
                    InterceptorSafeExecutor.Invoke(() => i.OnBeforePrepare(this, query));
                sw = System.Diagnostics.Stopwatch.StartNew();
            }
            NotDisposed();
            var state = NativeMethods.kuzu_connection_prepare(ref _handle.NativeStruct, query, out var ps);
            var prepared = new PreparedStatement(ps, this);
            if (!prepared.IsSuccess)
            {
                // Always throw on prepare failure to fail fast and surface diagnostic details.
                var ex = new KuzuException($"Prepare failed: {prepared.ErrorMessage}");
                if (haveInterceptors)
                {
                    sw!.Stop();
                    foreach (var i in KuzuInterceptorRegistry.Snapshot())
                        InterceptorSafeExecutor.Invoke(() => i.OnAfterPrepare(this, query, prepared, ex, sw.Elapsed));
                }
                throw ex;
            }
            if (haveInterceptors)
            {
                sw!.Stop();
                foreach (var i in KuzuInterceptorRegistry.Snapshot())
                    InterceptorSafeExecutor.Invoke(() => i.OnAfterPrepare(this, query, prepared, null, sw.Elapsed));
            }
            return prepared;
        }

        /// <summary>
        /// Async wrapper for <see cref="Prepare"/>.
        /// </summary>
        public Task<PreparedStatement> PrepareAsync(string query, System.Threading.CancellationToken cancellationToken = default)
            => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Prepare(query);
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        InterruptSafe();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    throw;
                }
            }, cancellationToken);

        /// <summary>
        /// Executes a query and returns the result.
        /// </summary>
        public QueryResult Query(string query)
        {
            KuzuGuard.NotNullOrEmpty(query, nameof(query));

            // If there are interceptors, start timing and invoke
            var haveInterceptors = KuzuInterceptorRegistry.HasGlobal;
            System.Diagnostics.Stopwatch? sw = null;
            if (haveInterceptors)
            {
                foreach (var i in KuzuInterceptorRegistry.Snapshot())
                    InterceptorSafeExecutor.Invoke(() => i.OnBeforeQuery(this, query));
                sw = System.Diagnostics.Stopwatch.StartNew();
            }

            NotDisposed();
            var state = NativeMethods.kuzu_connection_query(ref _handle.NativeStruct, query, out var qr);
            KuzuGuard.CheckSuccess(state, $"Failed to execute query: {query}");

            // Create wrapped QueryResult
            QueryResult? result = null;
            Exception? exCaught = null;
            try
            {
                result = new QueryResult(qr);
                return result;
            }
            catch (Exception ex)
            {
                exCaught = ex;
                throw;
            }
            finally
            {
                // If there are interceptors, report the time taken
                if (haveInterceptors)
                {
                    sw!.Stop();
                    foreach (var i in KuzuInterceptorRegistry.Snapshot())
                        InterceptorSafeExecutor.Invoke(() => i.OnAfterQuery(this, query, result, exCaught, sw.Elapsed));
                }
            }
        }

        /// <summary>
        /// Convenience method to materialize all projected rows into a list.
        /// </summary>
        public IReadOnlyList<T> Query<T>(string query, Func<FlatTuple, T> projector)
        {
            var list = new List<T>();
            foreach (var item in QueryEnumerable(query, projector)) list.Add(item);
            return list;
        }

        /// <summary>
        /// Executes a query and materializes each row into a new instance of T using public settable properties and fields.
        /// Property/field names are matched (case-insensitive) against column names.
        /// Supported value assignments rely on existing KuzuValue concrete types; unmatched types attempt ChangeType via invariant culture.
        /// Use [KuzuName("Name")] attribute on properties and fields to override the member name.
        /// </summary>
        public IReadOnlyList<T> Query<T>(string query) where T : new()
        {
            using var result = Query(query);
            var list = new List<T>();
            if (!result.IsSuccess) return list; // empty on failure (would have thrown earlier normally)

            int columnCount = (int)result.ColumnCount;
            var columnNames = new string[columnCount];
            for (uint i = 0; i < result.ColumnCount; i++)
            {
                columnNames[i] = result.GetColumnName(i);
            }

            var cache = MaterializationCache<T>.Instance;
            var propMap = cache.PropMap;
            var fieldMap = cache.FieldMap;
            while (result.HasNext())
            {
                using var row = result.GetNext();
                var instance = new T();
                for (int c = 0; c < columnCount; c++)
                {
                    using var value = row.GetValue((ulong)c);
                    var colName = columnNames[c];
                    if (propMap.TryGetValue(colName, out var prop))
                    {
                        try
                        {
                            var converted = KuzuValue.ConvertKuzuValue(prop.PropertyType, value);
                            prop.SetValue(instance, converted);
                        }
                        catch (System.InvalidCastException) { }
                        catch (System.FormatException) { }
                        continue;
                    }
                    if (fieldMap.TryGetValue(colName, out var field))
                    {
                        try
                        {
                            var converted = KuzuValue.ConvertKuzuValue(field.FieldType, value);
                            field.SetValue(instance, converted);
                        }
                        catch (System.InvalidCastException) { }
                        catch (System.FormatException) { }
                    }
                }
                list.Add(instance);
            }
            return list;
        }

        /// <summary>
        /// Async counterpart to Query<T>(string) with cancellation support.
        /// </summary>
        public Task<IReadOnlyList<T>> QueryAsync<T>(string query, System.Threading.CancellationToken cancellationToken = default) where T : new()
            => System.Threading.Tasks.Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Query<T>(query);
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        InterruptSafe();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    throw;
                }
            }, cancellationToken);

        /// <summary>
        /// Async wrapper for <see cref="Query"/> using Task.Run (initial implementation; no true async I/O yet).
        /// </summary>
        public Task<QueryResult> QueryAsync(string query, System.Threading.CancellationToken cancellationToken = default)
            => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Query(query);
                }
                catch
                {
                    // If cancellation requested during native call window, propagate.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        InterruptSafe();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    throw;
                }
            }, cancellationToken);

        /// <summary>
        /// Async wrapper for QueryEnumerable(projector) materializing to list.
        /// </summary>
        public Task<IReadOnlyList<T>> QueryAsync<T>(string query, Func<FlatTuple, T> projector, System.Threading.CancellationToken cancellationToken = default)
            => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Query(query, projector);
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        InterruptSafe();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    throw;
                }
            }, cancellationToken);

        // alias
        /// <summary>
        /// Streams tuples for the given query via an iterator. The underlying native QueryResult
        /// is disposed when enumeration completes.
        /// </summary>
        public IEnumerable<FlatTuple> QueryEnumerable(string query)
        {
            using var qr = Query(query);
            while (qr.HasNext())
            {
                var tuple = qr.GetNext();
                yield return tuple; // consumer responsible for disposing each FlatTuple
            }
        }

        /// <summary>
        /// Executes a query and projects each row using the supplied projector function.
        /// Caller is responsible for disposing any KuzuValue instances they pull from the FlatTuple inside the projector.
        /// </summary>
        public IEnumerable<T> QueryEnumerable<T>(string query, Func<FlatTuple, T> projector)
        {
            KuzuGuard.NotNull(projector, nameof(projector));
            using var qr = Query(query);
            while (qr.HasNext())
            {
                using var row = qr.GetNext();
                yield return projector(row);
            }
        }

        /// <summary>
        /// Sets the query timeout value in milliseconds for this connection.
        /// </summary>
        public void SetQueryTimeout(ulong timeoutMs)
        {
            NotDisposed();
            var state = NativeMethods.kuzu_connection_set_query_timeout(ref _handle.NativeStruct, timeoutMs);
            KuzuGuard.CheckSuccess(state, "Failed to set query timeout");
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            if (_handle.IsInvalid) return "Connection(Disposed)";
            var ptr = _handle.RawHandle;
            return string.Format(CultureInfo.InvariantCulture, "Connection(Ptr=0x{0:X})", ptr);
        }

        /// <summary>
        /// Attempts to prepare a statement capturing errors instead of throwing.
        /// </summary>
        public bool TryPrepare(string query, out PreparedStatement? statement, out string? errorMessage)
        {
            try
            {
                statement = Prepare(query);
                errorMessage = null;
                return true;
            }
            catch (KuzuException ex)
            {
                statement = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Attempts to execute a query capturing errors instead of throwing.
        /// </summary>
        public bool TryQuery(string query, out QueryResult? result, out string? errorMessage)
        {
            try
            {
                result = Query(query);
                errorMessage = null;
                return true;
            }
            catch (KuzuException ex)
            {
                result = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        internal QueryResult Execute(PreparedStatement preparedStatement)
        {
            KuzuGuard.NotNull(preparedStatement, nameof(preparedStatement));
            var haveInterceptors = KuzuInterceptorRegistry.HasGlobal;
            System.Diagnostics.Stopwatch? sw = null;
            if (haveInterceptors)
            {
                foreach (var i in KuzuInterceptorRegistry.Snapshot())
                    InterceptorSafeExecutor.Invoke(() => i.OnBeforeExecutePrepared(this, preparedStatement));
                sw = System.Diagnostics.Stopwatch.StartNew();
            }
            NotDisposed();
            var state = NativeMethods.kuzu_connection_execute(ref _handle.NativeStruct, ref preparedStatement.NativeStruct, out var qr);
            if (state != KuzuState.Success)
            {
                string details = preparedStatement.IsSuccess ? string.Empty : preparedStatement.ErrorMessage;
                if (!string.IsNullOrEmpty(details)) details = " Details: " + details;
                var ex = new KuzuException("Failed to execute prepared statement." + details);
                if (haveInterceptors)
                {
                    sw!.Stop();
                    foreach (var i in KuzuInterceptorRegistry.Snapshot())
                        InterceptorSafeExecutor.Invoke(() => i.OnAfterExecutePrepared(this, preparedStatement, null, ex, sw.Elapsed));
                }
                throw ex;
            }
            QueryResult? result = null;
            Exception? exCaught = null;
            try
            {
                result = new QueryResult(qr);
                return result;
            }
            catch (Exception ex)
            {
                exCaught = ex;
                throw;
            }
            finally
            {
                if (haveInterceptors)
                {
                    sw!.Stop();
                    foreach (var i in KuzuInterceptorRegistry.Snapshot())
                        InterceptorSafeExecutor.Invoke(() => i.OnAfterExecutePrepared(this, preparedStatement, result, exCaught, sw.Elapsed));
                }
            }
        }

        private void InterruptSafe()
        {
            try { Interrupt(); }
            catch (ObjectDisposedException) { }
            catch (KuzuException) { }
        }

        
        private void NotDisposed()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(Connection));
        }

        private sealed class ConnectionSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuConnection NativeStruct;

            internal ConnectionSafeHandle(NativeKuzuConnection native) : base("Connection")
            {
                NativeStruct = native;
                Initialize(native.Connection);
            }

            protected override void Release()
            {
                NativeMethods.kuzu_connection_destroy(ref NativeStruct);
                NativeStruct = default;
            }
        }
        private sealed class MaterializationCache<T> where T : new()
        {
            internal static readonly MaterializationCache<T> Instance = new MaterializationCache<T>();
            internal readonly Dictionary<string, System.Reflection.FieldInfo> FieldMap;
            internal readonly Dictionary<string, System.Reflection.PropertyInfo> PropMap;
            private MaterializationCache()
            {
                PropMap = new Dictionary<string, System.Reflection.PropertyInfo>(System.StringComparer.OrdinalIgnoreCase);
                FieldMap = new Dictionary<string, System.Reflection.FieldInfo>(System.StringComparer.OrdinalIgnoreCase);
                var type = typeof(T);
                var props = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var p in props)
                {
                    if (!p.CanWrite) continue;
                    var attr = (KuzuNameAttribute?)Attribute.GetCustomAttribute(p, typeof(KuzuNameAttribute));
                    var logical = attr?.Name ?? p.Name;
                    if (!PropMap.ContainsKey(logical)) PropMap[logical] = p; // first win
                }
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var f in fields)
                {
                    var attr = (KuzuNameAttribute?)Attribute.GetCustomAttribute(f, typeof(KuzuNameAttribute));
                    var logical = attr?.Name ?? f.Name;
                    if (!FieldMap.ContainsKey(logical)) FieldMap[logical] = f;
                }
            }
        }
    }
}