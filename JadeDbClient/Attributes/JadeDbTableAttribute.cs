using System;

namespace JadeDbClient.Attributes;

/// <summary>
/// Maps a C# class to a custom database table name.
/// Use when the class name differs from the database table name.
/// This attribute is reserved for future use in query generation features.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class JadeDbTableAttribute : Attribute
{
    /// <summary>
    /// The name of the database table.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Initializes a new instance of the JadeDbTableAttribute.
    /// </summary>
    /// <param name="tableName">The name of the database table.</param>
    public JadeDbTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}
