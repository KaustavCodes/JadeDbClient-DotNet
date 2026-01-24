# JadeDbClient

[![.NET 8](https://img.shields.io/badge/.NET-8-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![.NET 9](https://img.shields.io/badge/.NET-9-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![.NET 10](https://img.shields.io/badge/.NET-10-blue.svg)]([https://aka.ms/new-console-template](https://aka.ms/new-console-template))
[![Nuget](https://img.shields.io/nuget/v/JadeDbClient.svg)](https://www.nuget.org/packages/JadeDbClient)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![AOT Compatible](https://img.shields.io/badge/AOT-Compatible-green.svg)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

**JadeDbClient** is a versatile and efficient .NET NuGet package designed to simplify database connections and query execution across multiple database systems: **MySQL**, **SQL Server**, and **PostgreSQL**. It provides common methods to execute queries and stored procedures, making database switching seamless and eliminating the hassle of managing different database clients.

## Features

- **Multi-database Support**: Effortlessly connect to MySQL, SQL Server, and PostgreSQL.
- **Streamlined Query Execution**: Perform queries with ease using a common interface, regardless of the database system.
- **Stored Procedure Support**: Execute stored procedures across different databases without rewriting code.
- **Transaction Support**: Full support for database transactions with commit and rollback capabilities across all database types.
- **Consistent API**: Provides a unified API to eliminate the headaches of switching databases.
- **Native AOT Compatible**: Fully compatible with .NET Native AOT compilation for faster startup times and smaller deployment sizes.

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

Before we begin we need to let the plugin know what atabase we are using and where the plugin needs to connect to.

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
    "DbConnection": "Host=localhost;Database=TestingDb;Username=[DB User Name];Password=[Db Password];SearchPath=JadedSoftwares;"
}
```

Next we need to load the plugin on application start. We can do this in the **Program.cs** file


We need these 2 lines

### Add the using statement

```
using JadeDbClient.Initialize;
```

Initialize the plugin
```
// Call the method to add the database service
builder.Services.AddJadeDbService();
```

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


## How to inteact with the database

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

## Native AOT Compatibility

JadeDbClient is fully compatible with .NET Native AOT (Ahead-of-Time) compilation, which enables:
- **Faster Startup Times**: Your application starts almost instantly
- **Smaller Deployment Size**: Reduced memory footprint and disk space
- **No JIT Compilation**: Everything is compiled ahead of time

### Using JadeDbClient with Native AOT

To use JadeDbClient in a Native AOT application, ensure your model classes follow these guidelines:

1. **Public Parameterless Constructor**: Your model classes must have a public parameterless constructor (or rely on the default constructor).

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    
    // Default parameterless constructor works fine
}
```

2. **Public Properties**: All properties that map to database columns must be public with both getter and setter.

3. **Property Names Match Column Names**: Ensure property names match your database column names.

### Example: Publishing with Native AOT

Add the following to your project file (`.csproj`):

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

Then publish your application:

```bash
dotnet publish -c Release
```

Your application will be compiled with Native AOT, and JadeDbClient will work seamlessly without any reflection warnings!

### How It Works

JadeDbClient uses the `DynamicallyAccessedMembers` attribute on all generic type parameters that require reflection. This tells the AOT compiler to preserve the necessary metadata for your model classes, ensuring everything works correctly at runtime.

Happy Coding! 