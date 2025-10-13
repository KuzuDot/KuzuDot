using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;

namespace KuzuDot.Tests.FuzzTests;

[TestClass]
public sealed class FuzzMalformedUtf8Tests : IDisposable
{
    private Database? _db; private Connection? _conn; private string? _initError;

    [TestInitialize]
    public void Init()
    {
        try
        {
            _db = Database.FromMemory();
            _conn = _db.Connect();
            _conn.Query("CREATE NODE TABLE Doc(id INT64, txt STRING, PRIMARY KEY(id))").Dispose();
        }
        catch (KuzuException ex)
        {
            _initError = ex.Message; _db = null; _conn = null;
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        _conn?.Dispose(); _db?.Dispose(); _conn = null; _db = null;
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    private void RequireNative()
    {
        if (_db == null || _conn == null) Assert.Inconclusive("Native unavailable: " + _initError);
    }

    // ID15: Fuzz malformed UTF-8 and embedded nulls. We insert hex encoded blobs as STRING via dynamic construction
    // because native engine expects valid UTF-8; invalid sequences should either round-trip lossily or raise KuzuException,
    // but must not crash or corrupt memory.
    [TestMethod]
    public void EmbeddedNullAndControlChars()
    {
        RequireNative();
        byte[] bytes = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(); // includes many control chars and a null
        string payload = BuildUtf8Unsafe(bytes);
        if (!TryInsertDoc(1, payload, out var insertErr)) Assert.Inconclusive($"Engine rejected embedded null/control sequence: {insertErr}");
        using var result = _conn!.Query("MATCH (d:Doc {id:1}) RETURN d.txt");
        using var row = result.GetNext();
        using var val = row.GetValue(0);
        string fetched = val.ToString();
        Assert.IsTrue(fetched.Length >= 0); // survived fetch
        Assert.IsTrue(fetched.Contains('\0', StringComparison.Ordinal), "Expected embedded null retained");
    }

    [TestMethod]
    public void MalformedOverlongSequencesDoNotCrash()
    {
        RequireNative();
        // Overlong encodings for '/': 0xC0 0xAF (invalid per UTF-8 spec)
        byte[] overlong = new byte[] { 0xC0, 0xAF, 0xC0, 0x80, 0x00 }; // includes embedded null terminator inside sequence tail
        string payload = BuildUtf8Unsafe(overlong);
        if (!TryInsertDoc(2, payload, out _)) return; // rejection acceptable
        using var result = _conn!.Query("MATCH (d:Doc {id:2}) RETURN d.txt");
        using var row = result.GetNext();
        using var val = row.GetValue(0);
        string fetched = val.ToString();
        Assert.IsTrue(fetched.Length >= 0);
    }

    [TestMethod]
    public void RandomMalformedCorpusSamples()
    {
        RequireNative();
        var rnd = new Random(1234); // deterministic, security not required
        for (int docId = 10; docId < 30; docId++)
        {
            // deterministic pseudo-random corpus (non-crypto) acceptable for fuzz probing
#pragma warning disable CA5394
            var bytes = new byte[rnd.Next(1, 48)];
            rnd.NextBytes(bytes);
            // Force at least one start-of-multi-byte followed by truncated continuation to simulate truncation
            if (bytes.Length >= 2)
            {
                bytes[0] = 0xE2; // start 3-byte sequence
                bytes[1] = (byte)rnd.Next(0x00, 0x7F); // likely not a valid continuation
#pragma warning restore CA5394
            }
            string payload = BuildUtf8Unsafe(bytes);
            if (!TryInsertDoc(docId, payload, out _)) continue; // rejection acceptable
            using var result = _conn!.Query($"MATCH (d:Doc {{id:{docId}}}) RETURN d.txt");
            using var row = result.GetNext();
            using var val = row.GetValue(0);
            _ = val.ToString(); // ensure no exception
        }
    }

    private bool TryInsertDoc(long id, string txt, out string? error)
    {
        string escaped = txt.Replace("'", "''", StringComparison.Ordinal);
        try
        {
            using var r = _conn!.Query($"CREATE (:Doc {{id:{id}, txt: '{escaped}'}})");
            error = null; return true;
        }
        catch (KuzuException ex)
        {
            error = ex.Message; return false;
        }
    }

    // Build a .NET string reinterpreting arbitrary byte[] via Latin1 to bypass UTF-8 validation, then it will be inserted.
    // This intentionally creates sequences the engine will attempt to treat as UTF-8 when reading back.
    private static string BuildUtf8Unsafe(byte[] raw)
    {
#if NET8_0_OR_GREATER
        return Encoding.Latin1.GetString(raw);
#else
        return string.Concat(raw.Select(b => (char)b));
#endif
    }
}
