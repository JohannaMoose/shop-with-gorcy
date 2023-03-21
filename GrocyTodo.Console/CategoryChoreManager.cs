using Grocy.RestAPI;
using Grocy.RestAPI.Models;

namespace GrocyTodo.Console;

public class CategoryChoreManager
{
    private readonly ChoesApi _choresApi;
    private readonly IEnumerable<Chore> _allChores;
    private readonly string _priorityName;
    private readonly string _priorityValue;

    public CategoryChoreManager(ChoesApi choresApi, string priorityName, string priorityValue)
    {
        _choresApi = choresApi;
        _priorityName = priorityName;
        _priorityValue = priorityValue;
        _allChores = choresApi.Get().Result;
    }

    public async Task ScheduleCategory(string categoryName, int nbrOfChoresInCategoryToHave)
    {
        var choresInCategory = _allChores.Where(x => x.Userfields["category"] == categoryName).ToList();
        var dueChoresInCategory = choresInCategory.Where(x => x.NextDueDate.Date <= DateTime.Today.Date);

        await ScheduleChores(nbrOfChoresInCategoryToHave, dueChoresInCategory, choresInCategory);
    }

    private async Task ScheduleChores(int nbrOfChoresInCategoryToHave, IEnumerable<Chore> dueChoresInCategory,
        IReadOnlyCollection<Chore> choresInCategory)
    {
        var priorityChores = dueChoresInCategory.Where(x => x.Userfields[_priorityName] == _priorityValue).ToList();
        var nonePriority = dueChoresInCategory.Where(x => x.Userfields[_priorityName] != _priorityValue).ToList();

        if (!priorityChores.Any())
        {
            System.Console.WriteLine("No priority chores to handle");
            System.Console.WriteLine("Keeping some none priority to fill slots, rescheduling rest");
            await Reschedule(nonePriority.Skip(nbrOfChoresInCategoryToHave), choresInCategory, nbrOfChoresInCategoryToHave);
        }
        else if (priorityChores.Count > nbrOfChoresInCategoryToHave)
        {
            System.Console.WriteLine(
                "Too many priority chores to handle, will have to reschedule some along with all none priority");
            await Reschedule(priorityChores.Skip(nbrOfChoresInCategoryToHave).Concat(nonePriority), choresInCategory, nbrOfChoresInCategoryToHave);
        }
        else
        {
            System.Console.Write("Correct number, or less, of priority, will keep it as is: ");
            foreach (var chore in priorityChores)
            {
                System.Console.WriteLine(chore.Name);
            }

            if (nonePriority.Count + priorityChores.Count > nbrOfChoresInCategoryToHave)
            {
                System.Console.WriteLine("Too many chores in total, will have to reschedule some none priority");
                await Reschedule(nonePriority.Skip(nbrOfChoresInCategoryToHave - priorityChores.Count), choresInCategory, nbrOfChoresInCategoryToHave);
            }
            else
            {
                System.Console.WriteLine("Correct number, or less, of none priority, will keep it as is");
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

            var forSameDate = allChoresInCategory.Where(x => x.NextDueDate.Date == rescheduleTo.Date).ToList();
            if (forSameDate.Any())
            {
                if (forSameDate.All(x => x.Userfields[_priorityName] != _priorityValue))
                {
                   
                    toAlsoReschedule.AddRange(forSameDate);
                }
            }
            else
            {
                System.Console.WriteLine($"Rescheduling {chore.Name} to {rescheduleTo}");
                await _choresApi.RescheduleChore(chore.Id, rescheduleTo);
            }

            
            count++;
        }

        if (toAlsoReschedule.Any())
        {
            System.Console.WriteLine("Need to reschedule other chores as well");
            await Reschedule(toAlsoReschedule, allChoresInCategory, nbrOfChoresInCategoryToHave, rescheduleTo);
        }
    }
}