using KuzuDot.Enums;
using KuzuDot.Native;
using KuzuDot.Utils;
using KuzuDot.Value;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KuzuDot
{
    /// <summary>
    /// Represents a connection to a Kuzu database, used to execute queries and manage transactions.
    /// </summary>
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
        /// Adds a new column to the specified table with the given data type and optional default expression.
        /// </summary>
        /// <param name="tableName">The name of the table to which the column will be added. Cannot be null or empty.</param>
        /// <param name="columName">The name of the column to add. Cannot be null or empty.</param>
        /// <param name="typeId">The data type identifier for the new column.</param>
        /// <param name="defaultExpression">An optional default expression for the column. If null, the column will not have a default value.</param>
        public void AddColumn(string tableName, string columName, KuzuDataTypeId typeId, string? defaultExpression = null)
        {
            AddColumnInternal(tableName, columName, typeId, defaultExpression);
        }

        /// <summary>
        /// Adds a new column of the specified type to the given table, optionally setting a default value expression.
        /// </summary>
        /// <typeparam name="T">The data type of the column to add. Must be a supported type recognized by the data model.</typeparam>
        /// <param name="tableName">The name of the table to which the column will be added. Cannot be null or empty.</param>
        /// <param name="columName">The name of the column to add. Cannot be null or empty.</param>
        /// <param name="defaultExpression">An optional expression that defines the default value for the new column. If null, no default value is set.</param>
        public void AddColumn<T>(string tableName, string columName, string? defaultExpression = null)
        {
            var typeId = DataType.GetIdFromType(typeof(T));
            AddColumnInternal(tableName, columName, typeId, defaultExpression);
        }

        /// <summary>
        /// Adds a new column to the specified table if a column with the given name does not already exist.
        /// </summary>
        /// <remarks>If the column already exists in the specified table, no changes are made and the
        /// operation is ignored. This method is useful for schema migrations where adding duplicate columns should be
        /// avoided.</remarks>
        /// <param name="tableName">The name of the table to which the column will be added. Cannot be null or empty.</param>
        /// <param name="columName">The name of the column to add. Cannot be null or empty.</param>
        /// <param name="typeId">The data type identifier for the new column.</param>
        /// <param name="defaultExpression">An optional default expression to assign to the new column. If null, no default value is set.</param>
        public void AddColumnIfNotExists(string tableName, string columName, KuzuDataTypeId typeId, string? defaultExpression = null)
        {
            AddColumnInternal(tableName, columName, typeId, defaultExpression, ifNotExists: true);
        }

        /// <summary>
        /// Adds a connection (relationship) between two tables (nodes) in the database schema.
        /// Shorthand for "ALTER TABLE {label} ADD FROM {from} TO {to};"
        /// </summary>
        /// <param name="label">Relationship Label</param>
        /// <param name="from">From Node Table</param>
        /// <param name="to">To Node Table</param>
        public void AddConnection(string label, string from, string to)
        {
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            KuzuGuard.NotNullOrEmpty(from, nameof(from));
            KuzuGuard.NotNullOrEmpty(to, nameof(to));
            var query = $"ALTER TABLE {label} ADD FROM {from} TO {to};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to add connection on rel table '{label}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Adds a connection between two nodes in the specified relationship table if the connection does not already
        /// exist.
        /// </summary>
        /// <param name="label">The name of the relationship table to which the connection will be added. Cannot be null or empty.</param>
        /// <param name="from">The source node. Cannot be null or empty.</param>
        /// <param name="to">The target node. Cannot be null or empty.</param>
        /// <exception cref="KuzuException">Thrown if the connection could not be added.</exception>
        public void AddConnectionIfNotExists(string label, string from, string to)
        {
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            KuzuGuard.NotNullOrEmpty(from, nameof(from));
            KuzuGuard.NotNullOrEmpty(to, nameof(to));
            var query = $"ALTER TABLE {label} ADD IF NOT EXISTS FROM {from} TO {to};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to add connection on rel table '{label}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Alias for COMMENT ON TABLE {name} IS '{comment}'; query
        /// </summary>
        /// <param name="tableName"></param>
        public void CommentOnTable(string tableName, string comment)
        {
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            var query = $"COMMENT ON TABLE {tableName} IS '{comment}';";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to set comment on table '{tableName}': {result.ErrorMessage}");
        }

        public bool CreateNodeTable(string label, IEnumerable<SchemaProperty>? properties = null)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            var propsString = "";
            if (properties != null && properties.Any())
            {
                var propsList = properties.Select(kv => $"{kv.Name} {kv.Type}"
                                                      + (!string.IsNullOrEmpty(kv.DefaultExpression)
                                                        ? $" DEFAULT {kv.DefaultExpression}" : "")
                                                      + (kv.IsPrimaryKey ? " PRIMARY KEY" : ""));
                propsString = "{" + string.Join(", ", propsList) + "}";
            }
            var query = $"CREATE NODE TABLE {label}({propsString});";
            using var result = Query(query);
            return result.IsSuccess;
        }

        public bool CreateRelTable(string label, string from, string to, IEnumerable<SchemaProperty>? properties = null)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            KuzuGuard.NotNullOrEmpty(from, nameof(from));
            KuzuGuard.NotNullOrEmpty(to, nameof(to));
            var propsString = "";
            if (properties != null && properties.Any())
            {
                var propsList = properties.Select(kv => $"{kv.Name} {kv.Type}"
                                                      + (!string.IsNullOrEmpty(kv.DefaultExpression)
                                                        ? $" DEFAULT {kv.DefaultExpression}" : "")
                                                 );
                propsString = "{" + string.Join(", ", propsList) + "}";
            }
            var query = $"CREATE REL TABLE {label}(FROM {from} TO {to}, {propsString});";
            using var result = Query(query);
            return result.IsSuccess;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Connection"/>.
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
        }

        /// <summary>
        /// Removes a column from the specified table in the database schema.
        /// </summary>
        /// <param name="tableName">The name of the table from which the column will be removed. Cannot be null or empty.</param>
        /// <param name="columnName">The name of the column to remove from the table. Cannot be null.</param>
        /// <exception cref="KuzuException">Thrown if the column cannot be dropped from the specified table, such as when the operation fails or the
        /// table or column does not exist.</exception>
        public void DropColumn(string tableName, string columnName)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            KuzuGuard.NotNull(columnName, nameof(columnName));
            var query = $"ALTER TABLE {tableName} DROP {columnName};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to drop column '{columnName}' from table '{tableName}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Drops the specified column from the given table if the column exists.
        /// </summary>
        /// <param name="tableName">The name of the table from which the column will be dropped. Cannot be null or empty.</param>
        /// <param name="columnName">The name of the column to drop. Cannot be null.</param>
        /// <exception cref="KuzuException">Thrown if the operation fails to drop the column from the specified table.</exception>
        public void DropColumnIfExists(string tableName, string columnName)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            KuzuGuard.NotNull(columnName, nameof(columnName));
            var query = $"ALTER TABLE {tableName} DROP IF EXISTS {columnName};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to drop column '{columnName}' from table '{tableName}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Removes a connection between two nodes in the specified relationship table.
        /// </summary>
        /// <param name="label">The name of the relationship table.</param>
        /// <param name="from">The source node table in the connection to be removed.</param>
        /// <param name="to">The target node table in the connection to be removed.</param>
        /// <exception cref="KuzuException">Thrown if the connection cannot be dropped due to an error in the underlying database operation.</exception>
        public void DropConnection(string label, string from, string to)
        {
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            KuzuGuard.NotNullOrEmpty(from, nameof(from));
            KuzuGuard.NotNullOrEmpty(to, nameof(to));
            var query = $"ALTER TABLE {label} DROP FROM {from} TO {to};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to drop connection on rel table '{label}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Drops the specified table from the database schema.
        /// </summary>
        /// <param name="tableName">The name of the table to drop. Cannot be null or empty.</param>
        /// <param name="ifExists">Specifies whether to drop the table only if it exists. If <see langword="true"/>, no error is raised if the
        /// table does not exist; otherwise, an exception is thrown if the table is missing.</param>
        /// <exception cref="KuzuException">Thrown if the table cannot be dropped, such as when the table does not exist and <paramref name="ifExists"/>
        /// is <see langword="false"/>, or if a database error occurs.</exception>
        public void DropTable(string tableName, bool ifExists = false)
        {
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            var query = $"DROP TABLE " + (ifExists ? "IF EXISTS " : "") + $"{tableName};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to drop table '{tableName}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Executes a prepared statement asynchronously.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement to execute.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with a <see cref="QueryResult"/> as result.</returns>
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
        /// <exception cref="KuzuException">Thrown if the query fails inside KuzuDB.</exception>
        public bool ExecuteNonQuery(string query) => NonQuery(query);

        /// <summary>
        /// Executes a previously prepared (and optionally bound) statement returning a <see cref="QueryResult"/>.
        /// </summary>
        /// <param name="statement">The prepared statement to execute.</param>
        /// <returns>The query result.</returns>
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
        /// Retrieves a read-only list of schema connections for the specified relationship table label.
        /// </summary>
        /// <param name="label">The label of the relationship table for which to show connections. Cannot be null or empty.</param>
        /// <returns>An IReadOnlyList containing SchemaConnection objects that describe the source and target tables, along with
        /// their primary keys, for the specified relationship table. The list will be empty if no connections are
        /// found.</returns>
        /// <exception cref="KuzuException">Thrown if the operation fails to retrieve connections for the specified relationship table label.</exception>
        public IReadOnlyList<SchemaConnection> GetConnections(string label)
        {
            KuzuGuard.NotNullOrEmpty(label, nameof(label));
            var query = $"CALL show_connection('{label}') RETURN *;";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to show connections on rel table '{label}': {result.ErrorMessage}");
            var connections = new List<SchemaConnection>();
            while (result.HasNext())
            {
                using var row = result.GetNext();
                connections.Add(new SchemaConnection
                {
                    SourceTable = row.GetValueAs<string>(0),
                    TargetTable = row.GetValueAs<string>(1),
                    SourcePrimaryKey = row.GetValueAs<string>(2),
                    TargetPrimaryKey = row.GetValueAs<string>(3)
                });
            }
            return connections;
        }

        public IReadOnlyList<SchemaTable> GetNodeTables()
        {
            NotDisposed();
            using var results = Query("CALL show_tables() WHERE type = 'NODE' RETURN *;");
            return GetTablesFromResults(results);
        }

        /// <summary>
        /// Get the Relationship tables in the database
        /// </summary>
        /// <returns>List of Relationship tables</returns>
        public IReadOnlyList<SchemaTable> GetRelTables()
        {
            NotDisposed();
            using var results = Query("CALL show_tables() WHERE type = 'REL' RETURN *;");
            return GetTablesFromResults(results);
        }

        /// <summary>
        /// Get schema info for a given table by ID
        /// </summary>
        /// <param name="id">Table ID</param>
        /// <returns>Schema Info for the table</returns>
        public SchemaTable GetTableById(ulong id)
        {
            NotDisposed();
            using var ps = Prepare("CALL show_tables() WHERE id = $id RETURN *;");
            ps.BindUInt64(nameof(id), id);
            using var results = ps.Execute();
            return GetTablesFromResults(results).Single();
        }

        /// <summary>
        /// Retrieves the schema information for the table identified by the specified table ID.
        /// </summary>
        /// <param name="tableId">The unique identifier of the table for which to retrieve schema information.</param>
        /// <returns>A read-only list of properties describing the schema of the specified table. The list will be empty if the
        /// table has no properties.</returns>
        public IReadOnlyList<SchemaProperty> GetTableInfo(ulong tableId)
        {
            NotDisposed();
            var table = GetTableById(tableId);
            return GetTableInfo(table.Name!);
        }

        /// <summary>
        /// Retrieves column definitions and primary key status for a given table
        /// </summary>
        /// <param name="tableName">The name of the table. Cannot be null or empty.</param>
        /// <returns>A read-only list of <see cref="SchemaProperty"/>.</returns>
        /// <exception cref="KuzuException">Thrown if the schema information isn't found.</exception>
        public IReadOnlyList<SchemaProperty> GetTableInfo(string tableName)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));

            var columns = new List<SchemaProperty>();

            // Note: can't use prepared statement, table name must be literal, not param
            using var results = Query($"CALL table_info('{tableName}') RETURN *;");
            if (!results.IsSuccess) throw new KuzuException($"Failed to get table info for table '{tableName}': {results.ErrorMessage}");
            while (results.HasNext())
            {
                using var row = results.GetNext();
                columns.Add(new SchemaProperty
                {
                    Id = row.GetValueAs<int>(0),
                    Name = row.GetValueAs<string>(1),
                    Type = row.GetValueAs<string>(2),
                    DefaultExpression = row.GetValueAs<string>(3),
                    IsPrimaryKey = row.GetValueAs<bool>(4)
                });
            }
            return columns;
        }

        /// <summary>
        /// Retrieves list of all tables defined in the current database schema.
        /// </summary>
        /// <returns>List of <see cref="SchemaTable"/></returns>
        public IReadOnlyList<SchemaTable> GetTables()
        {
            NotDisposed();
            using var results = Query("CALL show_tables() RETURN *;");
            return GetTablesFromResults(results);
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

            ulong columnCount = result.ColumnCount;
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
                for (ulong c = 0; c < columnCount; c++)
                {
                    using var value = row.GetValue(c);
                    if (value.DataTypeId == KuzuDataTypeId.KuzuNode)
                    {
                        using var aKuzuNode = (KuzuNode)KuzuValue.FromNativeStruct(value.Handle.NativeStruct);
                        //if(aKuzuNode.Label==typeof(T).Name) // use this to match Label to <T> typeName
                        //{
                        foreach (var aProp in aKuzuNode.Properties)
                        {
                            if (propMap.TryGetValue(aProp.Key, out var propp))
                            {
                                try
                                {
                                    var converted = KuzuValue.ConvertKuzuValue(propp.PropertyType, aProp.Value);
                                    propp.SetValue(instance, converted);
                                }
                                catch (System.InvalidCastException) { }
                                catch (System.FormatException) { }
                                continue;
                            }
                            if (fieldMap.TryGetValue(aProp.Key, out var fieldd))
                            {
                                try
                                {
                                    var converted = KuzuValue.ConvertKuzuValue(fieldd.FieldType, aProp.Value);
                                    fieldd.SetValue(instance, converted);
                                }
                                catch (System.InvalidCastException) { }
                                catch (System.FormatException) { }
                            }
                        }
                        //}
                    }
                    else
                    {
                        var colName = columnNames[c];

                        // Try exact match first
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
                            continue;
                        }

                        // Try with stripped prefix (e.g., "p.name" -> "name")
                        var strippedName = StripColumnPrefix(colName);
                        if (strippedName != colName)
                        {
                            if (propMap.TryGetValue(strippedName, out prop))
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
                            if (fieldMap.TryGetValue(strippedName, out field))
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
            => Task.Run(() =>
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
        /// Renames an existing column in the specified table to a new name.
        /// </summary>
        /// <param name="tableName">The name of the table containing the column to rename. Cannot be null or empty.</param>
        /// <param name="oldColumnName">The current name of the column to be renamed. Cannot be null or empty.</param>
        /// <param name="newColumnName">The new name to assign to the column. Cannot be null or empty.</param>
        /// <exception cref="KuzuException">Thrown if the column cannot be renamed, such as when the table or column does not exist, or if the operation
        /// fails for any other reason.</exception>
        public void RenameColumn(string tableName, string oldColumnName, string newColumnName)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            KuzuGuard.NotNullOrEmpty(oldColumnName, nameof(oldColumnName));
            KuzuGuard.NotNullOrEmpty(newColumnName, nameof(newColumnName));
            var query = $"ALTER TABLE {tableName} RENAME {oldColumnName} TO {newColumnName};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to rename column '{oldColumnName}' to '{newColumnName}' on table '{tableName}': {result.ErrorMessage}");
        }

        /// <summary>
        /// Renames an existing table in the database to a new specified name.
        /// </summary>
        /// <param name="oldName">The current name of the table to be renamed.</param>
        /// <param name="newName">The new name to assign to the table.</param>
        /// <exception cref="KuzuException">Thrown if the table cannot be renamed, such as when the operation fails or the specified table does not
        /// exist.</exception>
        public void RenameTable(string oldName, string newName)
        {
            KuzuGuard.NotNullOrEmpty(oldName, nameof(oldName));
            KuzuGuard.NotNullOrEmpty(newName, nameof(newName));
            var query = $"ALTER TABLE {oldName} RENAME TO {newName};";
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to rename table '{oldName}' to '{newName}': {result.ErrorMessage}");
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

        private static List<SchemaTable> GetTablesFromResults(QueryResult results)
        {
            if (!results.IsSuccess)
                throw new KuzuException($"Failed to get table names: {results.ErrorMessage}");

            var tables = new List<SchemaTable>();

            while (results.HasNext())
            {
                using var row = results.GetNext();
                tables.Add(new SchemaTable
                {
                    Id = row.GetValueAs<ulong>(0),
                    Name = row.GetValueAs<string>(1),
                    Type = row.GetValueAs<string>(2),
                    Comment = row.GetValueAs<string>(3)
                });
            }

            return tables;
        }

        /// <summary>
        /// Strips common column prefixes from column names to improve POCO mapping.
        /// Examples: "p.name" -> "name", "a.age" -> "age", "user.email" -> "email"
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>")]
        private static string StripColumnPrefix(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return columnName;

            // Look for the first dot in the column name
            var dotIndex = columnName!.IndexOf('.');
            if (dotIndex > 0 && dotIndex < columnName.Length - 1)
            {
                // Extract the part after the dot
                return columnName.Substring(dotIndex + 1);
            }

            return columnName;
        }

        private void AddColumnInternal(string tableName, string columName, KuzuDataTypeId typeId, string? defaultExpression = null, bool ifNotExists = false)
        {
            NotDisposed();
            KuzuGuard.NotNullOrEmpty(tableName, nameof(tableName));
            KuzuGuard.NotNull(columName, nameof(columName));
            var query = $"ALTER TABLE {tableName} ADD "
                        + (ifNotExists ? "IF NOT EXISTS " : "")
                        + $"{columName} {DataType.GetNameFromType(typeId)}"
                        + (!string.IsNullOrEmpty(defaultExpression)
                            ? $" DEFAULT {defaultExpression};" : ";");
            using var result = Query(query);
            if (!result.IsSuccess)
                throw new KuzuException($"Failed to add column '{columName}' to table '{tableName}': {result.ErrorMessage}");
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
            internal static readonly MaterializationCache<T> Instance = new();
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

                    // Add both the logical name and snake_case version for backward compatibility
                    if (!PropMap.ContainsKey(logical)) PropMap[logical] = p; // first win

                    // Also add snake_case version if it's different
                    var snakeCase = ToSnakeCase(logical);
                    if (snakeCase != logical && !PropMap.ContainsKey(snakeCase))
                        PropMap[snakeCase] = p;
                }
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var f in fields)
                {
                    var attr = (KuzuNameAttribute?)Attribute.GetCustomAttribute(f, typeof(KuzuNameAttribute));
                    var logical = attr?.Name ?? f.Name;

                    // Add both the logical name and snake_case version for backward compatibility
                    if (!FieldMap.ContainsKey(logical)) FieldMap[logical] = f;

                    // Also add snake_case version if it's different
                    var snakeCase = ToSnakeCase(logical);
                    if (snakeCase != logical && !FieldMap.ContainsKey(snakeCase))
                        FieldMap[snakeCase] = f;
                }
            }

            private static string ToSnakeCase(string input)
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
        }
    }
}