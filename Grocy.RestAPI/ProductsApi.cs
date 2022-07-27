using Grocy.RestAPI.Models;
using Newtonsoft.Json;

namespace Grocy.RestAPI;

public class ProductsApi
{
    private readonly HttpClient _httpClient;
    private readonly string _grocyUrl;

    public ProductsApi(HttpClient httpClient, string grocyUrl, string apiKey)
    {
        _httpClient = httpClient;
        httpClient.DefaultRequestHeaders.Add("GROCY-API-KEY", apiKey);
        _grocyUrl = grocyUrl;
    }

    public async Task<IEnumerable<Product>> GetProducts(IEnumerable<QueryFilter>? filter = default)
    {
        var url = _grocyUrl + "api/objects/products";
        if (filter != null)
        {
            url += "?";
            for (int i = 0; i < filter.Count(); i++)
            {
                
            }
        }

        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<IEnumerable<Product>>(json);
            return parsedResponse;
        }

        throw new NotImplementedException();
    }
}