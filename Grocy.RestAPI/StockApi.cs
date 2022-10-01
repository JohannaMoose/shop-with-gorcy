using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grocy.RestAPI.Json;
using Grocy.RestAPI.Models;
using Grocy.RestAPI.VMs;
using Newtonsoft.Json;

namespace Grocy.RestAPI;

public class StockApi : GrocyApiBase<StockEntry>
{
    private const string ApiBase = "api/stock/products";

    public StockApi(HttpClient httpClient, string grocyUrl) : base(httpClient, grocyUrl)
    {
    }


    public async Task<ProductDetails> GetProduct(int productId)
    {
        var url = ConstructGrocyUrl($"{ApiBase}/{productId}");

        try
        {
            var httpResponse = await HttpClient.GetAsync(url);
            var json = await httpResponse.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<ProductDetails>(json);
            return parsedResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task AddAmount(int productId, double amount, double price,
        StockTransactionType transactionType = StockTransactionType.Purchase, DateTime? bestBeforeDate = null,
        int? locationId = null, int? shoppingLocationId = null, int? stockLabelType = null,
        string? note = default)
    {
        var vm = new ProductChangeAmount(amount, transactionType.ToApiString(), price, bestBeforeDate, locationId,
            shoppingLocationId,
            stockLabelType, note);

        var result = await PostToGrocy($"{ApiBase}/{productId}/add", vm);


        if (result.IsSuccessStatusCode)
        {

        }
        else
        {
            var error = await result.Content.ReadAsStringAsync();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productId">The id of the product to adjust the product inventory for</param>
    /// <param name="newAmount">The new current amount for the given product - please note that when tare weight handling for the product is enabled, this needs to be the amount including the container weight (gross), the amount to be posted will be automatically calculated based on what is in stock and the defined tare weight</param>
    /// <param name="bestBeforeDate">The due date which applies to added products</param>
    /// <param name="shoppingLocationId">If omitted, no store will be affected</param>
    /// <param name="locationId">If omitted, the default location of the product is used (only applies to added products)</param>
    /// <param name="price">If omitted, the last price of the product is used (only applies to added products)</param>
    /// <param name="stockLabelType">1 = No label, 2 = Single label, 3 = Label per unit (only applies to added products)</param>
    /// <param name="note">An optional note for the corresponding stock entry (only applies to added products)</param>
    /// <returns></returns>
    public async Task AdjustProductInventory(int productId, double newAmount, DateTime bestBeforeDate = default,
        int shoppingLocationId = default, int locationId = default, double price = default,
        int stockLabelType = default, string? note = default)
    {
        var vm = new ProductInventory(newAmount, bestBeforeDate, shoppingLocationId, locationId, price, stockLabelType,
            note);

        var result = await PostToGrocy($"{ApiBase}/{productId}/inventory", vm);


        if (result.IsSuccessStatusCode)
        {

        }
        else
        {
            var error = await result.Content.ReadAsStringAsync();
        }

    }

    protected override string ApiEndpoint => "stock";
}