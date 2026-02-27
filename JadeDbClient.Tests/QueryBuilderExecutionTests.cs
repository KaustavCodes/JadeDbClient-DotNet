using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using JadeDbClient.Attributes;
using JadeDbClient.Enums;
using JadeDbClient.Helpers;
using JadeDbClient.Interfaces;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for the terminal execution methods on QueryBuilder:
/// ToListAsync, ToListAsync&lt;TResult&gt;, ToDynamicListAsync,
/// FirstOrDefaultAsync, FirstOrDefaultAsync&lt;TResult&gt;, FirstOrDefaultDynamicAsync.
/// </summary>
public class QueryBuilderExecutionTests
{
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a mock IDatabaseService pre-wired with ExecuteQueryAsync and
    /// ExecuteQueryFirstRowAsync to return the supplied typed lists/items, and
    /// ExecuteQueryDynamicAsync / ExecuteQueryFirstRowDynamicAsync to return
    /// the supplied dynamic lists/items.
    /// </summary>
    private static Mock<IDatabaseService> CreateMockService(
        IEnumerable<Product>? typedList = null,
        Product? typedFirst = null,
        IEnumerable<dynamic>? dynamicList = null,
        dynamic? dynamicFirst = null)
    {
        var mock = new Mock<IDatabaseService>();
        mock.Setup(s => s.Dialect).Returns(DatabaseDialect.MsSql);
        mock.Setup(s => s.PluralizeTableNames).Returns(false);

        mock.Setup(s => s.GetParameter(It.IsAny<string>(), It.IsAny<object>(),
                It.IsAny<DbType>(), It.IsAny<ParameterDirection>(), It.IsAny<int>()))
            .Returns<string, object, DbType, ParameterDirection, int>((name, value, type, dir, size) =>
            {
                var p = new Mock<IDbDataParameter>();
                p.Setup(x => x.ParameterName).Returns(name);
                p.Setup(x => x.Value).Returns(value);
                return p.Object;
            });

        mock.Setup(s => s.ExecuteQueryAsync<Product>(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .ReturnsAsync(typedList ?? Array.Empty<Product>());

        mock.Setup(s => s.ExecuteQueryFirstRowAsync<Product>(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .ReturnsAsync(typedFirst);

        mock.Setup(s => s.ExecuteQueryDynamicAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .ReturnsAsync(dynamicList ?? Array.Empty<dynamic>());

        mock.Setup(s => s.ExecuteQueryFirstRowDynamicAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .Returns(Task.FromResult<dynamic?>(dynamicFirst));

        return mock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1. ToListAsync (typed shorthand — maps to T)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToListAsync_CallsExecuteQueryAsyncWithBuiltSql()
    {
        var expected = new[] { new Product { Id = 1, Name = "Widget", Price = 9.99m } };
        var mock = CreateMockService(typedList: expected);

        var qb = new QueryBuilder<Product>(mock.Object);
        var results = await qb.Where(p => p.Price > 5m).ToListAsync();

        results.Should().BeEquivalentTo(expected);
        mock.Verify(s => s.ExecuteQueryAsync<Product>(
            It.Is<string>(sql => sql.Contains("FROM products") && sql.Contains("WHERE")),
            It.IsAny<IEnumerable<IDbDataParameter>?>()), Times.Once);
    }

    [Fact]
    public async Task ToListAsync_WithNoRows_ReturnsEmpty()
    {
        var mock = CreateMockService(typedList: Array.Empty<Product>());
        var qb = new QueryBuilder<Product>(mock.Object);

        var results = await qb.ToListAsync();

        results.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. ToListAsync<TResult> (explicitly typed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToListAsyncTyped_CallsExecuteQueryAsyncWithTResult()
    {
        var expected = new[] { new Product { Id = 2, Name = "Gadget" } };
        var mock = CreateMockService(typedList: expected);

        var qb = new QueryBuilder<Product>(mock.Object);
        var results = await qb.ToListAsync<Product>();

        results.Should().BeEquivalentTo(expected);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. ToDynamicListAsync (dynamic rows for JOIN results)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToDynamicListAsync_CallsExecuteQueryDynamicAsync()
    {
        dynamic row = new ExpandoObject();
        ((IDictionary<string, object?>)row)["product_name"] = "Widget";
        ((IDictionary<string, object?>)row)["category_name"] = "Tools";
        IEnumerable<dynamic> dynamicList = new[] { row };

        var mock = CreateMockService(dynamicList: dynamicList);

        var qb = new QueryBuilder<Product>(mock.Object);
        var results = await qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .SelectColumns(cols => cols
                .From<Product>(p => p.Name)
                .From<Category>(c => c.Name))
            .ToDynamicListAsync();

        mock.Verify(s => s.ExecuteQueryDynamicAsync(
            It.Is<string>(sql =>
                sql.Contains("INNER JOIN categories") &&
                sql.Contains("products.product_name") &&
                sql.Contains("categories.category_name")),
            It.IsAny<IEnumerable<IDbDataParameter>?>()), Times.Once);

        var list = (results as IEnumerable<dynamic>)!;
        list.Should().ContainSingle();
        string productName = ((IDictionary<string, object?>)list.First())["product_name"]!.ToString()!;
        productName.Should().Be("Widget");
    }

    [Fact]
    public async Task ToDynamicListAsync_WithNoRows_ReturnsEmpty()
    {
        var mock = CreateMockService(dynamicList: Array.Empty<dynamic>());
        var qb = new QueryBuilder<Product>(mock.Object);

        var results = await qb.ToDynamicListAsync();

        results.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. FirstOrDefaultAsync (typed shorthand — maps to T)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FirstOrDefaultAsync_WhenRowExists_ReturnsMappedObject()
    {
        var expected = new Product { Id = 5, Name = "Widget", Price = 19.99m };
        var mock = CreateMockService(typedFirst: expected);

        var qb = new QueryBuilder<Product>(mock.Object);
        var result = await qb.Where(p => p.Id == 5).FirstOrDefaultAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenNoRow_ReturnsNull()
    {
        var mock = CreateMockService(typedFirst: null);
        var qb = new QueryBuilder<Product>(mock.Object);

        var result = await qb.Where(p => p.Id == 99).FirstOrDefaultAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_CallsExecuteQueryFirstRowAsync()
    {
        var mock = CreateMockService(typedFirst: new Product { Id = 1 });

        var qb = new QueryBuilder<Product>(mock.Object);
        await qb.Where(p => p.Id == 1).FirstOrDefaultAsync();

        mock.Verify(s => s.ExecuteQueryFirstRowAsync<Product>(
            It.Is<string>(sql => sql.Contains("FROM products") && sql.Contains("WHERE")),
            It.IsAny<IEnumerable<IDbDataParameter>?>()), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. FirstOrDefaultAsync<TResult> (explicitly typed)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FirstOrDefaultAsyncTyped_WhenRowExists_ReturnsMappedObject()
    {
        var expected = new Product { Id = 3, Name = "Sprocket" };
        var mock = CreateMockService(typedFirst: expected);

        var qb = new QueryBuilder<Product>(mock.Object);
        var result = await qb.FirstOrDefaultAsync<Product>();

        result.Should().NotBeNull();
        result!.Name.Should().Be("Sprocket");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6. FirstOrDefaultDynamicAsync (dynamic row for JOIN results)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FirstOrDefaultDynamicAsync_WhenRowExists_ReturnsDynamicObject()
    {
        dynamic row = new ExpandoObject();
        ((IDictionary<string, object?>)row)["product_name"] = "Sprocket";
        ((IDictionary<string, object?>)row)["category_name"] = "Parts";

        var mock = CreateMockService(dynamicFirst: row);

        var qb = new QueryBuilder<Product>(mock.Object);
        var result = await qb
            .Join<Category>((p, c) => p.CategoryId == c.Id)
            .FirstOrDefaultDynamicAsync();

        ((object?)result).Should().NotBeNull();
        string name = ((IDictionary<string, object?>)result!)["product_name"]!.ToString()!;
        name.Should().Be("Sprocket");
    }

    [Fact]
    public async Task FirstOrDefaultDynamicAsync_WhenNoRow_ReturnsNull()
    {
        var mock = CreateMockService(dynamicFirst: null);
        var qb = new QueryBuilder<Product>(mock.Object);

        var result = await qb.FirstOrDefaultDynamicAsync();

        ((object?)result).Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultDynamicAsync_CallsExecuteQueryFirstRowDynamicAsync()
    {
        var mock = CreateMockService(dynamicFirst: null);
        var qb = new QueryBuilder<Product>(mock.Object);

        await qb.Join<Category>((p, c) => p.CategoryId == c.Id).FirstOrDefaultDynamicAsync();

        mock.Verify(s => s.ExecuteQueryFirstRowDynamicAsync(
            It.Is<string>(sql => sql.Contains("INNER JOIN categories")),
            It.IsAny<IEnumerable<IDbDataParameter>?>()), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7. SQL shape verification — execution methods build the same SQL as Build*
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToListAsync_GeneratesSameSqlAsBuildSelect()
    {
        var mock = CreateMockService(typedList: Array.Empty<Product>());
        string? capturedSql = null;

        mock.Setup(s => s.ExecuteQueryAsync<Product>(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .Callback<string, IEnumerable<IDbDataParameter>?>((sql, _) => capturedSql = sql)
            .ReturnsAsync(Array.Empty<Product>());

        var qb1 = new QueryBuilder<Product>(mock.Object)
            .Where(p => p.Price > 10m)
            .OrderBy(p => p.Price);

        var (expectedSql, _) = qb1.BuildSelect();

        // Fresh builder with the same config for execution
        var mock2 = CreateMockService(typedList: Array.Empty<Product>());
        capturedSql = null;
        mock2.Setup(s => s.ExecuteQueryAsync<Product>(
                It.IsAny<string>(), It.IsAny<IEnumerable<IDbDataParameter>?>()))
            .Callback<string, IEnumerable<IDbDataParameter>?>((sql, _) => capturedSql = sql)
            .ReturnsAsync(Array.Empty<Product>());

        await new QueryBuilder<Product>(mock2.Object)
            .Where(p => p.Price > 10m)
            .OrderBy(p => p.Price)
            .ToListAsync();

        capturedSql.Should().Be(expectedSql);
    }
}
