using JadeDbClient;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JadeDbClient.Initialize;

public static class JadeDbServiceRegistration
{
    public static void AddJadeDbService(this IServiceCollection services, Action<JadeDbMapperOptions>? configure = null)
    {
        var options = new JadeDbMapperOptions();
        configure?.Invoke(options);

        // Register JadeDbMapperOptions as a singleton
        services.AddSingleton(options);

        // Setup the database
        services.AddSingleton<DatabaseConfigurationService>();

        // Register the database service using a factory
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var databaseConfigService = serviceProvider.GetRequiredService<DatabaseConfigurationService>();
            var mapperOptions = serviceProvider.GetRequiredService<JadeDbMapperOptions>();
            var databaseType = databaseConfigService.GetDatabaseType();
            var licenseType = databaseConfigService.GetLicenseType();

            return databaseType switch
            {
                "MsSql" => (IDatabaseService)new MsSqlDbService(configuration, mapperOptions),
                "MySql" => (IDatabaseService)new MySqlDbService(configuration, mapperOptions),
                "PostgreSQL" => (IDatabaseService)new PostgreSqlDbService(configuration, mapperOptions),
                _ => throw new Exception("Unsupported database type"),
            };
        });
    }
}