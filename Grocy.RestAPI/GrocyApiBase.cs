using Newtonsoft.Json;

namespace Grocy.RestAPI;

public abstract class GrocyApiBase<T>
{
    private readonly HttpClient _httpClient;
    private readonly string _grocyUrl;

    protected GrocyApiBase(HttpClient httpClient, string grocyUrl)
    {
        _httpClient = httpClient;
        _grocyUrl = grocyUrl;

        if (_grocyUrl.EndsWith("/"))
            _grocyUrl = _grocyUrl.Remove(_grocyUrl.Length - 1);
    }

    public async Task<IEnumerable<T>> Get(IEnumerable<QueryFilter>? filters = default)
    {
        var url = $"api/objects/{ApiEndpoint}";
        var response = await Get(url, filters?.ToArray());

        if (response.IsSuccessStatusCode)
        {
            return await ParsedResponse(response);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    protected abstract string ApiEndpoint { get; }

    protected Task<HttpResponseMessage> Get(string url, params QueryFilter[]? filters)
    {
        if (url.StartsWith("/"))
            url = _grocyUrl + url;
        else
            url = $"{_grocyUrl}/{url}";

        if (filters is { Length: > 0 })
        {
            var filterString = CreateFilterString(filters);
            url += $"?{filterString}";
        }

        return _httpClient.GetAsync(url);
    }

    private static string CreateFilterString(IReadOnlyList<QueryFilter> filters)
    {
        var url = "";
        for (var i = 0; i < filters.Count; i++)
        {
            if (i > 0)
            {
                url += "&";
            }

            var filterPart = filters[i];
            url += "query[]=" + filterPart.Field + filterPart.Condition.AsQueryStringPart() + filterPart.Value;
        }

        return url;
    }

    private static async Task<IEnumerable<T>> ParsedResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var parsedResponse = JsonConvert.DeserializeObject<IEnumerable<T>>(json);
        return parsedResponse;
    }
}