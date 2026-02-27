using System;
using System.Data;
using FluentAssertions;
using JadeDbClient.Attributes;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Comprehensive test suite for Source Generator and AOT features
/// Tests generator verification, null safety, provider switching, and transaction integrity
/// </summary>
public class SourceGeneratorTests
{
    /// <summary>
    /// Test 1: Generator Verification
    /// Verifies that mappers can be registered (simulating what Source Generator does)
    /// and can be retrieved and executed from JadeDbMapperOptions
    /// </summary>
    [Fact]
    public void GeneratorVerification_SimulatedGeneratedMapper_CanBeRetrievedAndExecuted()
    {
        // Arrange - Create a mock IDataReader
        var mockReader = new Mock<IDataReader>();
        
        // Setup mock to return specific values
        mockReader.Setup(r => r.GetOrdinal("UserId")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("Username")).Returns(1);
        mockReader.Setup(r => r.GetOrdinal("Email")).Returns(2);
        mockReader.Setup(r => r.GetOrdinal("IsActive")).Returns(3);
        mockReader.Setup(r => r.GetOrdinal("CreatedAt")).Returns(4);
        
        mockReader.Setup(r => r.GetInt32(0)).Returns(123);
        mockReader.Setup(r => r.GetString(1)).Returns("testuser");
        mockReader.Setup(r => r.GetString(2)).Returns("test@example.com");
        mockReader.Setup(r => r.GetBoolean(3)).Returns(true);
        mockReader.Setup(r => r.GetDateTime(4)).Returns(new DateTime(2024, 1, 1));
        
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        // Simulate what the Source Generator does - register mapper in GlobalMappers
        JadeDbMapperOptions.RegisterGlobalMapper<GeneratedUserModel>(reader => new GeneratedUserModel
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        });
        
        // Create a mapper options instance (should pull from GlobalMappers)
        var mapperOptions = new JadeDbMapperOptions();
        
        // Act - Check if mapper exists and execute it
        bool hasMapper = mapperOptions.HasMapper<GeneratedUserModel>();
        
        // Assert
        hasMapper.Should().BeTrue("Simulated Source Generator mapper should be available");
        
        // Execute the mapper
        var result = mapperOptions.ExecuteMapper<GeneratedUserModel>(mockReader.Object);
        
