using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using System.Data;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Register JadeDbService with AOT-compatible pre-compiled mappers
builder.Services.AddJadeDbService(options =>
{
    // Register a pre-compiled mapper for DataModel (AOT-compatible)
    options.RegisterMapper<DataModel>(reader => new DataModel
    {
        id = reader.GetInt32(reader.GetOrdinal("id")),
        name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name"))
    });
    
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
    
    return Results.Ok(new 
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
    
    return Results.Ok(new 
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
    
    return Results.Ok(new 
    { 
        message = "Mixed usage: DataModel with mapper, UserModel with reflection",
        dataModelCount = dataResults.Count(),
        userModelCount = userResults.Count(),
        dataModels = dataResults,
        userModels = userResults
    });
});

app.Run();

// DataModel has a pre-compiled mapper registered
public class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}

// UserModel does NOT have a pre-compiled mapper - uses reflection fallback
public class UserModel
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
}

[JsonSerializable(typeof(IEnumerable<DataModel>))]
[JsonSerializable(typeof(List<DataModel>))]
[JsonSerializable(typeof(DataModel))]
[JsonSerializable(typeof(IEnumerable<UserModel>))]
[JsonSerializable(typeof(List<UserModel>))]
[JsonSerializable(typeof(UserModel))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
