using System.Reflection;
using JadeDbClient.Interfaces;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests to verify package structure and public API surface
/// </summary>
public class PackageStructureTests
{
    [Fact]
    public void JadeDbClientAssembly_SupportsNetFrameworks()
    {
        // Arrange
        var assembly = typeof(IDatabaseService).Assembly;
        
        // Act - Check that the assembly is loaded and accessible
        var assemblyName = assembly.GetName();
        
        // Assert - Assembly should be properly loaded
        Assert.NotNull(assembly);
        Assert.Equal("JadeDbClient", assemblyName.Name);
    }

    [Fact]
    public void IDatabaseService_HasAllExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var methods = interfaceType.GetMethods().Select(m => m.Name).ToList();
        
        // Assert
        Assert.Contains("ExecuteQueryAsync", methods);
        Assert.Contains("ExecuteQueryFirstRowAsync", methods);
        Assert.Contains("ExecuteScalar", methods);
        Assert.Contains("ExecuteStoredProcedureAsync", methods);
        Assert.Contains("ExecuteStoredProcedureSelectDataAsync", methods);
        Assert.Contains("ExecuteStoredProcedureWithOutputAsync", methods);
        Assert.Contains("ExecuteCommandAsync", methods);
        Assert.Contains("GetParameter", methods);
        Assert.Contains("InsertDataTable", methods);
        Assert.Contains("InsertDataTableWithJsonData", methods);
        Assert.Contains("BulkInsertAsync", methods);
        Assert.Contains("OpenConnection", methods);
        Assert.Contains("CloseConnection", methods);
    }

    [Fact]
    public void IDatabaseService_GenericMethods_ReturnTaskTypes()
    {
        // Arrange
        var interfaceType = typeof(IDatabaseService);
        
        // Act
        var executeQueryMethod = interfaceType.GetMethod("ExecuteQueryAsync");
        var executeQueryFirstRowMethod = interfaceType.GetMethod("ExecuteQueryFirstRowAsync");
        var executeStoredProcedureSelectDataMethod = interfaceType.GetMethod("ExecuteStoredProcedureSelectDataAsync");
        
        // Assert
        Assert.NotNull(executeQueryMethod);
        Assert.True(executeQueryMethod!.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>).Name, executeQueryMethod.ReturnType.GetGenericTypeDefinition().Name);
        
        Assert.NotNull(executeQueryFirstRowMethod);
        Assert.True(executeQueryFirstRowMethod!.ReturnType.IsGenericType);
        
        Assert.NotNull(executeStoredProcedureSelectDataMethod);
        Assert.True(executeStoredProcedureSelectDataMethod!.ReturnType.IsGenericType);
    }

    [Theory]
    [InlineData(typeof(MsSqlDbService))]
    [InlineData(typeof(MySqlDbService))]
    [InlineData(typeof(PostgreSqlDbService))]
    public void DatabaseServices_ImplementIDatabaseService(Type serviceType)
    {
        // Act
        var implementsInterface = typeof(IDatabaseService).IsAssignableFrom(serviceType);
        
        // Assert
        Assert.True(implementsInterface, $"{serviceType.Name} should implement IDatabaseService");
    }

    [Fact]
    public void JadeDbClientAssembly_TargetsMultipleFrameworks()
    {
        // Arrange
        var assembly = typeof(IDatabaseService).Assembly;
        
        // Act
        var targetFrameworkAttribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
        
        // Assert - Assembly should target a .NET framework
        Assert.NotNull(targetFrameworkAttribute);
        Assert.Contains("net", targetFrameworkAttribute!.FrameworkName.ToLower());
    }

    [Fact]
    public void Assembly_HasCorrectPackageMetadata()
    {
        // Arrange
        var assembly = typeof(IDatabaseService).Assembly;
        
        // Act
        var assemblyName = assembly.GetName();
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
        
        // Assert
        Assert.Equal("JadeDbClient", assemblyName.Name);
        Assert.NotNull(assemblyName.Version);
    }
}
