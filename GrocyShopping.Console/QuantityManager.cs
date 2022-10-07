using Grocy.RestAPI;
using Grocy.RestAPI.Models;
using System.Text.RegularExpressions;
using Serilog;

namespace GrocyShopping.Console;

internal class QuantityManager
{
    private readonly QuantityUnitsApi _api;
    private readonly ILogger _logger;
    private IReadOnlyList<QuantityUnit>? _quantityUnits;

    public QuantityManager(QuantityUnitsApi api, ILogger logger)
    {
        _api = api;
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

    private IEnumerable<string> AllAbbreviations => QuantityUnits.Select(x => x.UserFields["abbreviation"]);

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
        var unit1 = QuantityUnits.Single(x => x.UserFields["abbreviation"] == abrv);

        _logger.Debug("Using {unit} with abbreviation {abbreviation} for amount found in {amount}", unit1.Name, abrv, unitString);

        return unit1;
    }
}