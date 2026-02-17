using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using JadeDbClient.Initialize;

using System.Collections.Concurrent;

namespace JadeDbClient.Helpers;

internal class Mapper
{
    private readonly JadeDbMapperOptions _mapperOptions;
    private readonly JadeDbServiceRegistration.JadeDbServiceOptions? _serviceOptions;

    public Mapper(JadeDbMapperOptions mapperOptions, JadeDbServiceRegistration.JadeDbServiceOptions? serviceOptions = null)
    {
        _mapperOptions = mapperOptions;
        _serviceOptions = serviceOptions;
    }

    /// <summary>
    /// Maps a data reader row to an object of type T using pre-compiled mapper or reflection fallback.
    /// </summary>
    internal T MapObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IDataReader reader)
    {
        // Try to use pre-compiled mapper first
        if (_mapperOptions.TryGetMapper<T>(out var mapper))
        {
            if (_serviceOptions?.EnableLogging == true)
                Console.WriteLine($"[MAPPER] Using SOURCE GENERATOR mapper for {typeof(T).Name}");
            return mapper!(reader);
        }

        // Fall back to reflection-based mapping
        if (_serviceOptions?.EnableLogging == true)
            Console.WriteLine($"[MAPPER] Falling back to REFLECTION for {typeof(T).Name}");
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
