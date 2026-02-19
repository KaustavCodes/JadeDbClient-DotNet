using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JadeDbClient.Interfaces;
using JadeDbClient.Enums;
using JadeDbClient.Helpers;

namespace JadeDbClient.Helpers;

public class QueryBuilder<T> where T : class
{
    private readonly IDatabaseService _dbService;
    private readonly DatabaseDialect _dialect;
    private readonly string _tableName;

    private string[]? _selectColumns;
    private Expression<Func<T, bool>>? _whereExpression;
    private string? _orderBy;
    private int? _limit;
    private int? _skip;
    private readonly List<IDbDataParameter> _parameters = new();

    public QueryBuilder(IDatabaseService dbService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _dialect = dbService.Dialect;
        _tableName = ReflectionHelper.GetTableName(typeof(T));
    }

    // Fluent methods (same as before)
    public QueryBuilder<T> Select(params string[] columns)
    {
        _selectColumns = columns;
        return this;
    }

    public QueryBuilder<T> Where(Expression<Func<T, bool>> where)
    {
        _whereExpression = where;
        return this;
    }

    public QueryBuilder<T> OrderBy(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    public QueryBuilder<T> Take(int limit)
    {
        _limit = limit;
        return this;
    }

    public QueryBuilder<T> Skip(int skip)
    {
        _skip = skip;
        return this;
    }

    // Build SELECT
    public (string Sql, IEnumerable<IDbDataParameter> Parameters) BuildSelect()
    {
        var sb = new StringBuilder("SELECT ");

        var props = ReflectionHelper.GetMappableProperties(typeof(T));
        var columns = _selectColumns?.Length > 0
            ? _selectColumns
            : ReflectionHelper.GetColumnNames(props);

        sb.Append(string.Join(", ", columns));
        sb.Append(" FROM ").Append(_tableName);

        AppendWhere(sb);

        if (!string.IsNullOrWhiteSpace(_orderBy))
            sb.Append(" ORDER BY ").Append(_orderBy);

        AppendPaging(sb);

        return (sb.ToString(), _parameters);
    }

    // Build INSERT (example - expand as needed)
    public (string Sql, IEnumerable<IDbDataParameter> Parameters) BuildInsert(T entity, bool returnIdentity = false)
    {
        var props = ReflectionHelper.GetMappableProperties(typeof(T))
            .Where(p => p.Name != "Id" && p.CanWrite) // skip auto-id by convention
            .ToArray();

        var columns = ReflectionHelper.GetColumnNames(props);
        var paramPlaceholders = new List<string>();
        int paramIndex = 0;

        foreach (var prop in props)
        {
            var paramName = $"@p{paramIndex++}";
            paramPlaceholders.Add(paramName);
            var value = prop.GetValue(entity);
            _parameters.Add(_dbService.GetParameter(paramName, value ?? DBNull.Value, InferDbType(prop.PropertyType)));
        }

        var sql = new StringBuilder($"INSERT INTO {_tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramPlaceholders)})");

        if (returnIdentity)
        {
            sql.Append(_dialect switch
            {
                DatabaseDialect.PostgreSql => " RETURNING id",
                DatabaseDialect.MsSql => " OUTPUT INSERTED.id",
                DatabaseDialect.MySql => "; SELECT LAST_INSERT_ID()",
                _ => ""
            });
        }

        return (sql.ToString(), _parameters);
    }

    // Build UPDATE
    public (string Sql, IEnumerable<IDbDataParameter> Parameters) BuildUpdate(T entity)
    {
        if (_whereExpression == null)
            throw new InvalidOperationException("WHERE clause is required for UPDATE operations.");

        var props = ReflectionHelper.GetMappableProperties(typeof(T))
            .Where(p => p.Name != "Id" && p.CanWrite)
            .ToArray();

        var setClauses = new List<string>();
        int paramIndex = 0;

        foreach (var prop in props)
        {
            var paramName = $"@p{paramIndex++}";
            setClauses.Add($"{ReflectionHelper.GetColumnName(prop)} = {paramName}");
            var value = prop.GetValue(entity);
            _parameters.Add(_dbService.GetParameter(paramName, value ?? DBNull.Value, InferDbType(prop.PropertyType)));
        }

        var sql = new StringBuilder($"UPDATE {_tableName} SET {string.Join(", ", setClauses)}");

        AppendWhere(sql);

        return (sql.ToString(), _parameters);
    }

    // Build DELETE
    public (string Sql, IEnumerable<IDbDataParameter> Parameters) BuildDelete()
    {
        if (_whereExpression == null)
            throw new InvalidOperationException("WHERE clause is required for DELETE operations to prevent accidental full table deletion.");

        var sql = new StringBuilder($"DELETE FROM {_tableName}");
        AppendWhere(sql);

        return (sql.ToString(), _parameters);
    }

    private void AppendWhere(StringBuilder sb)
    {
        if (_whereExpression == null) return;

        var visitor = new ExpressionToSqlVisitor<T>(_dbService);
        var (whereClause, whereParams) = visitor.Translate(_whereExpression);

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sb.Append(" WHERE ").Append(whereClause);
            _parameters.AddRange(whereParams);
        }
    }

    private void AppendPaging(StringBuilder sb)
    {
        if (_limit == null && _skip == null) return;

        switch (_dialect)
        {
            case DatabaseDialect.MsSql:
                if (string.IsNullOrWhiteSpace(_orderBy))
                    throw new InvalidOperationException("ORDER BY is required when using Skip/Take with SQL Server.");

                if (_skip.HasValue)
                    sb.Append($" OFFSET {_skip.Value} ROWS");

                if (_limit.HasValue)
                    sb.Append($" FETCH NEXT {_limit.Value} ROWS ONLY");
                break;

            case DatabaseDialect.PostgreSql:
            case DatabaseDialect.MySql:
                if (_limit.HasValue)
                    sb.Append($" LIMIT {_limit.Value}");

                if (_skip.HasValue)
                    sb.Append($" OFFSET {_skip.Value}");
                break;

            default:
                throw new NotSupportedException($"Paging not implemented for dialect {_dialect}");
        }
    }

    private static DbType InferDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type switch
        {
            Type t when t == typeof(bool) => DbType.Boolean,
            Type t when t == typeof(byte) => DbType.Byte,
            Type t when t == typeof(short) => DbType.Int16,
            Type t when t == typeof(int) => DbType.Int32,
            Type t when t == typeof(long) => DbType.Int64,
            Type t when t == typeof(float) => DbType.Single,
            Type t when t == typeof(double) => DbType.Double,
            Type t when t == typeof(decimal) => DbType.Decimal,
            Type t when t == typeof(DateTime) => DbType.DateTime2,
            Type t when t == typeof(Guid) => DbType.Guid,
            Type t when t == typeof(string) => DbType.String,
            Type t when t == typeof(byte[]) => DbType.Binary,
            _ => DbType.Object
        };
    }
}