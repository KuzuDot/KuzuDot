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
        /// Allocates memory and creates a kuzu database instance at database_path with
        /// bufferPoolSize=buffer_pool_size. Caller is responsible for calling kuzu_database_destroy() to
        /// release the allocated memory.
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

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeKuzuSystemConfig kuzu_default_system_config();

        // Connection functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_init(ref NativeKuzuDatabase database, out NativeKuzuConnection outConnection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_destroy(ref NativeKuzuConnection connection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_max_num_thread_for_exec(ref NativeKuzuConnection connection, ulong numThreads);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_get_max_num_thread_for_exec(ref NativeKuzuConnection connection, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_query(ref NativeKuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out NativeKuzuQueryResult outQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_prepare(ref NativeKuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out NativeKuzuPreparedStatement outPreparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_execute(ref NativeKuzuConnection connection,
            ref NativeKuzuPreparedStatement preparedStatement, out NativeKuzuQueryResult outQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_interrupt(ref NativeKuzuConnection connection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_query_timeout(ref NativeKuzuConnection connection, ulong timeoutInMs);

        // Prepared Statement functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_prepared_statement_destroy(ref NativeKuzuPreparedStatement preparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_prepared_statement_is_success(ref NativeKuzuPreparedStatement preparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_prepared_statement_get_error_message(ref NativeKuzuPreparedStatement preparedStatement);

        // Prepared Statement bind functions - all data types
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_bool(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, bool value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int64(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, long value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int32(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, int value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int16(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, short value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int8(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, sbyte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint64(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ulong value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint32(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, uint value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint16(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ushort value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint8(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, byte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_double(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, double value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_float(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_date(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuDate value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestamp value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ns(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampNs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ms(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampMs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_sec(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampSec value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_tz(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuTimestampTz value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_interval(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, NativeKuzuInterval value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_string(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_value(ref NativeKuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, IntPtr value);

        // Query Result functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_destroy(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_is_success(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_get_error_message(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_columns(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_name(ref NativeKuzuQueryResult queryResult,
            ulong index, out IntPtr outColumnName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_data_type(ref NativeKuzuQueryResult queryResult,
            ulong index, out NativeKuzuLogicalType outColumnDataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_tuples(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next(ref NativeKuzuQueryResult queryResult, out NativeKuzuFlatTuple outFlatTuple);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next_query_result(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_query_result(ref NativeKuzuQueryResult queryResult,
            out NativeKuzuQueryResult outNextQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_to_string(ref NativeKuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_reset_iterator(ref NativeKuzuQueryResult queryResult);

        // New: Query summary & Arrow interoperability
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_query_summary(ref NativeKuzuQueryResult queryResult,
            out NativeKuzuQuerySummary outQuerySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_arrow_schema(ref NativeKuzuQueryResult queryResult,
            out ArrowSchema outSchema);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_arrow_chunk(ref NativeKuzuQueryResult queryResult,
            long chunkSize, out ArrowArray outArrowArray);

        // FlatTuple functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_flat_tuple_destroy(ref NativeKuzuFlatTuple flatTuple);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_flat_tuple_get_value(ref NativeKuzuFlatTuple flatTuple, ulong index, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_flat_tuple_to_string(ref NativeKuzuFlatTuple flatTuple);

        // DataType functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_create(KuzuDataTypeId id, ref NativeKuzuLogicalType childType,
            ulong numElementsInArray, out NativeKuzuLogicalType outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_clone(ref NativeKuzuLogicalType dataType, out NativeKuzuLogicalType outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_destroy(ref NativeKuzuLogicalType dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_data_type_equals(ref NativeKuzuLogicalType dataType1, ref NativeKuzuLogicalType dataType2);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuDataTypeId kuzu_data_type_get_id(ref NativeKuzuLogicalType dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_data_type_get_num_elements_in_array(ref NativeKuzuLogicalType dataType, out ulong outResult);

        // Value creation functions - all data types
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null_with_data_type(ref NativeKuzuLogicalType dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_default(ref NativeKuzuLogicalType dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_bool(bool value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int8(sbyte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int16(short value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int32(int value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int64(long value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint8(byte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint16(ushort value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint32(uint value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint64(ulong value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int128(NativeKuzuInt128 value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_float(float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_double(double value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_internal_id(NativeKuzuInternalId value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_date(NativeKuzuDate value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp(NativeKuzuTimestamp value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ns(NativeKuzuTimestampNs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ms(NativeKuzuTimestampMs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_sec(NativeKuzuTimestampSec value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_tz(NativeKuzuTimestampTz value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_interval(NativeKuzuInterval value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr kuzu_value_create_string([MarshalAs(UnmanagedType.LPStr)] string value);

        // UTF-8 manual marshaling variant (same native symbol) for precise control
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "kuzu_value_create_string")]
        internal static extern IntPtr kuzu_value_create_string_from_utf8(IntPtr utf8Value);

        // Collections / structured value creation
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_list(ulong numElements, IntPtr elements /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_struct(ulong numFields, IntPtr fieldNames /* const char** */, IntPtr fieldValues /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_map(ulong numFields, IntPtr keys /* kuzu_value** */, IntPtr values /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        // Value accessor functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_value_is_null(ref NativeKuzuValue value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_set_null(ref NativeKuzuValue value, bool isNull);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_get_data_type(ref NativeKuzuValue value, out NativeKuzuLogicalType outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_bool(ref NativeKuzuValue value, out bool outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int8(ref NativeKuzuValue value, out sbyte outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int16(ref NativeKuzuValue value, out short outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int32(ref NativeKuzuValue value, out int outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int64(ref NativeKuzuValue value, out long outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint8(ref NativeKuzuValue value, out byte outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint16(ref NativeKuzuValue value, out ushort outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint32(ref NativeKuzuValue value, out uint outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint64(ref NativeKuzuValue value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int128(ref NativeKuzuValue value, out NativeKuzuInt128 outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_float(ref NativeKuzuValue value, out float outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_double(ref NativeKuzuValue value, out double outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_internal_id(ref NativeKuzuValue value, out NativeKuzuInternalId outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_date(ref NativeKuzuValue value, out NativeKuzuDate outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp(ref NativeKuzuValue value, out NativeKuzuTimestamp outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ns(ref NativeKuzuValue value, out NativeKuzuTimestampNs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ms(ref NativeKuzuValue value, out NativeKuzuTimestampMs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_sec(ref NativeKuzuValue value, out NativeKuzuTimestampSec outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_tz(ref NativeKuzuValue value, out NativeKuzuTimestampTz outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_interval(ref NativeKuzuValue value, out NativeKuzuInterval outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_string(ref NativeKuzuValue value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_blob(ref NativeKuzuValue value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_decimal_as_string(ref NativeKuzuValue value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uuid(ref NativeKuzuValue value, out IntPtr outResult);

        // List/Array functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_size(ref NativeKuzuValue value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_element(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Struct functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_num_fields(ref NativeKuzuValue value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_name(ref NativeKuzuValue value, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_value(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Map functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_size(ref NativeKuzuValue value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_key(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_value(ref NativeKuzuValue value, ulong index, out NativeKuzuValue outValue);

        // Recursive rel functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_node_list(ref NativeKuzuValue value, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_rel_list(ref NativeKuzuValue value, out NativeKuzuValue outValue);

        // Node value helpers
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_id_val(ref NativeKuzuValue nodeVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_label_val(ref NativeKuzuValue nodeVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_size(ref NativeKuzuValue nodeVal, out ulong outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_name_at(ref NativeKuzuValue nodeVal, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_value_at(ref NativeKuzuValue nodeVal, ulong index, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_to_string(ref NativeKuzuValue nodeVal, out IntPtr outResult);

        // Rel value helpers
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_src_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_dst_id_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_label_val(ref NativeKuzuValue relVal, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_size(ref NativeKuzuValue relVal, out ulong outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_name_at(ref NativeKuzuValue relVal, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_value_at(ref NativeKuzuValue relVal, ulong index, out NativeKuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_to_string(ref NativeKuzuValue relVal, out IntPtr outResult);

        // Value utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_clone(ref NativeKuzuValue value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_copy(ref NativeKuzuValue value, IntPtr other);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_destroy(IntPtr value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_to_string(ref NativeKuzuValue value);

        // Int128 utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_int128_t_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out NativeKuzuInt128 outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_int128_t_to_string(NativeKuzuInt128 val, out IntPtr outResult);

        // Date utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_string(NativeKuzuDate date, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_date_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out NativeKuzuDate outResult);

        // Timestamp/date <-> tm conversion
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_to_tm(NativeKuzuTimestampNs timestamp, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_to_tm(NativeKuzuTimestampMs timestamp, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_to_tm(NativeKuzuTimestampSec timestamp, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_to_tm(NativeKuzuTimestampTz timestamp, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_to_tm(NativeKuzuTimestamp timestamp, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampNs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampMs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampSec outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_from_tm(NativeKuzuTm tm, out NativeKuzuTimestampTz outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_from_tm(NativeKuzuTm tm, out NativeKuzuTimestamp outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_tm(NativeKuzuDate date, out NativeKuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_from_tm(NativeKuzuTm tm, out NativeKuzuDate outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_to_difftime(NativeKuzuInterval interval, out double outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_from_difftime(double diffTime, out NativeKuzuInterval outResult);

        // QuerySummary functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_summary_destroy(ref NativeKuzuQuerySummary querySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_compiling_time(ref NativeKuzuQuerySummary querySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_execution_time(ref NativeKuzuQuerySummary querySummary);

        // String and memory management
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_string(IntPtr str);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_blob(IntPtr blob);

        // Version functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_get_version();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_get_storage_version();

#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes

    }
}