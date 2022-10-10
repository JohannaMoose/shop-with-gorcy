using Grocy.RestAPI;
using Grocy.RestAPI.Models;
using GrocyShopping.Citygross;
using Serilog;

namespace GrocyShopping.Console;

internal class ProductManager
{
    private readonly ProductsApi _api;
    private readonly QuantityManager _quantityManager;
    private readonly ILogger _logger;

    public ProductManager(ProductsApi api, QuantityManager quantityManager, ILogger logger)
    {
        _api = api;
        _quantityManager = quantityManager;
        _logger = logger;
    }

    public async Task<Product> AddProduct(BoughtProduct boughtProduct1, QuantityUnit quantityUnit)
    {
        var name = boughtProduct1.Name;

        var accepted = ConsoleProgramHelper.AcceptChoice($"Product name in grocy for new product suggested \"{name}\", accept (Y/n)?: ");
        if (!accepted)
        {
            System.Console.Write("Please enter product name to use: ");
            System.Console.WriteLine(name);
            System.Console.SetCursorPosition(name.Length, 0);
            name = System.Console.ReadLine();
        }

        accepted = ConsoleProgramHelper.AcceptChoice($"From bought product, unit {quantityUnit.Name} selected, accept (Y/n)?: ");
        if (!accepted)
        {
            quantityUnit = _quantityManager.UserSelectQuantity();
        }

        var product1 = await _api.AddProduct(name, quantityUnit.Id, quantityUnit.Id);

        _logger.Information("Added product {productName} to Grocy", product1.Name);

        return product1;
    }

}