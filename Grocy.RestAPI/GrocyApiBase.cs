using Grocy.RestAPI.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Grocy.RestAPI;

public abstract class GrocyApiBase<T>
{
    protected readonly HttpClient HttpClient;
    private readonly string _grocyUrl;

    protected GrocyApiBase(HttpClient httpClient, string grocyUrl)
    {
        HttpClient = httpClient;
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
        url = ConstructGrocyUrl(url);

        if (filters is { Length: > 0 })
        {
            var filterString = CreateFilterString(filters);
            url += $"?{filterString}";
        }

        return HttpClient.GetAsync(url);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = new JsonLowercaseUnderscorePolicy(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    protected async Task<HttpResponseMessage> PostToGrocy<TIn>(string uniqUrlPart, TIn objToJson)
    {
        var jsonBody = JsonContent.Create(objToJson, null, JsonOptions);

        var url = ConstructGrocyUrl(uniqUrlPart);

        try
        {
            var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }},
                Content = jsonBody
            };
            var result = await HttpClient.SendAsync(msg);
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    protected string ConstructGrocyUrl(string url)
    {
        if (url.StartsWith("/"))
            url = _grocyUrl + url;
        else
            url = $"{_grocyUrl}/{url}";
        return url;
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
        var parsedResponse = JsonSerializer.Deserialize<IEnumerable<T>>(json, JsonOptions);
        return parsedResponse;
    }
}