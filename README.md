# JadeDbClient

[![.NET 8](https://img.shields.io/badge/.NET-8-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![.NET 9](https://img.shields.io/badge/.NET-9-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![.NET 10](https://img.shields.io/badge/.NET-10-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![Nuget](https://img.shields.io/nuget/v/JadeDbClient.svg)](https://www.nuget.org/packages/JadeDbClient)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**JadeDbClient** is a versatile and efficient .NET NuGet package designed to simplify database connections and query execution across multiple database systems: **MySQL**, **SQL Server**, and **PostgreSQL**. It provides common methods to execute queries and stored procedures, making database switching seamless and eliminating the hassle of managing different database clients.

## Features

- **Multi-database Support**: Effortlessly connect to MySQL, SQL Server, and PostgreSQL.
- **Streamlined Query Execution**: Perform queries with ease using a common interface, regardless of the database system.
- **Stored Procedure Support**: Execute stored procedures across different databases without rewriting code.
- **Transaction Support**: Full support for database transactions with commit and rollback capabilities across all database types.
- **🚀 Source Generator for AOT**: Automatically generates optimized mappers at compile-time with the `[JadeDbObject]` attribute - no manual registration needed!
- **Native AOT Compatible**: Designed for .NET Native AOT applications with compile-time code generation (Note: Underlying database drivers may still have AOT limitations).
- **Consistent API**: Provides a unified API to eliminate the headaches of switching databases.

## Installation

Install the package via NuGet:

```bash
dotnet add package JadeDbClient
```

Or use the NuGet Package Manager:

```
Install-Package JadeDbClient
```

## Usage

Before we begin we need to let the plugin know what database we are using and where the plugin needs to connect to.

To do this we need to add the following to the web.config or appsettings.json file.

***Important:** Remember to change the connections string as per your database.*

### For MySql Database
```
"DatabaseType": "MySql",
"ConnectionStrings": {
    "DbConnection": "Server=localhost;Port=8889;Database=[Datase Name];User Id=[DB User Name];Password=[Db Password];"
}
```

### For Microsoft SqlServer Database
```
"DatabaseType": "MsSql",
"ConnectionStrings": {
    "DbConnection": "Server=localhost;Database=TestingDb;User Id=[DB User Name];Password=[Db Password];TrustServerCertificate=True;"
}
```

### For PostgreSql Database
```
"DatabaseType": "PostgreSQL",
"ConnectionStrings": {
    "DbConnection": "Host=localhost;Database=TestingDb;Username=[DB User Name];Password=[Db Password];SearchPath=KausCoder;"
}
```

Next we need to load the plugin on application start. We can do this in the **Program.cs** file


We need these 2 lines

### Add the using statement

```
using JadeDbClient.Initialize;
```

### Basic Initialization (Standard Approach)

Initialize the plugin without any custom configuration. The library will use reflection-based mapping automatically:

```csharp
// Call the method to add the database service
builder.Services.AddJadeDbService();
```

**This is the standard approach that works for all .NET applications. Your existing code will continue to work without any changes.**

### Configuration Options

JadeDbClient supports optional logging configuration for development and debugging:

```csharp
builder.Services.AddJadeDbService(
    configure: null, // Mapper configuration (optional)
    serviceOptionsConfigure: options =>
    {
        options.EnableLogging = true;      // Enable timing logs (default: false)
        options.LogExecutedQuery = true;   // Log SQL queries (default: false)
    });
```

**⚠️ Important:** Logging is **disabled by default** for production performance. Enable only during development.

**Backward Compatibility:** Existing code without logging configuration continues to work without any changes.

### Advanced: AOT-Compatible Mappers with Source Generator

> **✨ Recommended Approach** 🎉  
> JadeDbClient now includes a **Source Generator** that automatically creates optimized mappers at compile-time. Simply decorate your models with `[JadeDbObject]` and the mappers are generated for you!

#### The Modern Way: Using `[JadeDbObject]` Attribute

**For your own models**, you no longer need to write manual `RegisterMapper` calls. Just mark your models as `public partial` and add the `[JadeDbObject]` attribute:

```csharp
using JadeDbClient.Attributes;

[JadeDbObject]
public partial class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

[JadeDbObject]
public partial class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

That's it! The Source Generator automatically creates optimized mappers for these classes at compile-time using a `[ModuleInitializer]`, so they're available immediately when your application starts.

#### Simplified Setup

```csharp
using JadeDbClient.Initialize;

var builder = WebApplication.CreateBuilder(args);

// That's all you need! The Source Generator automatically registers all [JadeDbObject] models
builder.Services.AddJadeDbService();

var app = builder.Build();
```

#### Why Use the Source Generator?

**Advantages of `[JadeDbObject]`:**
- ✅ **Zero Boilerplate**: No manual mapper registration needed
- ✅ **.NET Native AOT Support**: Mappers generated at compile-time (works in our testing with trimming warnings)
- ✅ **Better Performance**: No reflection overhead
- ✅ **Compile-Time Type Safety**: Errors caught during compilation
- ✅ **Automatic Null Handling**: Supports nullable types (`int?`, `DateTime?`, `string?`)
- ✅ **Auto-Registration**: Uses `[ModuleInitializer]` for zero-config setup

**When to use each approach:**
- ✅ **Use `[JadeDbObject]`**: For all your own models (recommended for AOT)
- ✅ **Use `RegisterMapper`**: Only for third-party models you cannot modify
- ✅ **Normal JIT Build**: For standard .NET applications with reflection support

#### Manual Registration (Third-Party Models Only)

If you need to map a third-party model that you cannot modify with `[JadeDbObject]`, you can still register mappers manually:

```csharp
builder.Services.AddJadeDbService(options =>
{
    // Only needed for third-party models you cannot modify
    options.RegisterMapper<ThirdPartyModel>(reader => new ThirdPartyModel
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Name = reader.GetString(reader.GetOrdinal("Name"))
    });
});
```

#### Null Safety Example

The Source Generator automatically handles nullable types:

```csharp
[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Stock { get; set; }           // Nullable int
    public DateTime? LastUpdated { get; set; } // Nullable DateTime
    public string? Description { get; set; }   // Nullable string
}
```

When the database returns `DBNull`, the generated mapper assigns `null` for nullable types and appropriate defaults for non-nullable types.

**Benefits of the Source Generator Approach:**
- ✅ Works in .NET Native AOT (tested with SQL Server, MySQL, PostgreSQL)
- ✅ Better performance (no reflection overhead)
- ✅ Compile-time type safety
- ✅ Automatic null handling
- ✅ Compatible with standard JIT builds
- ✅ Mix and match approaches as needed

That's it for the setup part


Now using the plugin as as easy as adding the parameter :IDatabaseService dbConfig" to a function inside your controller or the controller constructor. For ease in this example we are goin to add it to the constructor so we have access to it in all the functions of that class.

```
using System.Data;
using System.Diagnostics;
using JadedDbClient.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebTest.Models;

namespace WebTest.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    IDatabaseService _dbConfig;
    public HomeController(ILogger<HomeController> logger, IDatabaseService dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;
    }
}

