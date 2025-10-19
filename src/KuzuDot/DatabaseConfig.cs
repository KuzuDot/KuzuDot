using KuzuDot.Native;

namespace KuzuDot
{
    /// <summary>
    /// Represents configuration options for a Kuzu database instance.
    /// </summary>
    public sealed class DatabaseConfig
    {
        /// <summary>Gets or sets the buffer pool size in bytes.</summary>
        public ulong BufferPoolSize { get; set; }
        /// <summary>Gets or sets the maximum number of threads for query execution.</summary>
        public ulong MaxNumThreads { get; set; }
        /// <summary>Gets or sets a value indicating whether compression is enabled.</summary>
        public bool EnableCompression { get; set; }
        /// <summary>Gets or sets a value indicating whether the database is read-only.</summary>
        public bool ReadOnly { get; set; }
        /// <summary>Gets or sets the maximum database size in bytes.</summary>
        public ulong MaxDbSize { get; set; }
        /// <summary>Gets or sets a value indicating whether auto-checkpointing is enabled.</summary>
        public bool AutoCheckpoint { get; set; }
        /// <summary>Gets or sets the checkpoint threshold in bytes.</summary>
        public ulong CheckpointThreshold { get; set; }

        /// <summary>
        /// Converts this configuration to a native Kuzu system config struct.
        /// </summary>
        /// <returns>A native Kuzu system config struct.</returns>
        internal NativeKuzuSystemConfig ToNative() => new()
        {
            BufferPoolSize = BufferPoolSize,
            MaxNumThreads = MaxNumThreads,
            EnableCompression = EnableCompression,
            ReadOnly = ReadOnly,
            MaxDbSize = MaxDbSize,
            AutoCheckpoint = AutoCheckpoint,
            CheckpointThreshold = CheckpointThreshold
        };

        /// <summary>
        /// Returns a <see cref="DatabaseConfig"/> instance with default values.
        /// </summary>
        /// <returns>A default <see cref="DatabaseConfig"/>.</returns>
        public static DatabaseConfig Default()
        {
            var n = NativeMethods.kuzu_default_system_config();
            return new DatabaseConfig
            {
                BufferPoolSize = n.BufferPoolSize,
                MaxNumThreads = n.MaxNumThreads,
                EnableCompression = n.EnableCompression,
                ReadOnly = n.ReadOnly,
                MaxDbSize = n.MaxDbSize,
                AutoCheckpoint = n.AutoCheckpoint,
                CheckpointThreshold = n.CheckpointThreshold
            };
        }

        /// <inheritdoc/>
        public override string ToString() => $"DatabaseConfig(BP={BufferPoolSize}, Threads={MaxNumThreads}, ReadOnly={ReadOnly})";
    }
}