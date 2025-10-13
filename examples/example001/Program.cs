// See https://aka.ms/new-console-template for more information
using KuzuDot;
using KuzuDot.Value;

using var db = new Database(":memory:");
using var conn = db.Connect();

conn.NonQuery("CREATE NODE TABLE Person(id INT64, name STRING, PRIMARY KEY(id))");
conn.NonQuery("CREATE (:Person {id: 1, name:'Alice'})");
conn.NonQuery("CREATE (:Person {id: 2, name:'Bob'})");
conn.NonQuery("CREATE (:Person {id: 3, name:'Charlie'})");

using var result = conn.Query("MATCH (n) RETURN n LIMIT 10;");

while (result.HasNext())
{
    using var row = result.GetNext(); // Fet the KuzuFlatTuple
    using var node = row.GetValue<KuzuNode>(0); // Read result item 1 as a KuzuNode
    Console.WriteLine("Node Label: {0}", node.Label);
    Console.WriteLine("Node Properties:");
    foreach(var (key, value) in node.Properties) 
    {
        Console.WriteLine("\t{0}: {1}", key, value);
        value.Dispose(); // Values are IDisposable
    }
}