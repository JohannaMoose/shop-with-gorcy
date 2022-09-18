// See https://aka.ms/new-console-template for more information

using GrocyShopping.Citygross;
using GrocyShopping.Infrastructure.Email;
using System.Net.Http;
using Grocy.RestAPI;
using Grocy.RestAPI.Models;

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

Console.Write("What is the adress to your Grocy instance: ");
var url = Console.ReadLine();
Console.Write("What API-key should be used?: ");
var apiKey = Console.ReadLine();

var http = new HttpClient();
http.DefaultRequestHeaders.Add("GROCY-API-KEY", apiKey);

var unitsApi = new QuantityUnitsApi(http, url);
var allUnits = await unitsApi.Get();
var unitAbbreviations = allUnits.Select(x => x.UserFields["abbreviation"]);

var productApi = new ProductsApi(http, url);
foreach (var boughtProduct in boughtProducts)
{
    var existingProductsMatching =
        await productApi.Get(new[] { new QueryFilter("name", QueryCondition.Equals, boughtProduct.Name) });
    var productsMatching = existingProductsMatching as Product[] ?? existingProductsMatching.ToArray();

    var abrv = unitAbbreviations.Single(x => boughtProduct.ProductAmount.EndsWith(x));
    var unit = allUnits.Single(x => x.UserFields["abbreviation"] == abrv);
    Product product;
    if (!productsMatching.Any() && (ProductWithoutBandDoesNotExists() || ProductWithBrandDoesNotExist()))
    {
        var userFiles = new Dictionary<string, string>();
        if (boughtProduct.Brand != null)
        {
            userFiles.Add("brand", boughtProduct.Brand);
        }

        var productToAdd = new Product("", boughtProduct.Name, unit.Id, unit.Id, userFiles); // Create product entry 
        product = await productApi.AddProduct(productToAdd);
    }
    else
    {
        if (boughtProduct.Brand != null)
        {
            product = productsMatching.Single(x =>
                x.UserFields.Contains(new KeyValuePair<string, string>("brand", boughtProduct.Brand)));
        }
        else
        {
            product = productsMatching.Single(x => !x.UserFields.ContainsKey("brand"));
        }
    }

    var amount = boughtProduct.ProductAmount.Substring(0, boughtProduct.ProductAmount.Length - abrv.Length).Trim();
    var amountToAdd = Convert.ToDouble(amount);
    // await productApi.AddToStock(product.Id, boughtProduct.Price, xx, boughtProduct.ProductAmount);

    bool ProductWithBrandDoesNotExist()
    {
        return !productsMatching.Any(x => x.UserFields.Contains(new KeyValuePair<string, string>("brand", boughtProduct.Brand)));
    }

    bool ProductWithoutBandDoesNotExists()
    {
        return (boughtProduct.Brand == null && !productsMatching.Any(x => x.UserFields.ContainsKey("brand")));
    }
}


/*
 * Example product: {BoughtProduct { Name = Äppelmos, Brand = Önos, Price = 16,95, NbrOfProducts = 1, ProductAmount = 350g }}
 * 1. Search for each product if it exists in Grocy,
 *      if not add and save product id,
 *      if exists, store product id
 * 2. Create bought update
 *      Convert amount into correct for product
 * 3. Add bought to Grocy
 * 4. Ask if add permanently
 *      If yes, do nothing
 *      If no, remove same amount from Grocy as added in step 3
 */

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