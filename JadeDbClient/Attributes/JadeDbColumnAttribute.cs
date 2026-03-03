using System;

namespace JadeDbClient.Attributes;

/// <summary>
/// Maps a C# property to a custom database column name and/or marks it as
/// database-managed (auto-increment / identity / computed).
/// </summary>
/// <remarks>
/// Usage examples:
/// <code>
/// [JadeDbColumn("user_name")]                       // column rename only
/// [JadeDbColumn("id", IgnoreOnInsert = true)]       // rename + exclude from INSERT/UPDATE
/// [JadeDbColumn(IgnoreOnInsert = true)]             // exclude from INSERT/UPDATE, keep property name as column
/// </code>
/// Properties decorated with <c>IgnoreOnInsert = true</c> are automatically
/// omitted from INSERT and UPDATE statements built by <see cref="JadeDbClient.Helpers.QueryBuilder{T}"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class JadeDbColumnAttribute : Attribute
{
    /// <summary>
    /// The name of the database column, or <c>null</c> if the property name should be used.
    /// </summary>
    public string? ColumnName { get; }

    /// <summary>
    /// When <c>true</c> the column is managed by the database (e.g. IDENTITY / AUTO_INCREMENT /
    /// computed default) and will be excluded from INSERT and UPDATE statements.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IgnoreOnInsert { get; set; }

    /// <summary>
    /// When <c>true</c> this column is the identity / primary-key column whose value is
    /// returned by <c>BuildInsert(returnIdentity: true)</c>.  Only the first property
    /// decorated with <c>IsIdentity = true</c> is used; if no property carries this flag
    /// the builder falls back to the conventional column name <c>id</c>.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Marks the property with a custom column name.
    /// </summary>
    /// <param name="columnName">The name of the database column.</param>
    public JadeDbColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }

    /// <summary>
    /// Marks the property as database-managed without renaming the column.
    /// The property name is used as the column name.
    /// </summary>
    public JadeDbColumnAttribute()
    {
        ColumnName = null;
    }
}
