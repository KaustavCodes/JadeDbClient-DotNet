using JadeDbClient.Initialize;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// //Setup the databse
// builder.Services.AddSingleton<DatabaseConfigurationService>();

// // Use the configuration service to determine which database service to register
// var dbServiceProvider = builder.Services.BuildServiceProvider();
// var databaseConfigService = dbServiceProvider.GetService<DatabaseConfigurationService>();
// var databaseType = databaseConfigService.GetDatabaseType();

// switch (databaseType)   
// {
//     case "MsSql":
//         builder.Services.AddSingleton<IDatabaseService, MsSqlDbService>();
//         break;
//     case "MySql":
//         builder.Services.AddSingleton<IDatabaseService, MySqlDbService>();
//         break;
//     case "PostgreSQL":
//         builder.Services.AddSingleton<IDatabaseService, PostgreSqlDbService>();
//         break;
//     // Add cases for other database types
//     default:
//         throw new Exception("Unsupported database type");
// }

// Call the method to add the database service
builder.Services.AddJadeDbService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
