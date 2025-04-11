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

    // public async Task<IActionResult> Index()
    // {
    //     // List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

    //     // dbDataParameters.Add(_dbConfig.GetParameter("p_name", "Jaded", DbType.String, ParameterDirection.Input, 250));

    //     // var data = await _dbConfig.ExecuteStoredProcedureWithOutputAsync("public.add_date", dbDataParameters); 

    //     string insrtQry = "INSERT INTO public.tbl_test(\"Name\") VALUES(@p_Name);";

    //     List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
    //     dbDataParameters.Add(_dbConfig.GetParameter("@p_Name", "Someone", DbType.String, ParameterDirection.Input, 250));

    //     await _dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);

    //     // string query = "SELECT * FROM public.tbl_test;";

    //     // IEnumerable<DataModel> results = await _dbConfig.ExecuteQueryAsync<DataModel>(query);


    //     IEnumerable<DataModel> results = await _dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("public.get_data", new List<IDbDataParameter> { _dbConfig.GetParameter("p_limit", 1, DbType.Int32, ParameterDirection.Input, 250) });
        
    //     return View(results);
    // }

    public async Task<IActionResult> Index()
    {
        //Execute a stored proceude with output parameter
        List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

        dbDataParameters.Add(_dbConfig.GetParameter("p_name", "Jaded", DbType.String, ParameterDirection.Input, 250));
        dbDataParameters.Add(_dbConfig.GetParameter("p_OutputParam", "test", DbType.String, ParameterDirection.Output, 250));

        Dictionary<string, object> outputParameters = await _dbConfig.ExecuteStoredProcedureWithOutputAsync("add_date", dbDataParameters); 

        foreach (var item in outputParameters)
        {
            //Print the values of the output parameters. These are parameters that you had set as output
            Console.WriteLine($"{item.Key} : {item.Value}");
        }
        
        //Execute a stored procedure without output parameter

        //List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

        dbDataParameters.Add(_dbConfig.GetParameter("p_name", "Jaded", DbType.String, ParameterDirection.Input, 250));

        int rowsAffected = await _dbConfig.ExecuteStoredProcedureAsync("add_date", dbDataParameters);


        // //Execute a parameterized command
        // string insrtQry = "INSERT INTO tbl_test(\"Name\") VALUES(@p_Name);";

        // List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
        // dbDataParameters.Add(_dbConfig.GetParameter("@p_Name", "Someone", DbType.String, ParameterDirection.Input, 250));

        // await _dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);


        //Execute a query
        string query = "SELECT * FROM public.tbl_test;";

        IEnumerable<DataModel> results = await _dbConfig.ExecuteQueryAsync<DataModel>(query);

        // // Execute a stored procedure and return the result
        //IEnumerable<DataModel> results = await _dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("get_data", new List<IDbDataParameter> { _dbConfig.GetParameter("p_limit", 100, DbType.Int32, ParameterDirection.Input, 250) });
        
        return View(results);
    }


    public async Task<IActionResult> MySqlDemo()
    {
        // List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();

        // dbDataParameters.Add(_dbConfig.GetParameter("p_Name", "Jaded", DbType.String, ParameterDirection.Input, 250));

        //var data = await _dbConfig.ExecuteStoredProcedureWithOutputAsync("add_date", dbDataParameters); 

        string insrtQry = "INSERT INTO tbl_test(Name) VALUES(@Name);";

        List<IDbDataParameter> dbDataParameters = new List<IDbDataParameter>();
        dbDataParameters.Add(_dbConfig.GetParameter("@Name", "Someone", DbType.String, ParameterDirection.Input, 250));

        await _dbConfig.ExecuteCommandAsync(insrtQry, dbDataParameters);

        // string query = "SELECT * FROM tbl_test;";

        // IEnumerable<DataModel> results = await _dbConfig.ExecuteQueryAsync<DataModel>(query);


        IEnumerable<DataModel> results = await _dbConfig.ExecuteStoredProcedureSelectDataAsync<DataModel>("get_data", new List<IDbDataParameter> { _dbConfig.GetParameter("p_limit", 1, DbType.Int32, ParameterDirection.Input, 250) });
        
        return View("Index", results);
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
