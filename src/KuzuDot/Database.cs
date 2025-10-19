using KuzuDot.Native;
using KuzuDot.Utils;
using System;
using System.Threading.Tasks;

namespace KuzuDot
{
    /// <summary>
    /// Represents a Kuzu database instance. Multiple <see cref="Connection"/> objects may be created from a
    /// single database and used concurrently.
    /// </summary>
    public sealed class Database : IDisposable
    {
        private readonly DatabaseSafeHandle _handle;

        private readonly string _path;

        internal ref NativeKuzuDatabase NativeStruct => ref _handle.NativeStruct;

        /// <summary>
        /// Initializes a new database instance at the specified path with default configuration.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created; ":memory:" or an empty string will create an in-memory database.</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="KuzuException">Thrown when database initialization fails.</exception>
        private Database(string path) : this(path, DatabaseConfig.Default()) { }

        /// <summary>
        /// Creates a new <see cref="Database"/> instance from the specified path using default configuration.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created.</param>
        /// <returns>A new <see cref="Database"/> instance.</returns>
        public static Database FromPath(string path) => new(path);
        /// <summary>
        /// Creates a new <see cref="Database"/> instance from the specified path and configuration.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created.</param>
        /// <param name="config">The database configuration.</param>
        /// <returns>A new <see cref="Database"/> instance.</returns>
        public static Database FromPath(string path, DatabaseConfig config)
        {
            KuzuGuard.NotNull(config, nameof(config));
            return new(path, config);
        }

        /// <summary>
        /// Creates a new in-memory <see cref="Database"/> instance using default configuration.
        /// </summary>
        /// <returns>A new in-memory <see cref="Database"/> instance.</returns>
        public static Database FromMemory() => FromPath(":memory:");
        /// <summary>
        /// Creates a new in-memory <see cref="Database"/> instance with the specified configuration.
        /// </summary>
        /// <param name="config">The database configuration.</param>
        /// <returns>A new in-memory <see cref="Database"/> instance.</returns>
        public static Database FromMemory(DatabaseConfig config) => FromPath(":memory:", config);

        /// <summary>
        /// Initializes a new database instance at the specified path with the given configuration.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created; ":memory:" or an empty string will create an in-memory database.</param>
        /// <param name="config">System configuration.</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="KuzuException">Thrown when database initialization fails.</exception>
        private Database(string path, DatabaseConfig config)
        {
            KuzuGuard.NotNull(path, nameof(path));
            KuzuGuard.NotNull(config, nameof(config));
            _path = path;
            try
            {
                var state = NativeMethods.kuzu_database_init(path, config!.ToNative(), out var nativeDb);
                KuzuGuard.CheckSuccess(state, $"Failed to initialize database at path: {path}");
                KuzuGuard.AssertNotZero(nativeDb.Database, $"Failed to initialize database at path: {path}");
                _handle = new(nativeDb);
            }
            catch (DllNotFoundException ex)
            {
                throw new KuzuException($"Native Kuzu library (kuzu_shared.dll) not found. {ex.Message}", ex);
            }
            catch (BadImageFormatException ex)
            {
                throw new KuzuException($"Invalid native library format (architecture mismatch). {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a new connection to this database.
        /// Multiple connections can be created and used concurrently.
        /// </summary>
        /// <returns>A new <see cref="Connection"/> instance.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the database has been disposed.</exception>
        /// <exception cref="KuzuException">Thrown when connection creation fails.</exception>
        public Connection Connect()
        {
            KuzuGuard.NotDisposed(_handle.IsInvalid, nameof(Database));
            return new Connection(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Database"/>.
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
        }

        /// <inheritdoc/>
        public override string ToString() => _handle.IsInvalid ? "Database(Disposed)" : $"Database(Path={_path ?? ""})";

        private sealed class DatabaseSafeHandle : KuzuSafeHandle
        {
            internal NativeKuzuDatabase NativeStruct;

            internal DatabaseSafeHandle(NativeKuzuDatabase native) : base("Database")
            {
                NativeStruct = native;
                Initialize(native.Database);
            }

            protected override void Release()
            {
                NativeMethods.kuzu_database_destroy(ref NativeStruct);
                NativeStruct = default;
            }
        }
    }
}