```

That's it. We are all ready to start making requests to the databse.


## How to interact with the database

### Understanding Automatic Mapping

**Good News!** 🎉 You don't need to do anything special to use the database methods. The library handles mapping automatically:

- **With `[JadeDbObject]` or manual mappers**: Uses pre-compiled mappers (recommended for AOT)
- **Without pre-compiled mappers**: Falls back to reflection (use standard JIT builds)

**Both approaches work with the same API calls!** Note: For Native AOT, always use `[JadeDbObject]` to avoid reflection.

#### Example: Same Code, Different Mapping Approaches

```csharp
// This code works identically whether you registered a mapper or not!
IEnumerable<UserModel> users = await _dbConfig.ExecuteQueryAsync<UserModel>("SELECT * FROM Users");

// If you registered a mapper for UserModel:
//   -> Uses fast pre-compiled mapper

// If you didn't register a mapper for UserModel:
//   -> Automatically uses reflection (still works!)
```

### All Database Methods

### GetParameter: Created database parameters that you send to databse
Creates a new instance of an <see cref="IDbDataParameter"/> for your Database.
Method Signature: **IDbDataParameter GetParameter(string name, object value, DbType dbType, ParameterDirection direction = ParameterDirection.Input, int size = 0);**

```
//eg: Sample parameterised query

string insrtQry = "INSERT INTO tbl_test(Name) VALUES(@Name);";

