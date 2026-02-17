using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using System.Data;
using JadeDbClient.Attributes;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Register JadeDbService with AOT-compatible pre-compiled mappers
builder.Services.AddJadeDbService(options =>
{
    // Register a pre-compiled mapper for DataModel (AOT-compatible)
    // options.RegisterMapper<DataModel>(reader => new DataModel
    // {
    //     id = reader.GetInt32(reader.GetOrdinal("id")),
    //     name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name"))
    // });

    // UserModel will use automatic reflection mapping (testing fallback)
    // No mapper registered for UserModel - it will use reflection automatically
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test-postgres", async (IDatabaseService dbConfig) =>
{
    //Execute a stored proceude with output parameter
    List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

    dbDataParameters.Add(dbConfig.GetParameter("p_name", "Jaded", DbType.String, ParameterDirection.Input, 250));
    dbDataParameters.Add(dbConfig.GetParameter("p_outputparam", "test", DbType.String, ParameterDirection.Output, 250));

    Dictionary<string, object> outputParameters = await dbConfig.ExecuteStoredProcedureWithOutputAsync("add_data", dbDataParameters);

    foreach (var item in outputParameters)
    {
        Console.WriteLine($"{item.Key} : {item.Value}");
    }

    //Execute a stored procedure without output parameter
    dbDataParameters = new List<IDbDataParameter>();
    dbDataParameters.Add(dbConfig.GetParameter("p_name", "Jaded", DbType.String, ParameterDirection.Input, 250));
    dbDataParameters.Add(dbConfig.GetParameter("p_outputparam", "test", DbType.String, ParameterDirection.Output, 250));

    await dbConfig.ExecuteStoredProcedureAsync("add_data", dbDataParameters);

    //Execute a query
    string query = "SELECT * FROM public.tbl_test;";

    IEnumerable<DataModel> results = await dbConfig.ExecuteQueryAsync<DataModel>(query);

    return results;
});

app.MapGet("/test-mysql", async (IDatabaseService dbConfig) =>
{
    string insrtQry = "INSERT INTO tbl_test(name) VALUES(@name);";

    List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
    dbDataParameters.Add(dbConfig.GetParameter("@name", "Someone1", DbType.String, ParameterDirection.Input, 250));

    await dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);

    IEnumerable<DataModel> results = await dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("get_data", new List<IDbDataParameter> { dbConfig.GetParameter("p_limit", 1000, DbType.Int32, ParameterDirection.Input, 250) });

    return results;
});

app.MapGet("/test-mssql", async (IDatabaseService dbConfig) =>
{
    string insrtQry = "INSERT INTO tbl_test(name) VALUES(@name);";

    List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
    dbDataParameters.Add(dbConfig.GetParameter("@name", "MsSqlUser", DbType.String, ParameterDirection.Input, 250));

    await dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);

    IEnumerable<DataModel> results = await dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("get_data", new List<IDbDataParameter> { dbConfig.GetParameter("p_limit", 1000, DbType.Int32, ParameterDirection.Input, 250) });

    return results;
});

// Test endpoint for AOT mapper with pre-compiled mapper
app.MapGet("/test-aot-mapper", async (IDatabaseService dbConfig) =>
{
    // This uses DataModel which has a pre-compiled mapper registered
    // Should use the fast, AOT-compatible mapper
    // Using TOP for SQL Server compatibility (works in MsSql, ignored in PostgreSQL/MySQL)
    string query = "SELECT TOP 10 * FROM tbl_test;";
    IEnumerable<DataModel> results = await dbConfig.ExecuteQueryAsync<DataModel>(query);

    return Results.Ok(new DataModelResponse
    {
        message = "Using pre-compiled mapper for DataModel",
        count = results.Count(),
        data = results
    });
});

// Test endpoint for automatic reflection fallback
app.MapGet("/test-aot-reflection", async (IDatabaseService dbConfig) =>
{
    // This uses UserModel which does NOT have a pre-compiled mapper
    // Should automatically fall back to reflection-based mapping
    string query = "SELECT TOP 10 id as UserId, name as UserName FROM tbl_test;";
    IEnumerable<UserModel> results = await dbConfig.ExecuteQueryAsync<UserModel>(query);

    return Results.Ok(new UserModelResponse
    {
        message = "Using automatic reflection for UserModel (no mapper registered)",
        count = results.Count(),
        data = results
    });
});

