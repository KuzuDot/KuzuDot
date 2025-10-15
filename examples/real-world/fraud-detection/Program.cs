using System;
using KuzuDot;

namespace KuzuDot.Examples.RealWorld
{
    /// <summary>
    /// Fraud detection example demonstrating detecting suspicious patterns
    /// </summary>
    public class FraudDetection
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KuzuDot Fraud Detection Example ===");
            
            try
            {
                RunExample();
            }
            catch (KuzuException ex)
            {
                Console.WriteLine($"KuzuDB Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void RunExample()
        {
            // Create an in-memory database
            Console.WriteLine("Creating in-memory database...");
            using var database = Database.FromMemory();
            using var connection = database.Connect();

            // Create schema
            Console.WriteLine("Creating fraud detection schema...");
            CreateSchema(connection);

            // Insert sample data
            Console.WriteLine("Inserting sample data...");
            InsertSampleData(connection);

            // Demonstrate fraud detection
            Console.WriteLine("\n=== Fraud Detection Examples ===");
            DemonstrateFraudDetection(connection);

            Console.WriteLine("\n=== Fraud Detection Example completed successfully! ===");
        }

        private static void CreateSchema(Connection connection)
        {
            // Create node tables
            connection.NonQuery(@"
                CREATE NODE TABLE Account(
                    id INT64, 
                    account_number STRING, 
                    account_type STRING,
                    balance DOUBLE,
                    created_at TIMESTAMP,
                    status STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Customer(
                    id INT64, 
                    name STRING, 
                    ssn STRING,
                    phone STRING,
                    email STRING,
                    address STRING,
                    city STRING,
                    state STRING,
                    zip_code STRING,
                    date_of_birth DATE,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Transaction(
                    id INT64, 
                    amount DOUBLE,
                    transaction_type STRING,
                    timestamp TIMESTAMP,
                    status STRING,
                    description STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Merchant(
                    id INT64, 
                    name STRING, 
                    category STRING,
                    location STRING,
                    risk_level STRING,
                    PRIMARY KEY(id)
                )");

            connection.NonQuery(@"
                CREATE NODE TABLE Device(
                    id INT64, 
                    device_id STRING, 
                    device_type STRING,
                    ip_address STRING,
                    user_agent STRING,
                    location STRING,
                    PRIMARY KEY(id)
                )");

            // Create relationship tables
            connection.NonQuery(@"
                CREATE REL TABLE Owns(
                    FROM Customer TO Account,
                    since TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Initiates(
                    FROM Account TO Transaction,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Processes(
                    FROM Merchant TO Transaction,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE Uses(
                    FROM Account TO Device,
                    timestamp TIMESTAMP
                )");

            connection.NonQuery(@"
                CREATE REL TABLE RelatedTo(
                    FROM Transaction TO Transaction,
                    relationship_type STRING,
                    confidence DOUBLE
                )");
        }

        private static void InsertSampleData(Connection connection)
        {
            // Insert customers
            var customers = GenerateCustomers(100);
            using var customerStmt = connection.Prepare(@"
                CREATE (:Customer {
                    id: $id, 
                    name: $name, 
                    ssn: $ssn,
                    phone: $phone,
                    email: $email,
                    address: $address,
                    city: $city,
                    state: $state,
                    zip_code: $zip_code,
                    date_of_birth: $date_of_birth
                })");

            foreach (var customer in customers)
            {
                customerStmt.Bind(customer);
                customerStmt.Execute();
            }

            // Insert accounts
            var accounts = GenerateAccounts(150);
            using var accountStmt = connection.Prepare(@"
                CREATE (:Account {
                    id: $id, 
                    account_number: $account_number, 
                    account_type: $account_type,
                    balance: $balance,
                    created_at: $created_at,
                    status: $status
                })");

            foreach (var account in accounts)
            {
                accountStmt.Bind(account);
                accountStmt.Execute();
            }

            // Insert merchants
            var merchants = GenerateMerchants(50);
            using var merchantStmt = connection.Prepare(@"
                CREATE (:Merchant {
                    id: $id, 
                    name: $name, 
                    category: $category,
                    location: $location,
                    risk_level: $risk_level
                })");

            foreach (var merchant in merchants)
            {
                merchantStmt.Bind(merchant);
                merchantStmt.Execute();
            }

            // Insert devices
            var devices = GenerateDevices(200);
            using var deviceStmt = connection.Prepare(@"
                CREATE (:Device {
                    id: $id, 
                    device_id: $device_id, 
                    device_type: $device_type,
                    ip_address: $ip_address,
                    user_agent: $user_agent,
                    location: $location
                })");

            foreach (var device in devices)
            {
                deviceStmt.Bind(device);
                deviceStmt.Execute();
            }

            // Insert transactions
            var transactions = GenerateTransactions(1000);
            using var transactionStmt = connection.Prepare(@"
                CREATE (:Transaction {
                    id: $id, 
                    amount: $amount,
                    transaction_type: $transaction_type,
                    timestamp: $timestamp,
                    status: $status,
                    description: $description
                })");

            foreach (var transaction in transactions)
            {
                transactionStmt.Bind(transaction);
                transactionStmt.Execute();
            }

            Console.WriteLine("  Created customers, accounts, merchants, devices, and transactions");

            // Create relationships
            CreateRelationships(connection);
        }

        private static void CreateRelationships(Connection connection)
        {
            Console.WriteLine("Creating relationships...");

            // Customer-Account relationships
            using var ownsStmt = connection.Prepare(@"
                MATCH (c:Customer), (a:Account) 
                WHERE c.id = $customer_id AND a.id = $account_id 
                CREATE (c)-[:Owns {since: $since}]->(a)");

            for (int i = 1; i <= 150; i++)
            {
                var customerId = (i % 100) + 1;
                var since = DateTime.UtcNow.AddDays(-Random.Shared.Next(365 * 2));

                ownsStmt.Bind("customer_id", customerId);
                ownsStmt.Bind("account_id", i);
                ownsStmt.BindTimestamp("since", since);
                ownsStmt.Execute();
            }

            // Account-Transaction relationships
            using var initiatesStmt = connection.Prepare(@"
                MATCH (a:Account), (t:Transaction) 
                WHERE a.id = $account_id AND t.id = $transaction_id 
                CREATE (a)-[:Initiates {timestamp: $timestamp}]->(t)");

            for (int i = 1; i <= 1000; i++)
            {
                var accountId = Random.Shared.Next(1, 151);
                var timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(30));

                initiatesStmt.Bind("account_id", accountId);
                initiatesStmt.Bind("transaction_id", i);
                initiatesStmt.BindTimestamp("timestamp", timestamp);
                initiatesStmt.Execute();
            }

            // Merchant-Transaction relationships
            using var processesStmt = connection.Prepare(@"
                MATCH (m:Merchant), (t:Transaction) 
                WHERE m.id = $merchant_id AND t.id = $transaction_id 
                CREATE (m)-[:Processes {timestamp: $timestamp}]->(t)");

            for (int i = 1; i <= 1000; i++)
            {
                var merchantId = Random.Shared.Next(1, 51);
                var timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(30));

                processesStmt.Bind("merchant_id", merchantId);
                processesStmt.Bind("transaction_id", i);
                processesStmt.BindTimestamp("timestamp", timestamp);
                processesStmt.Execute();
            }

            // Account-Device relationships
            using var usesStmt = connection.Prepare(@"
                MATCH (a:Account), (d:Device) 
                WHERE a.id = $account_id AND d.id = $device_id 
                CREATE (a)-[:Uses {timestamp: $timestamp}]->(d)");

            for (int i = 1; i <= 200; i++)
            {
                var accountId = Random.Shared.Next(1, 151);
                var timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(30));

                usesStmt.Bind("account_id", accountId);
                usesStmt.Bind("device_id", i);
                usesStmt.BindTimestamp("timestamp", timestamp);
                usesStmt.Execute();
            }

            // Transaction-Transaction relationships (for pattern detection)
            using var relatedStmt = connection.Prepare(@"
                MATCH (t1:Transaction), (t2:Transaction) 
                WHERE t1.id = $transaction_id1 AND t2.id = $transaction_id2 
                CREATE (t1)-[:RelatedTo {relationship_type: $type, confidence: $confidence}]->(t2)");

            // Create some suspicious transaction patterns
            var suspiciousPatterns = new[]
            {
                new { T1 = 1L, T2 = 2L, Type = "Same_Merchant", Confidence = 0.9 },
                new { T1 = 3L, T2 = 4L, Type = "Same_Amount", Confidence = 0.8 },
                new { T1 = 5L, T2 = 6L, Type = "Same_Device", Confidence = 0.95 },
                new { T1 = 7L, T2 = 8L, Type = "Same_Time", Confidence = 0.7 },
                new { T1 = 9L, T2 = 10L, Type = "Same_Location", Confidence = 0.85 }
            };

            foreach (var pattern in suspiciousPatterns)
            {
                relatedStmt.Bind("transaction_id1", pattern.T1);
                relatedStmt.Bind("transaction_id2", pattern.T2);
                relatedStmt.Bind("type", pattern.Type);
                relatedStmt.Bind("confidence", pattern.Confidence);
                relatedStmt.Execute();
            }

            Console.WriteLine("  Created all relationships");
        }

        private static void DemonstrateFraudDetection(Connection connection)
        {
            // 1. Unusual transaction amounts
            Console.WriteLine("1. Unusual transaction amounts:");
            await DetectUnusualAmounts(connection);

            // 2. Rapid successive transactions
            Console.WriteLine("\n2. Rapid successive transactions:");
            await DetectRapidTransactions(connection);

            // 3. Geographic anomalies
            Console.WriteLine("\n3. Geographic anomalies:");
            await DetectGeographicAnomalies(connection);

            // 4. Device anomalies
            Console.WriteLine("\n4. Device anomalies:");
            await DetectDeviceAnomalies(connection);

            // 5. Merchant risk analysis
            Console.WriteLine("\n5. Merchant risk analysis:");
            await AnalyzeMerchantRisk(connection);

            // 6. Account behavior patterns
            Console.WriteLine("\n6. Account behavior patterns:");
            await AnalyzeAccountBehavior(connection);

            // 7. Network analysis
            Console.WriteLine("\n7. Network analysis:");
            await PerformNetworkAnalysis(connection);

            // 8. Time-based patterns
            Console.WriteLine("\n8. Time-based patterns:");
            await DetectTimeBasedPatterns(connection);
        }

        private static async Task DetectUnusualAmounts(Connection connection)
        {
            // Find transactions with unusually high amounts
            using var highAmountResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WHERE t.amount > 10000
                RETURN a.account_number, t.amount, t.timestamp, t.description
                ORDER BY t.amount DESC
                LIMIT 10");

            Console.WriteLine("  High amount transactions:");
            while (highAmountResult.HasNext())
            {
                using var row = highAmountResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var amount = row.GetValueAs<double>(1);
                var timestamp = row.GetValueAs<DateTime>(2);
                var description = row.GetValueAs<string>(3);
                
                Console.WriteLine($"    Account {accountNumber}: ${amount:F2} at {timestamp:yyyy-MM-dd HH:mm:ss} - {description}");
            }

            // Find transactions significantly above account average
            using var aboveAverageResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WITH a, t, AVG(t.amount) as avg_amount
                WHERE t.amount > avg_amount * 5
                RETURN a.account_number, t.amount, avg_amount, t.timestamp
                ORDER BY (t.amount / avg_amount) DESC
                LIMIT 10");

            Console.WriteLine("  Transactions significantly above average:");
            while (aboveAverageResult.HasNext())
            {
                using var row = aboveAverageResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var amount = row.GetValueAs<double>(1);
                var avgAmount = row.GetValueAs<double>(2);
                var timestamp = row.GetValueAs<DateTime>(3);
                
                Console.WriteLine($"    Account {accountNumber}: ${amount:F2} (avg: ${avgAmount:F2}) at {timestamp:yyyy-MM-dd HH:mm:ss}");
            }
        }

        private static async Task DetectRapidTransactions(Connection connection)
        {
            // Find accounts with multiple transactions in short time periods
            using var rapidResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WITH a, t, COLLECT(t) as transactions
                WHERE SIZE(transactions) > 5
                RETURN a.account_number, SIZE(transactions) as transaction_count,
                       MIN(t.timestamp) as first_transaction,
                       MAX(t.timestamp) as last_transaction
                ORDER BY transaction_count DESC
                LIMIT 10");

            Console.WriteLine("  Accounts with rapid transactions:");
            while (rapidResult.HasNext())
            {
                using var row = rapidResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var transactionCount = row.GetValueAs<long>(1);
                var firstTransaction = row.GetValueAs<DateTime>(2);
                var lastTransaction = row.GetValueAs<DateTime>(3);
                var timeSpan = lastTransaction - firstTransaction;
                
                Console.WriteLine($"    Account {accountNumber}: {transactionCount} transactions in {timeSpan.TotalHours:F1} hours");
            }

            // Find transactions with same amount in short time
            using var sameAmountResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WITH a, t.amount as amount, COLLECT(t) as transactions
                WHERE SIZE(transactions) > 3
                RETURN a.account_number, amount, SIZE(transactions) as count,
                       MIN(t.timestamp) as first_transaction,
                       MAX(t.timestamp) as last_transaction
                ORDER BY count DESC
                LIMIT 10");

            Console.WriteLine("  Same amount transactions:");
            while (sameAmountResult.HasNext())
            {
                using var row = sameAmountResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var amount = row.GetValueAs<double>(1);
                var count = row.GetValueAs<long>(2);
                var firstTransaction = row.GetValueAs<DateTime>(3);
                var lastTransaction = row.GetValueAs<DateTime>(4);
                var timeSpan = lastTransaction - firstTransaction;
                
                Console.WriteLine($"    Account {accountNumber}: {count} transactions of ${amount:F2} in {timeSpan.TotalHours:F1} hours");
            }
        }

        private static async Task DetectGeographicAnomalies(Connection connection)
        {
            // Find transactions from unusual locations
            using var locationResult = connection.Query(@"
                MATCH (a:Account)-[:Uses]->(d:Device)-[:Initiates]->(t:Transaction)
                WITH a, d.location as location, COUNT(t) as transaction_count
                WHERE transaction_count > 1
                RETURN a.account_number, location, transaction_count
                ORDER BY transaction_count DESC
                LIMIT 10");

            Console.WriteLine("  Transactions from unusual locations:");
            while (locationResult.HasNext())
            {
                using var row = locationResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var location = row.GetValueAs<string>(1);
                var transactionCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    Account {accountNumber}: {transactionCount} transactions from {location}");
            }

            // Find accounts with transactions from multiple countries
            using var multiCountryResult = connection.Query(@"
                MATCH (a:Account)-[:Uses]->(d:Device)-[:Initiates]->(t:Transaction)
                WITH a, COLLECT(DISTINCT d.location) as locations
                WHERE SIZE(locations) > 2
                RETURN a.account_number, locations
                ORDER BY SIZE(locations) DESC
                LIMIT 10");

            Console.WriteLine("  Accounts with transactions from multiple locations:");
            while (multiCountryResult.HasNext())
            {
                using var row = multiCountryResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var locations = row.GetValueAs<string>(1);
                
                Console.WriteLine($"    Account {accountNumber}: transactions from {locations}");
            }
        }

        private static async Task DetectDeviceAnomalies(Connection connection)
        {
            // Find accounts using multiple devices
            using var multiDeviceResult = connection.Query(@"
                MATCH (a:Account)-[:Uses]->(d:Device)
                WITH a, COUNT(d) as device_count
                WHERE device_count > 3
                RETURN a.account_number, device_count
                ORDER BY device_count DESC
                LIMIT 10");

            Console.WriteLine("  Accounts using multiple devices:");
            while (multiDeviceResult.HasNext())
            {
                using var row = multiDeviceResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var deviceCount = row.GetValueAs<long>(1);
                
                Console.WriteLine($"    Account {accountNumber}: using {deviceCount} devices");
            }

            // Find devices used by multiple accounts
            using var sharedDeviceResult = connection.Query(@"
                MATCH (a:Account)-[:Uses]->(d:Device)
                WITH d, COUNT(a) as account_count
                WHERE account_count > 2
                RETURN d.device_id, d.device_type, account_count
                ORDER BY account_count DESC
                LIMIT 10");

            Console.WriteLine("  Devices used by multiple accounts:");
            while (sharedDeviceResult.HasNext())
            {
                using var row = sharedDeviceResult.GetNext();
                var deviceId = row.GetValueAs<string>(0);
                var deviceType = row.GetValueAs<string>(1);
                var accountCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    Device {deviceId} ({deviceType}): used by {accountCount} accounts");
            }
        }

        private static async Task AnalyzeMerchantRisk(Connection connection)
        {
            // Find high-risk merchants
            using var highRiskResult = connection.Query(@"
                MATCH (m:Merchant)-[:Processes]->(t:Transaction)
                WHERE m.risk_level = 'High'
                RETURN m.name, m.category, COUNT(t) as transaction_count, AVG(t.amount) as avg_amount
                ORDER BY transaction_count DESC
                LIMIT 10");

            Console.WriteLine("  High-risk merchants:");
            while (highRiskResult.HasNext())
            {
                using var row = highRiskResult.GetNext();
                var merchantName = row.GetValueAs<string>(0);
                var category = row.GetValueAs<string>(1);
                var transactionCount = row.GetValueAs<long>(2);
                var avgAmount = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {merchantName} ({category}): {transactionCount} transactions, avg ${avgAmount:F2}");
            }

            // Find merchants with unusual transaction patterns
            using var unusualPatternResult = connection.Query(@"
                MATCH (m:Merchant)-[:Processes]->(t:Transaction)
                WITH m, t, COUNT(t) as transaction_count, AVG(t.amount) as avg_amount
                WHERE transaction_count > 10 AND avg_amount > 1000
                RETURN m.name, m.risk_level, transaction_count, avg_amount
                ORDER BY avg_amount DESC
                LIMIT 10");

            Console.WriteLine("  Merchants with unusual patterns:");
            while (unusualPatternResult.HasNext())
            {
                using var row = unusualPatternResult.GetNext();
                var merchantName = row.GetValueAs<string>(0);
                var riskLevel = row.GetValueAs<string>(1);
                var transactionCount = row.GetValueAs<long>(2);
                var avgAmount = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    {merchantName} ({riskLevel}): {transactionCount} transactions, avg ${avgAmount:F2}");
            }
        }

        private static async Task AnalyzeAccountBehavior(Connection connection)
        {
            // Find accounts with unusual spending patterns
            using var spendingResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WITH a, t, SUM(t.amount) as total_spent, COUNT(t) as transaction_count
                WHERE total_spent > 50000 AND transaction_count > 20
                RETURN a.account_number, total_spent, transaction_count, (total_spent / transaction_count) as avg_transaction
                ORDER BY total_spent DESC
                LIMIT 10");

            Console.WriteLine("  Accounts with unusual spending patterns:");
            while (spendingResult.HasNext())
            {
                using var row = spendingResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var totalSpent = row.GetValueAs<double>(1);
                var transactionCount = row.GetValueAs<long>(2);
                var avgTransaction = row.GetValueAs<double>(3);
                
                Console.WriteLine($"    Account {accountNumber}: ${totalSpent:F2} total, {transactionCount} transactions, avg ${avgTransaction:F2}");
            }

            // Find accounts with declining balance
            using var decliningBalanceResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WHERE t.transaction_type = 'Withdrawal'
                WITH a, SUM(t.amount) as total_withdrawn, COUNT(t) as withdrawal_count
                WHERE total_withdrawn > 20000
                RETURN a.account_number, total_withdrawn, withdrawal_count
                ORDER BY total_withdrawn DESC
                LIMIT 10");

            Console.WriteLine("  Accounts with high withdrawal amounts:");
            while (decliningBalanceResult.HasNext())
            {
                using var row = decliningBalanceResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var totalWithdrawn = row.GetValueAs<double>(1);
                var withdrawalCount = row.GetValueAs<long>(2);
                
                Console.WriteLine($"    Account {accountNumber}: ${totalWithdrawn:F2} withdrawn in {withdrawalCount} transactions");
            }
        }

        private static async Task PerformNetworkAnalysis(Connection connection)
        {
            // Find connected accounts through shared devices
            using var connectedResult = connection.Query(@"
                MATCH (a1:Account)-[:Uses]->(d:Device)<-[:Uses]-(a2:Account)
                WHERE a1.id < a2.id
                RETURN a1.account_number, a2.account_number, d.device_id
                LIMIT 10");

            Console.WriteLine("  Connected accounts through shared devices:");
            while (connectedResult.HasNext())
            {
                using var row = connectedResult.GetNext();
                var account1 = row.GetValueAs<string>(0);
                var account2 = row.GetValueAs<string>(1);
                var deviceId = row.GetValueAs<string>(2);
                
                Console.WriteLine($"    Accounts {account1} and {account2} share device {deviceId}");
            }

            // Find transaction chains
            using var chainResult = connection.Query(@"
                MATCH (t1:Transaction)-[:RelatedTo]->(t2:Transaction)
                WHERE t1.amount > 1000 AND t2.amount > 1000
                RETURN t1.id, t2.id, t1.amount, t2.amount, t1.timestamp, t2.timestamp
                LIMIT 10");

            Console.WriteLine("  Suspicious transaction chains:");
            while (chainResult.HasNext())
            {
                using var row = chainResult.GetNext();
                var transaction1 = row.GetValueAs<long>(0);
                var transaction2 = row.GetValueAs<long>(1);
                var amount1 = row.GetValueAs<double>(2);
                var amount2 = row.GetValueAs<double>(3);
                var timestamp1 = row.GetValueAs<DateTime>(4);
                var timestamp2 = row.GetValueAs<DateTime>(5);
                
                Console.WriteLine($"    Transaction {transaction1} (${amount1:F2} at {timestamp1:HH:mm:ss}) -> Transaction {transaction2} (${amount2:F2} at {timestamp2:HH:mm:ss})");
            }
        }

        private static async Task DetectTimeBasedPatterns(Connection connection)
        {
            // Find transactions at unusual hours
            using var unusualHoursResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WHERE HOUR(t.timestamp) < 6 OR HOUR(t.timestamp) > 22
                RETURN a.account_number, t.amount, t.timestamp
                ORDER BY t.amount DESC
                LIMIT 10");

            Console.WriteLine("  Transactions at unusual hours:");
            while (unusualHoursResult.HasNext())
            {
                using var row = unusualHoursResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var amount = row.GetValueAs<double>(1);
                var timestamp = row.GetValueAs<DateTime>(2);
                
                Console.WriteLine($"    Account {accountNumber}: ${amount:F2} at {timestamp:HH:mm:ss}");
            }

            // Find weekend transactions
            using var weekendResult = connection.Query(@"
                MATCH (a:Account)-[:Initiates]->(t:Transaction)
                WHERE DAYOFWEEK(t.timestamp) IN [1, 7]  -- Sunday and Saturday
                WITH a, COUNT(t) as weekend_transactions, SUM(t.amount) as weekend_total
                WHERE weekend_transactions > 5
                RETURN a.account_number, weekend_transactions, weekend_total
                ORDER BY weekend_total DESC
                LIMIT 10");

            Console.WriteLine("  High weekend activity:");
            while (weekendResult.HasNext())
            {
                using var row = weekendResult.GetNext();
                var accountNumber = row.GetValueAs<string>(0);
                var weekendTransactions = row.GetValueAs<long>(1);
                var weekendTotal = row.GetValueAs<double>(2);
                
                Console.WriteLine($"    Account {accountNumber}: {weekendTransactions} weekend transactions, ${weekendTotal:F2} total");
            }
        }

        // Helper methods to generate test data
        private static List<CustomerData> GenerateCustomers(int count)
        {
            var customers = new List<CustomerData>();
            var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego" };
            var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA" };

            for (int i = 1; i <= count; i++)
            {
                customers.Add(new CustomerData
                {
                    Id = i,
                    Name = $"Customer{i}",
                    SSN = $"000-00-{i:D4}",
                    Phone = $"555-{i:D4}",
                    Email = $"customer{i}@example.com",
                    Address = $"{i} Main St",
                    City = cities[i % cities.Length],
                    State = states[i % states.Length],
                    ZipCode = $"{10000 + i}",
                    DateOfBirth = DateTime.UtcNow.AddYears(-Random.Shared.Next(18, 80))
                });
            }

            return customers;
        }

        private static List<AccountData> GenerateAccounts(int count)
        {
            var accounts = new List<AccountData>();
            var accountTypes = new[] { "Checking", "Savings", "Credit", "Investment" };
            var statuses = new[] { "Active", "Suspended", "Closed" };

            for (int i = 1; i <= count; i++)
            {
                accounts.Add(new AccountData
                {
                    Id = i,
                    AccountNumber = $"ACC{i:D6}",
                    AccountType = accountTypes[i % accountTypes.Length],
                    Balance = Random.Shared.NextDouble() * 100000 + 1000,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(365 * 3)),
                    Status = statuses[i % statuses.Length]
                });
            }

            return accounts;
        }

        private static List<MerchantData> GenerateMerchants(int count)
        {
            var merchants = new List<MerchantData>();
            var categories = new[] { "Retail", "Restaurant", "Gas Station", "Online", "ATM", "Grocery", "Entertainment", "Travel" };
            var locations = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego" };
            var riskLevels = new[] { "Low", "Medium", "High" };

            for (int i = 1; i <= count; i++)
            {
                merchants.Add(new MerchantData
                {
                    Id = i,
                    Name = $"Merchant{i}",
                    Category = categories[i % categories.Length],
                    Location = locations[i % locations.Length],
                    RiskLevel = riskLevels[i % riskLevels.Length]
                });
            }

            return merchants;
        }

        private static List<DeviceData> GenerateDevices(int count)
        {
            var devices = new List<DeviceData>();
            var deviceTypes = new[] { "Mobile", "Desktop", "Tablet", "ATM", "POS" };
            var locations = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego" };

            for (int i = 1; i <= count; i++)
            {
                devices.Add(new DeviceData
                {
                    Id = i,
                    DeviceId = $"DEV{i:D6}",
                    DeviceType = deviceTypes[i % deviceTypes.Length],
                    IpAddress = $"192.168.1.{i % 255}",
                    UserAgent = $"Browser{i}",
                    Location = locations[i % locations.Length]
                });
            }

            return devices;
        }

        private static List<TransactionData> GenerateTransactions(int count)
        {
            var transactions = new List<TransactionData>();
            var transactionTypes = new[] { "Purchase", "Withdrawal", "Transfer", "Deposit", "Payment" };
            var statuses = new[] { "Completed", "Pending", "Failed", "Cancelled" };

            for (int i = 1; i <= count; i++)
            {
                transactions.Add(new TransactionData
                {
                    Id = i,
                    Amount = Random.Shared.NextDouble() * 5000 + 10,
                    TransactionType = transactionTypes[i % transactionTypes.Length],
                    Timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(30)),
                    Status = statuses[i % statuses.Length],
                    Description = $"Transaction {i}"
                });
            }

            return transactions;
        }
    }

    // Data classes
    public class CustomerData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SSN { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
    }

    public class AccountData
    {
        public long Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public double Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class MerchantData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
    }

    public class DeviceData
    {
        public long Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class TransactionData
    {
        public long Id { get; set; }
        public double Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
