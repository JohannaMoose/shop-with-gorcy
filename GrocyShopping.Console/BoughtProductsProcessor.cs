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
    private List<string> _approvedUnits = new();

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

            var unit = GetBoughtQuantity(boughtProduct);
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
        var product = _productManager.GetProduct(boughtProduct.Name, boughtProduct.Brand);

        if (product == null)
        {
            product = await _productManager.AddProduct(boughtProduct, unit);
        }
        else
        {
            System.Console.WriteLine("Found product, with name {0} to match", product.Name);
        }

        return product;
    }

    private async Task AddBoughtAmountOfProduct(BoughtProduct boughtProduct, QuantityUnit unit, Product product, int storeId)
    {
        var amountToAdd = AmountToAdd(boughtProduct);

        if (product.QuIdStock != unit.Id)
            amountToAdd = await ConvertAmountToUnit(product, unit, amountToAdd);

        var pricePerUnit = boughtProduct.Price / amountToAdd;

        System.Console.WriteLine($"Price per unit calculated to {pricePerUnit} kr/{unit.Name}.");

        await stockApi.AddAmount(product.Id, amountToAdd, pricePerUnit, shoppingLocationId: storeId);
    }

    private QuantityUnit GetBoughtQuantity(BoughtProduct boughtProduct)
    {
        var quantity = _quantityManager.GetUnitFor(boughtProduct.ProductAmount);

        var accept = _approvedUnits.Contains(quantity.Userfields["abbreviation"]);
        while (!accept)
        {
            accept =
                ConsoleProgramHelper.AcceptChoice(
                    $"Using {quantity.Name} with abbreviation {quantity.Userfields["abbreviation"]} for the bought amount {boughtProduct.ProductAmount}, acccept? (Y/n): ");

            if (!accept)
            {
                quantity = _quantityManager.UserSelectQuantity();
            }
            else
            {
                _approvedUnits.Add(quantity.Userfields["abbreviation"]);
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

        System.Console.WriteLine($"Calculated bought amount to be added to {boughtAmount}, for the {boughtProduct.ProductAmount} per pack, and {boughtProduct.NbrOfProducts} nbr.");

        return boughtAmount;
    }

    private async Task<double> ConvertAmountToUnit(Product product, QuantityUnit unit, double amountToAdd)
    {
        var convertedAmount = amountToAdd;
        var converter = await _quantityManager.GetConversion(unit.Id, product.QuIdStock, product.Id);

        if(converter == null)
            converter = await _quantityManager.GetConversion(unit.Id, product.QuIdStock);


        if (converter != null)
        {
            convertedAmount *= converter.Factor;
        }
        else
        {
            throw new NotImplementedException();
        }

        return convertedAmount;
    }
}