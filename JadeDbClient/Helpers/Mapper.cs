using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using JadeDbClient.Initialize;

using System.Collections.Concurrent;

namespace JadeDbClient.Helpers;

internal class Mapper
{
    private static readonly ConcurrentDictionary<Type, bool> _loggedTypes = new();
    private readonly JadeDbMapperOptions _mapperOptions;

    public Mapper(JadeDbMapperOptions mapperOptions)
    {
        _mapperOptions = mapperOptions;
    }

    /// <summary>
    /// Maps a data reader row to an object of type T using pre-compiled mapper or reflection fallback.
    /// </summary>
    internal T MapObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IDataReader reader)
    {
        bool useAot = _mapperOptions.TryGetMapper<T>(out var mapper);

        // Log only once per type to avoid spamming
        if (_loggedTypes.TryAdd(typeof(T), true))
        {
            if (useAot)
            {
                Console.WriteLine($"[JadeDbClient] Using AOT Mapper for: {typeof(T).Name}");
            }
            else
            {
                Console.WriteLine($"[JadeDbClient] Using Reflection Fallback for: {typeof(T).Name}");
            }
        }

        // Try to use pre-compiled mapper first
        if (useAot)
        {
            return mapper!(reader);
        }

        // Fall back to reflection-based mapping
        return MapObjectReflection<T>(reader);
    }

    // [RequiresUnreferencedCode("Reflection-based mapping is not AOT-safe. Use [JadeDbObject] on your model for full compatibility.")]
    // [RequiresDynamicCode("Reflection-based mapping requires dynamic code access.")]
    internal T MapObjectReflection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IDataReader reader)
    {
        var properties = typeof(T).GetProperties();
        var propertyDict = properties.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        T instance = Activator.CreateInstance<T>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            if (propertyDict.TryGetValue(columnName, out var property) && !reader.IsDBNull(i))
            {
                property.SetValue(instance, reader[i]);
            }
        }
        return instance;
    }

    /// <summary>
    /// Maps a DataRow to an object of type T using reflection fallback.
    /// Note: Pre-compiled mappers work with IDataReader, not DataRow.
    /// </summary>
    // internal T MapObjectFromDataRow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(DataRow row)
    // {

    //     var properties = typeof(T).GetProperties();
    //     T instance = Activator.CreateInstance<T>();

    //     foreach (var property in properties)
    //     {
    //         if (row.Table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
    //         {
    //             property.SetValue(instance, row[property.Name]);
    //         }
    //     }

    //     return instance;
    // }
}
