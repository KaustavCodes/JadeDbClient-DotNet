using System;
using System.Data;
using FluentAssertions;
using JadeDbClient.Attributes;
using JadeDbClient.Helpers;
using JadeDbClient.Initialize;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for JadeDbColumnAttribute functionality to ensure database column names
/// are correctly mapped to model properties
/// </summary>
public class JadeDbColumnAttributeTests
{
    /// <summary>
    /// Test that the source generator correctly uses ColumnName from JadeDbColumnAttribute
    /// for reading data from IDataReader
    /// </summary>
    [Fact]
    public void SourceGeneratorMapper_WithJadeDbColumnAttribute_UsesColumnName()
    {
        // Arrange - Create a mock IDataReader with database column names
        var mockReader = new Mock<IDataReader>();
        
        // Database columns: user_id, user_name, email_address
        mockReader.Setup(r => r.GetOrdinal("user_id")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("user_name")).Returns(1);
        mockReader.Setup(r => r.GetOrdinal("email_address")).Returns(2);
        
        mockReader.Setup(r => r.GetInt32(0)).Returns(100);
        mockReader.Setup(r => r.GetString(1)).Returns("john_doe");
        mockReader.Setup(r => r.GetString(2)).Returns("john@example.com");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        // Simulate source generator mapper that uses ColumnName
        JadeDbMapperOptions.RegisterGlobalMapper<UserWithColumnAttributes>(reader => new UserWithColumnAttributes
        {
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            UserName = reader.GetString(reader.GetOrdinal("user_name")),
            EmailAddress = reader.GetString(reader.GetOrdinal("email_address"))
        });
        
        var mapperOptions = new JadeDbMapperOptions();
        
        // Act
        var result = mapperOptions.ExecuteMapper<UserWithColumnAttributes>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(100);
        result.UserName.Should().Be("john_doe");
        result.EmailAddress.Should().Be("john@example.com");
    }
    
    /// <summary>
    /// Test that bulk insert accessor correctly uses ColumnName from JadeDbColumnAttribute
    /// when generating column names for INSERT statements
    /// </summary>
    [Fact]
    public void BulkInsertAccessor_WithJadeDbColumnAttribute_UsesColumnName()
    {
        // Arrange - Register bulk insert accessor with column names matching database
        JadeDbMapperOptions.RegisterBulkInsertAccessor<UserWithColumnAttributes>(
            columnNames: new[] { "user_id", "user_name", "email_address" },
            accessor: (obj) => new object?[] { obj.UserId, obj.UserName, obj.EmailAddress }
        );
        
        var user = new UserWithColumnAttributes
        {
            UserId = 200,
            UserName = "jane_doe",
            EmailAddress = "jane@example.com"
        };
        
        // Act
        JadeDbMapperOptions.TryGetBulkInsertAccessor<UserWithColumnAttributes>(out var accessor);
        var columnNames = accessor!.ColumnNames;
        var values = accessor.GetValues(user);
        
        // Assert
        columnNames.Should().Contain(new[] { "user_id", "user_name", "email_address" });
        columnNames.Should().NotContain(new[] { "UserId", "UserName", "EmailAddress" });
        
        values.Should().HaveCount(3);
        values[0].Should().Be(200);
        values[1].Should().Be("jane_doe");
        values[2].Should().Be("jane@example.com");
    }
    
    /// <summary>
    /// Test that reflection fallback correctly handles JadeDbColumnAttribute
    /// when source generator is not available
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithJadeDbColumnAttribute_MapsCorrectly()
    {
        // Arrange - Create a mock IDataReader with database column names
        var mockReader = new Mock<IDataReader>();
        
        // Setup database column names
        mockReader.Setup(r => r.FieldCount).Returns(3);
        mockReader.Setup(r => r.GetName(0)).Returns("user_id");
        mockReader.Setup(r => r.GetName(1)).Returns("user_name");
        mockReader.Setup(r => r.GetName(2)).Returns("email_address");
        
        mockReader.Setup(r => r[0]).Returns(300);
        mockReader.Setup(r => r[1]).Returns("alice_doe");
        mockReader.Setup(r => r[2]).Returns("alice@example.com");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act - Use reflection fallback (no source generator mapper registered for this type)
        var result = mapper.MapObjectReflection<UserWithColumnAttributes>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(300);
        result.UserName.Should().Be("alice_doe");
        result.EmailAddress.Should().Be("alice@example.com");
    }
    
