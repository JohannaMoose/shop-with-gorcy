using Grocy.RestAPI.Models;

namespace Grocy.RestAPI;

public class QuantityUnitsApi : GrocyApiBase<QuantityUnit>
{
    public QuantityUnitsApi(HttpClient httpClient, string grocyUrl)
        : base(httpClient, grocyUrl)
    {

    }

    protected override string ApiEndpoint => "quantity_units";
}