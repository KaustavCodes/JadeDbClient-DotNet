using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace JadeDbClient;

public class PostgreSqlDbService : IDatabaseService
{
    private readonly string _connectionString;

    public IDbConnection Connection { get; set; }

    public PostgreSqlDbService(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:DbConnection"];
    }

    /// <summary>
    /// Open a connection to the database
    /// </summary>
    public void OpenConnection()
    {
        Connection = new NpgsqlConnection(_connectionString);
        Connection.Open();
    }

    /// <summary>
    /// Close the connection to the database
    /// </summary>
    public void CloseConnection()
    {
        if (Connection != null && Connection.State == ConnectionState.Open)
        {
            Connection.Close();
        }
    }

    /// <summary>
    /// Creates a new instance of an <see cref="IDbDataParameter"/> for SQL Server.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="dbType">The <see cref="DbType"/> of the parameter.</param>
    /// <param name="direction">The <see cref="ParameterDirection"/> of the parameter. Default is <see cref="ParameterDirection.Input"/>.</param>
    /// <param name="size">The size of the parameter. Default is 0.</param>
    /// <returns>A new instance of <see cref="SqlParameter"/> configured with the specified properties.</returns>
    public IDbDataParameter GetParameter(string name, object value, DbType dbType, ParameterDirection direction = ParameterDirection.Input, int size = 0)
    {
        return new NpgsqlParameter
        {
            ParameterName = name,
            Value = value,
            DbType = dbType,
            Direction = direction,
            Size = size
        };
    }

