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

Happy Coding! 