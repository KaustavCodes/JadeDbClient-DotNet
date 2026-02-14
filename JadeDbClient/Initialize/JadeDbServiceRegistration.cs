using JadeDbClient;
using JadeDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JadeDbClient.Initialize;

public static class JadeDbServiceRegistration
{
    public static void AddJadeDbService(this IServiceCollection services, Action<JadeMapperOptions>? configure = null)
    {
        var options = new JadeMapperOptions();
        configure?.Invoke(options);

        // Register JadeMapperOptions as a singleton
        services.AddSingleton(options);

        // Setup the database
        services.AddSingleton<DatabaseConfigurationService>();

        // Register the database service using a factory
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var databaseConfigService = serviceProvider.GetRequiredService<DatabaseConfigurationService>();
            var mapperOptions = serviceProvider.GetRequiredService<JadeMapperOptions>();
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