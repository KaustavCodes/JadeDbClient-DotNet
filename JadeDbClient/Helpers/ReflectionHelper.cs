using System;
using System.Linq;
using System.Reflection;
using JadeDbClient.Attributes;

namespace JadeDbClient.Helpers;

/// <summary>
/// Helper methods for reflection-based operations.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Gets the database column name for a property, respecting the JadeDbColumn attribute if present.
    /// </summary>
    /// <param name="property">The property to get the column name for.</param>
    /// <returns>The database column name (from JadeDbColumn attribute or property name).</returns>
    public static string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<JadeDbColumnAttribute>();
        return columnAttribute?.ColumnName ?? property.Name;
    }

    /// <summary>
    /// Gets the database column names for an array of properties, respecting JadeDbColumn attributes.
    /// </summary>
    /// <param name="properties">The properties to get column names for.</param>
    /// <returns>An array of database column names.</returns>
    public static string[] GetColumnNames(PropertyInfo[] properties)
    {
        return properties.Select(GetColumnName).ToArray();
    }
}
