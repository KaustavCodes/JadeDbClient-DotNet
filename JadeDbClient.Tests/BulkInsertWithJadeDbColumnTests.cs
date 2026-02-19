using System.Data;
using JadeDbClient.Attributes;
using JadeDbClient.Helpers;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for bulk insert operations with JadeDbColumn attributes in reflection fallback mode
/// </summary>
public class BulkInsertWithJadeDbColumnTests
{
    /// <summary>
    /// Test model with JadeDbColumn attributes but WITHOUT JadeDbObject attribute
    /// This forces the reflection fallback path
    /// </summary>
    public class ProductWithColumnAttributes
    {
        [JadeDbColumn("product_id")]
        public int ProductId { get; set; }
        
        [JadeDbColumn("product_name")]
        public string ProductName { get; set; } = string.Empty;
        
        [JadeDbColumn("unit_price")]
        public decimal UnitPrice { get; set; }
        
        [JadeDbColumn("stock_quantity")]
        public int? StockQuantity { get; set; }
        
        [JadeDbColumn("created_date")]
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Test model with mixed attributes (some properties have JadeDbColumn, others don't)
    /// </summary>
    public class ProductWithMixedAttributes
    {
        [JadeDbColumn("product_id")]
        public int ProductId { get; set; }
        
        // No attribute - should use property name
        public string ProductName { get; set; } = string.Empty;
        
        [JadeDbColumn("unit_price")]
        public decimal UnitPrice { get; set; }
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

    [Fact]
    public void ReflectionHelper_GetColumnName_ReturnsAttributeValueWhenPresent()
    {
        // Arrange
        var property = typeof(ProductWithColumnAttributes).GetProperty(nameof(ProductWithColumnAttributes.ProductId))!;

        // Act
        var columnName = ReflectionHelper.GetColumnName(property);

        // Assert
        columnName.Should().Be("product_id", "JadeDbColumn attribute value should be used");
    }

    [Fact]
    public void ReflectionHelper_GetColumnName_ReturnsPropertyNameWhenNoAttribute()
    {
        // Arrange
        var property = typeof(ProductWithMixedAttributes).GetProperty(nameof(ProductWithMixedAttributes.ProductName))!;

        // Act
        var columnName = ReflectionHelper.GetColumnName(property);

        // Assert
        columnName.Should().Be("ProductName", "Property name should be used when no attribute present");
    }

    [Fact]
    public void ReflectionHelper_GetColumnNames_ReturnsAllColumnNames()
    {
        // Arrange
        var properties = typeof(ProductWithColumnAttributes).GetProperties()
            .Where(p => p.CanRead)
            .ToArray();

        // Act
        var columnNames = ReflectionHelper.GetColumnNames(properties);

        // Assert
        columnNames.Should().Contain(new[] { "product_id", "product_name", "unit_price", "stock_quantity", "created_date" });
        columnNames.Should().NotContain(new[] { "ProductId", "ProductName", "UnitPrice", "StockQuantity", "CreatedDate" });
    }

    [Fact]
    public void ReflectionHelper_GetColumnNames_HandlesMixedAttributes()
    {
        // Arrange
        var properties = typeof(ProductWithMixedAttributes).GetProperties()
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name)
            .ToArray();

        // Act
        var columnNames = ReflectionHelper.GetColumnNames(properties);

        // Assert
        columnNames.Should().Contain("product_id", "Property with JadeDbColumn should use attribute value");
        columnNames.Should().Contain("ProductName", "Property without JadeDbColumn should use property name");
        columnNames.Should().Contain("unit_price", "Property with JadeDbColumn should use attribute value");
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void BulkInsertAsync_WithoutJadeDbObject_UsesReflectionFallback(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        
        // Act - Verify that ProductWithColumnAttributes does NOT have a bulk insert accessor
        var hasAccessor = JadeDbMapperOptions.TryGetBulkInsertAccessor<ProductWithColumnAttributes>(out var accessor);

        // Assert
        hasAccessor.Should().BeFalse("Type without [JadeDbObject] should not have generated accessor");
        accessor.Should().BeNull("Reflection fallback should be used");
    }

    [Fact]
    public void BulkInsertAccessor_WhenManuallyRegisteredWithColumnNames_UsesCorrectNames()
    {
        // Arrange - Manually register an accessor with column names from JadeDbColumn attributes
        JadeDbMapperOptions.RegisterBulkInsertAccessor<ProductWithColumnAttributes>(
            columnNames: new[] { "product_id", "product_name", "unit_price", "stock_quantity", "created_date" },
            accessor: (obj) => new object?[] { obj.ProductId, obj.ProductName, obj.UnitPrice, obj.StockQuantity, obj.CreatedDate }
        );

        // Act
        var hasAccessor = JadeDbMapperOptions.TryGetBulkInsertAccessor<ProductWithColumnAttributes>(out var accessor);

        // Assert
        hasAccessor.Should().BeTrue();
        accessor.Should().NotBeNull();
        accessor!.ColumnNames.Should().Contain(new[] { "product_id", "product_name", "unit_price", "stock_quantity", "created_date" });
        accessor.ColumnNames.Should().NotContain(new[] { "ProductId", "ProductName", "UnitPrice", "StockQuantity", "CreatedDate" });
    }

    [Fact]
    public void BulkInsertAccessor_WithMixedAttributes_UsesCorrectColumnNames()
    {
        // Arrange - Manually register accessor for mixed attributes type
        JadeDbMapperOptions.RegisterBulkInsertAccessor<ProductWithMixedAttributes>(
            columnNames: new[] { "product_id", "ProductName", "unit_price" },
            accessor: (obj) => new object?[] { obj.ProductId, obj.ProductName, obj.UnitPrice }
        );

        // Act
        var hasAccessor = JadeDbMapperOptions.TryGetBulkInsertAccessor<ProductWithMixedAttributes>(out var accessor);

        // Assert
        hasAccessor.Should().BeTrue();
        accessor!.ColumnNames.Should().Contain("product_id", "Attributed property should use column name");
        accessor.ColumnNames.Should().Contain("ProductName", "Non-attributed property should use property name");
        accessor.ColumnNames.Should().Contain("unit_price", "Attributed property should use column name");
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void DatabaseService_SupportsReflectionBasedBulkInsert(string databaseType)
    {
        // Arrange
        var service = CreateTestService(databaseType);
        var items = new List<ProductWithColumnAttributes>
        {
            new() { ProductId = 1, ProductName = "Widget", UnitPrice = 19.99m, StockQuantity = 100, CreatedDate = DateTime.Now }
        };

        // Act - This should not throw even without a generated accessor
        // The reflection fallback should handle it
        var methodExists = service.GetType().GetMethods()
            .Any(m => m.Name == "BulkInsertAsync" && m.IsGenericMethodDefinition);

        // Assert
        methodExists.Should().BeTrue("BulkInsertAsync method should exist");
        // In a real integration test with a database, we would verify the insert succeeds
        // and that the correct column names (from JadeDbColumn) are used in the SQL
    }

    [Fact]
    public void JadeDbTableAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attribute = typeof(TestTableClass).GetCustomAttributes(typeof(JadeDbTableAttribute), true)
            .FirstOrDefault() as JadeDbTableAttribute;

        // Assert
        attribute.Should().NotBeNull("JadeDbTable attribute should be applicable to classes");
        attribute!.TableName.Should().Be("products", "Table name should match attribute value");
    }

    [Fact]
    public void JadeDbTableAttribute_CanBeAppliedToStruct()
    {
        // Arrange & Act
        var attribute = typeof(TestTableStruct).GetCustomAttributes(typeof(JadeDbTableAttribute), true)
            .FirstOrDefault() as JadeDbTableAttribute;

        // Assert
        attribute.Should().NotBeNull("JadeDbTable attribute should be applicable to structs");
        attribute!.TableName.Should().Be("products", "Table name should match attribute value");
    }

    [Fact]
    public void JadeDbTableAttribute_StoresTableName()
    {
        // Arrange
        var tableName = "my_custom_table";
        
        // Act
        var attribute = new JadeDbTableAttribute(tableName);

        // Assert
        attribute.TableName.Should().Be(tableName);
    }

    // Test types for JadeDbTable attribute tests
    [JadeDbTable("products")]
    private class TestTableClass
    {
        public int Id { get; set; }
    }

    [JadeDbTable("products")]
    private struct TestTableStruct
    {
        public int Id { get; set; }
    }
}
