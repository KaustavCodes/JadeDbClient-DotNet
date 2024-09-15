using System.Data;
using System.Diagnostics;
using JadedDbClient.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebTest.Models;

namespace WebTest.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    IDatabaseService _dbConfig;
    public HomeController(ILogger<HomeController> logger, IDatabaseService dbConfig)
    {
        _dbConfig = dbConfig;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

        // dbDataParameters.Add(_dbConfig.GetParameter("p_Name", "Jaded", DbType.String, ParameterDirection.Input, 250));

        //var data = await _dbConfig.ExecuteStoredProcedureWithOutputAsync("add_date", dbDataParameters); 

        // string insrtQry = "INSERT INTO tbl_test(Name) VALUES(@Name);";

        // List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
        // dbDataParameters.Add(_dbConfig.GetParameter("@Name", "Someone", DbType.String, ParameterDirection.Input, 250));

        // await _dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);

        string query = "SELECT * FROM tbl_test;";

        IEnumerable<DataModel> results = await _dbConfig.ExecuteQueryAsync<DataModel>(query);
        
        return View(results);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
