using System.Diagnostics.CodeAnalysis;
using JadeDbClient.Interfaces;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests to verify AOT compatibility attributes are properly applied
/// </summary>
public class AotCompatibilityTests
{
    [Fact]
    public void ExecuteQueryAsync_HasDynamicallyAccessedMembersAttribute()
    {
        // Arrange
        var method = typeof(IDatabaseService).GetMethod(nameof(IDatabaseService.ExecuteQueryAsync));
        
        // Assert
        Assert.NotNull(method);
        var genericParam = method!.GetGenericArguments()[0];
        var attributes = genericParam.GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), false);
        Assert.NotEmpty(attributes);
        
        var attribute = (DynamicallyAccessedMembersAttribute)attributes[0];
        Assert.Equal(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
            attribute.MemberTypes
        );
    }

    [Fact]
    public void ExecuteQueryFirstRowAsync_HasDynamicallyAccessedMembersAttribute()
    {
        // Arrange
        var method = typeof(IDatabaseService).GetMethod(nameof(IDatabaseService.ExecuteQueryFirstRowAsync));
        
        // Assert
        Assert.NotNull(method);
        var genericParam = method!.GetGenericArguments()[0];
        var attributes = genericParam.GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), false);
        Assert.NotEmpty(attributes);
        
        var attribute = (DynamicallyAccessedMembersAttribute)attributes[0];
        Assert.Equal(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
            attribute.MemberTypes
        );
    }

    [Fact]
    public void ExecuteStoredProcedureSelectDataAsync_HasDynamicallyAccessedMembersAttribute()
    {
        // Arrange
        var method = typeof(IDatabaseService).GetMethod(nameof(IDatabaseService.ExecuteStoredProcedureSelectDataAsync));
        
        // Assert
        Assert.NotNull(method);
        var genericParam = method!.GetGenericArguments()[0];
        var attributes = genericParam.GetCustomAttributes(typeof(DynamicallyAccessedMembersAttribute), false);
        Assert.NotEmpty(attributes);
        
        var attribute = (DynamicallyAccessedMembersAttribute)attributes[0];
        Assert.Equal(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
            attribute.MemberTypes
        );
    }

    [Fact]
    public void TestModel_CanBeInstantiatedWithActivator()
    {
        // This test verifies that a typical model class can be instantiated
        // using Activator.CreateInstance, which is what the library does internally
        
        // Act
        var instance = Activator.CreateInstance<TestModel>();
        
        // Assert
        Assert.NotNull(instance);
        Assert.Equal(0, instance.Id);
        Assert.Equal(string.Empty, instance.Name);
    }

    [Fact]
    public void TestModel_HasPublicProperties()
    {
        // This test verifies that properties can be discovered via reflection
        
        // Act
        var properties = typeof(TestModel).GetProperties();
        
        // Assert
        Assert.Contains(properties, p => p.Name == "Id");
        Assert.Contains(properties, p => p.Name == "Name");
        Assert.Contains(properties, p => p.Name == "CreatedDate");
    }

    [Fact]
    public void ExecuteQueryDynamicAsync_ExistsOnInternalInterface()
    {
        // The dynamic execution path lives on the library-internal IDynamicQueryExecutor,
        // NOT on the public IDatabaseService, so existing custom implementations are
        // never broken by this feature.
        var publicMethod = typeof(IDatabaseService).GetMethod("ExecuteQueryDynamicAsync");
        Assert.Null(publicMethod); // must NOT be on the public interface

        var internalMethod = typeof(IDynamicQueryExecutor).GetMethod("ExecuteQueryDynamicAsync");
        Assert.NotNull(internalMethod);
        Assert.Empty(internalMethod!.GetGenericArguments());
    }

    [Fact]
    public void ExecuteQueryFirstRowDynamicAsync_ExistsOnInternalInterface()
    {
        var publicMethod = typeof(IDatabaseService).GetMethod("ExecuteQueryFirstRowDynamicAsync");
        Assert.Null(publicMethod); // must NOT be on the public interface

        var internalMethod = typeof(IDynamicQueryExecutor).GetMethod("ExecuteQueryFirstRowDynamicAsync");
        Assert.NotNull(internalMethod);
        Assert.Empty(internalMethod!.GetGenericArguments());
    }

    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
