using System.Data;
using JadeDbClient.Initialize;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for database service registration and configuration
/// </summary>
public class DatabaseConfigurationTests
{
    [Fact]
    public void AddJadeDbService_WithMsSqlConfiguration_RegistersMsSqlService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = "MsSql",
                ["ConnectionStrings:DbConnection"] = "Server=localhost;Database=Test;User Id=sa;Password=test;"
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Act
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        var dbService = serviceProvider.GetService<JadeDbClient.Interfaces.IDatabaseService>();
        
        // Assert
        Assert.NotNull(dbService);
        Assert.IsType<MsSqlDbService>(dbService);
    }

    [Fact]
    public void AddJadeDbService_WithMySqlConfiguration_RegistersMySqlService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = "MySql",
                ["ConnectionStrings:DbConnection"] = "Server=localhost;Database=Test;User=root;Password=test;"
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Act
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        var dbService = serviceProvider.GetService<JadeDbClient.Interfaces.IDatabaseService>();
        
        // Assert
        Assert.NotNull(dbService);
        Assert.IsType<MySqlDbService>(dbService);
    }

    [Fact]
    public void AddJadeDbService_WithPostgreSqlConfiguration_RegistersPostgreSqlService()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = "PostgreSQL",
                ["ConnectionStrings:DbConnection"] = "Host=localhost;Database=Test;Username=postgres;Password=test;"
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Act
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        var dbService = serviceProvider.GetService<JadeDbClient.Interfaces.IDatabaseService>();
        
        // Assert
        Assert.NotNull(dbService);
        Assert.IsType<PostgreSqlDbService>(dbService);
    }

    [Fact]
    public void AddJadeDbService_WithInvalidDatabaseType_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = "InvalidDb",
                ["ConnectionStrings:DbConnection"] = "Server=localhost;"
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddJadeDbService();
        
        // Act & Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<Exception>(() => serviceProvider.GetService<JadeDbClient.Interfaces.IDatabaseService>());
    }

    [Theory]
    [InlineData("MsSql")]
    [InlineData("MySql")]
    [InlineData("PostgreSQL")]
    public void DatabaseService_GetParameter_CreatesValidParameter(string databaseType)
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = databaseType,
                ["ConnectionStrings:DbConnection"] = "Server=localhost;Database=Test;"
            }!)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        var dbService = serviceProvider.GetService<JadeDbClient.Interfaces.IDatabaseService>();
        
        // Act
        var parameter = dbService!.GetParameter("@TestParam", "TestValue", DbType.String, ParameterDirection.Input, 100);
        
        // Assert
        Assert.NotNull(parameter);
        Assert.Equal("@TestParam", parameter.ParameterName);
        Assert.Equal("TestValue", parameter.Value);
        Assert.Equal(DbType.String, parameter.DbType);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.Equal(100, parameter.Size);
    }

    [Fact]
    public void DatabaseConfigurationService_GetDatabaseType_ReturnsConfiguredType()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DatabaseType"] = "MsSql"
            }!)
            .Build();
        
        var configService = new DatabaseConfigurationService(configuration);
        
        // Act
        var databaseType = configService.GetDatabaseType();
        
        // Assert
        Assert.Equal("MsSql", databaseType);
    }
}
