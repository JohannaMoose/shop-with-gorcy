// See https://aka.ms/new-console-template for more information

using GrocyShopping.Citygross;
using GrocyShopping.Infrastructure.Email;
using GrocyShopping.Console;
using Serilog;
using Serilog.Core;

const string citygrossOrderEmailAddress = "noreply@citygross.se";

string[] GetEmailInfo()
{
    Console.Write("Please provide your email host, port, username and password, in that order: ");
    var strings = Console.ReadLine()?.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    return strings;
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();

var log = Log.Logger;

Console.WriteLine("Welcome to Grocy Citygross client!");
string[]? emailInfo = null;
while (emailInfo == null)
{
    emailInfo = GetEmailInfo();
}

var emailClient = new MailkitImapEmailClient(emailInfo[0], Convert.ToInt32(emailInfo[1]), false, emailInfo[2], emailInfo[3]);
var allCitygrossOrderEmails = await emailClient.GetAllEmailsFrom(citygrossOrderEmailAddress);

var orderParser = new CitygrossOnlineOrderParser();
var boughtProducts = new List<BoughtProduct>();

ProcessEmails(allCitygrossOrderEmails);

Console.Write("Do you want to search through the email archive as well? (y/N): ");
var searchArchive = Console.ReadLine();

if (searchArchive != null && searchArchive.ToLower() == "y")
{
    var archivedCitygrossOrderEmails = await emailClient.GetAllEmailsFromArchiveSentBy(citygrossOrderEmailAddress);
    ProcessEmails(archivedCitygrossOrderEmails);
}

Console.Write("What is the adress to your Grocy instance: ");
var url = Console.ReadLine();
Console.Write("What API-key should be used?: ");
var apiKey = Console.ReadLine();

var http = new HttpClient();
http.DefaultRequestHeaders.Add("GROCY-API-KEY", apiKey);

Console.Write("Do you want to add the products amounts permanently to Grocy (Y/n)?: ");
var addPermanently = Console.ReadLine()?.ToLower().Trim() != "n";

var processor = new BoughtProductsProcessor(url, http, log);

await processor.Process(boughtProducts, addPermanently);

void ProcessEmails(IReadOnlyList<Email> emails)
{
    foreach (var orderEmail in emails.Where(x => x.Title == "Orderbekräftelse"))
    {
        var products = orderParser.ParseOnlineOrder(orderEmail.Body);
        var newProducts = products.Where(x => !boughtProducts.Any(e => e.Name == x.Name && e.Brand == x.Brand));
        boughtProducts.AddRange(newProducts);
    }

    if (boughtProducts.Any())
    {
        Console.WriteLine($"Found {boughtProducts.Count} products in emails");
    }
}

