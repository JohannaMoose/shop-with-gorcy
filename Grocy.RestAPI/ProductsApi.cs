using Grocy.RestAPI.Models;

namespace Grocy.RestAPI;

public class ProductsApi : GrocyApiBase<Product>
{

    public ProductsApi(HttpClient httpClient, string grocyUrl)
    :base(httpClient, grocyUrl)
    {
      
    }


    public async Task<Product> AddProduct(Product product)
    {
        throw new NotImplementedException();
    }

    protected override string ApiEndpoint => "products";
}