List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
dbDataParameters.Add(_dbConfig.GetParameter("@Name", "Someone", DbType.String, ParameterDirection.Input, 250));
```

### ExecuteStoredProcedureWithOutputAsync: Stored Procedure with Output Parameters
Executes a stored procedure asynchronously and retrieves the output parameters.
Method Signature: **Task<Dictionary<string, object>> ExecuteStoredProcedureWithOutputAsync(string storedProcedureName, IEnumerable<IDbDataParameter> parameters);**


```
//Execute a stored proceude with output parameter
List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

dbDataParameters.Add(_dbConfig.GetParameter("p_name", "John Doe", DbType.String, ParameterDirection.Input, 250));
dbDataParameters.Add(_dbConfig.GetParameter("p_OutputParam", "test", DbType.String, ParameterDirection.Output, 250));

Dictionary<string, object> outputParameters = await _dbConfig.ExecuteStoredProcedureWithOutputAsync("add_date", dbDataParameters); 

foreach (var item in outputParameters)
{
    //Print the values of the output parameters. These are parameters that you had set as output
    Console.WriteLine($"{item.Key} : {item.Value}");
}
```


### ExecuteQueryAsync: Execute a query and return results
Executes a SQL query asynchronously and maps the result to a collection of objects of type T.
Method Signature: **Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, IEnumerable<IDbDataParameter> parameters = null);**

```
//Execute a query
string query = "SELECT * FROM tbl_test;";

IEnumerable<DataModel> results = await _dbConfig.ExecuteQueryAsync<DataModel>(query);
```

### ExecuteQueryFirstRowAsync: Execute a query and return the first row
Executes a SQL query asynchronously and maps the first result row to an object of type T.
Method Signature: **Task<T?> ExecuteQueryFirstRowAsync<T>(string query, IEnumerable<IDbDataParameter> parameters = null);**

```
//Execute a query and get the first row only
string query = "SELECT * FROM tbl_test WHERE Id = @Id;";

List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
dbDataParameters.Add(_dbConfig.GetParameter("@Id", 1, DbType.Int32));

DataModel? result = await _dbConfig.ExecuteQueryFirstRowAsync<DataModel>(query, dbDataParameters);
if (result != null)
{
    // Use the result object
    Console.WriteLine($"Name: {result.Name}");
}
else
{
    Console.WriteLine("No data found.");
}
```

### ExecuteScalar: Executes a query and returns a single data item
Use this function to execute any query which returns a single vaule. eg: row count.
Method Signature: **Task<T?> ExecuteScalar<T>(string query, IEnumerable<IDbDataParameter> parameters = null);**

```
//eg: Bulk Insert data into the table

string checkTableExistsQuery = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tbl_test');";
bool dataPresent = await _databaseService.ExecuteScalar<bool>(checkTableExistsQuery);

```

### ExecuteStoredProcedureAsync: Execute a stored procedure without output parameters
Executes a stored procedure asynchronously and returns the number of rows affected.
Method Signature: **Task<int> ExecuteStoredProcedureAsync(string storedProcedureName, IEnumerable<IDbDataParameter> parameters = null);**

```
//Execute a stored procedure without output parameter

List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

dbDataParameters.Add(_dbConfig.GetParameter("p_name", "John Doe", DbType.String, ParameterDirection.Input, 250));

int rowsAffected = await _dbConfig.ExecuteStoredProcedureAsync("add_date", dbDataParameters);
```

### ExecuteStoredProcedureSelectDataAsync: Execute a stored procedure that returns data that can be bound to a model
Executes a stored procedure asynchronously and maps the result to a collection of objects of type T.
Method Signature: **Task<IEnumerable<T>> ExecuteStoredProcedureSelectDataAsync<T>(string storedProcedureName, IEnumerable<IDbDataParameter> parameters = null);**

```
//Execute a stored procedure and return data bound to a model class

IEnumerable<DataModel> results = await _dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("get_data", new List<IDbDataParameter> { _dbConfig.GetParameter("p_limit", 100, DbType.Int32, ParameterDirection.Input, 250) });
```

### ExecuteCommandAsync: Execute a DML query to the database
Executes a SQL command asynchronously.
Method Signature: **Task ExecuteCommandAsync(string command, IEnumerable<IDbDataParameter> parameters = null);**

```
//eg: Insert data into the table

