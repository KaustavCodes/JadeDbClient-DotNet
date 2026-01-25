using System.Data;
using System.Reflection;
using JadeDbClient.Interfaces;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for database transaction functionality
/// </summary>
public class TransactionTests
{
    [Fact]
    public void IDatabaseService_HasBeginTransactionMethod()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var methods = interfaceType.GetMethods().Where(m => m.Name == "BeginTransaction").ToList();
        
        // Assert
        Assert.NotEmpty(methods);
        Assert.Equal(2, methods.Count); // Should have 2 overloads
    }

    [Fact]
    public void IDatabaseService_HasBeginTransactionWithIsolationLevel()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var method = interfaceType.GetMethods()
            .FirstOrDefault(m => m.Name == "BeginTransaction" && m.GetParameters().Length == 1);
        
        // Assert
        Assert.NotNull(method);
        var parameter = method!.GetParameters()[0];
        Assert.Equal(typeof(IsolationLevel), parameter.ParameterType);
        Assert.Equal(typeof(IDbTransaction), method.ReturnType);
    }

    [Fact]
    public void IDatabaseService_HasBeginTransactionWithoutParameters()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var method = interfaceType.GetMethods()
            .FirstOrDefault(m => m.Name == "BeginTransaction" && m.GetParameters().Length == 0);
        
        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(IDbTransaction), method!.ReturnType);
    }

    [Fact]
    public void IDatabaseService_HasCommitTransactionMethod()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var method = interfaceType.GetMethod("CommitTransaction");
        
        // Assert
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(IDbTransaction), parameters[0].ParameterType);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public void IDatabaseService_HasRollbackTransactionMethod()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var method = interfaceType.GetMethod("RollbackTransaction");
        
        // Assert
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(IDbTransaction), parameters[0].ParameterType);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void DatabaseServices_ImplementTransactionMethods(Type serviceType)
    {
        // Act
        var beginTransactionMethod = serviceType.GetMethod("BeginTransaction", Type.EmptyTypes);
        var beginTransactionWithIsolationMethod = serviceType.GetMethod("BeginTransaction", new[] { typeof(IsolationLevel) });
        var commitMethod = serviceType.GetMethod("CommitTransaction");
        var rollbackMethod = serviceType.GetMethod("RollbackTransaction");
        
        // Assert
        Assert.NotNull(beginTransactionMethod);
        Assert.NotNull(beginTransactionWithIsolationMethod);
        Assert.NotNull(commitMethod);
        Assert.NotNull(rollbackMethod);
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void DatabaseServices_TransactionMethodsReturnCorrectTypes(Type serviceType)
    {
        // Act
        var beginTransactionMethod = serviceType.GetMethod("BeginTransaction", Type.EmptyTypes);
        var beginTransactionWithIsolationMethod = serviceType.GetMethod("BeginTransaction", new[] { typeof(IsolationLevel) });
        
        // Assert
        Assert.Equal(typeof(IDbTransaction), beginTransactionMethod!.ReturnType);
        Assert.Equal(typeof(IDbTransaction), beginTransactionWithIsolationMethod!.ReturnType);
    }
}
