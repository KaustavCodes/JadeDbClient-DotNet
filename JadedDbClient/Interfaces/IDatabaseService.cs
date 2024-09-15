using System.Data;

namespace JadedDbClient.Interfaces;

public interface IDatabaseService
{
    IDbConnection Connection { get; set; }
    void OpenConnection();
    void CloseConnection();
    
    Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, IEnumerable<IDbDataParameter> parameters = null);
    Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string storedProcedureName, IEnumerable<IDbDataParameter> parameters = null);
    Task<Dictionary<string, object>> ExecuteStoredProcedureWithOutputAsync(string storedProcedureName, IEnumerable<IDbDataParameter> parameters);
    Task ExecuteCommandAsync(string command, IEnumerable<IDbDataParameter> parameters = null);

    IDbDataParameter GetParameter(string name, object value, DbType dbType, ParameterDirection direction = ParameterDirection.Input, int size = 0);
}