    /// <summary>
    /// Test mixed scenario where some properties have JadeDbColumnAttribute and some don't
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithMixedColumnAttributes_MapsCorrectly()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.FieldCount).Returns(3);
        mockReader.Setup(r => r.GetName(0)).Returns("product_id");  // Has attribute
        mockReader.Setup(r => r.GetName(1)).Returns("ProductName");  // No attribute
        mockReader.Setup(r => r.GetName(2)).Returns("unit_price");   // Has attribute
        
        mockReader.Setup(r => r[0]).Returns(1001);
        mockReader.Setup(r => r[1]).Returns("Widget");
        mockReader.Setup(r => r[2]).Returns(19.99m);
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act
        var result = mapper.MapObjectReflection<ProductWithMixedAttributes>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(1001);
        result.ProductName.Should().Be("Widget");
        result.UnitPrice.Should().Be(19.99m);
    }
    
    /// <summary>
    /// Test that JadeDbColumnAttribute is case-insensitive
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithCaseInsensitiveColumnName_MapsCorrectly()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("USER_ID");       // Uppercase
        mockReader.Setup(r => r.GetName(1)).Returns("Email_Address"); // Mixed case
        
        mockReader.Setup(r => r[0]).Returns(500);
        mockReader.Setup(r => r[1]).Returns("test@example.com");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act
        var result = mapper.MapObjectReflection<UserWithColumnAttributes>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(500);
        result.EmailAddress.Should().Be("test@example.com");
    }
    
    /// <summary>
    /// Test that properties without matching column names are left at their default values
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithMissingColumns_LeavesDefaultValues()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.FieldCount).Returns(1);
        mockReader.Setup(r => r.GetName(0)).Returns("user_id");
        
        mockReader.Setup(r => r[0]).Returns(600);
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act
        var result = mapper.MapObjectReflection<UserWithColumnAttributes>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(600);
        result.UserName.Should().BeEmpty(); // Default value
        result.EmailAddress.Should().BeEmpty(); // Default value
    }
    
    /// <summary>
    /// Test that nullable properties remain null when columns are missing
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithMissingNullableColumns_LeavesNullValues()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.FieldCount).Returns(1);
        mockReader.Setup(r => r.GetName(0)).Returns("order_id");
        
        mockReader.Setup(r => r[0]).Returns(100);
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act
        var result = mapper.MapObjectReflection<OrderWithNullableColumns>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(100);
        result.CustomerId.Should().BeNull(); // Nullable int remains null
        result.OrderDate.Should().BeNull(); // Nullable DateTime remains null
        result.Notes.Should().BeNull(); // Nullable string remains null
    }
    
    /// <summary>
    /// Test that DBNull values are correctly handled for properties with JadeDbColumnAttribute
    /// </summary>
    [Fact]
    public void ReflectionMapper_WithDBNullForAttributedColumns_SetsNullValues()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.FieldCount).Returns(4);
        mockReader.Setup(r => r.GetName(0)).Returns("order_id");
        mockReader.Setup(r => r.GetName(1)).Returns("customer_id");
        mockReader.Setup(r => r.GetName(2)).Returns("order_date");
        mockReader.Setup(r => r.GetName(3)).Returns("notes");
        
        // Order ID has a value
        mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
        mockReader.Setup(r => r[0]).Returns(200);
        
        // All other columns are DBNull
        mockReader.Setup(r => r.IsDBNull(1)).Returns(true);
        mockReader.Setup(r => r.IsDBNull(2)).Returns(true);
        mockReader.Setup(r => r.IsDBNull(3)).Returns(true);
        
        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);
        
        // Act
        var result = mapper.MapObjectReflection<OrderWithNullableColumns>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(200);
        result.CustomerId.Should().BeNull(); // DBNull for nullable int
        result.OrderDate.Should().BeNull(); // DBNull for nullable DateTime
        result.Notes.Should().BeNull(); // DBNull for nullable string
    }
    
    /// <summary>
    /// Test that demonstrates DataModel scenario with database column "name" mapped to property "FullName"
    /// This simulates the real-world ApiAotTester scenario
    /// </summary>
    [Fact]
    public void SourceGeneratorMapper_WithDatabaseColumnNameMappedToFullName_WorksCorrectly()
    {
        // Arrange - Simulate database returning "id" and "name" columns
        var mockReader = new Mock<IDataReader>();
        
        mockReader.Setup(r => r.GetOrdinal("id")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("name")).Returns(1);
        
        mockReader.Setup(r => r.GetInt32(0)).Returns(42);
        mockReader.Setup(r => r.GetString(1)).Returns("John Doe");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        // Simulate source generator mapper that uses JadeDbColumn mapping
        JadeDbMapperOptions.RegisterGlobalMapper<DataModelWithColumnMapping>(reader => new DataModelWithColumnMapping
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            FullName = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name"))
        });
        
        var mapperOptions = new JadeDbMapperOptions();
        
        // Act
        var result = mapperOptions.ExecuteMapper<DataModelWithColumnMapping>(mockReader.Object);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.FullName.Should().Be("John Doe");
    }
}

// Test Models

[JadeDbObject]
public partial class UserWithColumnAttributes
{
    [JadeDbColumn("user_id")]
    public int UserId { get; set; }
    
    [JadeDbColumn("user_name")]
    public string UserName { get; set; } = string.Empty;
    
    [JadeDbColumn("email_address")]
    public string EmailAddress { get; set; } = string.Empty;
}

public class ProductWithMixedAttributes
{
    [JadeDbColumn("product_id")]
    public int ProductId { get; set; }
    
    // No attribute - uses property name
    public string ProductName { get; set; } = string.Empty;
    
    [JadeDbColumn("unit_price")]
    public decimal UnitPrice { get; set; }
}

public class OrderWithNullableColumns
{
    [JadeDbColumn("order_id")]
    public int OrderId { get; set; }
    
    [JadeDbColumn("customer_id")]
    public int? CustomerId { get; set; }
    
    [JadeDbColumn("order_date")]
    public DateTime? OrderDate { get; set; }
    
    [JadeDbColumn("notes")]
    public string? Notes { get; set; }
}

// Simulates the ApiAotTester DataModel scenario
[JadeDbObject]
public partial class DataModelWithColumnMapping
{
    public int Id { get; set; }
    
    [JadeDbColumn("name")]
    public string? FullName { get; set; }
}
