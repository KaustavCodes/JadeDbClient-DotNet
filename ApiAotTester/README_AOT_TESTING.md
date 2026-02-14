# ApiAotTester - AOT Mapper Registry Testing

This project tests the JadeDbClient library's AOT (Ahead-of-Time) compilation compatibility, specifically focusing on the new AOT mapper registry feature.

## Purpose

The ApiAotTester project validates that:
1. JadeDbClient works correctly with .NET Native AOT compilation
2. Pre-compiled mappers function properly in AOT applications
3. Automatic reflection fallback works for unmapped types
4. Mixed usage (both approaches) works seamlessly

## Configuration

The project is configured with `PublishAot=true` in the `.csproj` file to enable Native AOT compilation.

### Mapper Registration

The application registers a pre-compiled mapper for `DataModel` while leaving `UserModel` unmapped to test the reflection fallback:

```csharp
builder.Services.AddJadeDbService(options =>
{
    // Register a pre-compiled mapper for DataModel (AOT-compatible)
    options.RegisterMapper<DataModel>(reader => new DataModel
    {
        id = reader.GetInt32(reader.GetOrdinal("id")),
        name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name"))
    });
    
    // UserModel will use automatic reflection mapping (testing fallback)
    // No mapper registered for UserModel - it will use reflection automatically
});
```

## Test Endpoints

### 1. `/test-aot-mapper` - Pre-compiled Mapper Test
Tests the fast, AOT-compatible pre-compiled mapper for `DataModel`.
- **Method**: GET
- **Purpose**: Validates that registered mappers work correctly in AOT
- **Model**: DataModel (has registered mapper)

### 2. `/test-aot-reflection` - Reflection Fallback Test
Tests automatic reflection-based mapping for `UserModel`.
- **Method**: GET
- **Purpose**: Validates that unmapped types still work via reflection fallback
- **Model**: UserModel (no registered mapper)

### 3. `/test-aot-mixed` - Mixed Usage Test
Tests both approaches in a single request.
- **Method**: GET
- **Purpose**: Validates that mapped and unmapped types can be used together
- **Models**: DataModel (mapper) + UserModel (reflection)

### 4. Original Database-Specific Tests
- `/test-postgres` - PostgreSQL integration test
- `/test-mysql` - MySQL integration test
- `/test-mssql` - SQL Server integration test

## Model Classes

### DataModel (Pre-compiled Mapper)
```csharp
public class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}
```
- **Mapping**: Uses pre-compiled mapper (fast, AOT-compatible)
- **Performance**: Better (no reflection)

### UserModel (Reflection Fallback)
```csharp
public class UserModel
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
}
```
- **Mapping**: Uses automatic reflection (backward compatible)
- **Performance**: Standard (uses reflection)

## JSON Serialization Context

Both models are registered in the `AppJsonSerializerContext` for AOT-compatible JSON serialization:

```csharp
[JsonSerializable(typeof(IEnumerable<DataModel>))]
[JsonSerializable(typeof(List<DataModel>))]
[JsonSerializable(typeof(DataModel))]
[JsonSerializable(typeof(IEnumerable<UserModel>))]
[JsonSerializable(typeof(List<UserModel>))]
[JsonSerializable(typeof(UserModel))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
```

## Building and Testing

### Debug Build
```bash
dotnet build
```

### Release Build with AOT
```bash
dotnet publish -c Release
```

### Running the Application
```bash
dotnet run
```

The application will start on the configured port (default: 5000/5001).

## Testing Approach

1. **Pre-compiled Mapper**: Call `/test-aot-mapper` to verify fast mapper works
2. **Reflection Fallback**: Call `/test-aot-reflection` to verify unmapped types work
3. **Mixed Usage**: Call `/test-aot-mixed` to verify both work together
4. **Database Integration**: Use database-specific endpoints for full integration tests

## Key Takeaways

- ✅ **Pre-compiled mappers** work in Native AOT applications
- ✅ **Reflection fallback** still works for unmapped types
- ✅ **Mixed usage** allows gradual adoption
- ✅ **Same API** regardless of mapping approach
- ✅ **Backward compatible** - existing code works without changes

## Notes

- Database connection strings are configured in `appsettings.json`
- Some warnings from database provider libraries (SqlClient, MySqlConnector) are expected during AOT compilation
- These warnings are from third-party libraries, not from JadeDbClient code
