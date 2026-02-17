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
            var licenseType = databaseConfigService.GetLicenseType();

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
}