// Test endpoint for mixed usage (both approaches in one request)
app.MapGet("/test-aot-mixed", async (IDatabaseService dbConfig) =>
{
    // First query uses pre-compiled mapper
    string query1 = "SELECT TOP 5 * FROM tbl_test;";
    IEnumerable<DataModel> dataResults = await dbConfig.ExecuteQueryAsync<DataModel>(query1);

    // Second query uses reflection fallback
    string query2 = "SELECT TOP 5 id as UserId, name as UserName FROM tbl_test;";
    IEnumerable<UserModel> userResults = await dbConfig.ExecuteQueryAsync<UserModel>(query2);

    return Results.Ok(new MixedResponse
    {
        message = "Mixed usage: DataModel with mapper, UserModel with reflection",
        dataModelCount = dataResults.Count(),
        userModelCount = userResults.Count(),
        dataModels = dataResults,
        userModels = userResults
    });
});

// ========== PERFORMANCE TESTING APIs ==========

// PostgreSQL Performance Test
app.MapPost("/perf-test-postgres-bulk-insert", async (IDatabaseService dbConfig) =>
{
    var modes = new List<PerformanceModeResult>();
    var testData = GenerateTestProducts(1000);

    // Mode 1: InsertDataTable (Legacy)
    await TruncateProductsTable(dbConfig);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var dataTable = ProductsToDataTable(testData);
    await dbConfig.InsertDataTable("products", dataTable);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "InsertDataTable (Legacy DataTable)",
        rowsInserted = 1000,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(1000.0 / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 2: BulkInsertAsync with IEnumerable
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    int rows = await dbConfig.BulkInsertAsync("products", testData, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IEnumerable (Reflection-Free)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 3: BulkInsertAsync with IAsyncEnumerable
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    rows = await dbConfig.BulkInsertAsync("products", GenerateTestProductsAsync(1000), progress: null, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IAsyncEnumerable (Streaming)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    return Results.Ok(new BulkInsertPerformanceResponse
    {
        database = "PostgreSQL",
        totalRecords = 1000,
        modes = modes
    });
});

// MySQL Performance Test
app.MapPost("/perf-test-mysql-bulk-insert", async (IDatabaseService dbConfig) =>
{
    var modes = new List<PerformanceModeResult>();
    var testData = GenerateTestProducts(1000);

    // Mode 1: InsertDataTable (Legacy)
    await TruncateProductsTable(dbConfig);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var dataTable = ProductsToDataTable(testData);
    await dbConfig.InsertDataTable("products", dataTable);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "InsertDataTable (Legacy DataTable)",
        rowsInserted = 1000,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(1000.0 / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 2: BulkInsertAsync with IEnumerable (Batched INSERT)
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    int rows = await dbConfig.BulkInsertAsync("products", testData, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IEnumerable (Batched Multi-Value INSERT)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 3: BulkInsertAsync with IAsyncEnumerable
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    rows = await dbConfig.BulkInsertAsync("products", GenerateTestProductsAsync(1000), progress: null, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IAsyncEnumerable (Streaming + Batched INSERT)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    return Results.Ok(new BulkInsertPerformanceResponse
    {
        database = "MySQL",
        totalRecords = 1000,
        modes = modes
    });
});

// SQL Server Performance Test
app.MapPost("/perf-test-mssql-bulk-insert", async (IDatabaseService dbConfig) =>
{
    var modes = new List<PerformanceModeResult>();
    var testData = GenerateTestProducts(1000);

    // Mode 1: InsertDataTable (Legacy)
    await TruncateProductsTable(dbConfig);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var dataTable = ProductsToDataTable(testData);
    await dbConfig.InsertDataTable("products", dataTable);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "InsertDataTable (Legacy DataTable)",
        rowsInserted = 1000,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(1000.0 / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 2: BulkInsertAsync with IEnumerable (SqlBulkCopy)
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    int rows = await dbConfig.BulkInsertAsync("products", testData, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IEnumerable (SqlBulkCopy Reflection-Free)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    // Mode 3: BulkInsertAsync with IAsyncEnumerable
    await TruncateProductsTable(dbConfig);
    sw.Restart();
    rows = await dbConfig.BulkInsertAsync("products", GenerateTestProductsAsync(1000), progress: null, batchSize: 1000);
    sw.Stop();
    modes.Add(new PerformanceModeResult
    {
        mode = "BulkInsertAsync IAsyncEnumerable (SqlBulkCopy Streaming)",
        rowsInserted = rows,
        elapsedMilliseconds = sw.ElapsedMilliseconds,
        recordsPerSecond = Math.Round(rows / (sw.ElapsedMilliseconds / 1000.0), 2)
    });

    return Results.Ok(new BulkInsertPerformanceResponse
    {
        database = "MSSQL",
        totalRecords = 1000,
        modes = modes
    });
});

// ========== BULK INSERT TESTS ==========

// PostgreSQL Bulk Insert Tests
app.MapGet("/test-postgres-bulk-insert", async (IDatabaseService dbConfig) =>
{
    // Generate test data
    var products = GenerateTestProducts(100);

    // Test bulk insert with IEnumerable (uses reflection-free path with [JadeDbObject])
    int rowsInserted = await dbConfig.BulkInsertAsync("products", products, batchSize: 50);

    return Results.Ok(new BulkInsertResponse
    {
        message = "PostgreSQL bulk insert with IEnumerable (reflection-free with [JadeDbObject])",
        database = "PostgreSQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        totalItems = products.Count
    });
});

app.MapGet("/test-postgres-bulk-insert-stream", async (IDatabaseService dbConfig) =>
{
    var progressValues = new List<int>();
    var progress = new Progress<int>(count => progressValues.Add(count));

    // Generate async stream of test data
    var productStream = GenerateTestProductsAsync(200);

    // Test bulk insert with IAsyncEnumerable and progress reporting
    int rowsInserted = await dbConfig.BulkInsertAsync("products", productStream, progress, batchSize: 50);

    return Results.Ok(new BulkInsertStreamResponse
    {
        message = "PostgreSQL bulk insert with IAsyncEnumerable and progress (reflection-free)",
        database = "PostgreSQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        progressReports = progressValues
    });
});

// MySQL Bulk Insert Tests
app.MapGet("/test-mysql-bulk-insert", async (IDatabaseService dbConfig) =>
{
    var products = GenerateTestProducts(100);

    // Test bulk insert with IEnumerable (optimized batched INSERT)
    int rowsInserted = await dbConfig.BulkInsertAsync("products", products, batchSize: 50);

    return Results.Ok(new BulkInsertResponse
    {
        message = "MySQL bulk insert with batched multi-value INSERT (reflection-free)",
        database = "MySQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        totalItems = products.Count
    });
});

app.MapGet("/test-mysql-bulk-insert-stream", async (IDatabaseService dbConfig) =>
{
    var progressValues = new List<int>();
    var progress = new Progress<int>(count => progressValues.Add(count));

    var productStream = GenerateTestProductsAsync(200);

    // Test bulk insert with IAsyncEnumerable and progress
    int rowsInserted = await dbConfig.BulkInsertAsync("products", productStream, progress, batchSize: 50);

    return Results.Ok(new BulkInsertStreamResponse
    {
        message = "MySQL bulk insert with IAsyncEnumerable and progress (batched INSERT)",
        database = "MySQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        progressReports = progressValues
    });
});

// SQL Server Bulk Insert Tests
app.MapGet("/test-mssql-bulk-insert", async (IDatabaseService dbConfig) =>
{
    var products = GenerateTestProducts(100);

    // Test bulk insert with IEnumerable (uses SqlBulkCopy)
    int rowsInserted = await dbConfig.BulkInsertAsync("products", products, batchSize: 50);

    return Results.Ok(new BulkInsertResponse
    {
        message = "SQL Server bulk insert with SqlBulkCopy (reflection-free)",
        database = "MSSQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        totalItems = products.Count
    });
});

app.MapGet("/test-mssql-bulk-insert-stream", async (IDatabaseService dbConfig) =>
{
    var progressValues = new List<int>();
    var progress = new Progress<int>(count => progressValues.Add(count));

    var productStream = GenerateTestProductsAsync(200);

    // Test bulk insert with IAsyncEnumerable and progress
    int rowsInserted = await dbConfig.BulkInsertAsync("products", productStream, progress, batchSize: 50);

    return Results.Ok(new BulkInsertStreamResponse
    {
        message = "SQL Server bulk insert with IAsyncEnumerable and progress (SqlBulkCopy)",
        database = "MSSQL",
        rowsInserted = rowsInserted,
        batchSize = 50,
        progressReports = progressValues
    });
});

app.Run();

// ========== HELPER METHODS ==========

static List<Product> GenerateTestProducts(int count)
{
    var products = new List<Product>();
    var random = new Random();

    for (int i = 1; i <= count; i++)
    {
        products.Add(new Product
        {
            ProductId = i,
            ProductName = $"Product_{i}",
            Price = Math.Round((decimal)(random.NextDouble() * 1000), 2),
            Stock = random.Next(0, 2) == 0 ? random.Next(1, 500) : null
        });
    }

    return products;
}

static async IAsyncEnumerable<Product> GenerateTestProductsAsync(int count)
{
    var random = new Random();

    for (int i = 1; i <= count; i++)
    {
        await Task.Delay(1); // Simulate async operation
        yield return new Product
        {
            ProductId = i,
            ProductName = $"StreamProduct_{i}",
            Price = Math.Round((decimal)(random.NextDouble() * 1000), 2),
            Stock = random.Next(0, 2) == 0 ? random.Next(1, 500) : null
        };
    }
}

static async Task TruncateProductsTable(IDatabaseService dbConfig)
{
    try
    {
        // Try TRUNCATE first (works for PostgreSQL, MySQL, SQL Server)
        await dbConfig.ExecuteCommandAsync("TRUNCATE TABLE products", null);
    }
    catch
    {
        // Fallback to DELETE if TRUNCATE fails
        await dbConfig.ExecuteCommandAsync("DELETE FROM products", null);
    }
}

static DataTable ProductsToDataTable(List<Product> products)
{
    var dataTable = new DataTable();
    dataTable.Columns.Add("ProductId", typeof(int));
    dataTable.Columns.Add("ProductName", typeof(string));
    dataTable.Columns.Add("Price", typeof(decimal));
    dataTable.Columns.Add("Stock", typeof(int));

    foreach (var product in products)
    {
        var row = dataTable.NewRow();
        row["ProductId"] = product.ProductId;
        row["ProductName"] = product.ProductName;
        row["Price"] = product.Price;
        row["Stock"] = product.Stock ?? (object)DBNull.Value;
        dataTable.Rows.Add(row);
    }

    return dataTable;
}

// ========== MODELS ==========

// DataModel has a pre-compiled mapper registered
[JadeDbObject]
public partial class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}

// UserModel does NOT have a pre-compiled mapper - uses reflection fallback
[JadeDbObject]
public partial class UserModel
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
}

// Product model for bulk insert tests (uses reflection-free source generator)
[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Stock { get; set; }
}

public class DataModelResponse
{
    public string message { get; set; } = "";
    public int count { get; set; }
    public IEnumerable<DataModel> data { get; set; } = Array.Empty<DataModel>();
}

public class UserModelResponse
{
    public string message { get; set; } = "";
    public int count { get; set; }
    public IEnumerable<UserModel> data { get; set; } = Array.Empty<UserModel>();
}

public class MixedResponse
{
    public string message { get; set; } = "";
    public int dataModelCount { get; set; }
    public int userModelCount { get; set; }
    public IEnumerable<DataModel> dataModels { get; set; } = Array.Empty<DataModel>();
    public IEnumerable<UserModel> userModels { get; set; } = Array.Empty<UserModel>();
}

public class BulkInsertResponse
{
    public string message { get; set; } = "";
    public string database { get; set; } = "";
    public int rowsInserted { get; set; }
    public int batchSize { get; set; }
    public int totalItems { get; set; }
}

public class BulkInsertStreamResponse
{
    public string message { get; set; } = "";
    public string database { get; set; } = "";
    public int rowsInserted { get; set; }
    public int batchSize { get; set; }
    public List<int> progressReports { get; set; } = new();
}

public class BulkInsertPerformanceResponse
{
    public string database { get; set; } = "";
    public int totalRecords { get; set; }
    public List<PerformanceModeResult> modes { get; set; } = new();
}

public class PerformanceModeResult
{
    public string mode { get; set; } = "";
    public int rowsInserted { get; set; }
    public long elapsedMilliseconds { get; set; }
    public double recordsPerSecond { get; set; }
}

[JsonSerializable(typeof(IEnumerable<DataModel>))]
[JsonSerializable(typeof(List<DataModel>))]
[JsonSerializable(typeof(DataModel))]
[JsonSerializable(typeof(IEnumerable<UserModel>))]
[JsonSerializable(typeof(List<UserModel>))]
[JsonSerializable(typeof(UserModel))]
[JsonSerializable(typeof(IEnumerable<Product>))]
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(DataModelResponse))]
[JsonSerializable(typeof(UserModelResponse))]
[JsonSerializable(typeof(MixedResponse))]
[JsonSerializable(typeof(BulkInsertResponse))]
[JsonSerializable(typeof(BulkInsertStreamResponse))]
[JsonSerializable(typeof(BulkInsertPerformanceResponse))]
[JsonSerializable(typeof(PerformanceModeResult))]
[JsonSerializable(typeof(List<PerformanceModeResult>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
