using System.Data;
using FluentAssertions;
using JadeDbClient.Attributes;
using JadeDbClient.Enums;
using JadeDbClient.Helpers;
using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for the PluralizeTableName option in JadeDbServiceOptions and its effect on QueryBuilder.
/// </summary>
public class PluralizeTableNameTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<IDatabaseService> CreateMockService(bool pluralizeTableNames = false)
    {
        var mock = new Mock<IDatabaseService>();
        mock.Setup(s => s.Dialect).Returns(DatabaseDialect.MsSql);
        mock.Setup(s => s.PluralizeTableNames).Returns(pluralizeTableNames);
        mock.Setup(s => s.GetParameter(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DbType>(),
                It.IsAny<ParameterDirection>(), It.IsAny<int>()))
            .Returns<string, object, DbType, ParameterDirection, int>((name, value, type, dir, size) =>
            {
                var p = new Mock<IDbDataParameter>();
                p.Setup(x => x.ParameterName).Returns(name);
                p.Setup(x => x.Value).Returns(value);
                return p.Object;
            });
        return mock;
    }

    // ── Test models ───────────────────────────────────────────────────────────

    public class Order
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Box
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Library
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [JadeDbTable("custom_orders")]
    public class OrderWithAttribute
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ── Tests for JadeDbServiceOptions default ────────────────────────────────

    [Fact]
    public void JadeDbServiceOptions_PluralizeTableName_DefaultsToFalse()
    {
        var options = new JadeDbServiceRegistration.JadeDbServiceOptions();
        options.PluralizeTableName.Should().BeFalse();
    }

    // ── Tests for ReflectionHelper.GetTableName ────────────────────────────────

    [Fact]
    public void GetTableName_WhenPluralizeIsFalse_ReturnsClassName()
    {
        ReflectionHelper.GetTableName(typeof(Order), pluralize: false).Should().Be("Order");
    }

    [Fact]
    public void GetTableName_WhenPluralizeIsTrue_ReturnsPluralized()
    {
        ReflectionHelper.GetTableName(typeof(Order), pluralize: true).Should().Be("Orders");
    }

    [Fact]
    public void GetTableName_WhenPluralizeIsTrue_AppliesIesRule()
    {
        // Category ends in consonant + 'y' → ies
        ReflectionHelper.GetTableName(typeof(Category), pluralize: true).Should().Be("Categories");
    }

    [Fact]
    public void GetTableName_WhenPluralizeIsTrue_AppliesEsRule()
    {
        // Box ends in 'x' → es
        ReflectionHelper.GetTableName(typeof(Box), pluralize: true).Should().Be("Boxes");
    }

    [Fact]
    public void GetTableName_WithAttribute_ReturnsAttributeValue_RegardlessOfPluralizeOption()
    {
        ReflectionHelper.GetTableName(typeof(OrderWithAttribute), pluralize: false).Should().Be("custom_orders");
        ReflectionHelper.GetTableName(typeof(OrderWithAttribute), pluralize: true).Should().Be("custom_orders");
    }

    [Fact]
    public void GetTableName_DefaultParameter_DoesNotPluralize()
    {
        // Default parameter is false, so no pluralization
        ReflectionHelper.GetTableName(typeof(Order)).Should().Be("Order");
    }

    // ── Tests for QueryBuilder with PluralizeTableNames = false ──────────────

    [Fact]
    public void QueryBuilder_WhenPluralizeTableNamesIsFalse_UsesClassNameAsTableName()
    {
        var qb = new QueryBuilder<Order>(CreateMockService(pluralizeTableNames: false).Object);
        var (sql, _) = qb.BuildSelect();
        sql.Should().Contain("FROM Order");
        sql.Should().NotContain("FROM Orders");
    }

    // ── Tests for QueryBuilder with PluralizeTableNames = true ───────────────

    [Fact]
    public void QueryBuilder_WhenPluralizeTableNamesIsTrue_UsesPluralizedTableName()
    {
        var qb = new QueryBuilder<Order>(CreateMockService(pluralizeTableNames: true).Object);
        var (sql, _) = qb.BuildSelect();
        sql.Should().Contain("FROM Orders");
    }

    [Fact]
    public void QueryBuilder_WhenPluralizeTableNamesIsTrue_AppliesIesRule()
    {
        var qb = new QueryBuilder<Category>(CreateMockService(pluralizeTableNames: true).Object);
        var (sql, _) = qb.BuildSelect();
        sql.Should().Contain("FROM Categories");
    }

    [Fact]
    public void QueryBuilder_JoinedTable_RespectsPluralizeSetting()
    {
        var qb = new QueryBuilder<Order>(CreateMockService(pluralizeTableNames: true).Object);
        var (sql, _) = qb
            .Join<Library>((o, l) => o.Id == l.Id)
            .BuildSelect();

        sql.Should().Contain("FROM Orders");
        sql.Should().Contain("JOIN Libraries");
    }

    [Fact]
    public void QueryBuilder_JoinedTable_WhenPluralizeIsFalse_UsesClassName()
    {
        var qb = new QueryBuilder<Order>(CreateMockService(pluralizeTableNames: false).Object);
        var (sql, _) = qb
            .Join<Library>((o, l) => o.Id == l.Id)
            .BuildSelect();

        sql.Should().Contain("FROM Order");
        sql.Should().Contain("JOIN Library");
    }

    [Fact]
    public void QueryBuilder_AttributeTableName_IsNotAffectedByPluralizeOption()
    {
        var qb = new QueryBuilder<OrderWithAttribute>(CreateMockService(pluralizeTableNames: true).Object);
        var (sql, _) = qb.BuildSelect();
        sql.Should().Contain("FROM custom_orders");
    }
}
