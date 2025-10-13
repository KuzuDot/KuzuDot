# KuzuDot

KuzuDot is a .NET client library for interacting with the [KùzuDB](https://kuzudb.com/) graph database.

## Note
This is still very alpha. I have a lot of documentation and examples to create.
Lists, Structures, Maps are still not super fleshed out.
Scalar types are pretty solid though.

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
