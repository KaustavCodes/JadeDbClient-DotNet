using System;
using System.Collections.Generic;
using System.Data;
using FluentAssertions;
using JadeDbClient.Attributes;
using JadeDbClient.Enums;
using JadeDbClient.Helpers;
using JadeDbClient.Interfaces;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for expression-based Select and JOIN support added to QueryBuilder.
/// </summary>
public class QueryBuilderSelectJoinTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<IDatabaseService> CreateMockService(DatabaseDialect dialect = DatabaseDialect.MsSql)
    {
        var mock = new Mock<IDatabaseService>();
        mock.Setup(s => s.Dialect).Returns(dialect);
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

    [JadeDbTable("products")]
    public class Product
    {
        public int Id { get; set; }

        [JadeDbColumn("product_name")]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        [JadeDbColumn("category_id")]
        public int CategoryId { get; set; }
    }

    [JadeDbTable("categories")]
    public class Category
    {
        public int Id { get; set; }

        [JadeDbColumn("category_name")]
        public string Name { get; set; } = string.Empty;
    }

    [JadeDbTable("orders")]
    public class Order
    {
        public int Id { get; set; }

        [JadeDbColumn("customer_id")]
        public int CustomerId { get; set; }

        public decimal Total { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1. Expression-based Select – single property
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Select_WithSinglePropertyExpression_IncludesColumnName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb.Select(p => p.Name).BuildSelect();

        sql.Should().StartWith("SELECT product_name");
        sql.Should().NotContain("Price");
    }

    [Fact]
    public void Select_WithValueTypePropertyExpression_IncludesColumnName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        // decimal is a value type and will appear as Convert(p.Price) in the expression tree
        var (sql, _) = qb.Select(p => p.Price).BuildSelect();

        sql.Should().Contain("Price");
        sql.Should().NotContain("product_name");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. Expression-based Select – anonymous type projection
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Select_WithAnonymousTypeProjection_IncludesAllSelectedColumns()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb.Select(p => new { p.Name, p.Price }).BuildSelect();

        sql.Should().StartWith("SELECT product_name, Price");
        sql.Should().NotContain("category_id");
    }

    [Fact]
    public void Select_WithAnonymousTypeProjection_ResolvesColumnAttributes()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb.Select(p => new { p.Name, p.CategoryId }).BuildSelect();

        // Name maps to product_name, CategoryId maps to category_id via [JadeDbColumn]
        sql.Should().Contain("product_name");
        sql.Should().Contain("category_id");
        sql.Should().NotContain("Price");
    }

    [Fact]
    public void Select_WithInvalidExpression_ThrowsArgumentException()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        // Expressions that are NOT simple member accesses should throw
        var act = () => qb.Select(p => new { Complex = p.Name + "!" });

        act.Should().Throw<ArgumentException>();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. JOIN – basic SQL structure
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_InnerJoin_GeneratesCorrectSqlStructure()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        sql.Should().Contain("INNER JOIN categories ON");
        sql.Should().Contain("products.category_id");
        sql.Should().Contain("categories.Id");
    }

    [Fact]
    public void LeftJoin_GeneratesLeftJoinKeyword()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        sql.Should().Contain("LEFT JOIN categories ON");
    }

    [Fact]
    public void RightJoin_GeneratesRightJoinKeyword()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .RightJoin<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        sql.Should().Contain("RIGHT JOIN categories ON");
    }

    [Fact]
    public void FullJoin_GeneratesFullJoinKeyword()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .FullJoin<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        sql.Should().Contain("FULL JOIN categories ON");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. JOIN – column qualification in SELECT and WHERE
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_DefaultSelectColumns_AreQualifiedWithMainTableName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        // All columns from the main table must be prefixed with "products."
        sql.Should().Contain("products.product_name");
        sql.Should().Contain("products.Price");
        sql.Should().Contain("products.category_id");
    }

    [Fact]
    public void Join_WhereClause_ColumnsAreQualifiedWithMainTableName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Where(p => p.Price > 10m)
            .BuildSelect();

        sql.Should().Contain("WHERE (products.Price > @p0)");
    }

    [Fact]
    public void Join_ExpressionSelectColumns_AreQualifiedWithMainTableName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select(p => new { p.Name, p.Price })
            .BuildSelect();

        sql.Should().Contain("products.product_name");
        sql.Should().Contain("products.Price");
    }

    [Fact]
    public void Join_StringSelectColumns_AreNotAutoQualified()
    {
        // Raw-string Select() caller is responsible for their own table qualification
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select("products.product_name", "categories.category_name")
            .BuildSelect();

        sql.Should().StartWith("SELECT products.product_name, categories.category_name");
        // Should NOT double-qualify
        sql.Should().NotContain("products.products.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. JOIN – ON clause uses correct column names (including [JadeDbColumn] attribute)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_OnClause_RespectsJadeDbColumnAttribute()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .BuildSelect();

        // p.CategoryId has [JadeDbColumn("category_id")], so it should appear as category_id
        sql.Should().Contain("products.category_id");
        // c.Id has no attribute, so it appears as Id
        sql.Should().Contain("categories.Id");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6. JOIN – compound ON condition
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_CompoundOnCondition_GeneratesAndClause()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id && p.Price > 0m)
            .BuildSelect();

        sql.Should().Contain(" AND ");
        sql.Should().Contain("products.Price");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7. JOIN – multiple joins
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleJoins_GenerateAllJoinClauses()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .LeftJoin<Order>((p, o) => p.Id == o.Id)
            .BuildSelect();

        sql.Should().Contain("INNER JOIN categories ON");
        sql.Should().Contain("LEFT JOIN orders ON");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 8. JOIN – no joins means no table qualification
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NoJoin_SelectColumns_AreNotQualified()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb.BuildSelect();

        // Without a join, columns should NOT have a table prefix
        sql.Should().NotContain("products.product_name");
        sql.Should().Contain("product_name");
    }

    [Fact]
    public void NoJoin_WhereClause_ColumnsAreNotQualified()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Where(p => p.Price > 5m)
            .BuildSelect();

        sql.Should().Contain("WHERE (Price > @p0)");
        sql.Should().NotContain("products.Price");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 9. ORDER BY qualification when joins are present
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_OrderBy_ExpressionColumn_IsQualifiedWithMainTableName()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .OrderBy(p => p.Name)
            .BuildSelect();

        sql.Should().Contain("ORDER BY products.product_name ASC");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 10. JOIN – two-parameter Select expression (cross-table column selection)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Join_TwoParamSelect_AnonymousProjection_IncludesColumnsFromBothTables()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => new { ProductName = p.Name, CategoryName = c.Name })
            .BuildSelect();

        sql.Should().Contain("products.product_name");
        sql.Should().Contain("categories.category_name");
        // Should not include other columns
        sql.Should().NotContain("Price");
    }

    [Fact]
    public void Join_TwoParamSelect_SingleColumnFromJoinedTable_IsQualified()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => c.Name)
            .BuildSelect();

        sql.Should().StartWith("SELECT categories.category_name");
        // The SELECT list should not include main-table columns
        sql.Should().NotContain("products.product_name");
        sql.Should().NotContain("products.Price");
    }

    [Fact]
    public void Join_TwoParamSelect_SingleColumnFromMainTable_IsQualified()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => p.Price)
            .BuildSelect();

        sql.Should().StartWith("SELECT products.Price");
        // The SELECT list should not include joined-table columns
        sql.Should().NotContain("categories.category_name");
    }

    [Fact]
    public void Join_TwoParamSelect_RespectsJadeDbColumnAttribute()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => new { p.CategoryId, c.Name })
            .BuildSelect();

        // CategoryId has [JadeDbColumn("category_id")]
        sql.Should().Contain("products.category_id");
        // Category.Name has [JadeDbColumn("category_name")]
        sql.Should().Contain("categories.category_name");
    }

    [Fact]
    public void Join_TwoParamSelect_WithWhereClause_GeneratesCorrectSql()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => new { ProductName = p.Name, CategoryName = c.Name })
            .Where(p => p.Price > 10m)
            .BuildSelect();

        sql.Should().Contain("products.product_name");
        sql.Should().Contain("categories.category_name");
        sql.Should().Contain("WHERE (products.Price > @p0)");
    }

    [Fact]
    public void Join_TwoParamSelect_WithInvalidExpression_ThrowsArgumentException()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var act = () => qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => new { Complex = p.Name + "!" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Join_TwoParamSelect_DoesNotDoubleQualifyColumns()
    {
        var qb = new QueryBuilder<Product>(CreateMockService().Object);

        var (sql, _) = qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .Select((Product p, Category c) => new { ProductName = p.Name, CategoryName = c.Name })
            .BuildSelect();

        // Should not have "products.products." or "categories.categories."
        sql.Should().NotContain("products.products.");
        sql.Should().NotContain("categories.categories.");
    }
}