string insrtQry = "INSERT INTO tbl_test(Name) VALUES(@Name);";

List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
dbDataParameters.Add(_dbConfig.GetParameter("@Name", "Someone", DbType.String, ParameterDirection.Input, 250));

await _dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);
```

### InsertDataTable: Bulk insert a data table into the database
Bulk inserts data into a database table. For this to work, the DataTable columns names need to match the Column names in the actual database and the table also needs to exist.
Method Signature: **Task<bool> InsertDataTable(string tableName, DataTable dataTable);**

```
//eg: Bulk Insert data into the table

DataTable tbl = new DataTable(); // This will be your actual data table with columns mathing your actual database table
string tableName = "tbl_ToInsertInto"; //This will be the name of the table in the database.
await _dbConfig.InsertDataTable(tableName, tbl);

```

### BulkInsertAsync: Stream-based bulk insert with strongly-typed objects
Bulk inserts a collection or stream of strongly-typed objects into a database table with optimized performance. This method is more flexible than InsertDataTable as it works directly with your model classes and doesn't require creating a DataTable.

**Two overloads available:**

#### 1. IEnumerable<T> Overload
Method Signature: **Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> items, int batchSize = 1000);**

Best for: In-memory collections, lists, arrays

```csharp
// Example: Bulk insert from a list of objects
public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int? Stock { get; set; }
}

// Generate or load your data
var products = new List<Product>
{
    new Product { ProductId = 1, ProductName = "Laptop", Price = 999.99m, Stock = 50 },
    new Product { ProductId = 2, ProductName = "Mouse", Price = 25.99m, Stock = 200 },
    new Product { ProductId = 3, ProductName = "Keyboard", Price = 79.99m, Stock = null }
};

// Bulk insert with default batch size (1000)
int rowsInserted = await _dbConfig.BulkInsertAsync("Products", products);
Console.WriteLine($"Inserted {rowsInserted} products");

// Or specify a custom batch size
int rowsInserted = await _dbConfig.BulkInsertAsync("Products", products, batchSize: 500);
```

#### 2. IAsyncEnumerable<T> Overload with Progress Reporting
Method Signature: **Task<int> BulkInsertAsync<T>(string tableName, IAsyncEnumerable<T> items, IProgress<int>? progress = null, int batchSize = 1000);**

Best for: Streaming data from APIs, databases, files, or other async sources

```csharp
// Example: Stream data from an API and bulk insert with progress reporting
public async IAsyncEnumerable<Product> FetchProductsFromApiAsync()
{
    int page = 1;
    while (true)
    {
        var response = await httpClient.GetAsync($"https://api.example.com/products?page={page}");
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        
        if (products == null || products.Count == 0)
            break;
            
        foreach (var product in products)
        {
            yield return product;
        }
        
        page++;
    }
}

// Bulk insert with progress reporting
var progress = new Progress<int>(rowCount =>
{
    Console.WriteLine($"Inserted {rowCount} rows so far...");
});

var stream = FetchProductsFromApiAsync();
int totalInserted = await _dbConfig.BulkInsertAsync("Products", stream, progress, batchSize: 1000);
Console.WriteLine($"Completed! Total rows inserted: {totalInserted}");
```

#### Performance Benefits

**Reflection-Free Mode (Recommended):**

When you use `[JadeDbObject]` on your models, bulk insert operations become **reflection-free** and **AOT-compatible**:

```csharp
using JadeDbClient.Attributes;

// Mark your model with [JadeDbObject] for maximum performance
[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int? Stock { get; set; }
}

// Source generator automatically creates property accessors
// Bulk insert uses reflection-free path automatically
var products = GetProducts();
int inserted = await _dbConfig.BulkInsertAsync("Products", products);
```

**Benefits of Reflection-Free Mode:**
- ✅ **Faster Initialization**: No `typeof(T).GetProperties()` calls
- ✅ **Faster Property Access**: Direct property access via generated delegates
- ✅ **AOT Compatible**: Works with .NET Native AOT
- ✅ **Better Performance**: Eliminates reflection overhead
- ✅ **Zero Configuration**: Just add `[JadeDbObject]` attribute

**Fallback Mode:**

Without `[JadeDbObject]`, bulk insert still works using reflection:

```csharp
// Works automatically, but uses reflection
public class Product  // No [JadeDbObject] attribute
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
}

