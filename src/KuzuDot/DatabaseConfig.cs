using KuzuDot.Native;

namespace KuzuDot
{
    public sealed class DatabaseConfig
    {
        public ulong BufferPoolSize { get; set; }
        public ulong MaxNumThreads { get; set; }
        public bool EnableCompression { get; set; }
        public bool ReadOnly { get; set; }
        public ulong MaxDbSize { get; set; }
        public bool AutoCheckpoint { get; set; }
        public ulong CheckpointThreshold { get; set; }

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
        public override string ToString() => $"DatabaseConfig(BP={BufferPoolSize}, Threads={MaxNumThreads}, ReadOnly={ReadOnly})";
    }
}