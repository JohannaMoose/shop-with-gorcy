// See https://aka.ms/new-console-template for more information

using GrocyShopping.Citygross;
using GrocyShopping.Infrastructure.Email;

const string citygrossOrderEmailAddress = "noreply@citygross.se";


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