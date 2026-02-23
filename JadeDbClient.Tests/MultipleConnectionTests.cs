using JadeDbClient.Initialize;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests for the multiple named database connections feature.
/// </summary>
public class MultipleConnectionTests
{
    // -------------------------------------------------------------------------
    // JadeDbNamedConnectionsBuilder
    // -------------------------------------------------------------------------

    [Fact]
    public void JadeDbNamedConnectionsBuilder_AddConnection_StoresEntry()
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        builder.AddConnection("main", "MsSql", "Server=localhost;Database=Main;");

        Assert.Single(builder.Connections);
        Assert.True(builder.Connections.ContainsKey("main"));
        Assert.Equal("MsSql", builder.Connections["main"].DatabaseType);
        Assert.Equal("Server=localhost;Database=Main;", builder.Connections["main"].ConnectionString);
    }

    [Fact]
    public void JadeDbNamedConnectionsBuilder_AddMultipleConnections_StoresAll()
    {
        var builder = new JadeDbNamedConnectionsBuilder()
            .AddConnection("main",    "MsSql",      "Server=localhost;Database=Main;")
            .AddConnection("reports", "PostgreSQL",  "Host=localhost;Database=Reports;")
            .AddConnection("cache",   "MySql",       "Server=localhost;Database=Cache;");

        Assert.Equal(3, builder.Connections.Count);
        Assert.True(builder.Connections.ContainsKey("main"));
        Assert.True(builder.Connections.ContainsKey("reports"));
        Assert.True(builder.Connections.ContainsKey("cache"));
    }

    [Fact]
    public void JadeDbNamedConnectionsBuilder_AddConnection_IsCaseInsensitive()
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        builder.AddConnection("MyConn", "MsSql", "Server=localhost;");
        // Overwrite with different case key
        builder.AddConnection("myconn", "MySql", "Server=other;");

        // Should have only one entry since names are case-insensitive
        Assert.Single(builder.Connections);
    }

    [Fact]
    public void JadeDbNamedConnectionsBuilder_AddConnection_LastWriteWins()
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        builder.AddConnection("conn", "MsSql",      "Server=first;");
        builder.AddConnection("conn", "PostgreSQL",  "Host=second;");

        Assert.Single(builder.Connections);
        Assert.Equal("PostgreSQL", builder.Connections["conn"].DatabaseType);
    }

    [Theory]
    [InlineData(null,  "MsSql", "Server=localhost;")]
    [InlineData("",    "MsSql", "Server=localhost;")]
    [InlineData("   ", "MsSql", "Server=localhost;")]
    public void JadeDbNamedConnectionsBuilder_AddConnection_NullOrWhitespaceNameThrows(string? name, string dbType, string connStr)
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        Assert.Throws<ArgumentException>(() => builder.AddConnection(name!, dbType, connStr));
    }

    [Theory]
    [InlineData("conn", null,  "Server=localhost;")]
    [InlineData("conn", "",    "Server=localhost;")]
    public void JadeDbNamedConnectionsBuilder_AddConnection_NullOrEmptyDbTypeThrows(string name, string? dbType, string connStr)
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        Assert.Throws<ArgumentException>(() => builder.AddConnection(name, dbType!, connStr));
    }

    [Theory]
    [InlineData("conn", "MsSql", null)]
    [InlineData("conn", "MsSql", "")]
    public void JadeDbNamedConnectionsBuilder_AddConnection_NullOrEmptyConnectionStringThrows(string name, string dbType, string? connStr)
    {
        var builder = new JadeDbNamedConnectionsBuilder();
        Assert.Throws<ArgumentException>(() => builder.AddConnection(name, dbType, connStr!));
    }

    // -------------------------------------------------------------------------
    // AddJadeDbNamedConnections – programmatic registration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddJadeDbNamedConnections_ProgrammaticMsSql_RegistersMsSqlService()
    {
        var services = BuildServiceProvider(null, connections =>
            connections.AddConnection("main", "MsSql", "Server=localhost;Database=Test;User Id=sa;Password=test;"));

        var factory = services.GetService<IJadeDbServiceFactory>();
        Assert.NotNull(factory);

        var db = factory!.GetService("main");
        Assert.NotNull(db);
        Assert.IsType<MsSqlDbService>(db);
    }

    [Fact]
    public void AddJadeDbNamedConnections_ProgrammaticMySql_RegistersMySqlService()
    {
        var services = BuildServiceProvider(null, connections =>
            connections.AddConnection("mysql", "MySql", "Server=localhost;Database=Test;User=root;Password=test;"));

        var factory = services.GetService<IJadeDbServiceFactory>();
        var db = factory!.GetService("mysql");

        Assert.IsType<MySqlDbService>(db);
    }

    [Fact]
    public void AddJadeDbNamedConnections_ProgrammaticPostgreSql_RegistersPostgreSqlService()
    {
        var services = BuildServiceProvider(null, connections =>
            connections.AddConnection("pg", "PostgreSQL", "Host=localhost;Database=Test;Username=postgres;Password=test;"));

        var factory = services.GetService<IJadeDbServiceFactory>();
        var db = factory!.GetService("pg");

        Assert.IsType<PostgreSqlDbService>(db);
    }

    [Fact]
    public void AddJadeDbNamedConnections_MultipleConnections_ReturnsCorrectServiceForEachName()
    {
        var services = BuildServiceProvider(null, connections =>
        {
            connections
                .AddConnection("mssql", "MsSql",      "Server=localhost;Database=Sql;User Id=sa;Password=test;")
                .AddConnection("pg",    "PostgreSQL",  "Host=localhost;Database=Pg;Username=postgres;Password=test;")
                .AddConnection("mysql", "MySql",       "Server=localhost;Database=My;User=root;Password=test;");
        });

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.IsType<MsSqlDbService>(factory.GetService("mssql"));
        Assert.IsType<PostgreSqlDbService>(factory.GetService("pg"));
        Assert.IsType<MySqlDbService>(factory.GetService("mysql"));
    }

    [Fact]
    public void AddJadeDbNamedConnections_UnknownName_ThrowsKeyNotFoundException()
    {
        var services = BuildServiceProvider(null, connections =>
            connections.AddConnection("main", "MsSql", "Server=localhost;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.Throws<KeyNotFoundException>(() => factory.GetService("doesNotExist"));
    }

    // -------------------------------------------------------------------------
    // AddJadeDbNamedConnections – configuration-based registration
    // -------------------------------------------------------------------------

    [Fact]
    public void AddJadeDbNamedConnections_ConfigurationBased_LoadsConnections()
    {
        var config = new Dictionary<string, string?>
        {
            ["JadeDb:Connections:primary:DatabaseType"]   = "MsSql",
            ["JadeDb:Connections:primary:ConnectionString"] = "Server=localhost;Database=Primary;",
            ["JadeDb:Connections:reports:DatabaseType"]   = "PostgreSQL",
            ["JadeDb:Connections:reports:ConnectionString"] = "Host=localhost;Database=Reports;",
        };

        var services = BuildServiceProvider(config);

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.IsType<MsSqlDbService>(factory.GetService("primary"));
        Assert.IsType<PostgreSqlDbService>(factory.GetService("reports"));
    }

    [Fact]
    public void AddJadeDbNamedConnections_ProgrammaticOverridesConfiguration()
    {
        // Config says "main" is MySql; programmatic override says MsSql.
        var config = new Dictionary<string, string?>
        {
            ["JadeDb:Connections:main:DatabaseType"]    = "MySql",
            ["JadeDb:Connections:main:ConnectionString"] = "Server=localhost;Database=Config;",
        };

        var services = BuildServiceProvider(config, connections =>
            connections.AddConnection("main", "MsSql", "Server=localhost;Database=Override;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        // Programmatic should win.
        Assert.IsType<MsSqlDbService>(factory.GetService("main"));
    }

    // -------------------------------------------------------------------------
    // Dialect property on each service type
    // -------------------------------------------------------------------------

    [Fact]
    public void AddJadeDbNamedConnections_MsSqlService_HasCorrectDialect()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("x", "MsSql", "Server=localhost;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();
        Assert.Equal(Enums.DatabaseDialect.MsSql, factory.GetService("x").Dialect);
    }

    [Fact]
    public void AddJadeDbNamedConnections_MySqlService_HasCorrectDialect()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("x", "MySql", "Server=localhost;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();
        Assert.Equal(Enums.DatabaseDialect.MySql, factory.GetService("x").Dialect);
    }

    [Fact]
    public void AddJadeDbNamedConnections_PostgreSqlService_HasCorrectDialect()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("x", "PostgreSQL", "Host=localhost;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();
        Assert.Equal(Enums.DatabaseDialect.PostgreSql, factory.GetService("x").Dialect);
    }

    // -------------------------------------------------------------------------
    // Unsupported database type
    // -------------------------------------------------------------------------

    [Fact]
    public void AddJadeDbNamedConnections_UnsupportedDatabaseType_ThrowsOnFactoryResolution()
    {
        var config = new Dictionary<string, string?>
        {
            ["JadeDb:Connections:bad:DatabaseType"]    = "Oracle",
            ["JadeDb:Connections:bad:ConnectionString"] = "Data Source=localhost;",
        };

        var services = BuildServiceProvider(config);

        // The factory builds all services eagerly; an unsupported type must throw.
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IJadeDbServiceFactory>());
    }

    // -------------------------------------------------------------------------
    // IJadeDbServiceFactory interface shape
    // -------------------------------------------------------------------------

    [Fact]
    public void IJadeDbServiceFactory_HasGetServiceWithNameMethod()
    {
        var method = typeof(IJadeDbServiceFactory).GetMethod("GetService", new[] { typeof(string) });

        Assert.NotNull(method);
        var param = method!.GetParameters();
        Assert.Single(param);
        Assert.Equal(typeof(string), param[0].ParameterType);
        Assert.Equal(typeof(IDatabaseService), method.ReturnType);
    }

    [Fact]
    public void IJadeDbServiceFactory_HasGetServiceNoArgMethod()
    {
        var method = typeof(IJadeDbServiceFactory).GetMethod("GetService", Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.Empty(method!.GetParameters());
        Assert.Equal(typeof(IDatabaseService), method.ReturnType);
    }

    // -------------------------------------------------------------------------
    // Default connection – programmatic
    // -------------------------------------------------------------------------

    [Fact]
    public void SetDefaultConnection_FactoryGetServiceNoArg_ReturnsDefault()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("main",    "MsSql",     "Server=localhost;")
             .AddConnection("reports", "PostgreSQL", "Host=localhost;")
             .SetDefaultConnection("main"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        var defaultDb = factory.GetService();
        Assert.IsType<MsSqlDbService>(defaultDb);
    }

    [Fact]
    public void SetDefaultConnection_IDatabaseServiceInjection_ReturnsDefault()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("main",    "MsSql",     "Server=localhost;")
             .AddConnection("reports", "PostgreSQL", "Host=localhost;")
             .SetDefaultConnection("main"));

        // IDatabaseService should resolve to the default without touching the factory.
        var db = services.GetRequiredService<IDatabaseService>();
        Assert.IsType<MsSqlDbService>(db);
    }

    [Fact]
    public void SetDefaultConnection_NamedGetServiceStillWorks()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("main",    "MsSql",     "Server=localhost;")
             .AddConnection("reports", "PostgreSQL", "Host=localhost;")
             .SetDefaultConnection("main"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.IsType<MsSqlDbService>(factory.GetService("main"));
        Assert.IsType<PostgreSqlDbService>(factory.GetService("reports"));
    }

    [Fact]
    public void NoDefaultConnection_FactoryGetServiceNoArg_ThrowsInvalidOperation()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("main", "MsSql", "Server=localhost;"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.Throws<InvalidOperationException>(() => factory.GetService());
    }

    [Fact]
    public void SetDefaultConnection_SameDbType_MultipleConnections_ResolvesCorrectDefault()
    {
        // Two MsSql connections; default is "secondary".
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("primary",   "MsSql", "Server=primary;")
             .AddConnection("secondary", "MsSql", "Server=secondary;")
             .SetDefaultConnection("secondary"));

        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        // Both are MsSql; the default is "secondary".
        Assert.IsType<MsSqlDbService>(factory.GetService("primary"));
        Assert.IsType<MsSqlDbService>(factory.GetService("secondary"));
        Assert.Same(factory.GetService("secondary"), factory.GetService());
    }

    [Fact]
    public void SetDefaultConnection_UnknownName_ThrowsOnFactoryResolution()
    {
        var services = BuildServiceProvider(null, c =>
            c.AddConnection("main", "MsSql", "Server=localhost;")
             .SetDefaultConnection("doesNotExist"));

        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IJadeDbServiceFactory>());
    }

    // -------------------------------------------------------------------------
    // Default connection – configuration-based
    // -------------------------------------------------------------------------

    [Fact]
    public void DefaultConnection_ConfigurationBased_ReturnsDesignatedDefault()
    {
        var config = new Dictionary<string, string?>
        {
            ["JadeDb:DefaultConnection"] = "reports",
            ["JadeDb:Connections:main:DatabaseType"]       = "MsSql",
            ["JadeDb:Connections:main:ConnectionString"]   = "Server=localhost;",
            ["JadeDb:Connections:reports:DatabaseType"]    = "PostgreSQL",
            ["JadeDb:Connections:reports:ConnectionString"] = "Host=localhost;",
        };

        var services = BuildServiceProvider(config);
        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.IsType<PostgreSqlDbService>(factory.GetService());
    }

    [Fact]
    public void DefaultConnection_ProgrammaticOverridesConfigDefault()
    {
        // Config default is "reports" (PostgreSQL), programmatic overrides to "main" (MsSql).
        var config = new Dictionary<string, string?>
        {
            ["JadeDb:DefaultConnection"] = "reports",
            ["JadeDb:Connections:main:DatabaseType"]       = "MsSql",
            ["JadeDb:Connections:main:ConnectionString"]   = "Server=localhost;",
            ["JadeDb:Connections:reports:DatabaseType"]    = "PostgreSQL",
            ["JadeDb:Connections:reports:ConnectionString"] = "Host=localhost;",
        };

        var services = BuildServiceProvider(config, c => c.SetDefaultConnection("main"));
        var factory = services.GetRequiredService<IJadeDbServiceFactory>();

        Assert.IsType<MsSqlDbService>(factory.GetService());
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static IServiceProvider BuildServiceProvider(
        Dictionary<string, string?>? configEntries = null,
        Action<JadeDbNamedConnectionsBuilder>? configure = null)
    {
        var sc = new ServiceCollection();

        var configBuilder = new ConfigurationBuilder();
        if (configEntries != null)
            configBuilder.AddInMemoryCollection(configEntries);

        sc.AddSingleton<IConfiguration>(configBuilder.Build());
        sc.AddJadeDbNamedConnections(configure);

        return sc.BuildServiceProvider();
    }
}
