using System;
using System.Data;

namespace JadeDbClient.Initialize;

public class JadeMapperOptions
{
    // Dictionary mapping Type -> Function that takes a DataReader and returns that Type
    internal readonly Dictionary<Type, Func<IDataReader, object>> Mappers = new();

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