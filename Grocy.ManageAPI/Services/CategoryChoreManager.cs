using Grocy.Domain;
using Grocy.RestAPI;

namespace Grocy.ManageAPI.Services;

public class CategoryChoreManager
{
    private readonly ChoesApi _choresApi;
    private IEnumerable<Chore> _allChores;

    public CategoryChoreManager(ChoesApi choresApi)
    {
        _choresApi = choresApi;
        var load = LoadChores();
        load.Wait();
    }

    private async Task LoadChores()
    {
        var baseChore = _choresApi.Get();
        var info = _choresApi.GetChoreInfo();

        await Task.WhenAll(baseChore, info);

        _allChores = baseChore.Result.Select(x => new Chore(info.Result.First(y => y.Id == x.Id), x));
    }

    public async Task ScheduleCategory(string categoryName, int nbrOfChoresInCategoryToHave)
    {
        var choresInCategory = _allChores.Where(x => x.Userfields["category"] == categoryName).ToList();
        var dueChoresInCategory = choresInCategory.Where(x => x.NextEstimatedExecutionTime.Date <= DateTime.Today.Date);

        await ScheduleChores(nbrOfChoresInCategoryToHave, dueChoresInCategory, choresInCategory);
    }

    private async Task ScheduleChores(int nbrOfChoresInCategoryToHave, IEnumerable<Chore> dueChoresInCategory,
        IReadOnlyCollection<Chore> choresInCategory)
    {
        var priorityChores = dueChoresInCategory.Where(x => x.IsPriority).ToList();
        var nonePriority = dueChoresInCategory.Where(x => !x.IsPriority).ToList();

        if (!priorityChores.Any())
        {
            Console.WriteLine("No priority chores to handle");
            Console.WriteLine("Keeping some none priority to fill slots, rescheduling rest");
            await Reschedule(nonePriority.Skip(nbrOfChoresInCategoryToHave), choresInCategory, nbrOfChoresInCategoryToHave);
        }
        else if (priorityChores.Count > nbrOfChoresInCategoryToHave)
        {
            Console.WriteLine(
                "Too many priority chores to handle, will have to reschedule some along with all none priority");
            await Reschedule(priorityChores.Skip(nbrOfChoresInCategoryToHave).Concat(nonePriority), choresInCategory, nbrOfChoresInCategoryToHave);
        }
        else
        {
            Console.Write("Correct number, or less, of priority, will keep it as is: ");
            foreach (var chore in priorityChores)
            {
                Console.WriteLine(chore.Name);
            }

            if (nonePriority.Count + priorityChores.Count > nbrOfChoresInCategoryToHave)
            {
                Console.WriteLine("Too many chores in total, will have to reschedule some none priority");
                await Reschedule(nonePriority.Skip(nbrOfChoresInCategoryToHave - priorityChores.Count), choresInCategory, nbrOfChoresInCategoryToHave);
            }
            else
            {
                Console.WriteLine("Correct number, or less, of none priority, will keep it as is");
            }
        }
    }

    private async Task Reschedule(IEnumerable<Chore> toReschedule, IReadOnlyCollection<Chore> allChoresInCategory, int nbrOfChoresInCategoryToHave, DateTime rescheduleTo = default)
    {
        if (rescheduleTo == default)
            rescheduleTo = DateTime.Today;

        var choresToReschedule = toReschedule.ToList();
        var toAlsoReschedule = new List<Chore>();
        int count = 0;
        foreach (var chore in choresToReschedule)
        {
            if (count % nbrOfChoresInCategoryToHave == 0)
                rescheduleTo = rescheduleTo.AddDays(1);

            var forSameDate = allChoresInCategory.Where(x => x.NextEstimatedExecutionTime.Date == rescheduleTo.Date).ToList();
            if (forSameDate.Any())
            {
                if (forSameDate.All(x => !x.IsPriority))
                {
                   
                    toAlsoReschedule.AddRange(forSameDate);
                }
            }
            else
            {
                Console.WriteLine($"Rescheduling {chore.Name} to {rescheduleTo}");
                await _choresApi.RescheduleChore(chore.Id, rescheduleTo);
            }

            
            count++;
        }

        if (toAlsoReschedule.Any())
        {
            Console.WriteLine("Need to reschedule other chores as well");
            await Reschedule(toAlsoReschedule, allChoresInCategory, nbrOfChoresInCategoryToHave, rescheduleTo);
        }
    }
}