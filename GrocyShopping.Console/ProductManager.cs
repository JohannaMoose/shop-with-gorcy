using System.Text.RegularExpressions;
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
    private IList<Product>? _products;
    private static string BrandName = "BrandNames";

    public ProductManager(ProductsApi api, QuantityManager quantityManager, ILogger logger)
    {
        _api = api;
        _quantityManager = quantityManager;
        _logger = logger;
    }

    private IList<Product> Products => _products ??= _api.Get().Result.ToList();

    public async Task<Product> AddProduct(BoughtProduct boughtProduct, QuantityUnit quantityUnit)
    {
        var name = boughtProduct.Name;

        var accepted = ConsoleProgramHelper.AcceptChoice($"Product name in grocy for new product suggested \"{name}\", accept (Y/n)?: ");
        if (!accepted)
        {
            System.Console.Write("Please enter product name to use: ");
            name = System.Console.ReadLine();
        }

        var existingProduct = GetProduct(name, boughtProduct.Brand);

        if (existingProduct != null)
        {
            accepted = ConsoleProgramHelper.AcceptChoice($"Found product with the name {existingProduct.Name} already exists, use that for this product? (Y/n): ");
            if (accepted)
            {
                await AddBrandNameToProduct(existingProduct.Id, boughtProduct.Name, boughtProduct.Brand);
                return existingProduct;
            }
        }

        accepted = ConsoleProgramHelper.AcceptChoice($"From bought product, unit {quantityUnit.Name} selected, accept (Y/n)?: ");
        if (!accepted)
        {
            quantityUnit = _quantityManager.UserSelectQuantity();
        }

        var product = await _api.AddProduct(name, quantityUnit.Id, quantityUnit.Id);
        Products.Add(product);

        _logger.Information("Added product {productName} to Grocy", product.Name);

        var findsProduct = GetProducts(boughtProduct.Name, boughtProduct.Brand);
        if (findsProduct.All(x => x.Id != product.Id))
        {
            await AddBrandNameToProduct(product.Id, boughtProduct.Name, boughtProduct.Brand);
        }

        return product;
    }

    public IReadOnlyList<Product> GetProducts(string name, string brand)
    {
        var existingProductsMatching = Products.Where(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || HasBrandName(name, brand, x));

        return existingProductsMatching.ToList();
    }

    private static bool HasBrandName(string name, string brand, Product x)
    {
        return x.Userfields.ContainsKey(BrandName) && 
               x.Userfields[BrandName] != null && Regex.IsMatch(x.Userfields[BrandName]!, 
                   $@"^{name}, {brand}", RegexOptions.Multiline);
    }

    public Product? GetProduct(string name, string brand)
    {
        var foundProduct = Products.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (foundProduct == null)
            foundProduct = Products.SingleOrDefault(x => HasBrandName(name, brand, x));

        return foundProduct;
    }

    private async Task AddBrandNameToProduct(int productId, string brandName, string brand)
    {
        var currentValue = await _api.GetUserfiled(productId, BrandName);
        var newFiledVaue = currentValue;
        if (!string.IsNullOrWhiteSpace(newFiledVaue))
            newFiledVaue += "\n";
        newFiledVaue += $"{brandName}, {brand}";


        await _api.EditUserfield(productId, BrandName, newFiledVaue);
        var product = Products.Single(x => x.Id == productId);
        product.Userfields[BrandName] = newFiledVaue;
    }
}