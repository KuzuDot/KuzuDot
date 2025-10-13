# KuzuDot Demo

Simple demo allowing queries agaist an in-memory KùzuDB instance.

Inital database setup:
```cypher
CREATE NODE TABLE Person(id STRING, name STRING, age INT64, PRIMARY KEY(id))
CREATE (:Person {id:'1', name:'Alice', age:30})
CREATE (:Person {id:'2', name:'Bob', age:36})
```

Initial sample query:
```cypher
MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age
```