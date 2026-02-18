using System;

namespace JadeDbClient.Attributes;

/// <summary>
/// Maps a C# property to a custom database column name.
/// Use when the property name differs from the database column name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class JadeDbColumnAttribute : Attribute
{
    /// <summary>
    /// The name of the database column.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Initializes a new instance of the JadeDbColumnAttribute.
    /// </summary>
    /// <param name="columnName">The name of the database column.</param>
    public JadeDbColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}
