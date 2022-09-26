using Grocy.RestAPI.Models;

namespace Grocy.RestAPI;

public class QuantityConversionApi : GrocyApiBase<QuantityUnitConversion>
{
    public QuantityConversionApi(HttpClient httpClient, string grocyUrl) : base(httpClient, grocyUrl)
    {
    }

    protected override string ApiEndpoint => "quantity_unit_conversions";
}