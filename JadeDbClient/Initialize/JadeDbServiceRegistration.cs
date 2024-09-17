using JadedDbClient;
using JadedDbClient.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JadeDbClient.Initialize;

public static class JadeDbServiceRegistration
{
    public static void AddJadeDbService(this IServiceCollection services)
    {
        // Setup the database
        services.AddSingleton<DatabaseConfigurationService>();

        // Register the database service using a factory
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var databaseConfigService = serviceProvider.GetRequiredService<DatabaseConfigurationService>();
            var databaseType = databaseConfigService.GetDatabaseType();
            var licenseType = databaseConfigService.GetLicenseType();

            return databaseType switch
            {
                "MsSql" => (IDatabaseService)new MsSqlDbService(configuration),
                "MySql" => (IDatabaseService)new MySqlDbService(configuration),
                "PostgreSQL" => (IDatabaseService)new PostgreSqlDbService(configuration),
                _ => throw new Exception("Unsupported database type"),
            };
        });
    }
}