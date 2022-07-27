// See https://aka.ms/new-console-template for more information

using GrocyShopping.Citygross;
using GrocyShopping.Infrastructure.Email;

string[] GetEmailInfo()
{
    Console.Write("Please provide your email host, port, username and password, in that order: ");
    var strings = Console.ReadLine()?.Split(" ", StringSplitOptions.RemoveEmptyEntries);
    return strings;
}

Console.WriteLine("Welcome to Grocy Citygross client!");
string[]? emailInfo = null;
while (emailInfo == null)
{
    emailInfo = GetEmailInfo();
}

var emailClient = new MailkitImapEmailClient(emailInfo[0], Convert.ToInt32(emailInfo[1]), false, emailInfo[2], emailInfo[3]);
var allCitygrossOrderEmails = await emailClient.GetAllEmailsFrom("noreply@citygross.se");

var boughtProducts = new List<BoughtProduct>();
var orderParser = new CitygrossOnlineOrderParser();
foreach (var orderEmail in allCitygrossOrderEmails)
{
    var products = orderParser.ParseOnlineOrder(orderEmail.Body);
    var newProducts = products.Where(x => !boughtProducts.Any(e => e.Name == x.Name && e.Brand == x.Brand));
    boughtProducts.AddRange(newProducts);
}

if (boughtProducts.Any())
{
    Console.WriteLine($"Found {boughtProducts.Count} products in emails");
}