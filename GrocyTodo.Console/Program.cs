// See https://aka.ms/new-console-template for more information

using Grocy.ManageAPI.Services;
using Grocy.RestAPI;

Console.WriteLine("Hello Grocy ToDo World!");
var grocyInstance = Environment.GetEnvironmentVariable("GROCY_INSTANCE");

while (grocyInstance == null || string.IsNullOrWhiteSpace(grocyInstance))
{
    Console.Write("Please provide the URL to your Grocy instance: ");
    grocyInstance = Console.ReadLine();
}

var apiKey = Environment.GetEnvironmentVariable("GROCY_API_KEY");

Console.WriteLine("All setup to connect to grocy, getting ready to work");
var http = new HttpClient();
http.DefaultRequestHeaders.Add("GROCY-API-KEY", apiKey);

var choresApi = new ChoesApi(http, grocyInstance);

var manager = new CategoryChoreManager(choresApi, "priority", "1");
await manager.ScheduleCategory("Städning", 1);
await manager.ScheduleCategory("Tvätt", 2);

