# ApiAotTester - AOT Mapper Registry Testing

This project tests the JadeDbClient library's AOT (Ahead-of-Time) compilation compatibility, specifically focusing on the AOT mapper registry and reflection-free bulk insert features.

## Purpose

The ApiAotTester project validates that:
1. JadeDbClient works correctly with .NET Native AOT compilation
2. Pre-compiled mappers function properly in AOT applications
3. Automatic reflection fallback works for unmapped types
4. Mixed usage (both approaches) works seamlessly
5. **NEW:** Reflection-free bulk insert operations work correctly across all databases

## Configuration

The project is configured with `PublishAot=true` in the `.csproj` file to enable Native AOT compilation.

### Mapper Registration

The application uses the source generator approach with `[JadeDbObject]` attribute for all models:

```csharp
// All models use [JadeDbObject] for reflection-free mapping and bulk inserts
[JadeDbObject]
public partial class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}

[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int? Stock { get; set; }
}
```

## Test Endpoints

### Original Mapper Tests

#### 1. `/test-aot-mapper` - Pre-compiled Mapper Test
Tests the fast, AOT-compatible pre-compiled mapper for `DataModel`.
- **Method**: GET
- **Purpose**: Validates that registered mappers work correctly in AOT
- **Model**: DataModel (has registered mapper)

#### 2. `/test-aot-reflection` - Reflection Fallback Test
Tests automatic reflection-based mapping for `UserModel`.
- **Method**: GET
- **Purpose**: Validates that unmapped types still work via reflection fallback
- **Model**: UserModel (no registered mapper)

#### 3. `/test-aot-mixed` - Mixed Usage Test
Tests both approaches in a single request.
- **Method**: GET
- **Purpose**: Validates that mapped and unmapped types can be used together
- **Models**: DataModel (mapper) + UserModel (reflection)

### Database-Specific Tests
- `/test-postgres` - PostgreSQL integration test (GET)
- `/test-mysql` - MySQL integration test (GET)
- `/test-mssql` - SQL Server integration test (GET)

### NEW: Bulk Insert Tests

#### PostgreSQL Bulk Insert Tests

##### `/test-postgres-bulk-insert`
- **Method**: GET
- **Purpose**: Tests `BulkInsertAsync` with `IEnumerable<T>` (reflection-free)
- **Features**:
  - Uses native COPY BINARY protocol
  - Inserts 100 products with batch size of 50
  - Demonstrates reflection-free property access via source generator
  
##### `/test-postgres-bulk-insert-stream`
- **Method**: GET
- **Purpose**: Tests `BulkInsertAsync` with `IAsyncEnumerable<T>` and progress reporting
- **Features**:
  - Streaming insertion with progress callbacks
  - Inserts 200 products with batch size of 50
  - Shows progress reporting at each batch

#### MySQL Bulk Insert Tests

##### `/test-mysql-bulk-insert`
- **Method**: GET
- **Purpose**: Tests optimized batched multi-value INSERT
- **Features**:
  - Uses batched `INSERT INTO table VALUES (row1), (row2)...` statements
  - 5-10x faster than row-by-row inserts
  - Reflection-free with `[JadeDbObject]`
  
##### `/test-mysql-bulk-insert-stream`
- **Method**: GET
- **Purpose**: Tests async streaming with progress
- **Features**:
  - Demonstrates IAsyncEnumerable support
  - Progress reporting for long-running operations
  - Transaction safety

#### SQL Server Bulk Insert Tests

##### `/test-mssql-bulk-insert`
- **Method**: GET
- **Purpose**: Tests SqlBulkCopy with reflection-free property access
- **Features**:
  - Uses SqlBulkCopy for optimal performance
  - Reflection-free via source generator
  - Batch size configuration
  
##### `/test-mssql-bulk-insert-stream`
- **Method**: GET
- **Purpose**: Tests SqlBulkCopy with async streaming
- **Features**:
  - IAsyncEnumerable support
  - Progress reporting
  - Efficient memory usage

## Model Classes

