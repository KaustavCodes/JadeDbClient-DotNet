using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace JadeDbClient.Interfaces;

/// <summary>
/// Library-internal contract for dynamic (ExpandoObject) query execution.
/// This interface is intentionally NOT part of <see cref="IDatabaseService"/> so
/// that the public contract — and any existing custom implementations — remain
/// unchanged. Only the built-in database service classes implement it.
/// </summary>
internal interface IDynamicQueryExecutor
{
    /// <summary>
    /// Executes a SQL query and maps each row to a <see langword="dynamic"/>
    /// (<see cref="System.Dynamic.ExpandoObject"/>) object keyed by column name.
    /// </summary>
    Task<IEnumerable<dynamic>> ExecuteQueryDynamicAsync(string query, IEnumerable<IDbDataParameter>? parameters = null);

    /// <summary>
    /// Executes a SQL query and returns the first row as a <see langword="dynamic"/>
    /// (<see cref="System.Dynamic.ExpandoObject"/>) object, or <c>null</c> when the
    /// result set is empty.
    /// </summary>
    Task<dynamic?> ExecuteQueryFirstRowDynamicAsync(string query, IEnumerable<IDbDataParameter>? parameters = null);
}
