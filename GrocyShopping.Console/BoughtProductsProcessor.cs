using System.Text.RegularExpressions;
using Grocy.RestAPI;
using Grocy.RestAPI.Models;
using GrocyShopping.Citygross;
using Serilog;

namespace GrocyShopping.Console;

public class BoughtProductsProcessor
{
    private readonly ILogger _logger;
    private QuantityUnitsApi unitsApi;
    private ProductsApi productsApi;
    private QuantityConversionApi quantityConversionApi;
    private StockApi stockApi;
    private QuantityManager _quantityManager;

    public BoughtProductsProcessor(string grocyUrl, HttpClient client, ILogger logger)
    {
        _logger = logger;
        unitsApi = new QuantityUnitsApi(client, grocyUrl);
        productsApi = new ProductsApi(client, grocyUrl);
        quantityConversionApi = new QuantityConversionApi(client, grocyUrl);
        stockApi = new StockApi(client, grocyUrl);
        _quantityManager = new QuantityManager(unitsApi, _logger);
    }

    public async Task Process(IEnumerable<BoughtProduct> boughtProducts, bool addPermanently, string storeId)
    {
        var allUnits = await unitsApi.Get();
        var unitAbbreviations = allUnits.Select(x => x.UserFields["abbreviation"]);

        foreach (var boughtProduct in boughtProducts)
        {
            _logger.Information("Processes product {productName} from {brand}, bought {amount}, {productAmount} for {price}", boughtProduct.Name, boughtProduct.Brand, boughtProduct.ProductAmount, boughtProduct.ProductAmount, boughtProduct.Price);

            var unit = GetQuantityUnit(unitAbbreviations, boughtProduct, allUnits);
            var product = await ProcessProduct(boughtProduct, unit);
            var details = await stockApi.GetProduct(product.Id);
            await AddBoughtAmountOfProduct(boughtProduct, unit, product);

            if (!addPermanently)
            {
                await stockApi.AdjustProductInventory(product.Id, details.StockAmount);
            }
        }
    }

    private async Task<Product> ProcessProduct(BoughtProduct boughtProduct, QuantityUnit unit)
    {
        var existingProductsMatching =
            await productsApi.Get(new[] { new QueryFilter("name", QueryCondition.Like, boughtProduct.Name) });
        var productsMatching = existingProductsMatching as Product[] ?? existingProductsMatching.ToArray();

        Product product;
        if (!productsMatching.Any())
        {
            _logger.Debug("No matching products found, adding new");
            product = await AddProduct(boughtProduct, unit);
        }
        else
        {
            _logger.Debug("Found the following products {foundProducts}", productsMatching.Select(x => x.Name));

            var foundMatchingProduct = productsMatching.SingleOrDefault(x =>
                x.Name == $"{boughtProduct.Brand} {boughtProduct.Name}");

            if (foundMatchingProduct != null)
            {
                product = foundMatchingProduct;
                _logger.Debug("Found precise product match");
            }
            else if (productsMatching.Any())
            {
                var ordered = productsMatching.OrderBy(x => x.Name.Length);
                product = ordered.First();
                _logger.Debug("Found generic match with {name}", product.Name);
            }
            else
            {
                _logger.Debug("No product found matching, adding new one");
                product = await AddProduct(boughtProduct, unit);
            }
        }

        System.Console.Write("Accept this choice? (Y/n)");
        var userInput = System.Console.ReadLine()?.ToLower().Trim();
        var accept = userInput != null && userInput == "y" || string.IsNullOrWhiteSpace(userInput);

        if (accept)
            return product;
        else
        {
            throw new NotImplementedException();
        }
    }

    private async Task AddBoughtAmountOfProduct(BoughtProduct boughtProduct, QuantityUnit unit, Product product)
    {
        var amountToAdd = AmountToAdd(boughtProduct);

        if (product.Qu_id_stock != unit.Id)
            amountToAdd = await ConvertAmountToUnit(product, unit, amountToAdd);

        var pricePerUnit = boughtProduct.Price / amountToAdd;

        var acceptPrice = AcceptChoice($"Price per unit calculated to {pricePerUnit} kr/{unit.Name}, accept (Y/n)?: ");

        if (!acceptPrice)
            throw new NotImplementedException();

        await stockApi.AddAmount(product.Id, amountToAdd, pricePerUnit);
    }
    private async Task<Product> AddProduct(BoughtProduct boughtProduct1, QuantityUnit quantityUnit)
    {
        var name = boughtProduct1.Name;

        var accepted = AcceptChoice($"Product name in grocy for new product suggested {name}, ");
        if (!accepted)
        {
            System.Console.Write("Please enter product name to use: ");
            System.Console.WriteLine(name);
            System.Console.SetCursorPosition(name.Length, 0);
            name = System.Console.ReadLine();
        }

        var product1 = await productsApi.AddProduct(name, quantityUnit.Id, quantityUnit.Id);

        _logger.Information("Added product {productName} to Grocy", product1.Name);

        return product1;
    }

