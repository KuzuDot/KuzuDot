# KuzuDot

KuzuDot is a .NET client library for interacting with the [KùzuDB](https://kuzudb.com/) graph database.

## Features

- Connect to Kùzu graph database instances
- Execute Cypher queries and retrieve results
- Support for parameterized queries
- Map query results to POCOs
- Support for Lists, Structs, Maps, Arrays
- Support for Apache Arrow result format

- TODO: Test for any native memory leaks
- TODO: Test out async support

## Getting Started

### Prerequisites

- .NET 8.0 or later
- .NET 4.7.2 or later (.NET Standard 2.0)

### Installation

Add the `kuzu_shared.dll` and `KuzuDot.dll` into your project output directory.

### Usage

See the [KuzuDot.Examples](./KuzuDot.Examples) project for a variety of usage examples.

## Documentation

TODO: Create a docs/ folder and add more detailed documentation.
TODO: - [API Reference](docs/README.md)
TODO: - [Examples](docs/EXAMPLES.md)

## Contributing

I'm not sure. Please open an issue to discuss what you would like to contribute.

## License

This project is licensed under the MIT License.

## Thread Safety

No promises are made yet. It seems to work, but use at your own risk.


## Performance Benchmarks

The repository includes a `KuzuDot.Benchmarks` project using BenchmarkDotNet to track core hot paths:

Covered scenarios:
- Simple full table scan materialization
- Scalar COUNT(*) query
- Prepared statement create/bind/execute (cold)
- Prepared statement reuse bind/execute (warm)
- POCO enumeration materialization

### Running Benchmarks

Run all (Release recommended):
```
dotnet run -c Release --project KuzuDot.Benchmarks
```

Filter to a single benchmark:
```
dotnet run -c Release --project KuzuDot.Benchmarks -- --filter *PreparedReuseSingleBind*
```

Artifacts (Markdown/HTML/CSV) are emitted under:
`KuzuDot.Benchmarks/BenchmarkDotNet.Artifacts/results` 
(Note: this isn't true at the moment, they get emitted in the root solution folder...)