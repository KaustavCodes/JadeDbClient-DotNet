using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using JadeDbClient.Initialize;
using JadeDbClient.Attributes;

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace JadeDbClient.Helpers;

internal class Mapper
{
    private readonly JadeDbMapperOptions _mapperOptions;
    private readonly JadeDbServiceRegistration.JadeDbServiceOptions? _serviceOptions;
    
    // Cache for property-to-column-name mappings per type
    // Note: The inner Dictionary is immutable after creation, making this thread-safe
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyColumnCache = new();

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
        // Get or create cached property-to-column-name mappings for this type
        var propertyDict = _propertyColumnCache.GetOrAdd(typeof(T), type =>
        {
            var properties = type.GetProperties();
            var dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var property in properties)
            {
                // Check for JadeDbColumnAttribute
                var columnAttr = property.GetCustomAttributes(typeof(JadeDbColumnAttribute), true)
                    .FirstOrDefault() as JadeDbColumnAttribute;
                
                var columnName = columnAttr?.ColumnName ?? property.Name;
                dict[columnName] = property;
            }
            
            return dict;
        });
        
        T instance = Activator.CreateInstance<T>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            if (propertyDict.TryGetValue(columnName, out var property) && !reader.IsDBNull(i))
            {
                var value = reader[i];
                var propType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (value is DateTime dt && propType == typeof(DateOnly))
                    property.SetValue(instance, DateOnly.FromDateTime(dt));
                else if (value is DateOnly dateOnly && propType == typeof(DateTime))
                    property.SetValue(instance, dateOnly.ToDateTime(TimeOnly.MinValue));
                else
                    property.SetValue(instance, value);
            }
        }
        return instance;
    }

    /// <summary>
    /// Maps a data reader row to a <see cref="dynamic"/> object (<see cref="ExpandoObject"/>).
    /// Each column in the reader becomes a property on the returned object, using the
    /// column name as the key.  <see cref="DBNull"/> values are mapped to <c>null</c>.
    /// This is the preferred mapping strategy for JOIN queries whose result set spans
    /// multiple tables and does not correspond to any single strongly-typed model.
    /// </summary>
    internal dynamic MapDynamic(IDataReader reader)
    {
        IDictionary<string, object?> expando = new ExpandoObject();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            expando[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        return (ExpandoObject)expando;
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
