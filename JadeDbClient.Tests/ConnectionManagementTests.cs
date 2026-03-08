using System.Data;
using System.Reflection;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests verifying that connection management (Open, Close, Dispose) works
/// correctly and does not leak connections.
/// </summary>
public class ConnectionManagementTests
{
    // -------------------------------------------------------------------------
    // IDatabaseService interface contract
    // -------------------------------------------------------------------------

    [Fact]
    public void IDatabaseService_ImplementsIDisposable()
    {
        // The interface must extend IDisposable so that DI containers and
        // calling code can safely wrap services in a using block.
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IDatabaseService)));
    }

    [Fact]
    public void IDatabaseService_HasOpenConnectionMethod()
    {
        var method = typeof(IDatabaseService).GetMethod("OpenConnection");
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void IDatabaseService_HasCloseConnectionMethod()
    {
        var method = typeof(IDatabaseService).GetMethod("CloseConnection");
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // -------------------------------------------------------------------------
    // Service-level implementation checks
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void DatabaseServices_ImplementIDisposable(Type serviceType)
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(serviceType),
            $"{serviceType.Name} must implement IDisposable to prevent connection leaks.");
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void DatabaseServices_HaveDisposeMethod(Type serviceType)
    {
        var method = serviceType.GetMethod("Dispose", Type.EmptyTypes);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // -------------------------------------------------------------------------
    // CloseConnection clears the Connection property
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void CloseConnection_SetsConnectionToNull_WhenConnectionWasNull(Type serviceType)
    {
        // Arrange – create a service instance without opening a connection
        var service = CreateService(serviceType);

        // Act – calling CloseConnection with no open connection should not throw
        var exception = Record.Exception(() => service.CloseConnection());

        // Assert
        Assert.Null(exception);
        Assert.Null(service.Connection);
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void Dispose_DoesNotThrow_WhenConnectionIsNull(Type serviceType)
    {
        // Arrange
        var service = CreateService(serviceType);

        // Act + Assert — disposing a service that never opened a connection must be safe
        var exception = Record.Exception(() => service.Dispose());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void Dispose_CalledMultipleTimes_DoesNotThrow(Type serviceType)
    {
        // Arrange
        var service = CreateService(serviceType);

        // Act + Assert — repeated disposal must be safe (idempotent)
        var exception = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });
        Assert.Null(exception);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static IDatabaseService CreateService(Type serviceType)
    {
        // All three services have a constructor that accepts
        // (string connectionString, JadeDbMapperOptions, JadeDbServiceOptions)
        var mapperOptions = new JadeDbMapperOptions();
        var serviceOptions = new JadeDbClient.Initialize.JadeDbServiceRegistration.JadeDbServiceOptions();
        const string dummyConnStr = "Server=localhost;Database=test;";

        return (IDatabaseService)Activator.CreateInstance(
            serviceType,
            dummyConnStr,
            mapperOptions,
            serviceOptions)!;
    }
}
