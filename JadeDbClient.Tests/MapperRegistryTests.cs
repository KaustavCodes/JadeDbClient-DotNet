using System.Data;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests to verify the mapper registry integration works correctly
/// </summary>
public class MapperRegistryTests
{
    [Fact]
    public void JadeMapperOptions_CanRegisterMapper()
    {
        // Arrange
        var options = new JadeDbMapperOptions();
        
        // Act & Assert - RegisterMapper should not throw
        options.RegisterMapper<TestModel>(reader => new TestModel
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1)
        });
    }

    [Fact]
    public void AddJadeDbService_RegistersJadeMapperOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DatabaseType", "MsSql" },
                { "ConnectionStrings:DbConnection", "Server=localhost;Database=test;User Id=test;Password=test;" }
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddJadeDbService();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var mapperOptions = serviceProvider.GetService<JadeDbMapperOptions>();
        Assert.NotNull(mapperOptions);
    }

    [Fact]
    public void AddJadeDbService_ConfigureAction_InvokesCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DatabaseType", "MsSql" },
                { "ConnectionStrings:DbConnection", "Server=localhost;Database=test;User Id=test;Password=test;" }
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        var callbackInvoked = false;

        // Act
        services.AddJadeDbService(options =>
        {
            callbackInvoked = true;
            options.RegisterMapper<TestModel>(reader => new TestModel
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        Assert.True(callbackInvoked);
        var mapperOptions = serviceProvider.GetRequiredService<JadeDbMapperOptions>();
        Assert.NotNull(mapperOptions);
    }

    [Fact]
    public void DatabaseServices_AcceptJadeMapperOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DbConnection", "Server=localhost;Database=test;User Id=test;Password=test;" }
            })
            .Build();
        var mapperOptions = new JadeDbMapperOptions();

        // Act & Assert - These should not throw
        var msSqlService = new JadeDbClient.MsSqlDbService(configuration, mapperOptions);
        Assert.NotNull(msSqlService);

        var mySqlService = new JadeDbClient.MySqlDbService(configuration, mapperOptions);
        Assert.NotNull(mySqlService);

        var postgreSqlService = new JadeDbClient.PostgreSqlDbService(configuration, mapperOptions);
        Assert.NotNull(postgreSqlService);
    }

    [Fact]
    public void DatabaseServices_ThrowWhenMapperOptionsIsNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DbConnection", "Server=localhost;Database=test;User Id=test;Password=test;" }
            })
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JadeDbClient.MsSqlDbService(configuration, null!));
        Assert.Throws<ArgumentNullException>(() => new JadeDbClient.MySqlDbService(configuration, null!));
        Assert.Throws<ArgumentNullException>(() => new JadeDbClient.PostgreSqlDbService(configuration, null!));
    }

    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