### DataModel (Pre-compiled Mapper)
```csharp
[JadeDbObject]
public partial class DataModel
{
    public int id { get; set; }
    public string? name { get; set; }
}
```
- **Mapping**: Uses source generator (reflection-free, AOT-compatible)
- **Performance**: Best (no reflection)

### UserModel (Source Generator)
```csharp
[JadeDbObject]
public partial class UserModel
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
}
```
- **Mapping**: Uses source generator
- **Performance**: Best (no reflection)

### Product (Bulk Insert Model)
```csharp
[JadeDbObject]
public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int? Stock { get; set; }
}
```
- **Mapping**: Uses source generator for both reads and bulk inserts
- **Performance**: Best (reflection-free property access)
- **Features**: Nullable support, works with all databases

## JSON Serialization Context

All models are registered in the `AppJsonSerializerContext` for AOT-compatible JSON serialization:

```csharp
[JsonSerializable(typeof(IEnumerable<DataModel>))]
[JsonSerializable(typeof(IEnumerable<UserModel>))]
[JsonSerializable(typeof(IEnumerable<Product>))]
[JsonSerializable(typeof(BulkInsertResponse))]
[JsonSerializable(typeof(BulkInsertStreamResponse))]
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

### Mapper Tests
1. **Pre-compiled Mapper**: Call `/test-aot-mapper` to verify fast mapper works
2. **Reflection Fallback**: Call `/test-aot-reflection` to verify unmapped types work
3. **Mixed Usage**: Call `/test-aot-mixed` to verify both work together

### Bulk Insert Tests
1. **PostgreSQL**: POST to `/test-postgres-bulk-insert` and `/test-postgres-bulk-insert-stream`
2. **MySQL**: POST to `/test-mysql-bulk-insert` and `/test-mysql-bulk-insert-stream`
3. **MSSQL**: POST to `/test-mssql-bulk-insert` and `/test-mssql-bulk-insert-stream`

### Example Test Commands

```bash
# Test PostgreSQL bulk insert
curl -X POST http://localhost:5000/test-postgres-bulk-insert

# Test MySQL bulk insert with streaming
curl -X POST http://localhost:5000/test-mysql-bulk-insert-stream

# Test SQL Server bulk insert
curl -X POST http://localhost:5000/test-mssql-bulk-insert
```

## Response Format

### Bulk Insert Response
```json
{
  "message": "PostgreSQL bulk insert with IEnumerable (reflection-free)",
  "database": "PostgreSQL",
  "rowsInserted": 100,
  "batchSize": 50,
  "totalItems": 100
}
```

### Bulk Insert Stream Response (with progress)
```json
{
  "message": "MySQL bulk insert with IAsyncEnumerable and progress",
  "database": "MySQL",
  "rowsInserted": 200,
  "batchSize": 50,
  "progressReports": [50, 100, 150, 200]
}
```

## Key Takeaways

- ✅ **Pre-compiled mappers** work in Native AOT applications
- ✅ **Source generator** creates reflection-free property accessors
- ✅ **Bulk inserts** are reflection-free with `[JadeDbObject]`
- ✅ **Progress reporting** works with async streams
- ✅ **Database-specific optimizations** (COPY BINARY, batched INSERT, SqlBulkCopy)
- ✅ **Same API** regardless of database
- ✅ **AOT compatible** - all operations work with Native AOT

## Performance Benefits

### With `[JadeDbObject]` (Recommended)
- ✅ No `typeof(T).GetProperties()` calls
- ✅ No `property.GetValue()` calls
- ✅ Direct property access via generated delegates
- ✅ Faster bulk inserts
- ✅ Lower memory usage

### Database-Specific Optimizations
- **PostgreSQL**: Native COPY BINARY (fastest)
- **MySQL**: Batched multi-value INSERT (5-10x faster)
- **SQL Server**: SqlBulkCopy (native high-performance API)

## Notes

- Database connection strings are configured in `appsettings.json`
- Some warnings from database provider libraries (SqlClient, MySqlConnector, Npgsql) are expected during AOT compilation
- These warnings are from third-party libraries, not from JadeDbClient code
- The `products` table must exist in your database for bulk insert tests to work