// Still works, uses reflection fallback
await _dbConfig.BulkInsertAsync("Products", products);
```

**Database-Specific Optimizations:**

**PostgreSQL:**
- Uses native COPY BINARY protocol
- Extremely fast, direct streaming
- Minimal memory overhead

**MySQL:**
- Optimized batched multi-value INSERT statements
- Example: `INSERT INTO table VALUES (row1), (row2), (row3)...`
- Significantly faster than row-by-row inserts
- Reduces network round-trips

**SQL Server:**
- Leverages SqlBulkCopy API
- Batch processing for optimal throughput
- Native high-performance bulk insert

**Key Features:**
- ✅ **Memory Efficient**: Streams data instead of loading everything into memory
- ✅ **Type Safe**: Works directly with your model classes
- ✅ **Progress Tracking**: Optional IProgress<int> for real-time feedback
- ✅ **Configurable Batching**: Adjust batch size for your workload
- ✅ **Async/Await**: Fully asynchronous for non-blocking operations
- ✅ **Nullable Support**: Handles nullable properties correctly
- ✅ **Cross-Database**: Same API works across PostgreSQL, MySQL, and SQL Server

#### When to Use Each Method

| Method | Best For | Use Case | Performance |
|--------|----------|----------|-------------|
| **InsertDataTable** | Legacy code, DataTable sources | When you already have a DataTable | Good |
| **BulkInsertAsync (IEnumerable)** | In-memory collections | Bulk insert from lists, arrays, or collections | Fast (Reflection-free with `[JadeDbObject]`) |
| **BulkInsertAsync (IAsyncEnumerable)** | Streaming sources | API responses, file readers, database streaming | Fast (Reflection-free with `[JadeDbObject]`) |

**Performance Tip:** Always use `[JadeDbObject]` on your models for best performance. The source generator creates reflection-free property accessors at compile time.

## Database Transactions

JadeDbClient now supports database transactions across all three database types (SQL Server, MySQL, and PostgreSQL). Transactions allow you to group multiple database operations into a single atomic unit of work.

### BeginTransaction: Start a database transaction
Begins a new database transaction. The connection will be opened automatically if it's not already open.
Method Signature: **IDbTransaction BeginTransaction();**

```csharp
// Begin a transaction
IDbTransaction transaction = _dbConfig.BeginTransaction();
```

### BeginTransaction (with Isolation Level): Start a transaction with a specific isolation level
Begins a new database transaction with the specified isolation level.
Method Signature: **IDbTransaction BeginTransaction(IsolationLevel isolationLevel);**

```csharp
// Begin a transaction with ReadCommitted isolation level
IDbTransaction transaction = _dbConfig.BeginTransaction(IsolationLevel.ReadCommitted);
```

### CommitTransaction: Commit a transaction
Commits the current database transaction, making all changes permanent.
Method Signature: **void CommitTransaction(IDbTransaction transaction);**

```csharp
// Commit the transaction
_dbConfig.CommitTransaction(transaction);
```

### RollbackTransaction: Rollback a transaction
Rolls back the current database transaction, undoing all changes made within the transaction.
Method Signature: **void RollbackTransaction(IDbTransaction transaction);**

```csharp
// Rollback the transaction
_dbConfig.RollbackTransaction(transaction);
```

### Complete Transaction Example

Here's a complete example showing how to use transactions to ensure data consistency:

```csharp
IDbTransaction transaction = null;
try
{
    // Begin transaction
    transaction = _dbConfig.BeginTransaction();
    
    // Execute multiple operations within the transaction
    string insertQuery1 = "INSERT INTO Orders(CustomerId, OrderDate) VALUES(@CustomerId, @OrderDate);";
    List<IDbDataParameter> params1 = new List<IDbDataParameter>();
    params1.Add(_dbConfig.GetParameter("@CustomerId", 1, DbType.Int32));
    params1.Add(_dbConfig.GetParameter("@OrderDate", DateTime.Now, DbType.DateTime));
    
    await _dbConfig.ExecuteCommandAsync(insertQuery1, params1);
    
    string insertQuery2 = "INSERT INTO OrderItems(OrderId, ProductId, Quantity) VALUES(@OrderId, @ProductId, @Quantity);";
    List<IDbDataParameter> params2 = new List<IDbDataParameter>();
    params2.Add(_dbConfig.GetParameter("@OrderId", 1, DbType.Int32));
    params2.Add(_dbConfig.GetParameter("@ProductId", 100, DbType.Int32));
    params2.Add(_dbConfig.GetParameter("@Quantity", 5, DbType.Int32));
    
    await _dbConfig.ExecuteCommandAsync(insertQuery2, params2);
    
    // If all operations succeed, commit the transaction
    _dbConfig.CommitTransaction(transaction);
    Console.WriteLine("Transaction committed successfully.");
}
catch (Exception ex)
{
    // If any operation fails, rollback the transaction
    if (transaction != null)
    {
        _dbConfig.RollbackTransaction(transaction);
        Console.WriteLine("Transaction rolled back due to error: " + ex.Message);
    }
}
finally
{
    // Clean up
    transaction?.Dispose();
    _dbConfig.CloseConnection();
}
```

### Transaction Isolation Levels

JadeDbClient supports all standard isolation levels:
- `IsolationLevel.ReadUncommitted`
- `IsolationLevel.ReadCommitted` (default for most databases)
- `IsolationLevel.RepeatableRead`
- `IsolationLevel.Serializable`
- `IsolationLevel.Snapshot` (SQL Server only)

Choose the appropriate isolation level based on your concurrency requirements and database system.

## Complete Real-World Example

Here's a complete example showing how to use JadeDbClient with the Source Generator approach:

### Scenario: E-commerce Order Management

#### Step 1: Define Your Models with `[JadeDbObject]`

```csharp
using JadeDbClient.Attributes;

