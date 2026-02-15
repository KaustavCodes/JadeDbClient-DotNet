using System;
using System.Collections.Generic;
using System.Data;

namespace JadeDbClient.Initialize;

public class JadeDbMapperOptions
{
    // 🚀 The static "Bridge": Source Generator drops mappers here at startup
    internal static readonly Dictionary<Type, Func<IDataReader, object>> GlobalMappers = new();

    internal readonly Dictionary<Type, Func<IDataReader, object>> Mappers = new();

    public JadeDbMapperOptions()
    {
        // 🚀 Pull globally generated mappers into this instance automatically
        foreach (var mapper in GlobalMappers)
        {
            Mappers[mapper.Key] = mapper.Value;
        }
    }

    public void RegisterMapper<T>(Func<IDataReader, T> mapper) where T : class
    {
        Mappers[typeof(T)] = (reader) => mapper(reader);
    }

    internal bool TryGetMapper<T>(out Func<IDataReader, T>? mapper)
    {
        if (Mappers.TryGetValue(typeof(T), out var func))
        {
            mapper = (reader) => (T)func(reader);
            return true;
        }
        mapper = null;
        return false;
    }
}