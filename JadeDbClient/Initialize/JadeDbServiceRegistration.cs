using JadeDbClient;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JadeDbClient.Initialize;

public static class JadeDbServiceRegistration
{
    public class JadeDbServiceOptions
    {
        public bool EnableLogging { get; set; } = false;

        public bool LogExecutedQuery { get; set; } = false;
    }

    public static void AddJadeDbService(this IServiceCollection services, Action<JadeDbMapperOptions>? configure = null, Action<JadeDbServiceOptions>? serviceOptionsConfigure = null)
    {
        var options = new JadeDbMapperOptions();
        configure?.Invoke(options);

        var serviceOptions = new JadeDbServiceOptions();
        serviceOptionsConfigure?.Invoke(serviceOptions);

        // Register JadeDbMapperOptions as a singleton
        services.AddSingleton(options);
        // Register JadeDbServiceOptions as a singleton
        services.AddSingleton(serviceOptions);

        // Setup the database
        services.AddSingleton<DatabaseConfigurationService>();

        // Register the database service using a factory
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var databaseConfigService = serviceProvider.GetRequiredService<DatabaseConfigurationService>();
            var mapperOptions = serviceProvider.GetRequiredService<JadeDbMapperOptions>();
            var dbServiceOptions = serviceProvider.GetRequiredService<JadeDbServiceOptions>();
            var databaseType = databaseConfigService.GetDatabaseType();

            // Example: log the mode if logging is enabled
            if (dbServiceOptions.EnableLogging)
            {
                //Console.WriteLine($"[JadeDbClient] DatabaseType: {databaseType}, LicenseType: {licenseType}");
                Console.WriteLine($"[JadeDbClient] DatabaseType: {databaseType}");
            }

            return databaseType switch
            {
                "MsSql" => (IDatabaseService)new MsSqlDbService(configuration, mapperOptions, dbServiceOptions),
                "MySql" => (IDatabaseService)new MySqlDbService(configuration, mapperOptions, dbServiceOptions),
                "PostgreSQL" => (IDatabaseService)new PostgreSqlDbService(configuration, mapperOptions, dbServiceOptions),
                _ => throw new Exception("Unsupported database type"),
            };
        });
    }

    /// <summary>
    /// Registers multiple named database connections and an <see cref="IJadeDbServiceFactory"/> that can resolve them by name.
    /// <para>
    /// Connections can be configured programmatically via <paramref name="configure"/>, or loaded automatically from
    /// the <c>JadeDb:Connections</c> configuration section (see example below), or both.
    /// Programmatic entries take precedence over configuration entries with the same name.
    /// </para>
    /// <example>
    /// appsettings.json:
    /// <code>
    /// {
    ///   "JadeDb": {
    ///     "Connections": {
    ///       "default": { "DatabaseType": "MsSql",     "ConnectionString": "Server=localhost;..." },
    ///       "reports": { "DatabaseType": "PostgreSQL", "ConnectionString": "Host=localhost;..."  }
    ///     }
    ///   }
    /// }
    /// </code>
    /// Program.cs:
    /// <code>
    /// services.AddJadeDbNamedConnections(connections =>
    ///     connections.AddConnection("analytics", "MySql", "Server=localhost;..."));
    /// </code>
    /// Usage:
    /// <code>
    /// public class MyService(IJadeDbServiceFactory dbFactory)
    /// {
    ///     public async Task DoWork()
    ///     {
    ///         var db      = dbFactory.GetService("default");
    ///         var reports = dbFactory.GetService("reports");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public static void AddJadeDbNamedConnections(
        this IServiceCollection services,
        Action<JadeDbNamedConnectionsBuilder>? configure = null,
        Action<JadeDbMapperOptions>? mapperConfigure = null,
        Action<JadeDbServiceOptions>? serviceOptionsConfigure = null)
    {
        var mapperOptions = new JadeDbMapperOptions();
        mapperConfigure?.Invoke(mapperOptions);

        var serviceOptions = new JadeDbServiceOptions();
        serviceOptionsConfigure?.Invoke(serviceOptions);

        services.AddSingleton<IJadeDbServiceFactory>(serviceProvider =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();

            var builder = new JadeDbNamedConnectionsBuilder();

            // Load connections from the JadeDb:Connections configuration section first.
            if (configuration != null)
            {
                var connectionsSection = configuration.GetSection("JadeDb:Connections");
                foreach (var child in connectionsSection.GetChildren())
                {
                    var dbType = child["DatabaseType"] ?? string.Empty;
                    var connStr = child["ConnectionString"] ?? string.Empty;
                    if (!string.IsNullOrEmpty(dbType) && !string.IsNullOrEmpty(connStr))
                        builder.AddConnection(child.Key, dbType, connStr);
                }
            }

            // Apply programmatic configuration (overrides config-based entries with the same name).
            configure?.Invoke(builder);

            var namedServices = new Dictionary<string, IDatabaseService>();
            foreach (var (name, (dbType, connStr)) in builder.Connections)
            {
                namedServices[name] = CreateDbService(dbType, connStr, mapperOptions, serviceOptions);
            }

            return new JadeDbServiceFactory(namedServices);
        });
    }

    internal static IDatabaseService CreateDbService(
        string databaseType,
        string connectionString,
        JadeDbMapperOptions mapperOptions,
        JadeDbServiceOptions serviceOptions)
    {
        return databaseType switch
        {
            "MsSql"      => new MsSqlDbService(connectionString, mapperOptions, serviceOptions),
            "MySql"      => new MySqlDbService(connectionString, mapperOptions, serviceOptions),
            "PostgreSQL" => new PostgreSqlDbService(connectionString, mapperOptions, serviceOptions),
            _ => throw new InvalidOperationException($"Unsupported database type '{databaseType}'. Supported types: MsSql, MySql, PostgreSQL."),
        };
    }
}

/// <summary>
/// Fluent builder for registering multiple named database connections.
/// </summary>
public class JadeDbNamedConnectionsBuilder
{
    private readonly Dictionary<string, (string DatabaseType, string ConnectionString)> _connections
        = new(StringComparer.OrdinalIgnoreCase);

    internal IReadOnlyDictionary<string, (string DatabaseType, string ConnectionString)> Connections => _connections;

    /// <summary>
    /// Registers a named database connection.
    /// </summary>
    /// <param name="name">A unique name for this connection (e.g. <c>"default"</c>, <c>"reports"</c>).</param>
    /// <param name="databaseType">The database type: <c>"MsSql"</c>, <c>"MySql"</c>, or <c>"PostgreSQL"</c>.</param>
    /// <param name="connectionString">The connection string for the database.</param>
    public JadeDbNamedConnectionsBuilder AddConnection(string name, string databaseType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Connection name must not be null or whitespace.", nameof(name));
        if (string.IsNullOrWhiteSpace(databaseType)) throw new ArgumentException("Database type must not be null or whitespace.", nameof(databaseType));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string must not be null or whitespace.", nameof(connectionString));

        _connections[name] = (databaseType, connectionString);
        return this;
    }
}