    private QuantityUnit GetQuantityUnit(IEnumerable<string> allAbbreviations, BoughtProduct boughtProduct, IEnumerable<QuantityUnit> quantityUnits)
    {
        var amountStr = boughtProduct.ProductAmount; 

        if (boughtProduct.ProductAmount.EndsWith(")"))
        {
            var parts = Regex.Match(amountStr, @"(\d*.*) \(.*\)");
            amountStr = parts.Groups[1].Value;
        }

        var matchingAbrev = allAbbreviations.Where(x => amountStr.EndsWith(x)).OrderByDescending(x => x.Length);
        var abrv = matchingAbrev.First();
        var unit1 = GetQuantityWithAbbreviation(quantityUnits, abrv);
        var accept = false;
        while (!accept)
        {
            accept =
                AcceptChoice(
                    $"Using {unit1.Name} with abbreviation {abrv} for the bought amount {boughtProduct.ProductAmount}, acccept? (Y/n): ");

            if (!accept)
            {
                System.Console.WriteLine("Please write what abbreviation to use: ");
                var userAbbrev = System.Console.ReadLine();
                unit1 = GetQuantityWithAbbreviation(quantityUnits, userAbbrev);
            }
        }

        return unit1;
    }

    private static QuantityUnit GetQuantityWithAbbreviation(IEnumerable<QuantityUnit> quantityUnits, string abrv)
    {
        var unit1 = quantityUnits.Single(x => x.UserFields["abbreviation"] == abrv);
        return unit1;
    }

    private static double AmountToAdd(BoughtProduct boughtProduct)
    {
        double packageAmount;

        if (boughtProduct.ProductAmount.Contains("ca") && !boughtProduct.ProductAmount.EndsWith(")"))
        {
            var parts = Regex.Match(boughtProduct.ProductAmount, @".*ca ?([\d,]*)(.*)");
            var factorPerUnit = parts.Groups[1].Value;
            packageAmount = Convert.ToDouble(factorPerUnit);
        } else if (Regex.IsMatch(boughtProduct.ProductAmount, @"(\d)*px([\d,]*).*"))
        {
            var parts = Regex.Match(boughtProduct.ProductAmount, @"(\d)*px([\d,]*).*");
            var packages = Convert.ToDouble(parts.Groups[1].Value);
            var inPackage = Convert.ToDouble(parts.Groups[2].Value);
            packageAmount = packages * inPackage;
        }
        else
        {
            var parts = Regex.Match(boughtProduct.ProductAmount, @"([\d,]*).*");
            packageAmount = Convert.ToDouble(parts.Groups[1].Value);
        }

        var boughtAmount = boughtProduct.NbrOfProducts * packageAmount;

        var accept = AcceptChoice($"Calculated bought amount to be added to {boughtAmount}, for the {boughtProduct.ProductAmount} per pack, and {boughtProduct.NbrOfProducts} nbr. Is that correct (Y/n)?: ");
        if (!accept)
        {
            throw new NotImplementedException();
        }

        return boughtAmount;
    }

    private static bool AcceptChoice(string info)
    {
        System.Console.Write(info);
        var userAnser = System.Console.ReadLine()?.ToLower().Trim();

        var accept = string.IsNullOrWhiteSpace(userAnser) || userAnser == "y";
        return accept;
    }

    private async Task<double> ConvertAmountToUnit(Product product, QuantityUnit unit, double amountToAdd)
    {
        var convertedAmount = amountToAdd;
        var foundConversions = await quantityConversionApi.Get(new[]
        {
            new QueryFilter("product_id", QueryCondition.Equals, product.Id.ToString()),
            new QueryFilter("from_qu_id", QueryCondition.Equals, unit.Id.ToString())
        });

        var quantityUnitConversions = foundConversions as QuantityUnitConversion[] ?? foundConversions.ToArray();
        var converter = quantityUnitConversions.FirstOrDefault();
        if (converter != null)
        {
            convertedAmount *= Convert.ToDouble(converter.Factor);
        }
        else
        {
            throw new NotImplementedException();
        }

        var accept =
            AcceptChoice(
                $"Product unit and bought unit doesn't match. Convert from {amountToAdd}, {unit.Name} bought to {convertedAmount}, to add");
        if (!accept)
            throw new NotImplementedException();

        return convertedAmount;
    }
}