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

builder.Services.AddJadeDbService();

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

app.Run();

public class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}

[JsonSerializable(typeof(IEnumerable<DataModel>))]
[JsonSerializable(typeof(List<DataModel>))] // Often needed if interface implementation matches list
[JsonSerializable(typeof(DataModel))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
