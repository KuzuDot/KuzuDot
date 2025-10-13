# Phase 1 Benchmark Results

Date: 2025-10-06
Runtime: .NET 8.0.20 (BenchmarkDotNet 0.13.12)
Host CPU: Intel Core i7-1065G7 (AVX-512 capable)

## Purpose
Phase 1 targeted the first wave of micro-optimizations:

1. Lock-free scalar value getters
2. Column name cache rewrite (array + folded casing instead of dictionary)
3. Prepared statement binding normalization & member binding cache
4. Lightweight performance counter snapshot struct (`KuzuPerformanceSnapshot`)

These benchmarks establish a repeatable baseline for those areas.

## How To Run

Two equivalent options now that discovery is confirmed:

1. Manual phase set (runs only new Phase 1 benchmarks):
   ```powershell
   dotnet run -c Release --project KuzuDot.Benchmarks\KuzuDot.Benchmarks.csproj -- phase1
   ```
2. Wildcard filter (BenchmarkDotNet discovery, no manual branch):
   ```powershell
   dotnet run -c Release --project KuzuDot.Benchmarks\KuzuDot.Benchmarks.csproj -- --filter *ScalarAccessBenchmarks*
   ```
   (Replace the filter with `*ColumnLookupBenchmarks*`, `*PreparedBindBenchmarks*`, or `*PerfSnapshotBenchmarks*` as needed.)

> Note: Earlier discovery/listing anomalies were due to using a raw class name without wildcard. BenchmarkDotNet pattern matching requires `*` when not specifying the fully qualified benchmark name.

## Raw Results (Selected Summary Metrics)

### Scalar Access

| Benchmark | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Iterate & read typed values (lock-free path) | 1.941 ms | 132,972 B | Iterates 200 rows, exercises hot scalar getters. |
| Prepared single-row bind + scalar reads | 801.6 µs | 393 B | Single-row path; includes parameter bind + execution + value unwrap. |

### Column Lookup (Name vs Ordinal)

| ColumnCount | Name Lookup Mean | Ordinal Lookup Mean | Name Alloc (KB) | Ord Alloc (KB) | Observation |
|-------------|------------------|--------------------|-----------------|----------------|-------------|
| 3  | 857.8 µs | 924.3 µs | 15.69 | 13.81 | Ordinal unexpectedly slower at very small N (cache effects / variance). |
| 8  | 1.311 ms | 1.336 ms | 16.08 | 13.81 | Nearly identical; ordinal modestly lower allocation. |
| 16 | 2.041 ms | 1.823 ms | 17.20 | 13.81 | Ordinal faster as expected when column set grows. |

### Prepared Statement Binding

| Mode | Mean | Allocated | Comment |
|------|------|-----------|---------|
| Bind object (cached normalization) | 511.4 µs | 440 B | Uses cached reflection + normalized names. |
| Bind primitives individually | 539.5 µs | 392 B | Slightly slower; higher variance (multimodal). |

Difference: Object bind path is ~5% faster in this micro case while allocating marginally more (extra normalization artifacts amortized). Further runs can verify stability.

### Performance Snapshot Capture

| Benchmark | Mean | StdDev | Allocation |
|-----------|------|--------|------------|
| Capture performance snapshot | 39.13 ns | 0.68 ns | 0 |

The snapshot capture cost (~39 ns) is low enough for frequent sampling (e.g., per query completion) without material overhead.

## Interpretation & Follow-Ups

1. Scalar getters: The per-row typed iteration benchmark shows consistent sub-2 ms for full 200-row scan; confirms lock removal did not introduce instability (tight distribution). Additional comparison to pre-optimization commit would quantify absolute gain—consider adding historical baseline capture.
2. Column lookup: Name vs ordinal crossover appears after modest column counts; confirms benefit of ordinal access for wider projections. For small projections, remaining gap is negligible; caching improvements are functioning. Potential follow-up: Add a benchmark variant with repetitive access to the same column name inside the row loop to highlight cache hits explicitly.
3. Prepared binding: Normalized object binder is competitive and slightly faster here. This suggests the reflection + cache path is not a bottleneck. Consider a high-parameter-count (e.g., 12–20 fields) scenario to better stress normalization scaling.
4. Snapshot: Sub-50 ns target achieved. Equality & hash implementation now allow external diffing or delta calculations with minimal cost.

## Suggested Next Steps

| Priority | Action | Rationale |
|----------|--------|-----------|
| High | Add pre-optimization historical baseline JSON/CSV for comparison | Enables regression tracking over time. |
| Medium | Introduce large parameter prepared statement benchmark (e.g., 12 params) | Stress-test binder cache scaling & allocations. |
| Medium | Add row-materialization benchmark comparing current vs hypothetical pooled row objects | Quantify potential further allocation reductions. |
| Low | Remove manual `phase1` branch after confirming sustained discovery stability | Simplify Program.cs maintenance. |
| Low | Integrate snapshot diff benchmark (two consecutive captures) | Validate minimal incremental overhead. |

## Reproducing Full Suite

Run entire benchmark collection (all classes):

```powershell
dotnet run -c Release --project KuzuDot.Benchmarks\KuzuDot.Benchmarks.csproj -- --filter *
```

## Troubleshooting Notes

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| New benchmarks not listed | Missing wildcard in filter or stale build | Use `--filter *Name*`, ensure Release build. |
| PreparedBind benchmarks returning NA | Missing seed row for parameters | Ensure `Setup()` inserts matching row (fixed in Phase 1). |
| High variance in ColumnLookup 16 columns | Cache warmup jitter / GC | Increase `IterationCount` or isolate CPU load, optionally add `[IterationSetup]` to pre-touch structures. |

---

Maintained by: Phase 1 performance workstream.
If updating, append new measurements with date + commit hash for longitudinal tracking.