    /// <summary>
    /// Executes a SQL query asynchronously and maps the result to a collection of objects of type T.
    /// </summary>
    /// <typeparam name="T">The type of objects to which the query results will be mapped. The type T should have properties that match the column names in the query result.</typeparam>
    /// <param name="query">The SQL query to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the SQL query. Default is null.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of objects of type T that represent the rows returned by the query.</returns>
    /// <exception cref="NpgsqlException">Thrown when there is an error executing the query.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is an error creating an instance of type T.</exception>
    /// <exception cref="ArgumentException">Thrown when there is an error setting a property value.</exception>
    public async Task<IEnumerable<T>> ExecuteQueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string query, IEnumerable<IDbDataParameter> parameters = null)
    {
        var results = new List<T>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    DataTable schemaTable = reader.GetSchemaTable();
                    var columnNames = schemaTable.Rows.Cast<DataRow>()
                                        .Select(row => row["ColumnName"].ToString()).ToList();

                    var properties = typeof(T).GetProperties();

                    while (reader.Read())
                    {
                        T instance = Activator.CreateInstance<T>();
                        foreach (var property in properties)
                        {
                            if (columnNames.Contains(property.Name) && !reader.IsDBNull(reader.GetOrdinal(property.Name)))
                            {
                                property.SetValue(instance, reader[property.Name]);
                            }
                        }
                        results.Add(instance);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Executes a SQL query asynchronously and returns the first result object of type T.
    /// </summary>
    /// <typeparam name="T">The type of objects to which the query results will be mapped. The type T should have properties that match the column names in the query result.</typeparam>
    /// <param name="query">The SQL query to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the SQL query. Default is null.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of objects of type T that represent the rows returned by the query.</returns>
    /// <exception cref="NpgsqlException">Thrown when there is an error executing the query.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is an error creating an instance of type T.</exception>
    /// <exception cref="ArgumentException">Thrown when there is an error setting a property value.</exception>
    public async Task<T?> ExecuteQueryFirstRowAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string query, IEnumerable<IDbDataParameter> parameters = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    DataTable schemaTable = reader.GetSchemaTable();
                    var columnNames = schemaTable.Rows.Cast<DataRow>()
                                        .Select(row => row["ColumnName"].ToString()).ToList();

                    var properties = typeof(T).GetProperties();

                    if (reader.Read())
                    {
                        T instance = Activator.CreateInstance<T>();
                        foreach (var property in properties)
                        {
                            if (columnNames.Contains(property.Name) && !reader.IsDBNull(reader.GetOrdinal(property.Name)))
                            {
                                property.SetValue(instance, reader[property.Name]);
                            }
                        }
                        return instance;
                    }
                }
            }
        }

        return default(T);
    }

    /// <summary>
    /// Executes a query and returns a single value (scalar) result.
    /// </summary>
    /// <param name="query">The SQL query to be executed.</param>
    /// <param name="parameters">>A collection of parameters to be used in the SQL query. Default is null.</param>
    public async Task<T?> ExecuteScalar<T>(string query, IEnumerable<IDbDataParameter> parameters = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                var data = await command.ExecuteScalarAsync();
                if (data != null && data != DBNull.Value)
                {
                    return (T)data;
                }
                else
                {
                    return default(T);
                }
            }
        }
    }

    // <summary>
    /// Executes a stored procedure asynchronously and maps the result to a collection of objects of type T.
    /// </summary>
    /// <typeparam name="T">The type of objects to which the stored procedure results will be mapped. The type T should have properties that match the column names in the result set.</typeparam>
    /// <param name="storedProcedureName">The name of the stored procedure to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the stored procedure. Default is null.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of objects of type T that represent the rows returned by the stored procedure.</returns>
    /// <exception cref="SqlException">Thrown when there is an error executing the stored procedure.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is an error creating an instance of type T.</exception>
    /// <exception cref="ArgumentException">Thrown when there is an error setting a property value.</exception>
    public async Task<IEnumerable<T>> ExecuteStoredProcedureSelectDataAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string storedProcedureName, IEnumerable<IDbDataParameter> parameters = null)
    {
        var results = new List<T>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(storedProcedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                using (var adapter = new NpgsqlDataAdapter(command))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    var columnNames = dataTable.Columns.Cast<DataColumn>()
                                        .Select(column => column.ColumnName).ToList();

                    var properties = typeof(T).GetProperties();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        T instance = Activator.CreateInstance<T>();
                        foreach (var property in properties)
                        {
                            if (columnNames.Contains(property.Name) && row[property.Name] != DBNull.Value)
                            {
                                property.SetValue(instance, row[property.Name]);
                            }
                        }
                        results.Add(instance);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Executes a stored procedure asynchronously and returns the number of rows affected.
    /// </summary>
    /// <param name="storedProcedureName">The name of the stored procedure to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the stored procedure. Default is null.</param>
    /// <returns>The number of rows effected after executing the stored procedure.</returns>
    /// <exception cref="SqlException">Thrown when there is an error executing the stored procedure.</exception>
    public async Task<int> ExecuteStoredProcedureAsync(string storedProcedureName, IEnumerable<IDbDataParameter> parameters = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(storedProcedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                int affectedRows = await command.ExecuteNonQueryAsync();

                return affectedRows;
            }
        }
    }

    /// <summary>
    /// Executes a stored procedure asynchronously and retrieves the output parameters.
    /// </summary>
    /// <param name="storedProcedureName">The name of the stored procedure to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the stored procedure. This includes input, output, and input-output parameters.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary where the keys are the names of the output parameters and the values are their corresponding values.</returns>
    /// <exception cref="SqlException">Thrown when there is an error executing the stored procedure.</exception>
    public async Task<Dictionary<string, object>> ExecuteStoredProcedureWithOutputAsync(string storedProcedureName, IEnumerable<IDbDataParameter> parameters)
    {
        var outputValues = new Dictionary<string, object>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(storedProcedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                await command.ExecuteNonQueryAsync();

                foreach (NpgsqlParameter parameter in command.Parameters)
                {
                    if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput)
                    {
                        outputValues.Add(parameter.ParameterName, parameter.Value);
                    }
                }
            }
        }

        return outputValues;
    }


    /// <summary>
    /// Executes a SQL command asynchronously.
    /// </summary>
    /// <param name="commandText">The SQL command to be executed.</param>
    /// <param name="parameters">A collection of parameters to be used in the SQL command. Default is null.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="NpgsqlException">Thrown when there is an error executing the command.</exception>
    public async Task ExecuteCommandAsync(string commandText, IEnumerable<IDbDataParameter> parameters = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(commandText, connection))
            {
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Bulk inserts a DataTable into a PostgreSQL table.
    /// </summary>
    /// <param name="dataTable">The DataTable to insert.</param>
    /// <param name="tableName">The target PostgreSQL table name.</param>
    public async Task<bool> InsertDataTable(string tableName, DataTable dataTable)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // Use COPY command for efficient bulk insertion
        using var writer = connection.BeginBinaryImport($"COPY {tableName} ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}) FROM STDIN (FORMAT BINARY)");

        foreach (DataRow row in dataTable.Rows)
        {
            writer.StartRow();
            foreach (var item in row.ItemArray)
            {
                writer.Write(item ?? DBNull.Value);
            }
        }

        writer.Complete();

        return true;
    }
    
    /// <summary>
    /// Bulk inserts a DataTable with jsonObject into a PostgreSQL table.
    /// </summary>
    /// <param name="dataTable">The DataTable to insert.</param>
    /// <param name="tableName">The target PostgreSQL table name.</param>
    public async Task<bool> InsertDataTableWithJsonData(string tableName, DataTable dataTable)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
    
        using var writer = connection.BeginBinaryImport(
            $"COPY {tableName} ({string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}) FROM STDIN (FORMAT BINARY)");
    
        foreach (DataRow row in dataTable.Rows)
        {
            writer.StartRow();
            foreach (var item in row.ItemArray)
            {
                if (item == null || item == DBNull.Value)
                {
                    writer.Write(DBNull.Value);
                }
                else if (item is System.Text.Json.Nodes.JsonObject json)
                {
                    writer.Write(json.ToJsonString(), NpgsqlTypes.NpgsqlDbType.Jsonb);
                }
                else if (item is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    writer.Write(jsonElement.GetRawText(), NpgsqlTypes.NpgsqlDbType.Jsonb);
                }
                else if (item is Newtonsoft.Json.Linq.JObject jObj)
                {
                    writer.Write(jObj.ToString(Newtonsoft.Json.Formatting.None), NpgsqlTypes.NpgsqlDbType.Jsonb);
                }
                else
                {
                    writer.Write(item);
                }
            }
        }
    
        writer.Complete();
    
        return true;
    }
}