// Simply add [JadeDbObject] attribute - mappers are generated automatically!
[JadeDbObject]
public partial class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

[JadeDbObject]
public partial class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Stock { get; set; }  // Nullable - handled automatically
}
```

#### Step 2: Setup (Program.cs)

```csharp
using JadeDbClient.Initialize;

var builder = WebApplication.CreateBuilder(args);

// That's it! The Source Generator automatically registers all [JadeDbObject] models
// No manual mapper registration needed!
builder.Services.AddJadeDbService();

var app = builder.Build();
app.Run();
```

#### Step 3: Using in Your Service/Controller

```csharp
using System.Data;
using JadeDbClient.Interfaces;

public class OrderService
{
    private readonly IDatabaseService _dbConfig;
    
    public OrderService(IDatabaseService dbConfig)
    {
        _dbConfig = dbConfig;
    }
    
    // All models with [JadeDbObject] use optimized generated mappers
    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        string query = "SELECT * FROM Orders WHERE Status = @Status";
        
        var parameters = new List<IDbDataParameter>
        {
            _dbConfig.GetParameter("@Status", "Active", DbType.String)
        };
        
        // If Order mapper is registered -> uses pre-compiled mapper
        // If not registered -> uses automatic reflection
        return await _dbConfig.ExecuteQueryAsync<Order>(query, parameters);
    }
    
    // Get customer details (works with or without registered mapper!)
    public async Task<Customer?> GetCustomerAsync(int customerId)
    {
        string query = "SELECT * FROM Customers WHERE CustomerId = @Id";
        
        var parameters = new List<IDbDataParameter>
        {
            _dbConfig.GetParameter("@Id", customerId, DbType.Int32)
        };
        
        return await _dbConfig.ExecuteQueryFirstRowAsync<Customer>(query, parameters);
    }
    
    // Get products (automatically uses reflection since no mapper registered)
    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        string query = "SELECT * FROM Products";
        
        // Product doesn't have a registered mapper, so it uses reflection
        // This is perfectly fine for less frequently-queried models!
        return await _dbConfig.ExecuteQueryAsync<Product>(query);
    }
    
    // Create order with transaction
    public async Task<bool> CreateOrderAsync(Order order, List<OrderItem> items)
    {
        IDbTransaction? transaction = null;
        try
        {
            transaction = _dbConfig.BeginTransaction();
            
            // Insert order
            string insertOrderQuery = @"
                INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status) 
                VALUES (@CustomerId, @OrderDate, @TotalAmount, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            
            var orderParams = new List<IDbDataParameter>
            {
                _dbConfig.GetParameter("@CustomerId", order.CustomerId, DbType.Int32),
                _dbConfig.GetParameter("@OrderDate", order.OrderDate, DbType.DateTime),
                _dbConfig.GetParameter("@TotalAmount", order.TotalAmount, DbType.Decimal),
                _dbConfig.GetParameter("@Status", order.Status, DbType.String)
            };
            
            int orderId = await _dbConfig.ExecuteScalar<int>(insertOrderQuery, orderParams);
            
            // Insert order items
            foreach (var item in items)
            {
                string insertItemQuery = @"
                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price) 
                    VALUES (@OrderId, @ProductId, @Quantity, @Price)";
                
                var itemParams = new List<IDbDataParameter>
                {
                    _dbConfig.GetParameter("@OrderId", orderId, DbType.Int32),
                    _dbConfig.GetParameter("@ProductId", item.ProductId, DbType.Int32),
                    _dbConfig.GetParameter("@Quantity", item.Quantity, DbType.Int32),
                    _dbConfig.GetParameter("@Price", item.Price, DbType.Decimal)
                };
                
                await _dbConfig.ExecuteCommandAsync(insertItemQuery, itemParams);
            }
            
            _dbConfig.CommitTransaction(transaction);
            return true;
        }
        catch (Exception)
        {
            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
            _dbConfig.CloseConnection();
        }
    }
}
```

### Key Takeaways

1. **Zero Boilerplate**: Just add `[JadeDbObject]` to your models - from 35 lines to 1 attribute!
2. **Automatic Registration**: Source Generator uses `[ModuleInitializer]` for instant availability
3. **Mix and match**: Use `[JadeDbObject]` for your models, manual registration for third-party models, or standard JIT builds for dynamic scenarios
4. **AOT Support**: Works in our testing with .NET Native AOT (SQL Server, MySQL, PostgreSQL) - **thorough testing mandatory**
5. **Null Safety**: Nullable types (`int?`, `DateTime?`, `string?`) handled automatically


## Native AOT Compatibility & Limitations
 
**JadeDbClient** is designed to be AOT-friendly by using Source Generators to avoid runtime reflection for object mapping. 

**Testing Results**: In our testing, JadeDbClient worked successfully with .NET Native AOT for SQL Server, MySQL, and PostgreSQL, though with expected trimming warnings from database drivers.
 
**Important - Use with Caution**:

1. **Database Driver Warnings**: The underlying database drivers (`Microsoft.Data.SqlClient`, `MySqlConnector`, `Npgsql`) produce trim/AOT warnings during Native AOT publish (e.g., `IL2104`, `IL3053`). These warnings are:
   - **Expected and documented** by the driver maintainers
   - **Outside of JadeDbClient's control** - they originate from the driver packages
   - **Not blocking compilation** - your application will compile and run

2. **Testing is Non-Negotiable**: Due to the aggressive trimming nature of .NET Native AOT, thorough testing of every functionality is **essential** before production deployment. Without `[JadeDbObject]`, the application may fall back to reflection mode, which can cause unexpected behaviors in AOT builds.

3. **Normal JIT Builds**: If you do *not* use `[JadeDbObject]` and need reflection support, use standard .NET JIT builds instead of Native AOT.

**Example AOT Warnings You'll See**:
```bash
warning IL2104: Assembly 'Microsoft.Data.SqlClient' produced trim warnings
warning IL3053: Assembly 'Microsoft.Data.SqlClient' produced AOT analysis warnings
warning IL2104: Assembly 'MySqlConnector' produced trim warnings
warning IL2104: Assembly 'System.Configuration.ConfigurationManager' produced trim warnings
```

**Recommendation**: 
- ✅ Always use `[JadeDbObject]` for your models in AOT applications
- ⚠️ **Testing is mandatory** - test every functionality thoroughly in a staging environment
- ⚠️ Use Native AOT with caution - aggressive trimming may cause unexpected behaviors
- ✅ Monitor database driver releases for AOT compatibility improvements
- ✅ For dynamic scenarios, use standard JIT builds instead

## 📚 Documentation
- **[GitHub Repository](https://github.com/KaustavCodes/JadeDbClient-DotNet)** - Source code and issue tracker

Happy Coding! 

## License

This project is licensed under the MIT License.
