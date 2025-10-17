using KuzuDot.Enums;
using KuzuDot.Utils;
using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Native
{
    /// <summary>
    /// Wrapper for Native Kuzu methods via P/Invoke
    /// See libkuzu/kuzu.h for reference
    /// </summary>
    internal static class NativeMethods
    {
        private const string DllName = "kuzu_shared.dll";

#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments


        // Database functions
        /// <summary>
        /// Allocates memory and creates a kuzu database instance at database_path with bufferPoolSize=buffer_pool_size. Caller is responsible for calling kuzu_database_destroy() to release the allocated memory.
        /// </summary>
        /// <param name="databasePath">The path to the database.</param>
        /// <param name="systemConfig">The runtime configuration for creating or opening the database.</param>
        /// <param name="outDatabase">The output parameter that will hold the database instance.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_database_init(const char* database_path, kuzu_system_config system_config, kuzu_database* out_database);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_database_init([MarshalAs(UnmanagedType.LPStr)] string databasePath,
            NativeKuzuSystemConfig systemConfig, out NativeKuzuDatabase outDatabase);

        /// <summary>
        /// Destroys the kuzu database instance and frees the allocated memory.
        /// </summary>
        /// <param name="database">The database instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_database_destroy(kuzu_database* database);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_database_destroy(ref NativeKuzuDatabase database);

        /// <summary>
        /// Returns the default system configuration for creating or opening a Database.
        /// </summary>
        /// <returns>The default system configuration.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_system_config kuzu_default_system_config();</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeKuzuSystemConfig kuzu_default_system_config();

        // Connection functions
        /// <summary>
        /// Allocates memory and creates a connection to the database. Caller is responsible for calling kuzu_connection_destroy() to release the allocated memory.
        /// </summary>
        /// <param name="database">The database instance to connect to.</param>
        /// <param name="outConnection">The output parameter that will hold the connection instance.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_init(kuzu_database* database, kuzu_connection* out_connection);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_init(ref NativeKuzuDatabase database, out NativeKuzuConnection outConnection);

        /// <summary>
        /// Destroys the connection instance and frees the allocated memory.
        /// </summary>
        /// <param name="connection">The connection instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_connection_destroy(kuzu_connection* connection);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_destroy(ref NativeKuzuConnection connection);

        /// <summary>
        /// Sets the maximum number of threads to use for executing queries.
        /// </summary>
        /// <param name="connection">The connection instance to set max number of threads for execution.</param>
        /// <param name="numThreads">The maximum number of threads to use for executing queries.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_set_max_num_thread_for_exec(kuzu_connection* connection, uint64_t num_threads);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_max_num_thread_for_exec(ref NativeKuzuConnection connection, ulong numThreads);

        /// <summary>
        /// Returns the maximum number of threads of the connection to use for executing queries.
        /// </summary>
        /// <param name="connection">The connection instance to return max number of threads for execution.</param>
        /// <param name="outResult">The output parameter that will hold the maximum number of threads to use for executing queries.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_get_max_num_thread_for_exec(kuzu_connection* connection, uint64_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_get_max_num_thread_for_exec(ref NativeKuzuConnection connection, out ulong outResult);

        /// <summary>
        /// Executes the given query and returns the result.
        /// </summary>
        /// <param name="connection">The connection instance to execute the query.</param>
        /// <param name="query">The query to execute.</param>
        /// <param name="outQueryResult">The output parameter that will hold the result of the query.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_query(kuzu_connection* connection, const char* query, kuzu_query_result* out_query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_query(ref NativeKuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out NativeKuzuQueryResult outQueryResult);

        /// <summary>
        /// Prepares the given query and returns the prepared statement.
        /// </summary>
        /// <param name="connection">The connection instance to prepare the query.</param>
        /// <param name="query">The query to prepare.</param>
        /// <param name="outPreparedStatement">The output parameter that will hold the prepared statement.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_prepare(kuzu_connection* connection, const char* query, kuzu_prepared_statement* out_prepared_statement);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_prepare(ref NativeKuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out NativeKuzuPreparedStatement outPreparedStatement);

        /// <summary>
        /// Executes the prepared_statement using connection.
        /// </summary>
        /// <param name="connection">The connection instance to execute the prepared_statement.</param>
        /// <param name="preparedStatement">The prepared statement to execute.</param>
        /// <param name="outQueryResult">The output parameter that will hold the result of the query.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_execute(kuzu_connection* connection, kuzu_prepared_statement* prepared_statement, kuzu_query_result* out_query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_execute(ref NativeKuzuConnection connection,
            ref NativeKuzuPreparedStatement preparedStatement, out NativeKuzuQueryResult outQueryResult);

        /// <summary>
        /// Interrupts the current query execution in the connection.
        /// </summary>
        /// <param name="connection">The connection instance to interrupt.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_connection_interrupt(kuzu_connection* connection);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_interrupt(ref NativeKuzuConnection connection);

        /// <summary>
        /// Sets query timeout value in milliseconds for the connection.
        /// </summary>
        /// <param name="connection">The connection instance to set query timeout value.</param>
        /// <param name="timeoutInMs">The timeout value in milliseconds.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_connection_set_query_timeout(kuzu_connection* connection, uint64_t timeout_in_ms);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_query_timeout(ref NativeKuzuConnection connection, ulong timeoutInMs);

        // Prepared Statement functions
        /// <summary>
        /// Destroys the prepared statement instance and frees the allocated memory.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_prepared_statement_destroy(kuzu_prepared_statement* prepared_statement);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_prepared_statement_destroy(ref NativeKuzuPreparedStatement preparedStatement);

        /// <summary>
        /// Returns whether the query is prepared successfully or not.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to check.</param>
        /// <returns>True if prepared successfully, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_prepared_statement_is_success(kuzu_prepared_statement* prepared_statement);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_prepared_statement_is_success(ref NativeKuzuPreparedStatement preparedStatement);

        /// <summary>
        /// Returns the error message if the prepared statement is not prepared successfully. The caller is responsible for freeing the returned string with kuzu_destroy_string.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance.</param>
        /// <returns>The error message if the statement is not prepared successfully or null if the statement is prepared successfully.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_prepared_statement_get_error_message(kuzu_prepared_statement* prepared_statement);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_prepared_statement_get_error_message(ref NativeKuzuPreparedStatement preparedStatement);

        // Prepared Statement bind functions - all data types
        /// <summary>
        /// Binds the given boolean value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The boolean value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_bool(kuzu_prepared_statement* prepared_statement, const char* param_name, bool value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_bool(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, bool value);

        /// <summary>
        /// Binds the given int64_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The int64_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_int64(kuzu_prepared_statement* prepared_statement, const char* param_name, int64_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int64(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, long value);

        /// <summary>
        /// Binds the given int32_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The int32_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_int32(kuzu_prepared_statement* prepared_statement, const char* param_name, int32_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int32(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, int value);

        /// <summary>
        /// Binds the given int16_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The int16_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_int16(kuzu_prepared_statement* prepared_statement, const char* param_name, int16_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int16(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, short value);

        /// <summary>
        /// Binds the given int8_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The int8_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_int8(kuzu_prepared_statement* prepared_statement, const char* param_name, int8_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int8(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, sbyte value);

        /// <summary>
        /// Binds the given uint64_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The uint64_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_uint64(kuzu_prepared_statement* prepared_statement, const char* param_name, uint64_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint64(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ulong value);

        /// <summary>
        /// Binds the given uint32_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The uint32_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_uint32(kuzu_prepared_statement* prepared_statement, const char* param_name, uint32_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint32(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, uint value);

        /// <summary>
        /// Binds the given uint16_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The uint16_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_uint16(kuzu_prepared_statement* prepared_statement, const char* param_name, uint16_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint16(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ushort value);

        /// <summary>
        /// Binds the given uint8_t value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The uint8_t value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_uint8(kuzu_prepared_statement* prepared_statement, const char* param_name, uint8_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint8(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, byte value);

        /// <summary>
        /// Binds the given double value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The double value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_double(kuzu_prepared_statement* prepared_statement, const char* param_name, double value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_double(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, double value);

        /// <summary>
        /// Binds the given float value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The float value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_float(kuzu_prepared_statement* prepared_statement, const char* param_name, float value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_float(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, float value);

        /// <summary>
        /// Binds the given date value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The date value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_date(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_date_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_date(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuDate value);

        /// <summary>
        /// Binds the given timestamp value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The timestamp value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_timestamp(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_timestamp_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestamp value);

        /// <summary>
        /// Binds the given timestamp_ns value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The timestamp_ns value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_timestamp_ns(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_timestamp_ns_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ns(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampNs value);

        /// <summary>
        /// Binds the given timestamp_ms value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The timestamp_ms value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_timestamp_ms(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_timestamp_ms_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ms(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampMs value);

        /// <summary>
        /// Binds the given timestamp_sec value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The timestamp_sec value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_timestamp_sec(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_timestamp_sec_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_sec(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampSec value);

        /// <summary>
        /// Binds the given timestamp_tz value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The timestamp_tz value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_timestamp_tz(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_timestamp_tz_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_tz(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampTz value);

        /// <summary>
        /// Binds the given interval value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The interval value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_interval(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_interval_t value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_interval(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuInterval value);

        /// <summary>
        /// Binds the given string value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The string value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_string(kuzu_prepared_statement* prepared_statement, const char* param_name, const char* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_string(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, [MarshalAs(UnmanagedType.LPStr)] string value);

        /// <summary>
        /// Binds the given kuzu value to the given parameter name in the prepared statement.
        /// </summary>
        /// <param name="preparedStatement">The prepared statement instance to bind the value.</param>
        /// <param name="paramName">The parameter name to bind the value.</param>
        /// <param name="value">The kuzu value to bind.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_prepared_statement_bind_value(kuzu_prepared_statement* prepared_statement, const char* param_name, kuzu_value* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_value(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, IntPtr value);

        /// <summary>
        /// Destroys the given query result instance.
        /// </summary>
        /// <param name="queryResult">The query result instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_query_result_destroy(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_destroy(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns true if the query is executed successful, false otherwise.
        /// </summary>
        /// <param name="queryResult">The query result instance to check.</param>
        /// <returns>True if the query is executed successfully, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_query_result_is_success(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_is_success(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the error message if the query is failed. The caller is responsible for freeing the returned string with kuzu_destroy_string.
        /// </summary>
        /// <param name="queryResult">The query result instance to check and return error message.</param>
        /// <returns>The error message if the query has failed, or null if the query is successful.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_query_result_get_error_message(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_get_error_message(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the number of columns in the query result.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <returns>The number of columns in the query result.</returns>
        /// <remarks>Original C signature: KUZU_C_API uint64_t kuzu_query_result_get_num_columns(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_columns(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the column name at the given index.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="index">The index of the column to return name.</param>
        /// <param name="outColumnName">The output parameter that will hold the column name.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_column_name(kuzu_query_result* query_result, uint64_t index, char** out_column_name);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_name(ref NativeKuzuQueryResult queryResult,
            ulong index, out IntPtr outColumnName);

        /// <summary>
        /// Returns the data type of the column at the given index.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="index">The index of the column to return data type.</param>
        /// <param name="outColumnDataType">The output parameter that will hold the column data type.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_column_data_type(kuzu_query_result* query_result, uint64_t index, kuzu_logical_type* out_column_data_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_data_type(ref NativeKuzuQueryResult queryResult,
            ulong index, out NativeKuzuLogicalType outColumnDataType);

        /// <summary>
        /// Returns the number of tuples in the query result.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <returns>The number of tuples in the query result.</returns>
        /// <remarks>Original C signature: KUZU_C_API uint64_t kuzu_query_result_get_num_tuples(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_tuples(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns true if we have not consumed all tuples in the query result, false otherwise.
        /// </summary>
        /// <param name="queryResult">The query result instance to check.</param>
        /// <returns>True if there are more tuples, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_query_result_has_next(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the next tuple in the query result. Throws an exception if there is no more tuple. Note that to reduce resource allocation, all calls to kuzu_query_result_get_next() reuse the same FlatTuple object. Since its contents will be overwritten, please complete processing a FlatTuple or make a copy of its data before calling kuzu_query_result_get_next() again.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="outFlatTuple">The output parameter that will hold the next tuple.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_next(kuzu_query_result* query_result, kuzu_flat_tuple* out_flat_tuple);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next(ref NativeKuzuQueryResult queryResult, out NativeKuzuFlatTuple outFlatTuple);

        /// <summary>
        /// Returns true if we have not consumed all query results, false otherwise. Use this function for loop results of multiple query statements.
        /// </summary>
        /// <param name="queryResult">The query result instance to check.</param>
        /// <returns>True if there are more query results, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_query_result_has_next_query_result(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next_query_result(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the next query result. Use this function to loop multiple query statements' results.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="outNextQueryResult">The output parameter that will hold the next query result.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_next_query_result(kuzu_query_result* query_result, kuzu_query_result* out_next_query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_query_result(ref NativeKuzuQueryResult queryResult,
            out NativeKuzuQueryResult outNextQueryResult);

        /// <summary>
        /// Returns the query result as a string.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <returns>The query result as a string.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_query_result_to_string(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_to_string(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Resets the iterator of the query result to the beginning of the query result.
        /// </summary>
        /// <param name="queryResult">The query result instance to reset iterator.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_query_result_reset_iterator(kuzu_query_result* query_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_reset_iterator(ref NativeKuzuQueryResult queryResult);

        /// <summary>
        /// Returns the query summary of the query result.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="outQuerySummary">The output parameter that will hold the query summary.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_query_summary(kuzu_query_result* query_result, kuzu_query_summary* out_query_summary);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_query_summary(ref NativeKuzuQueryResult queryResult,
            out NativeKuzuQuerySummary outQuerySummary);

        /// <summary>
        /// Returns the query result's schema as ArrowSchema.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="outSchema">The output parameter that will hold the datatypes of the columns as an arrow schema.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_arrow_schema(kuzu_query_result* query_result, struct ArrowSchema* out_schema);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_arrow_schema(ref NativeKuzuQueryResult queryResult,
            out ArrowSchema outSchema);

        /// <summary>
        /// Returns the next chunk of the query result as ArrowArray.
        /// </summary>
        /// <param name="queryResult">The query result instance to return.</param>
        /// <param name="chunkSize">The number of tuples to return in the chunk.</param>
        /// <param name="outArrowArray">The output parameter that will hold the arrow array representation of the query result.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_query_result_get_next_arrow_chunk(kuzu_query_result* query_result, int64_t chunk_size, struct ArrowArray* out_arrow_array);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_arrow_chunk(ref NativeKuzuQueryResult queryResult,
            long chunkSize, out ArrowArray outArrowArray);

        /// <summary>
        /// Destroys the given flat tuple instance.
        /// </summary>
        /// <param name="flatTuple">The flat tuple instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_flat_tuple_destroy(kuzu_flat_tuple* flat_tuple);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_flat_tuple_destroy(ref NativeKuzuFlatTuple flatTuple);

        /// <summary>
        /// Returns the value at index of the flat tuple.
        /// </summary>
        /// <param name="flatTuple">The flat tuple instance to return.</param>
        /// <param name="index">The index of the value to return.</param>
        /// <param name="outValue">The output parameter that will hold the value at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_flat_tuple_get_value(kuzu_flat_tuple* flat_tuple, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_flat_tuple_get_value(ref NativeKuzuFlatTuple flatTuple, ulong index, out NativeKuzuValue outValue);

        /// <summary>
        /// Converts the flat tuple to a string.
        /// </summary>
        /// <param name="flatTuple">The flat tuple instance to convert.</param>
        /// <returns>The flat tuple as a string.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_flat_tuple_to_string(kuzu_flat_tuple* flat_tuple);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_flat_tuple_to_string(ref NativeKuzuFlatTuple flatTuple);

        /// <summary>
        /// Creates a data type instance with the given id, childType and num_elements_in_array. Caller is responsible for destroying the returned data type instance.
        /// </summary>
        /// <param name="id">The enum type id of the datatype to create.</param>
        /// <param name="childType">The child type of the datatype to create (only used for nested dataTypes).</param>
        /// <param name="numElementsInArray">The number of elements in the array (only used for ARRAY).</param>
        /// <param name="outType">The output parameter that will hold the data type instance.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_data_type_create(kuzu_data_type_id id, kuzu_logical_type* child_type, uint64_t num_elements_in_array, kuzu_logical_type* out_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_create(KuzuDataTypeId id, ref NativeKuzuLogicalType childType,
            ulong numElementsInArray, out NativeKuzuLogicalType outType);

        /// <summary>
        /// Creates a new data type instance by cloning the given data type instance.
        /// </summary>
        /// <param name="dataType">The data type instance to clone.</param>
        /// <param name="outType">The output parameter that will hold the cloned data type instance.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_data_type_clone(kuzu_logical_type* data_type, kuzu_logical_type* out_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_clone(ref NativeKuzuLogicalType dataType, out NativeKuzuLogicalType outType);

        /// <summary>
        /// Destroys the given data type instance.
        /// </summary>
        /// <param name="dataType">The data type instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_data_type_destroy(kuzu_logical_type* data_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_destroy(ref NativeKuzuLogicalType dataType);

        /// <summary>
        /// Returns true if the given data type is equal to the other data type, false otherwise.
        /// </summary>
        /// <param name="dataType1">The first data type instance to compare.</param>
        /// <param name="dataType2">The second data type instance to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_data_type_equals(kuzu_logical_type* data_type1, kuzu_logical_type* data_type2);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_data_type_equals(ref NativeKuzuLogicalType dataType1, ref NativeKuzuLogicalType dataType2);

        /// <summary>
        /// Returns the enum type id of the given data type.
        /// </summary>
        /// <param name="dataType">The data type instance to return.</param>
        /// <returns>The enum type id of the given data type.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_data_type_id kuzu_data_type_get_id(kuzu_logical_type* data_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuDataTypeId kuzu_data_type_get_id(ref NativeKuzuLogicalType dataType);

        /// <summary>
        /// Returns the number of elements for array.
        /// </summary>
        /// <param name="dataType">The data type instance to return.</param>
        /// <param name="outResult">The output parameter that will hold the number of elements in the array.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_data_type_get_num_elements_in_array(kuzu_logical_type* data_type, uint64_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_data_type_get_num_elements_in_array(ref NativeKuzuLogicalType dataType, out ulong outResult);

        /// <summary>
        /// Creates a NULL value of ANY type. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <returns>A pointer to the created NULL value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_null();</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null();

        /// <summary>
        /// Creates a value of the given data type. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="dataType">The data type of the value to create.</param>
        /// <returns>A pointer to the created NULL value with the given data type.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_null_with_data_type(kuzu_logical_type* data_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null_with_data_type(ref NativeKuzuLogicalType dataType);

        /// <summary>
        /// Creates a value of the given data type with default non-NULL value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="dataType">The data type of the value to create.</param>
        /// <returns>A pointer to the created default value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_default(kuzu_logical_type* data_type);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_default(ref NativeKuzuLogicalType dataType);

        /// <summary>
        /// Creates a value with boolean type and the given bool value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The bool value of the value to create.</param>
        /// <returns>A pointer to the created boolean value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_bool(bool val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_bool(bool value);

        /// <summary>
        /// Creates a value with int8 type and the given int8 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The int8 value of the value to create.</param>
        /// <returns>A pointer to the created int8 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_int8(int8_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int8(sbyte value);

        /// <summary>
        /// Creates a value with int16 type and the given int16 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The int16 value of the value to create.</param>
        /// <returns>A pointer to the created int16 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_int16(int16_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int16(short value);

        /// <summary>
        /// Creates a value with int32 type and the given int32 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The int32 value of the value to create.</param>
        /// <returns>A pointer to the created int32 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_int32(int32_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int32(int value);

        /// <summary>
        /// Creates a value with int64 type and the given int64 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The int64 value of the value to create.</param>
        /// <returns>A pointer to the created int64 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_int64(int64_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int64(long value);

        /// <summary>
        /// Creates a value with uint8 type and the given uint8 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The uint8 value of the value to create.</param>
        /// <returns>A pointer to the created uint8 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_uint8(uint8_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint8(byte value);

        /// <summary>
        /// Creates a value with uint16 type and the given uint16 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The uint16 value of the value to create.</param>
        /// <returns>A pointer to the created uint16 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_uint16(uint16_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint16(ushort value);

        /// <summary>
        /// Creates a value with uint32 type and the given uint32 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The uint32 value of the value to create.</param>
        /// <returns>A pointer to the created uint32 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_uint32(uint32_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint32(uint value);

        /// <summary>
        /// Creates a value with uint64 type and the given uint64 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The uint64 value of the value to create.</param>
        /// <returns>A pointer to the created uint64 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_uint64(uint64_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint64(ulong value);

        /// <summary>
        /// Creates a value with int128 type and the given int128 value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The int128 value of the value to create.</param>
        /// <returns>A pointer to the created int128 value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_int128(kuzu_int128_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int128(NativeKuzuInt128 value);

        /// <summary>
        /// Creates a value with float type and the given float value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The float value of the value to create.</param>
        /// <returns>A pointer to the created float value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_float(float val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_float(float value);

        /// <summary>
        /// Creates a value with double type and the given double value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The double value of the value to create.</param>
        /// <returns>A pointer to the created double value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_double(double val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_double(double value);

        /// <summary>
        /// Creates a value with internal_id type and the given internal_id value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The internal_id value of the value to create.</param>
        /// <returns>A pointer to the created internal_id value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_internal_id(kuzu_internal_id_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_internal_id(NativeKuzuInternalId value);

        /// <summary>
        /// Creates a value with date type and the given date value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The date value of the value to create.</param>
        /// <returns>A pointer to the created date value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_date(kuzu_date_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_date(NativeKuzuDate value);

        /// <summary>
        /// Creates a value with timestamp type and the given timestamp value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The timestamp value of the value to create.</param>
        /// <returns>A pointer to the created timestamp value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_timestamp(kuzu_timestamp_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp(NativeKuzuTimestamp value);

        /// <summary>
        /// Creates a value with timestamp_ns type and the given timestamp_ns value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The timestamp_ns value of the value to create.</param>
        /// <returns>A pointer to the created timestamp_ns value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_timestamp_ns(kuzu_timestamp_ns_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ns(NativeKuzuTimestampNs value);

        /// <summary>
        /// Creates a value with timestamp_ms type and the given timestamp_ms value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The timestamp_ms value of the value to create.</param>
        /// <returns>A pointer to the created timestamp_ms value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_timestamp_ms(kuzu_timestamp_ms_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ms(NativeKuzuTimestampMs value);

        /// <summary>
        /// Creates a value with timestamp_sec type and the given timestamp_sec value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The timestamp_sec value of the value to create.</param>
        /// <returns>A pointer to the created timestamp_sec value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_timestamp_sec(kuzu_timestamp_sec_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_sec(NativeKuzuTimestampSec value);

        /// <summary>
        /// Creates a value with timestamp_tz type and the given timestamp_tz value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The timestamp_tz value of the value to create.</param>
        /// <returns>A pointer to the created timestamp_tz value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_timestamp_tz(kuzu_timestamp_tz_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_tz(NativeKuzuTimestampTz value);

        /// <summary>
        /// Creates a value with interval type and the given interval value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The interval value of the value to create.</param>
        /// <returns>A pointer to the created interval value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_interval(kuzu_interval_t val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_interval(NativeKuzuInterval value);

        /// <summary>
        /// Creates a value with string type and the given string value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The string value of the value to create.</param>
        /// <returns>A pointer to the created string value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_string(const char* val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr kuzu_value_create_string([MarshalAs(UnmanagedType.LPStr)] string value);

        /// <summary>
        /// Creates a value with string type from a UTF-8 encoded string. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="utf8Value">A pointer to the UTF-8 encoded string.</param>
        /// <returns>A pointer to the created string value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_create_string(const char* val_);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "kuzu_value_create_string")]
        internal static extern IntPtr kuzu_value_create_string_from_utf8(IntPtr utf8Value);

        /// <summary>
        /// Creates a list value with the given number of elements and the given element values. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="numElements">The number of elements in the list.</param>
        /// <param name="elements">A pointer to the array of element values (kuzu_value**).</param>
        /// <param name="outValue">The output parameter that will hold a pointer to the created list value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_create_list(uint64_t num_elements, kuzu_value** elements, kuzu_value** out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_list(ulong numElements, IntPtr elements /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        /// <summary>
        /// Creates a struct value with the given number of fields and the given field names and values. The caller needs to make sure that all field names are unique. The field names and values are copied into the struct value, so destroying the field names and values after creating the struct value is safe. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="numFields">The number of fields in the struct.</param>
        /// <param name="fieldNames">The field names of the struct.</param>
        /// <param name="fieldValues">The field values of the struct.</param>
        /// <param name="outValue">The output parameter that will hold a pointer to the created struct value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_create_struct(uint64_t num_fields, const char** field_names, kuzu_value** field_values, kuzu_value** out_value);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_struct(ulong numFields, IntPtr fieldNames /* const char** */, IntPtr fieldValues /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        /// <summary>
        /// Creates a map value with the given number of fields and the given keys and values. The caller needs to make sure that all keys are unique, and all keys and values have the same type. The keys and values are copied into the map value, so destroying the keys and values after creating the map value is safe. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="numFields">The number of fields in the map.</param>
        /// <param name="keys">The keys of the map.</param>
        /// <param name="values">The values of the map.</param>
        /// <param name="outValue">The output parameter that will hold a pointer to the created map value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_create_map(uint64_t num_fields, kuzu_value** keys, kuzu_value** values, kuzu_value** out_value);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_map(ulong numFields, IntPtr keys /* kuzu_value** */, IntPtr values /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        /// <summary>
        /// Returns true if the given value is NULL, false otherwise.
        /// </summary>
        /// <param name="value">The value to check for NULL.</param>
        /// <returns>True if the value is NULL, false otherwise.</returns>
        /// <remarks>Original C signature: KUZU_C_API bool kuzu_value_is_null(kuzu_value* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_value_is_null(ref NativeKuzuValue value);

        /// <summary>
        /// Sets the given value to NULL or not.
        /// </summary>
        /// <param name="value">The value instance to set.</param>
        /// <param name="isNull">True if sets the value to NULL, false otherwise.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_value_set_null(kuzu_value* value, bool is_null);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_set_null(ref NativeKuzuValue value, bool isNull);

        /// <summary>
        /// Returns internal type of the given value.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outType">The output parameter that will hold the internal type of the value.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_value_get_data_type(kuzu_value* value, kuzu_logical_type* out_type);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_get_data_type(ref NativeKuzuValue value, out NativeKuzuLogicalType outType);

        /// <summary>
        /// Returns the boolean value of the given value. The value must be of type BOOL.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the boolean value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_bool(kuzu_value* value, bool* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_bool(ref NativeKuzuValue value, out bool outResult);

        /// <summary>
        /// Returns the int8 value of the given value. The value must be of type INT8.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the int8 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_int8(kuzu_value* value, int8_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int8(ref NativeKuzuValue value, out sbyte outResult);

        /// <summary>
        /// Returns the int16 value of the given value. The value must be of type INT16.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the int16 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_int16(kuzu_value* value, int16_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int16(ref NativeKuzuValue value, out short outResult);

        /// <summary>
        /// Returns the int32 value of the given value. The value must be of type INT32.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the int32 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_int32(kuzu_value* value, int32_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int32(ref NativeKuzuValue value, out int outResult);

        /// <summary>
        /// Returns the int64 value of the given value. The value must be of type INT64 or SERIAL.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the int64 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_int64(kuzu_value* value, int64_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int64(ref NativeKuzuValue value, out long outResult);

        /// <summary>
        /// Returns the uint8 value of the given value. The value must be of type UINT8.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the uint8 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_uint8(kuzu_value* value, uint8_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint8(ref NativeKuzuValue value, out byte outResult);

        /// <summary>
        /// Returns the uint16 value of the given value. The value must be of type UINT16.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the uint16 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_uint16(kuzu_value* value, uint16_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint16(ref NativeKuzuValue value, out ushort outResult);

        /// <summary>
        /// Returns the uint32 value of the given value. The value must be of type UINT32.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the uint32 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_uint32(kuzu_value* value, uint32_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint32(ref NativeKuzuValue value, out uint outResult);

        /// <summary>
        /// Returns the uint64 value of the given value. The value must be of type UINT64.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the uint64 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_uint64(kuzu_value* value, uint64_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint64(ref NativeKuzuValue value, out ulong outResult);

        /// <summary>
        /// Returns the int128 value of the given value. The value must be of type INT128.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the int128 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_int128(kuzu_value* value, kuzu_int128_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int128(ref NativeKuzuValue value, out NativeKuzuInt128 outResult);

        /// <summary>
        /// Returns the float value of the given value. The value must be of type FLOAT.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the float value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_float(kuzu_value* value, float* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_float(ref NativeKuzuValue value, out float outResult);

        /// <summary>
        /// Returns the double value of the given value. The value must be of type DOUBLE.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the double value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_double(kuzu_value* value, double* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_double(ref NativeKuzuValue value, out double outResult);

        /// <summary>
        /// Returns the internal id value of the given value. The value must be of type INTERNAL_ID.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the internal id value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_internal_id(kuzu_value* value, kuzu_internal_id_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_internal_id(ref NativeKuzuValue value, out NativeKuzuInternalId outResult);

        /// <summary>
        /// Returns the date value of the given value. The value must be of type DATE.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the date value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_date(kuzu_value* value, kuzu_date_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_date(ref NativeKuzuValue value, out NativeKuzuDate outResult);

        /// <summary>
        /// Returns the timestamp value of the given value. The value must be of type TIMESTAMP.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_timestamp(kuzu_value* value, kuzu_timestamp_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp(ref NativeKuzuValue value, out NativeKuzuTimestamp outResult);

        /// <summary>
        /// Returns the timestamp_ns value of the given value. The value must be of type TIMESTAMP_NS.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_ns value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_timestamp_ns(kuzu_value* value, kuzu_timestamp_ns_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ns(ref NativeKuzuValue value, out NativeKuzuTimestampNs outResult);

        /// <summary>
        /// Returns the timestamp_ms value of the given value. The value must be of type TIMESTAMP_MS.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_ms value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_timestamp_ms(kuzu_value* value, kuzu_timestamp_ms_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ms(ref NativeKuzuValue value, out NativeKuzuTimestampMs outResult);

        /// <summary>
        /// Returns the timestamp_sec value of the given value. The value must be of type TIMESTAMP_SEC.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_sec value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_timestamp_sec(kuzu_value* value, kuzu_timestamp_sec_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_sec(ref NativeKuzuValue value, out NativeKuzuTimestampSec outResult);

        /// <summary>
        /// Returns the timestamp_tz value of the given value. The value must be of type TIMESTAMP_TZ.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_tz value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_timestamp_tz(kuzu_value* value, kuzu_timestamp_tz_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_tz(ref NativeKuzuValue value, out NativeKuzuTimestampTz outResult);

        /// <summary>
        /// Returns the interval value of the given value. The value must be of type INTERVAL.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the interval value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_interval(kuzu_value* value, kuzu_interval_t* out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_interval(ref NativeKuzuValue value, out NativeKuzuInterval outResult);

        /// <summary>
        /// Returns the string value of the given value. The value must be of type STRING.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the string value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_string(kuzu_value* value, char** out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_string(ref NativeKuzuValue value, out IntPtr outResult);

        /// <summary>
        /// Returns the blob value of the given value. The returned buffer is null-terminated similar to a string. The value must be of type BLOB.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the blob value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_blob(kuzu_value* value, uint8_t** out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_blob(ref NativeKuzuValue value, out IntPtr outResult);

        /// <summary>
        /// Returns the decimal value of the given value as a string. The value must be of type DECIMAL.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the decimal value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_decimal_as_string(kuzu_value* value, char** out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_decimal_as_string(ref NativeKuzuValue value, out IntPtr outResult);

        /// <summary>
        /// Returns the uuid value of the given value. The value must be of type UUID.
        /// </summary>
        /// <param name="value">The value to return.</param>
        /// <param name="outResult">The output parameter that will hold the uuid value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_uuid(kuzu_value* value, char** out_result);</remarks>

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uuid(ref NativeKuzuValue value, out IntPtr outResult);

        // List/Array functions
        /// <summary>
        /// Returns the number of elements per list of the given value. The value must be of type ARRAY.
        /// </summary>
        /// <param name="value">The ARRAY value to get list size.</param>
        /// <param name="outResult">The output parameter that will hold the number of elements per list.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_list_size(kuzu_value* value, uint64_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_size(ref NativeKuzuValue value, out ulong outResult);

        /// <summary>
        /// Returns the element at index of the given value. The value must be of type LIST.
        /// </summary>
        /// <param name="value">The LIST value to return.</param>
        /// <param name="index">The index of the element to return.</param>
        /// <param name="outValue">The output parameter that will hold the element at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_list_element(kuzu_value* value, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_element(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Struct functions
        /// <summary>
        /// Returns the number of fields of the given struct value. The value must be of type STRUCT.
        /// </summary>
        /// <param name="value">The STRUCT value to get number of fields.</param>
        /// <param name="outResult">The output parameter that will hold the number of fields.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_struct_num_fields(kuzu_value* value, uint64_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_num_fields(ref NativeKuzuValue value, out ulong outResult);

        /// <summary>
        /// Returns the field name at index of the given struct value. The value must be of physical type STRUCT (STRUCT, NODE, REL, RECURSIVE_REL, UNION).
        /// </summary>
        /// <param name="value">The STRUCT value to get field name.</param>
        /// <param name="index">The index of the field name to return.</param>
        /// <param name="outResult">The output parameter that will hold the field name at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_struct_field_name(kuzu_value* value, uint64_t index, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_name(ref NativeKuzuValue value, ulong index, out IntPtr outResult);

        /// <summary>
        /// Returns the field value at index of the given struct value. The value must be of physical type STRUCT (STRUCT, NODE, REL, RECURSIVE_REL, UNION).
        /// </summary>
        /// <param name="value">The STRUCT value to get field value.</param>
        /// <param name="index">The index of the field value to return.</param>
        /// <param name="outValue">The output parameter that will hold the field value at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_struct_field_value(kuzu_value* value, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_value(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Map functions
        /// <summary>
        /// Returns the size of the given map value. The value must be of type MAP.
        /// </summary>
        /// <param name="value">The MAP value to get size.</param>
        /// <param name="outResult">The output parameter that will hold the size of the map.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_map_size(kuzu_value* value, uint64_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_size(ref NativeKuzuValue value, out ulong outResult);

        /// <summary>
        /// Returns the key at index of the given map value. The value must be of physical type MAP.
        /// </summary>
        /// <param name="value">The MAP value to get key.</param>
        /// <param name="index">The index of the field name to return.</param>
        /// <param name="outKey">The output parameter that will hold the key at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_map_key(kuzu_value* value, uint64_t index, kuzu_value* out_key);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_key(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outKey);

        /// <summary>
        /// Returns the field value at index of the given map value. The value must be of physical type MAP.
        /// </summary>
        /// <param name="value">The MAP value to get field value.</param>
        /// <param name="index">The index of the field value to return.</param>
        /// <param name="outValue">The output parameter that will hold the field value at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_map_value(kuzu_value* value, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_value(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Recursive rel functions
        /// <summary>
        /// Returns the list of nodes for recursive rel value. The value must be of type RECURSIVE_REL.
        /// </summary>
        /// <param name="value">The RECURSIVE_REL value to return.</param>
        /// <param name="outValue">The output parameter that will hold the list of nodes.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_recursive_rel_node_list(kuzu_value* value, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_node_list(ref NativeKuzuValue value, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the list of rels for recursive rel value. The value must be of type RECURSIVE_REL.
        /// </summary>
        /// <param name="value">The RECURSIVE_REL value to return.</param>
        /// <param name="outValue">The output parameter that will hold the list of rels.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_value_get_recursive_rel_rel_list(kuzu_value* value, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_rel_list(ref NativeKuzuValue value, out NativeKuzuValue outValue);

        // Node value helpers
        /// <summary>
        /// Returns the internal id value of the given node value as a kuzu value.
        /// </summary>
        /// <param name="nodeVal">The node value to return.</param>
        /// <param name="outValue">The output parameter that will hold the internal id value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_get_id_val(kuzu_value* node_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_id_val(ref NativeKuzuValue nodeVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the label value of the given node value as a label value.
        /// </summary>
        /// <param name="nodeVal">The node value to return.</param>
        /// <param name="outValue">The output parameter that will hold the label value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_get_label_val(kuzu_value* node_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_label_val(ref NativeKuzuValue nodeVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the number of properties of the given node value.
        /// </summary>
        /// <param name="nodeVal">The node value to return.</param>
        /// <param name="outValue">The output parameter that will hold the number of properties.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_get_property_size(kuzu_value* node_val, uint64_t* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_size(ref NativeKuzuValue nodeVal, out ulong outValue);

        /// <summary>
        /// Returns the property name of the given node value at the given index.
        /// </summary>
        /// <param name="nodeVal">The node value to return.</param>
        /// <param name="index">The index of the property.</param>
        /// <param name="outResult">The output parameter that will hold the property name at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_get_property_name_at(kuzu_value* node_val, uint64_t index, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_name_at(ref NativeKuzuValue nodeVal, ulong index, out IntPtr outResult);

        /// <summary>
        /// Returns the property value of the given node value at the given index.
        /// </summary>
        /// <param name="nodeVal">The node value to return.</param>
        /// <param name="index">The index of the property.</param>
        /// <param name="outValue">The output parameter that will hold the property value at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_get_property_value_at(kuzu_value* node_val, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_value_at(ref NativeKuzuValue nodeVal, ulong index, out NativeKuzuValue outValue);

        /// <summary>
        /// Converts the given node value to string.
        /// </summary>
        /// <param name="nodeVal">The node value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the node value as a string.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_node_val_to_string(kuzu_value* node_val, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_to_string(ref NativeKuzuValue nodeVal, out IntPtr outResult);

        // Rel value helpers
        /// <summary>
        /// Returns the internal id value of the rel value as a kuzu value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="outValue">The output parameter that will hold the internal id value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_id_val(kuzu_value* rel_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the internal id value of the source node of the given rel value as a kuzu value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="outValue">The output parameter that will hold the internal id value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_src_id_val(kuzu_value* rel_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_src_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the internal id value of the destination node of the given rel value as a kuzu value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="outValue">The output parameter that will hold the internal id value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_dst_id_val(kuzu_value* rel_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_dst_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the label value of the given rel value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="outValue">The output parameter that will hold the label value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_label_val(kuzu_value* rel_val, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_label_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        /// <summary>
        /// Returns the number of properties of the given rel value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="outValue">The output parameter that will hold the number of properties.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_property_size(kuzu_value* rel_val, uint64_t* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_size(ref NativeKuzuValue relVal, out ulong outValue);

        /// <summary>
        /// Returns the property name of the given rel value at the given index.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="index">The index of the property.</param>
        /// <param name="outResult">The output parameter that will hold the property name at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_property_name_at(kuzu_value* rel_val, uint64_t index, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_name_at(ref NativeKuzuValue relVal, ulong index, out IntPtr outResult);

        /// <summary>
        /// Returns the property of the given rel value at the given index as kuzu value.
        /// </summary>
        /// <param name="relVal">The rel value to return.</param>
        /// <param name="index">The index of the property.</param>
        /// <param name="outValue">The output parameter that will hold the property value at index.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_get_property_value_at(kuzu_value* rel_val, uint64_t index, kuzu_value* out_value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_value_at(ref NativeKuzuValue relVal, ulong index, out NativeKuzuValue outValue);

        /// <summary>
        /// Converts the given rel value to string.
        /// </summary>
        /// <param name="relVal">The rel value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the rel value as a string.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_rel_val_to_string(kuzu_value* rel_val, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_to_string(ref NativeKuzuValue relVal, out IntPtr outResult);

        // Value utility functions
        /// <summary>
        /// Clones the given value and returns a pointer to the new value. Caller is responsible for destroying the returned value.
        /// </summary>
        /// <param name="value">The value to clone.</param>
        /// <returns>A pointer to the cloned value.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_value* kuzu_value_clone(kuzu_value* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_clone(ref NativeKuzuValue value);

        /// <summary>
        /// Copies the value from one kuzu_value to another.
        /// </summary>
        /// <param name="value">The source value to copy from.</param>
        /// <param name="other">The destination value to copy to.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_value_copy(kuzu_value* value, kuzu_value* other);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_copy(ref NativeKuzuValue value, IntPtr other);

        /// <summary>
        /// Destroys the given value instance and frees the allocated memory.
        /// </summary>
        /// <param name="value">The value instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_value_destroy(kuzu_value* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_destroy(IntPtr value);

        /// <summary>
        /// Converts the given value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The value as a string.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_value_to_string(kuzu_value* value);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_to_string(ref NativeKuzuValue value);

        // Int128 utility functions
        /// <summary>
        /// Converts a string to a kuzu_int128_t value.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="outResult">The output parameter that will hold the int128 value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_int128_t_from_string(const char* str, kuzu_int128_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_int128_t_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out NativeKuzuInt128 outResult);

        /// <summary>
        /// Converts a kuzu_int128_t value to a string.
        /// </summary>
        /// <param name="val">The int128 value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the string value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_int128_t_to_string(kuzu_int128_t val, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_int128_t_to_string(NativeKuzuInt128 val, out IntPtr outResult);

        // Date utility functions
        /// <summary>
        /// Converts a kuzu_date_t value to a string.
        /// </summary>
        /// <param name="date">The date value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the string value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_date_to_string(kuzu_date_t date, char** out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_string(NativeKuzuDate date, out IntPtr outResult);

        /// <summary>
        /// Converts a string to a kuzu_date_t value.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="outResult">The output parameter that will hold the date value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_date_from_string(const char* str, kuzu_date_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_date_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out NativeKuzuDate outResult);

        // Timestamp/date <-> tm conversion
        /// <summary>
        /// Converts a kuzu_timestamp_ns_t value to a tm struct.
        /// </summary>
        /// <param name="timestamp">The timestamp_ns value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_ns_to_tm(kuzu_timestamp_ns_t timestamp, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_to_tm(NativeKuzuTimestampNs timestamp, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a kuzu_timestamp_ms_t value to a tm struct.
        /// </summary>
        /// <param name="timestamp">The timestamp_ms value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_ms_to_tm(kuzu_timestamp_ms_t timestamp, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_to_tm(NativeKuzuTimestampMs timestamp, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a kuzu_timestamp_sec_t value to a tm struct.
        /// </summary>
        /// <param name="timestamp">The timestamp_sec value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_sec_to_tm(kuzu_timestamp_sec_t timestamp, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_to_tm(NativeKuzuTimestampSec timestamp, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a kuzu_timestamp_tz_t value to a tm struct.
        /// </summary>
        /// <param name="timestamp">The timestamp_tz value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_tz_to_tm(kuzu_timestamp_tz_t timestamp, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_to_tm(NativeKuzuTimestampTz timestamp, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a kuzu_timestamp_t value to a tm struct.
        /// </summary>
        /// <param name="timestamp">The timestamp value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_to_tm(kuzu_timestamp_t timestamp, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_to_tm(NativeKuzuTimestamp timestamp, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_timestamp_ns_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_ns value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_ns_from_tm(kuzu_tm tm, kuzu_timestamp_ns_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampNs outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_timestamp_ms_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_ms value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_ms_from_tm(kuzu_tm tm, kuzu_timestamp_ms_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampMs outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_timestamp_sec_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_sec value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_sec_from_tm(kuzu_tm tm, kuzu_timestamp_sec_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampSec outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_timestamp_tz_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp_tz value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_tz_from_tm(kuzu_tm tm, kuzu_timestamp_tz_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampTz outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_timestamp_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the timestamp value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_timestamp_from_tm(kuzu_tm tm, kuzu_timestamp_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_from_tm(NativeKuzuTm tm, out NativeKuzuTimestamp outResult);

        /// <summary>
        /// Converts a kuzu_date_t value to a tm struct.
        /// </summary>
        /// <param name="date">The date value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the tm struct.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_date_to_tm(kuzu_date_t date, kuzu_tm* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_tm(NativeKuzuDate date, out NativeKuzuTm outResult);

        /// <summary>
        /// Converts a tm struct to a kuzu_date_t value.
        /// </summary>
        /// <param name="tm">The tm struct to convert.</param>
        /// <param name="outResult">The output parameter that will hold the date value.</param>
        /// <returns>The state indicating the success or failure of the operation.</returns>
        /// <remarks>Original C signature: KUZU_C_API kuzu_state kuzu_date_from_tm(kuzu_tm tm, kuzu_date_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_from_tm(NativeKuzuTm tm, out NativeKuzuDate outResult);

        /// <summary>
        /// Converts a kuzu_interval_t value to a double representing the difference in time.
        /// </summary>
        /// <param name="interval">The interval value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the double value.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_interval_to_difftime(kuzu_interval_t interval, double* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_to_difftime(NativeKuzuInterval interval, out double outResult);

        /// <summary>
        /// Converts a double representing the difference in time to a kuzu_interval_t value.
        /// </summary>
        /// <param name="diffTime">The double value to convert.</param>
        /// <param name="outResult">The output parameter that will hold the interval value.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_interval_from_difftime(double diff_time, kuzu_interval_t* out_result);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_from_difftime(double diffTime, out NativeKuzuInterval outResult);

        // QuerySummary functions
        /// <summary>
        /// Destroys the given query summary instance and frees the allocated memory.
        /// </summary>
        /// <param name="querySummary">The query summary instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_query_summary_destroy(kuzu_query_summary* query_summary);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_summary_destroy(ref NativeKuzuQuerySummary querySummary);

        /// <summary>
        /// Returns the compiling time of the given query summary in milliseconds.
        /// </summary>
        /// <param name="querySummary">The query summary instance to return.</param>
        /// <returns>The compiling time in milliseconds.</returns>
        /// <remarks>Original C signature: KUZU_C_API double kuzu_query_summary_get_compiling_time(kuzu_query_summary* query_summary);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_compiling_time(ref NativeKuzuQuerySummary querySummary);

        /// <summary>
        /// Returns the execution time of the given query summary in milliseconds.
        /// </summary>
        /// <param name="querySummary">The query summary instance to return.</param>
        /// <returns>The execution time in milliseconds.</returns>
        /// <remarks>Original C signature: KUZU_C_API double kuzu_query_summary_get_execution_time(kuzu_query_summary* query_summary);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_execution_time(ref NativeKuzuQuerySummary querySummary);

        // String and memory management
        /// <summary>
        /// Destroys the given string instance and frees the allocated memory.
        /// </summary>
        /// <param name="str">The string instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_destroy_string(char* str);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_string(IntPtr str);

        /// <summary>
        /// Destroys the given blob instance and frees the allocated memory.
        /// </summary>
        /// <param name="blob">The blob instance to destroy.</param>
        /// <remarks>Original C signature: KUZU_C_API void kuzu_destroy_blob(uint8_t* blob);</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_blob(IntPtr blob);

        // Version functions
        /// <summary>
        /// Returns the version string of the Kuzu library.
        /// </summary>
        /// <returns>The version string.</returns>
        /// <remarks>Original C signature: KUZU_C_API char* kuzu_get_version();</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_get_version();

        /// <summary>
        /// Returns the storage version of the Kuzu library.
        /// </summary>
        /// <returns>The storage version.</returns>
        /// <remarks>Original C signature: KUZU_C_API uint64_t kuzu_get_storage_version();</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_get_storage_version();

#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes

    }
}