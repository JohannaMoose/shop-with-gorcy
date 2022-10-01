using System.Text.RegularExpressions;
using System.Web;
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

    public BoughtProductsProcessor(string grocyUrl, HttpClient client, ILogger logger)
    {
        _logger = logger;
        unitsApi = new QuantityUnitsApi(client, grocyUrl);
        productsApi = new ProductsApi(client, grocyUrl);
        quantityConversionApi = new QuantityConversionApi(client, grocyUrl);
        stockApi = new StockApi(client, grocyUrl);
    }

    public async Task Process(IEnumerable<BoughtProduct> boughtProducts, bool addPermanently)
    {
        var allUnits = await unitsApi.Get();
        var unitAbbreviations = allUnits.Select(x => x.UserFields["abbreviation"]);

        foreach (var boughtProduct in boughtProducts)
        {
            _logger.Information("Processing product {name}, {brand}", boughtProduct.Name, boughtProduct.Brand);
            var existingProductsMatching =
                await productsApi.Get(new[] { new QueryFilter("name", QueryCondition.Like, boughtProduct.Name) });
            var productsMatching = existingProductsMatching as Product[] ?? existingProductsMatching.ToArray();

            var unit = GetQuantityUnit(unitAbbreviations, boughtProduct, allUnits);
            Product product;
            if (!productsMatching.Any() && (ProductWithoutBandDoesNotExists() || ProductWithBrandDoesNotExist()))
            {
                product = await AddProduct(boughtProduct, unit);
            }
            else
            {
                if (boughtProduct.Brand != null)
                {
                    var brand = HttpUtility.HtmlDecode(boughtProduct.Brand);
                    var foundMatchingProduct = productsMatching.SingleOrDefault(x =>
                        x.UserFields.Contains(new KeyValuePair<string, string>("brand", brand)));

                    if (foundMatchingProduct != null)
                        product = foundMatchingProduct;
                    else
                        product = await AddProduct(boughtProduct, unit, true);

                }
                else
                {
                    product = productsMatching.Single(x => !x.UserFields.ContainsKey("brand"));
                }
            }
            var amountToAdd = AmountToAdd(boughtProduct, unit);

            if (product.Qu_id_stock != unit.Id)
            {
                var foundConversions = await quantityConversionApi.Get(new[]
                {
            new QueryFilter("product_id", QueryCondition.Equals, product.Id.ToString()),
            new QueryFilter("from_qu_id", QueryCondition.Equals, unit.Id.ToString())
        });

                var quantityUnitConversions = foundConversions as QuantityUnitConversion[] ?? foundConversions.ToArray();
                if (quantityUnitConversions.Any())
                {
                    var converter = quantityUnitConversions.First();
                    amountToAdd *= Convert.ToDouble(converter.Factor);
                }
            }

            var details = await stockApi.GetProduct(product.Id);
            await stockApi.AddAmount(product.Id, amountToAdd, boughtProduct.Price / amountToAdd);

            if (addPermanently)
            {
                await stockApi.AdjustProductInventory(product.Id, details.StockAmount);
            }

            bool ProductWithBrandDoesNotExist()
            {
                return !productsMatching.Any(x => x.UserFields.Contains(new KeyValuePair<string, string>("brand", boughtProduct.Brand)));
            }

            bool ProductWithoutBandDoesNotExists()
            {
                return (boughtProduct.Brand == null && !productsMatching.Any(x => x.UserFields.ContainsKey("brand")));
            }
        }
    }

    private async Task<Product> AddProduct(BoughtProduct boughtProduct1, QuantityUnit quantityUnit, bool addBrandToName = false)
    {
        Product product1;

        var name = boughtProduct1.Name;
        if (addBrandToName)
            name = $"{boughtProduct1.Brand} {boughtProduct1.Name}";

        product1 = await productsApi.AddProduct(name, quantityUnit.Id, quantityUnit.Id);
        if (boughtProduct1.Brand != null)
        {
            await productsApi.EditUserfield(product1.Id, "brand", boughtProduct1.Brand);
        }

        return product1;
    }

    private QuantityUnit GetQuantityUnit(IEnumerable<string> iEnumerable, BoughtProduct boughtProduct, IEnumerable<QuantityUnit> quantityUnits)
    {
        var amountStr = boughtProduct.ProductAmount; 

        if (boughtProduct.ProductAmount.EndsWith(")"))
        {
            var parts = Regex.Match(amountStr, @"(\d*.*) \(.*\)");
            amountStr = parts.Groups[1].Value;
        }

        var matchingAbrev = iEnumerable.Where(x => amountStr.EndsWith(x)).OrderByDescending(x => x.Length);
        var abrv = matchingAbrev.First();
        var unit1 = quantityUnits.Single(x => x.UserFields["abbreviation"] == abrv);

        _logger.Debug("Using {unit} with abbreviation {abbreviation} for amount found in {amount}", unit1.Name, abrv, boughtProduct.ProductAmount);

        return unit1;
    }

    private double AmountToAdd(BoughtProduct boughtProduct, QuantityUnit quantityUnit1)
    {
      
        double packageAmount = 0;


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
        return boughtAmount;
    }
}