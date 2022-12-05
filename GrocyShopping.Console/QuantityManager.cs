using Grocy.RestAPI;
using Grocy.RestAPI.Models;
using System.Text.RegularExpressions;
using Serilog;

namespace GrocyShopping.Console;

internal class QuantityManager
{
    private readonly QuantityUnitsApi _api;
    private readonly QuantityConversionApi _conversionApi;
    private readonly ILogger _logger;
    private IReadOnlyList<QuantityUnit>? _quantityUnits;

    public QuantityManager(QuantityUnitsApi api, QuantityConversionApi conversionApi, ILogger logger)
    {
        _api = api;
        _conversionApi = conversionApi;
        _logger = logger;
    }

    private IReadOnlyList<QuantityUnit> QuantityUnits
    {
        get
        {
            if (_quantityUnits == null)
            {
                var loadUnits = _api.Get();
                _quantityUnits = loadUnits.Result.ToList();
            }

            return _quantityUnits;
        }
    }

    private IEnumerable<string> AllAbbreviations => QuantityUnits.Select(x => x.Userfields["abbreviation"]);

    public QuantityUnit GetUnitFor(string unitString)
    {
        var amountStr = unitString;

        if (unitString.EndsWith(")"))
        {
            var parts = Regex.Match(amountStr, @"(\d*.*) \(.*\)");
            amountStr = parts.Groups[1].Value;
        }

        var matchingAbrev = AllAbbreviations.Where(x => amountStr.EndsWith(x)).OrderByDescending(x => x.Length);
        var abrv = matchingAbrev.First();
        var unit1 = QuantityUnits.Single(x => x.Userfields["abbreviation"] == abrv);

        _logger.Debug("Using {unit} with abbreviation {abbreviation} for amount found in {amount}", unit1.Name, abrv, unitString);

        return unit1;
    }

    public QuantityUnit UserSelectQuantity()
    {
        System.Console.Write("Please write what abbreviation to use: ");
        QuantityUnit? result = null;

        while (result == null)
        {
            var userAbbrev = System.Console.ReadLine();
            if (userAbbrev != null)
                result = GetQuantityWithAbbreviation(userAbbrev);
            else
                System.Console.Write("No unit with entered abbreviation found. Please enter a known abbreviation: ");
        }

        return result;
    }

    public QuantityUnit? GetQuantityWithAbbreviation(string abrv)
    {
        var unit1 = QuantityUnits.SingleOrDefault(x => x.Userfields["abbreviation"] == abrv);
        return unit1;
    }

    public async Task<QuantityUnitConversion?> GetConversion(int fromQuantity, int? toQuantity = null, int? productId = null)
    {
        if (toQuantity == null && productId == null)
            throw new ArgumentException("Won't be able to identify single conversion with provided filters");

        var filter = new List<QueryFilter>
        {
            new("from_qu_id", QueryCondition.Equals, fromQuantity.ToString())
        };

        if(toQuantity != null)
            filter.Add(new QueryFilter("to_qu_id", QueryCondition.Equals, toQuantity.ToString()));

        if (productId != null) 
            filter.Add(new QueryFilter("product_id", QueryCondition.Equals, productId.ToString()));


        var result = await _conversionApi.Get(filter);

        if (result.Count() > 1)
        {
            throw new ApplicationException("No single conversion identified, try again");
        }

        return result.FirstOrDefault();
    }

}