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
    internal static string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<JadeDbColumnAttribute>();
        return columnAttribute?.ColumnName ?? property.Name;
    }

    /// <summary>
    /// Gets the database column names for an array of properties, respecting JadeDbColumn attributes.
    /// </summary>
    /// <param name="properties">The properties to get column names for.</param>
    /// <returns>An array of database column names.</returns>
    internal static string[] GetColumnNames(PropertyInfo[] properties)
    {
        return properties.Select(GetColumnName).ToArray();
    }

    /// <summary>
    /// Gets the database table name for a type, respecting the JadeDbTable attribute if present.
    /// Falls back to the class name, optionally pluralized.
    /// </summary>
    internal static string GetTableName(Type type, bool pluralize = false)
    {
        var tableAttribute = type.GetCustomAttribute<JadeDbTableAttribute>();
        if (tableAttribute != null)
        {
            return tableAttribute.TableName;
        }

        var name = type.Name;

        if (!pluralize)
        {
            return name;
        }

        // Simple pluralization convention (can be improved later with more rules or a library)
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1 &&
            !"aeiouAEIOU".Contains(name[^2]))
        {
            return name[..^1] + "ies";
        }

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return name + "es";
        }

        return name + "s";
    }

    /// <summary>
    /// Gets all mappable properties for a type (public instance properties that can be read and written).
    /// This is used by QueryBuilder and other reflection-based features.
    /// </summary>
    internal static PropertyInfo[] GetMappableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && p.CanWrite)
                   .ToArray();
    }
}
