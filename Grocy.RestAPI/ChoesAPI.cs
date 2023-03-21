using Grocy.RestAPI.Models;
using Grocy.RestAPI.VMs;

namespace Grocy.RestAPI;

public class ChoesApi : GrocyApiBase<Chore>
{
    public ChoesApi(HttpClient httpClient, string grocyUrl) : base(httpClient, grocyUrl)
    {
        
    }

    protected override string ApiEndpoint => "chores";

    public async Task RescheduleChore(int choreId, DateTime newDate)
    {
        var reschedule = new ChoreReschedule(newDate);

        var result = await PutToGrocy($"api/objects/chores/{choreId}", reschedule);

        if (!result.IsSuccessStatusCode)
        {
            var error = await result.Content.ReadAsStringAsync();
            throw new ApplicationException("Failed to reschedule chore");
        }
    }

    public async Task<IEnumerable<ChoreInfo>> GetChoreInfo()
    {
        var response = await Get("api/chores");

        if (response.IsSuccessStatusCode)
        {
            return await ParsedResponse<ChoreInfo>(response);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}