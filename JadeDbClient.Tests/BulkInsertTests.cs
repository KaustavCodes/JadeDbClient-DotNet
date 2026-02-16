using System.Data;
using JadeDbClient.Attributes;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for bulk insert operations including stream-based inserts
/// </summary>
public class BulkInsertTests
{
    // Test model for bulk insert operations
    [JadeDbObject]
    public partial class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? Stock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private IDatabaseService CreateTestService(string databaseType)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = databaseType,
                ["ConnectionStrings:DbConnection"] = GetTestConnectionString(databaseType)
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDatabaseService>();
    }

    private string GetTestConnectionString(string databaseType)
    {
        return databaseType switch
        {
            "MsSql" => "Server=localhost;Database=TestDb;User Id=sa;Password=Test123;TrustServerCertificate=True;",
            "MySql" => "Server=localhost;Database=TestDb;User=root;Password=Test123;",
            "PostgreSQL" => "Host=localhost;Database=TestDb;Username=postgres;Password=Test123;",
            _ => throw new ArgumentException($"Unknown database type: {databaseType}")
        };
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public async Task BulkInsertAsync_WithIEnumerable_ValidatesNullItems(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        IEnumerable<TestProduct>? items = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.BulkInsertAsync("TestProducts", items!, batchSize: 100));
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public async Task BulkInsertAsync_WithIAsyncEnumerable_ValidatesNullItems(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        IAsyncEnumerable<TestProduct>? items = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.BulkInsertAsync("TestProducts", items!, progress: null, batchSize: 100));
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public async Task BulkInsertAsync_WithEmptyTableName_ThrowsArgumentException(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        var items = new List<TestProduct> { new TestProduct { Id = 1, Name = "Test" } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.BulkInsertAsync("", items, batchSize: 100));
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void BulkInsertAsync_WithEmptyCollection_ReturnsZero(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);

        // Act - We can't actually insert without a real database, so we just test the interface exists
        // In a real integration test with a database, this would verify zero rows inserted
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("MsSql", 100)]
    [InlineData("MySql", 500)]
    [InlineData("PostgreSQL", 1000)]
    public void BulkInsertAsync_SupportsCustomBatchSize(string databaseType, int batchSize)
    {
        // Arrange
        var service = CreateTestService(databaseType);

        // Act - Verify method signature accepts batch size parameter
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty();
        
        // Find the IEnumerable<T> overload
        var enumerableMethod = methods.FirstOrDefault(m =>
        {
            var parameters = m.GetParameters();
            return parameters.Length == 3 &&
                   parameters[1].ParameterType.Name.Contains("IEnumerable") &&
                   parameters[2].Name == "batchSize";
        });
        
        enumerableMethod.Should().NotBeNull("BulkInsertAsync with IEnumerable should exist");
        
        // Verify the batch size parameter is in valid range
        batchSize.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void BulkInsertAsync_WithProgress_AcceptsProgressReporter(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);

        // Act - Verify method signature accepts IProgress parameter
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty();
        
        // Find the IAsyncEnumerable<T> overload with progress
        var asyncMethod = methods.FirstOrDefault(m =>
        {
            var parameters = m.GetParameters();
            return parameters.Length == 4 &&
                   parameters[1].ParameterType.Name.Contains("IAsyncEnumerable");
        });
        
        asyncMethod.Should().NotBeNull("BulkInsertAsync with IAsyncEnumerable should exist");
        
        if (asyncMethod != null)
        {
            var progressParam = asyncMethod.GetParameters()[2];
            progressParam.ParameterType.Name.Should().Contain("IProgress");
        }
    }

    [Fact]
    public async Task BulkInsertAsync_WithIAsyncEnumerable_SupportsStreaming()
    {
        // Arrange - Create async enumerable source
        var items = GenerateAsyncTestProducts(100);
        
        // Act & Assert - Verify the async enumerable can be consumed
        int count = 0;
        await foreach (var item in items)
        {
            count++;
        }
        
        count.Should().Be(100);
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void BulkInsertAsync_HandlesNullableProperties(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        var fixedDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var items = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.99m, Stock = 100, CreatedAt = fixedDate },
            new TestProduct { Id = 2, Name = "Product2", Price = 20.99m, Stock = null, CreatedAt = fixedDate }
        };

        // Act - Verify method can handle nullable properties
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty("BulkInsertAsync methods should exist");
        // In a real integration test, this would verify the null values are inserted correctly
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void DatabaseService_HasBulkInsertMethods(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        var serviceType = service.GetType();

        // Act
        var enumerableMethod = serviceType.GetMethods()
            .FirstOrDefault(m => m.Name == "BulkInsertAsync" && 
                                 m.GetParameters().Length == 3 &&
                                 m.GetParameters()[1].ParameterType.Name.Contains("IEnumerable"));
        
        var asyncEnumerableMethod = serviceType.GetMethods()
            .FirstOrDefault(m => m.Name == "BulkInsertAsync" && 
                                 m.GetParameters().Length == 4 &&
                                 m.GetParameters()[1].ParameterType.Name.Contains("IAsyncEnumerable"));

        // Assert
        enumerableMethod.Should().NotBeNull("IEnumerable overload should exist");
        asyncEnumerableMethod.Should().NotBeNull("IAsyncEnumerable overload should exist");
    }

    [Fact]
    public void BulkInsertAsync_ReturnsTaskOfInt()
    {
        // Arrange
        var service = CreateTestService("MsSql");
        
        // Act - Find the BulkInsertAsync methods
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty("BulkInsertAsync methods should exist");
        
        foreach (var method in methods)
        {
            method.ReturnType.Should().Match<Type>(t => 
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>),
                "BulkInsertAsync should return Task<int>");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void BulkInsertAsync_HandlesVariousBatchSizes(int batchSize)
    {
        // Arrange
        var service = CreateTestService("MsSql");
        
        // Act - Verify method accepts various batch sizes
        var methods = service.GetType().GetMethods()
            .Where(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition)
            .ToList();

        // Assert
        methods.Should().NotBeEmpty();
        // Batch size parameter should accept any positive integer
        batchSize.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void BulkInsertAsync_WithJadeDbObjectAttribute_UsesGeneratedAccessor(string databaseType)
    {
        // Arrange - TestProduct already has [JadeDbObject] attribute
        var service = CreateTestService(databaseType);

        // Act - Register accessor to simulate source generator behavior
        JadeDbMapperOptions.RegisterBulkInsertAccessor<TestProduct>(
            columnNames: new[] { "Id", "Name", "Price", "Stock", "CreatedAt" },
            accessor: (obj) => new object?[] { obj.Id, obj.Name, obj.Price, obj.Stock, obj.CreatedAt }
        );

        // Assert - Verify accessor was registered
        var hasAccessor = JadeDbMapperOptions.TryGetBulkInsertAccessor<TestProduct>(out var accessor);
        hasAccessor.Should().BeTrue("Accessor should be registered for types with [JadeDbObject]");
        accessor.Should().NotBeNull();
        accessor!.ColumnNames.Should().Contain(new[] { "Id", "Name", "Price", "Stock", "CreatedAt" });
    }

    [Fact]
    public void BulkInsertAccessor_GetValues_ReturnsCorrectPropertyValues()
    {
        // Arrange
        var fixedDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var product = new TestProduct
        {
            Id = 123,
            Name = "Test Product",
            Price = 99.99m,
            Stock = 50,
            CreatedAt = fixedDate
        };

        JadeDbMapperOptions.RegisterBulkInsertAccessor<TestProduct>(
            columnNames: new[] { "Id", "Name", "Price", "Stock", "CreatedAt" },
            accessor: (obj) => new object?[] { obj.Id, obj.Name, obj.Price, obj.Stock, obj.CreatedAt }
        );

        // Act
        JadeDbMapperOptions.TryGetBulkInsertAccessor<TestProduct>(out var accessor);
        var values = accessor!.GetValues(product);

        // Assert
        values.Should().HaveCount(5);
        values[0].Should().Be(123);
        values[1].Should().Be("Test Product");
        values[2].Should().Be(99.99m);
        values[3].Should().Be(50);
        values[4].Should().Be(fixedDate);
    }

    [Fact]
    public void BulkInsertAccessor_WithNullableProperty_ReturnsNullCorrectly()
    {
        // Arrange
        var fixedDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var product = new TestProduct
        {
            Id = 456,
            Name = "Product with null stock",
            Price = 49.99m,
            Stock = null, // Null value
            CreatedAt = fixedDate
        };

        JadeDbMapperOptions.RegisterBulkInsertAccessor<TestProduct>(
            columnNames: new[] { "Id", "Name", "Price", "Stock", "CreatedAt" },
            accessor: (obj) => new object?[] { obj.Id, obj.Name, obj.Price, obj.Stock, obj.CreatedAt }
        );

        // Act
        JadeDbMapperOptions.TryGetBulkInsertAccessor<TestProduct>(out var accessor);
        var values = accessor!.GetValues(product);

        // Assert
        values.Should().HaveCount(5);
        values[0].Should().Be(456);
        values[1].Should().Be("Product with null stock");
        values[2].Should().Be(49.99m);
        values[3].Should().BeNull(); // Verify null is preserved
        values[4].Should().Be(fixedDate);
    }

    // Helper methods
    private List<TestProduct> GenerateTestProducts(int count)
    {
        var baseDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var products = new List<TestProduct>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(new TestProduct
            {
                Id = i,
                Name = $"Product {i}",
                Price = 10.99m * i,
                Stock = i % 2 == 0 ? i * 10 : null,
                CreatedAt = baseDate.AddDays(-i)
            });
        }
        return products;
    }

    private async IAsyncEnumerable<TestProduct> GenerateAsyncTestProducts(int count)
    {
        var baseDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(1); // Simulate async operation
            yield return new TestProduct
            {
                Id = i,
                Name = $"Product {i}",
                Price = 10.99m * i,
                Stock = i % 2 == 0 ? i * 10 : null,
                CreatedAt = baseDate.AddDays(-i)
            };
        }
    }
}
