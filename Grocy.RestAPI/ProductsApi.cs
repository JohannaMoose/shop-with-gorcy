using System.Net;
using System.Text;
using Grocy.RestAPI.Models;
using Grocy.RestAPI.VMs;
using Newtonsoft.Json;

namespace Grocy.RestAPI;

public class ProductsApi : GrocyApiBase<Product>
{
    public ProductsApi(HttpClient httpClient, string grocyUrl)
        : base(httpClient, grocyUrl)
    {

    }


    public async Task<Product> AddProduct(string name, int quStockId, int quPurchaseId,
        double quFactorPurchaseToStock = 1, int locationId = 18)
    {
        var productToAdd = new ProductAdd(name, quStockId, quPurchaseId, quFactorPurchaseToStock, locationId);
        var result = await PostToGrocy("api/objects/products", productToAdd);

        if (result.IsSuccessStatusCode)
        {
            var json = await result.Content.ReadAsStringAsync();
            var entityResult =
                JsonConvert.DeserializeObject<CreatedEntityResult>(json);
            return new Product(entityResult.created_object_id, name, quStockId, quPurchaseId, new Dictionary<string, string>());
        }
        else
        {
            var error = await result.Content.ReadAsStringAsync();
            throw new ApplicationException("Failed to add product");
        }
    }

    public async Task EditUserfield(int productId, string name, string newValue)
    {
        var jsonBody = await UpdateFieldInJson(productId, name, newValue);

        var url = ConstructGrocyUrl($"api/userfields/products/{productId}");

        try
        {
            var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri(url),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }},
                Content = jsonBody
            };

            var result = await HttpClient.SendAsync(msg);

            if (!result.IsSuccessStatusCode)
            {
                var error = await result.Content.ReadAsStringAsync();
                throw new ApplicationException("Failed to update userfields");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<StringContent> UpdateFieldInJson(int productId, string name, string newValue)
    {
        var json = await GetUserfields(productId);
        dynamic jsonObj = JsonConvert.DeserializeObject(json);
        jsonObj[name] = newValue;

        var jsonBody = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");
        return jsonBody;
    }

    private async Task<string> GetUserfields(int productId)
    {
        var httpResult = await Get($"api/userfields/products/{productId}");
        var resultString = await httpResult.Content.ReadAsStringAsync();

        if (!httpResult.IsSuccessStatusCode)
        {
            throw new ApplicationException("Failed to get userfields");
        }

        return resultString;
    }


    protected override string ApiEndpoint => "products";
}