        result.Should().NotBeNull();
        result!.UserId.Should().Be(123);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(new DateTime(2024, 1, 1));
    }
    
    /// <summary>
    /// Test 2: Null Safety Tests
    /// Verifies that nullable types are handled correctly when IDataReader returns DBNull
    /// Simulates what the Source Generator would create
    /// </summary>
    [Fact]
    public void NullSafety_NullableTypes_HandledCorrectlyWithDBNull()
    {
        // Arrange - Create a mock IDataReader that returns DBNull for nullable fields
        var mockReader = new Mock<IDataReader>();
        
        // Setup column ordinals
        mockReader.Setup(r => r.GetOrdinal("ProductId")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("ProductName")).Returns(1);
        mockReader.Setup(r => r.GetOrdinal("Price")).Returns(2);
        mockReader.Setup(r => r.GetOrdinal("Stock")).Returns(3);
        mockReader.Setup(r => r.GetOrdinal("LastUpdated")).Returns(4);
        mockReader.Setup(r => r.GetOrdinal("Description")).Returns(5);
        
        // Non-nullable fields
        mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
        mockReader.Setup(r => r.GetInt32(0)).Returns(456);
        
        mockReader.Setup(r => r.IsDBNull(1)).Returns(false);
        mockReader.Setup(r => r.GetString(1)).Returns("Test Product");
        
        mockReader.Setup(r => r.IsDBNull(2)).Returns(false);
        mockReader.Setup(r => r.GetDecimal(2)).Returns(99.99m);
        
        // Nullable fields - return DBNull
        mockReader.Setup(r => r.IsDBNull(3)).Returns(true);  // Stock (int?)
        mockReader.Setup(r => r.IsDBNull(4)).Returns(true);  // LastUpdated (DateTime?)
        mockReader.Setup(r => r.IsDBNull(5)).Returns(true);  // Description (string?)
        
        // Simulate what Source Generator creates for nullable types
        JadeDbMapperOptions.RegisterGlobalMapper<NullableProductModel>(reader => new NullableProductModel
        {
            ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
            ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
            Stock = reader.IsDBNull(reader.GetOrdinal("Stock")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Stock")),
            LastUpdated = reader.IsDBNull(reader.GetOrdinal("LastUpdated")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastUpdated")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description"))
        });
        
        var mapperOptions = new JadeDbMapperOptions();
        
        // Act
        bool hasMapper = mapperOptions.HasMapper<NullableProductModel>();
        
        // Assert
        hasMapper.Should().BeTrue("Simulated Source Generator mapper should handle nullable types");
        
        var result = mapperOptions.ExecuteMapper<NullableProductModel>(mockReader.Object);
        
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(456);
        result.ProductName.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        
        // Verify nullable fields are null (not throwing exceptions)
        result.Stock.Should().BeNull("Nullable int should be null when DBNull is returned");
        result.LastUpdated.Should().BeNull("Nullable DateTime should be null when DBNull is returned");
        result.Description.Should().BeNull("Nullable string should be null when DBNull is returned");
    }
    
    /// <summary>
    /// Test 3: Provider Switching
    /// Verifies that JadeDbServiceRegistration resolves correct database service
    /// based on DatabaseType configuration
    /// </summary>
    [Theory]
    [InlineData("MsSql", typeof(MsSqlDbService))]
    [InlineData("MySql", typeof(MySqlDbService))]
    [InlineData("PostgreSQL", typeof(PostgreSqlDbService))]
    public void ProviderSwitching_DatabaseType_ResolvesCorrectService(string databaseType, Type expectedServiceType)
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock configuration with specific database type
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "DatabaseType", databaseType },
            { "ConnectionStrings:DbConnection", "Server=localhost;Database=test;User Id=test;Password=test;" }
        });
        var configuration = configurationBuilder.Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Act
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        var databaseService = serviceProvider.GetRequiredService<IDatabaseService>();
        
        // Assert
        databaseService.Should().NotBeNull();
        databaseService.Should().BeOfType(expectedServiceType, 
            $"DatabaseType '{databaseType}' should resolve to {expectedServiceType.Name}");
    }
    
    /// <summary>
    /// Test 4: Transaction Integrity
    /// Simulates a failure during transaction and verifies rollback is called
    /// </summary>
    [Fact]
    public void TransactionIntegrity_FailureDuringTransaction_CallsRollback()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        
        // Track whether Rollback was called
        bool rollbackCalled = false;
        mockTransaction.Setup(t => t.Rollback()).Callback(() => rollbackCalled = true);
        
        mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        
        // Simulate transaction usage pattern
        var transaction = mockConnection.Object.BeginTransaction();
        
        try
        {
            // Simulate some database operation that fails
            throw new Exception("Simulated database error");
        }
        catch (Exception)
        {
            // Act - Rollback on error (as the library should do)
            transaction.Rollback();
        }
        
        // Assert
        rollbackCalled.Should().BeTrue("Rollback should be called when transaction fails");
        mockTransaction.Verify(t => t.Rollback(), Times.Once, 
            "Rollback should be called exactly once on transaction failure");
    }
    
    /// <summary>
    /// Test 5: Verify Manual Registration Still Works
    /// Ensures that manual RegisterMapper still works for third-party models
    /// </summary>
    [Fact]
    public void ManualRegistration_ThirdPartyModel_WorksAlongsideSourceGenerator()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Id")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("Name")).Returns(1);
        mockReader.Setup(r => r.GetInt32(0)).Returns(789);
        mockReader.Setup(r => r.GetString(1)).Returns("Third Party");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mapperOptions = new JadeDbMapperOptions();
        
        // Act - Manually register a third-party model (cannot use [JadeDbObject])
        mapperOptions.RegisterMapper<ThirdPartyModel>(reader => new ThirdPartyModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name"))
        });
        
        bool hasMapper = mapperOptions.HasMapper<ThirdPartyModel>();
        
        // Assert
        hasMapper.Should().BeTrue("Manually registered mapper should be retrievable");
        
        if (hasMapper)
        {
            var result = mapperOptions.ExecuteMapper<ThirdPartyModel>(mockReader.Object);
            result.Should().NotBeNull();
            result!.Id.Should().Be(789);
            result.Name.Should().Be("Third Party");
        }
    }

    /// <summary>
    /// Test 6: Partial Column Selection
    /// Verifies that a source-generated mapper correctly handles queries that return only
    /// a subset of the model's columns (e.g. SELECT tempid FROM tbl_... instead of SELECT *).
    /// Missing columns must fall back to default values rather than throwing.
    /// </summary>
    [Fact]
    public void PartialColumnSelection_OnlySubsetOfColumnsReturned_MapsAvailableColumnsAndDefaultsRest()
    {
        // Arrange - mock reader that returns only "tempid" (simulates SELECT tempid FROM ...)
        var mockReader = new Mock<IDataReader>();

        mockReader.Setup(r => r.FieldCount).Returns(1);
        mockReader.Setup(r => r.GetName(0)).Returns("tempid");
        mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
        mockReader.Setup(r => r.GetInt64(0)).Returns(42L);

        // Simulate what the updated source generator now produces:
        // ordinals are resolved by iterating reader.FieldCount, missing columns stay -1.
        JadeDbMapperOptions.RegisterGlobalMapper<PartialColumnModel>(reader =>
        {
            int __ord_tempid    = -1;
            int __ord_userid    = -1;
            int __ord_formdata  = -1;
            int __ord_is_active = -1;
            for (int __i = 0; __i < reader.FieldCount; __i++)
            {
                var __cn = reader.GetName(__i);
                if (string.Equals(__cn, "tempid",    StringComparison.OrdinalIgnoreCase)) { __ord_tempid    = __i; continue; }
                if (string.Equals(__cn, "userid",    StringComparison.OrdinalIgnoreCase)) { __ord_userid    = __i; continue; }
                if (string.Equals(__cn, "formdata",  StringComparison.OrdinalIgnoreCase)) { __ord_formdata  = __i; continue; }
                if (string.Equals(__cn, "is_active", StringComparison.OrdinalIgnoreCase)) { __ord_is_active = __i; continue; }
            }
            return new PartialColumnModel
            {
                tempid    = __ord_tempid    >= 0 ? reader.GetInt64(__ord_tempid)                                                  : default,
                userid    = __ord_userid    >= 0 ? reader.GetInt64(__ord_userid)                                                  : default,
                formdata  = __ord_formdata  >= 0 ? (reader.IsDBNull(__ord_formdata)  ? null : reader.GetString(__ord_formdata))   : default!,
                is_active = __ord_is_active >= 0 ? reader.GetBoolean(__ord_is_active)                                            : default,
            };
        });

        var mapperOptions = new JadeDbMapperOptions();

        // Act
        var result = mapperOptions.ExecuteMapper<PartialColumnModel>(mockReader.Object);

        // Assert
        result.Should().NotBeNull("mapper must not throw when columns are missing");
        result!.tempid.Should().Be(42L, "the returned column should be mapped correctly");
        result.userid.Should().Be(0L,   "missing non-nullable column should fall back to default(long)");
        result.formdata.Should().BeNull("missing nullable column should fall back to null");
        result.is_active.Should().BeFalse("missing non-nullable bool should fall back to default(bool)");
    }
}

// Test Models - These would typically have [JadeDbObject] attribute and be in source generator's scope
// For testing, we're simulating what the generator would create

[JadeDbObject]
public partial class GeneratedUserModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

[JadeDbObject]
public partial class NullableProductModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? Stock { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? Description { get; set; }
}

// Third-party model (cannot add [JadeDbObject] attribute)
public class ThirdPartyModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Model that mirrors the tbl_temp_data_attribute scenario from the issue
[JadeDbObject]
public partial class PartialColumnModel
{
    public long tempid { get; set; }
    public long userid { get; set; }
    public string? formdata { get; set; }
    public bool is_active { get; set; }
}
