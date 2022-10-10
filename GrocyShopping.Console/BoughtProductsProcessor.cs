using System.Text.RegularExpressions;
using Grocy.RestAPI;
using Grocy.RestAPI.Models;
using GrocyShopping.Citygross;
using Serilog;

namespace GrocyShopping.Console;

public class BoughtProductsProcessor
{
    private readonly ILogger _logger;
    private ProductsApi productsApi;
    private StockApi stockApi;
    private QuantityManager _quantityManager;
    private ProductManager _productManager;

    public BoughtProductsProcessor(string grocyUrl, HttpClient client, ILogger logger)
    {
        _logger = logger;
        productsApi = new ProductsApi(client, grocyUrl);
        stockApi = new StockApi(client, grocyUrl);
        _quantityManager = new QuantityManager(new QuantityUnitsApi(client, grocyUrl), new QuantityConversionApi(client, grocyUrl),  _logger);
        _productManager = new ProductManager(productsApi, _quantityManager, _logger);
    }

    public async Task Process(IEnumerable<BoughtProduct> boughtProducts, bool addPermanently, int storeId)
    {
        foreach (var boughtProduct in boughtProducts)
        {
            _logger.Information("Processes product {productName} from {brand}, bought {amount}, {productAmount} for {price}", boughtProduct.Name, boughtProduct.Brand, boughtProduct.ProductAmount, boughtProduct.ProductAmount, boughtProduct.Price);

            var unit = GetQuantityUnit(boughtProduct);
            var product = await ProcessProduct(boughtProduct, unit);
            var details = await stockApi.GetProduct(product.Id);
            await AddBoughtAmountOfProduct(boughtProduct, unit, product, storeId);

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
            product = await _productManager.AddProduct(boughtProduct, unit);
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
                product = await _productManager.AddProduct(boughtProduct, unit);
            }
        }

        System.Console.Write("Accept this choice? (Y/n)");
        var userInput = System.Console.ReadLine()?.ToLower().Trim();
        var accept = userInput is "y" || string.IsNullOrWhiteSpace(userInput);

        if (accept)
            return product;
        else
        {
            throw new NotImplementedException();
        }
    }

    private async Task AddBoughtAmountOfProduct(BoughtProduct boughtProduct, QuantityUnit unit, Product product, int storeId)
    {
        var amountToAdd = AmountToAdd(boughtProduct);

        if (product.Qu_id_stock != unit.Id)
            amountToAdd = await ConvertAmountToUnit(product, unit, amountToAdd);

        var pricePerUnit = boughtProduct.Price / amountToAdd;

        var acceptPrice = ConsoleProgramHelper.AcceptChoice($"Price per unit calculated to {pricePerUnit} kr/{unit.Name}, accept (Y/n)?: ");

        if (!acceptPrice)
            throw new NotImplementedException();

        await stockApi.AddAmount(product.Id, amountToAdd, pricePerUnit, shoppingLocationId: storeId);
    }

    private QuantityUnit GetQuantityUnit(BoughtProduct boughtProduct)
    {
        var quantity = _quantityManager.GetUnitFor(boughtProduct.ProductAmount);

        var accept = false;
        while (!accept)
        {
            accept =
                ConsoleProgramHelper.AcceptChoice(
                    $"Using {quantity.Name} with abbreviation {quantity.UserFields["abbreviation"]} for the bought amount {boughtProduct.ProductAmount}, acccept? (Y/n): ");

            if (!accept)
            {
                quantity = _quantityManager.UserSelectQuantity();
            }
        }

        return quantity;
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

        var accept = ConsoleProgramHelper.AcceptChoice($"Calculated bought amount to be added to {boughtAmount}, for the {boughtProduct.ProductAmount} per pack, and {boughtProduct.NbrOfProducts} nbr. Is that correct (Y/n)?: ");
        if (!accept)
        {
            throw new NotImplementedException();
        }

        return boughtAmount;
    }

    private async Task<double> ConvertAmountToUnit(Product product, QuantityUnit unit, double amountToAdd)
    {
        var convertedAmount = amountToAdd;
        var converter = await _quantityManager.GetConversion(unit.Id, product.Qu_id_stock, product.Id);

        if(converter == null)
            converter = await _quantityManager.GetConversion(unit.Id, product.Qu_id_stock);


        if (converter != null)
        {
            convertedAmount *= converter.Factor;
        }
        else
        {
            throw new NotImplementedException();
        }

        var accept =
            ConsoleProgramHelper.AcceptChoice(
                $"Product unit and bought unit doesn't match. Convert from {amountToAdd}, {unit.Name} bought to {convertedAmount}, to add. Do you accept (Y/n)?: ");
        if (!accept)
            throw new NotImplementedException();

        return convertedAmount;
    }
}